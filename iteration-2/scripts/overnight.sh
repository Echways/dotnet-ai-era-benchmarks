#!/usr/bin/env bash
set -uo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

HOURS=8
AB_RUNS=5
AB_POINTS="e0-7,e2-10"
AB_INTERVALS=(20 1000 0)
SKIP_AB=0
LOG_DIR="results/overnight"
ROUND_ARGS=()

usage() {
  cat <<USAGE
usage: $0 [--hours H] [--skip-ab] [--ab-runs N] [--ab-points a,b] [-- run-rounds.sh options]
  1. applies the stand settings (asks for sudo once) and restores them on exit
  2. counter interval A/B on --ab-points: 20 ms, 1000 ms and 0 into results/ab-counter/<ms>
  3. repeated rounds of the full matrix until --hours have passed (default 8)
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --hours) HOURS="$2"; shift 2 ;;
    --skip-ab) SKIP_AB=1; shift ;;
    --ab-runs) AB_RUNS="$2"; shift 2 ;;
    --ab-points) AB_POINTS="$2"; shift 2 ;;
    --) shift; ROUND_ARGS=("$@"); break ;;
    -h | --help) usage; exit 0 ;;
    *) echo "unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
done

mkdir -p "$LOG_DIR"
LOG="$LOG_DIR/$(date -u +%Y%m%dT%H%M%SZ).log"
exec > >(tee -a "$LOG") 2>&1

if [[ -n "$(git status --porcelain)" ]]; then
  echo "error: working tree is dirty; commit or stash first, run-meta.json records the commit" >&2
  exit 1
fi

sudo -v
./scripts/ensure-sdk11.sh >/dev/null
./scripts/ensure-python.sh >/dev/null
./scripts/setup-stand.sh --apply
trap './scripts/setup-stand.sh --restore' EXIT

started_at=$(date +%s)

if [[ "$SKIP_AB" == 0 ]]; then
  for interval in "${AB_INTERVALS[@]}"; do
    echo "== Counter interval A/B: ${interval} ms"
    ./scripts/run-matrix.sh --track a --runs "$AB_RUNS" --points "$AB_POINTS" --counter-interval-ms "$interval" \
      --prune-traces --skip-analysis --raw "results/ab-counter/$interval/raw" --summary "results/ab-counter/$interval/summary" \
      || echo "warning: A/B at ${interval} ms failed, continuing" >&2
  done
fi

remaining_hours=$(python3 -c "import sys; print(max(0.0, float(sys.argv[1]) - (int(sys.argv[2]) - int(sys.argv[3])) / 3600))" "$HOURS" "$(date +%s)" "$started_at")
echo "== Rounds for ${remaining_hours} h"
./scripts/run-rounds.sh --stop-after-hours "$remaining_hours" --prune-traces "${ROUND_ARGS[@]}"
status=$?
echo "== Overnight finished with status $status; log: $LOG"
exit "$status"
