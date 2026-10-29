#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DATA="results/rounds"
ROUNDS=0
PAUSE=0
POINTS=""
MATRIX_ARGS=()

usage() {
  cat <<USAGE
usage: $0 [--data DIR] [--rounds N] [--pause SECONDS] [run-matrix.sh options]
  --rounds 0 (default) repeats until stopped
  --points e0-7,e2-10 limits Track A to these points; their order is shuffled every round
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --data) DATA="$2"; shift 2 ;;
    --rounds) ROUNDS="$2"; shift 2 ;;
    --pause) PAUSE="$2"; shift 2 ;;
    --points) POINTS="$2"; shift 2 ;;
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
while (( ROUNDS == 0 || completed < ROUNDS )); do
  id="$(date -u +%Y%m%dT%H%M%SZ)"
  round="$DATA/incomplete/$id"
  order="$(tr ',' '\n' <<<"$POINTS" | shuf | paste -sd, -)"
  echo "== Round $((completed + 1)): $id, Track A order $order"
  mkdir -p "$round"
  echo "$order" > "$round/track-a-order.txt"
  ./scripts/run-matrix.sh "${MATRIX_ARGS[@]}" --points "$order" --raw "$round/raw" --summary "$round/summary"
  mv "$round" "$DATA/rounds/$id"
  completed=$((completed + 1))

  echo "== Accumulated summary over $(find "$DATA/rounds" -mindepth 1 -maxdepth 1 -type d | wc -l) rounds"
  dotnet run --project src/Compare/Compare.csproj -c Release -- --raw "$DATA/rounds" --summary "$DATA/summary" --matrix matrix/versions.json
  "$PYTHON" analysis/stats.py --summary "$DATA/summary"

  if (( PAUSE > 0 )); then
    sleep "$PAUSE"
  fi
done
