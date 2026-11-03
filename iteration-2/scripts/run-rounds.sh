#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DATA="results/rounds"
ROUNDS=0
PAUSE=0
POINTS=""
PRUNE=0
STOP_AFTER_HOURS=0
MAX_FAILED_ROUNDS=3
MATRIX_ARGS=()

usage() {
  cat <<USAGE
usage: $0 [--data DIR] [--rounds N] [--stop-after-hours H] [--pause SECONDS] [run-matrix.sh options]
  --rounds 0 (default) repeats until stopped
  --stop-after-hours H starts no new round after H hours; a failed round is kept in incomplete/ and the loop gives up after 3 failures in a row
  --points e0-7,e2-10 limits Track A to these points
  --prune-traces deletes .ember traces that are not a point's representative once their aggregates are stored
  the points of each scenario run in a shuffled order inside every run, and builds happen once, in the first round
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --data) DATA="$2"; shift 2 ;;
    --rounds) ROUNDS="$2"; shift 2 ;;
    --stop-after-hours) STOP_AFTER_HOURS="$2"; shift 2 ;;
    --pause) PAUSE="$2"; shift 2 ;;
    --points) POINTS="$2"; shift 2 ;;
    --prune-traces) PRUNE=1; shift ;;
    -h | --help) usage; exit 0 ;;
    *) MATRIX_ARGS+=("$1"); shift ;;
  esac
done

mkdir -p "$DATA/rounds" "$DATA/incomplete"
PYTHON="$(./scripts/ensure-python.sh)"
if [[ -z "$POINTS" ]]; then
  POINTS="$(python3 -c 'import json; print(",".join(p["label"] for p in json.load(open("matrix/versions.json"))["points"]))')"
fi

completed=0
failed_in_a_row=0
started_at=$(date +%s)
SKIP_BUILD=()
COMPARE_ARGS=()
[[ "$PRUNE" == 1 ]] && COMPARE_ARGS+=(--prune-traces)
while (( ROUNDS == 0 || completed < ROUNDS )); do
  if python3 -c "import sys; sys.exit(0 if float(sys.argv[1]) > 0 and $(date +%s) - $started_at > float(sys.argv[1]) * 3600 else 1)" "$STOP_AFTER_HOURS"; then
    echo "== Time budget of $STOP_AFTER_HOURS h used up after $completed rounds"
    break
  fi
  id="$(date -u +%Y%m%dT%H%M%SZ)"
  round="$DATA/incomplete/$id"
  echo "== Round $((completed + 1)): $id"
  mkdir -p "$round"
  if ! ./scripts/run-matrix.sh "${MATRIX_ARGS[@]}" "${SKIP_BUILD[@]}" --skip-analysis --points "$POINTS" --raw "$round/raw" --summary "$round/summary"; then
    failed_in_a_row=$((failed_in_a_row + 1))
    echo "== Round $id failed ($failed_in_a_row in a row); kept in $round" >&2
    if (( failed_in_a_row >= MAX_FAILED_ROUNDS )); then
      echo "== Giving up after $MAX_FAILED_ROUNDS failed rounds" >&2
      exit 1
    fi
    continue
  fi
  failed_in_a_row=0
  SKIP_BUILD=(--skip-build)
  mv "$round" "$DATA/rounds/$id"
  completed=$((completed + 1))

  echo "== Accumulated summary over $(find "$DATA/rounds" -mindepth 1 -maxdepth 1 -type d | wc -l) rounds"
  dotnet run --project src/Compare/Compare.csproj -c Release -- --raw "$DATA/rounds" --summary "$DATA/summary" --matrix matrix/versions.json "${COMPARE_ARGS[@]}"
  "$PYTHON" analysis/stats.py --summary "$DATA/summary"

  if (( PAUSE > 0 )); then
    sleep "$PAUSE"
  fi
done
