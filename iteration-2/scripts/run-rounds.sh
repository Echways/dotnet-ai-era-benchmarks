#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DATA="results/rounds"
ROUNDS=0
PAUSE=0
POINTS=""
PRUNE=0
MATRIX_ARGS=()

usage() {
  cat <<USAGE
usage: $0 [--data DIR] [--rounds N] [--pause SECONDS] [run-matrix.sh options]
  --rounds 0 (default) repeats until stopped
  --points e0-7,e2-10 limits Track A to these points
  --prune-traces deletes .ember traces that are not a point's representative once their aggregates are stored
  the points of each scenario run in a shuffled order inside every run, and builds happen once, in the first round
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --data) DATA="$2"; shift 2 ;;
    --rounds) ROUNDS="$2"; shift 2 ;;
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
SKIP_BUILD=()
COMPARE_ARGS=()
[[ "$PRUNE" == 1 ]] && COMPARE_ARGS+=(--prune-traces)
while (( ROUNDS == 0 || completed < ROUNDS )); do
  id="$(date -u +%Y%m%dT%H%M%SZ)"
  round="$DATA/incomplete/$id"
  echo "== Round $((completed + 1)): $id"
  mkdir -p "$round"
  ./scripts/run-matrix.sh "${MATRIX_ARGS[@]}" "${SKIP_BUILD[@]}" --points "$POINTS" --raw "$round/raw" --summary "$round/summary"
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
