#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

HOURS=8
AB_RUNS=5
AB_POINTS="e0-7,e2-10"
AB_INTERVALS=(20 1000 0)
SKIP_AB=0
NET11_ONLY=0
ALLOW_DIRTY=0
LOG_DIR="results/overnight"
AB_DIR="results/ab-counter"
ROUND_ARGS=()

usage() {
  cat <<USAGE
usage: $0 [--hours H] [--net11-only] [--skip-ab] [--ab-runs N] [--ab-points a,b] [--allow-dirty] [-- run-rounds.sh options]
  1. applies the stand settings (asks for sudo once) and restores them on exit
  2. measures counter intervals of 20 ms, 1000 ms and 0 unless --skip-ab is set
  3. repeats rounds until --hours have passed (default 8)
  --net11-only uses e2-11 for counter A/B and collects e2-11 and b-net11 in the rounds
  --allow-dirty permits an uncommitted working tree
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --hours) HOURS="$2"; shift 2 ;;
    --net11-only) NET11_ONLY=1; shift ;;
    --skip-ab) SKIP_AB=1; shift ;;
    --ab-runs) AB_RUNS="$2"; shift 2 ;;
    --ab-points) AB_POINTS="$2"; shift 2 ;;
    --allow-dirty) ALLOW_DIRTY=1; shift ;;
    --) shift; ROUND_ARGS=("$@"); break ;;
    -h | --help) usage; exit 0 ;;
    *) echo "unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
done

MATRIX_ARGS=(--points "$AB_POINTS")
if [[ "$NET11_ONLY" == 1 ]]; then
  LOG_DIR="results/net11-ga/overnight"
  AB_DIR="results/net11-ga/ab-counter"
  MATRIX_ARGS=(--net11-only)
  ROUND_ARGS=(--net11-only "${ROUND_ARGS[@]}")
fi

mkdir -p "$LOG_DIR"
LOG="$LOG_DIR/$(date -u +%Y%m%dT%H%M%SZ).log"
exec > >(tee -a "$LOG") 2>&1

if [[ -n "$(git status --porcelain)" ]]; then
  if [[ "$ALLOW_DIRTY" == 0 ]]; then
    echo "error: working tree is dirty; commit the changes or pass --allow-dirty" >&2
    exit 1
  fi
  echo "warning: the working tree has uncommitted changes; run metadata will mark it as dirty" >&2
fi

sudo -v
./scripts/ensure-sdk11.sh >/dev/null
./scripts/ensure-python.sh >/dev/null
trap './scripts/setup-stand.sh --restore' EXIT
./scripts/setup-stand.sh --apply

started_at=$(date +%s)

if [[ "$SKIP_AB" == 0 ]]; then
  for interval in "${AB_INTERVALS[@]}"; do
    echo "== Counter interval A/B: ${interval} ms"
    ./scripts/run-matrix.sh "${MATRIX_ARGS[@]}" --track a --runs "$AB_RUNS" --counter-interval-ms "$interval" \
      --prune-traces --skip-analysis --raw "$AB_DIR/$interval/raw" --summary "$AB_DIR/$interval/summary" \
      || echo "warning: A/B at ${interval} ms failed, continuing" >&2
  done
fi

remaining_hours=$(python3 -c "import sys; print(max(0.0, float(sys.argv[1]) - (int(sys.argv[2]) - int(sys.argv[3])) / 3600))" "$HOURS" "$(date +%s)" "$started_at")
if [[ "$remaining_hours" == "0.0" ]]; then
  echo "== Time budget used up before rounds; log: $LOG"
  exit 0
fi
echo "== Rounds for ${remaining_hours} h"
status=0
./scripts/run-rounds.sh --stop-after-hours "$remaining_hours" --prune-traces "${ROUND_ARGS[@]}" || status=$?
echo "== Overnight finished with status $status; log: $LOG"
exit "$status"
