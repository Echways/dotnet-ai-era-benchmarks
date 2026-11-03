#!/usr/bin/env bash
set -euo pipefail
EXPERIMENTS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$EXPERIMENTS/.." && pwd)"
cd "$REPO_ROOT"
[[ $# -ge 4 ]] || { echo "usage: $0 <scenario,...> <rounds> <out> <bin-name>..." >&2; exit 2; }
IFS=, read -r -a SCENARIOS <<<"$1"
ROUNDS="$2"
OUT="$3"
shift 3
for round in $(seq 1 "$ROUNDS"); do
  for variant in $(printf '%s\n' "$@" | shuf); do
    for scenario in "${SCENARIOS[@]}"; do
      taskset -c 2-5 dotnet "bin/$variant/Harness.dll" --scenario "$scenario" --label e2-11 --iterations 100 --warmup 20 --out "$OUT/$variant/rounds/$round/raw" | tail -1
    done
  done
done
for variant in "$@"; do
  dotnet src/Compare/bin/Release/net10.0/Compare.dll --raw "$OUT/$variant/rounds" --summary "$OUT/$variant/summary" --matrix matrix/versions.json
done
