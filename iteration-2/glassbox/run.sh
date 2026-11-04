#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
cd "$REPO_ROOT"
ITERATIONS=10
RUNS=10
CPUS="2,4"
OUT="results/glassbox"
REPORT=()
BUILD_TARGETS=()
COUNTER_INTERVAL=20
TAGS=()
METHODS=glassbox/methods.txt
while [[ $# -gt 0 ]]; do
  case "$1" in
    --iterations) ITERATIONS="$2"; shift 2 ;;
    --runs) RUNS="$2"; shift 2 ;;
    --cpus) CPUS="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    --counter-interval-ms) COUNTER_INTERVAL="$2"; shift 2 ;;
    --report) REPORT=(--report); shift ;;
    --targets) BUILD_TARGETS=(--targets "$2"); METHODS=glassbox/generated/target-ids.txt; shift 2 ;;
    *) TAGS+=("$1"); shift ;;
  esac
done
[[ ${#TAGS[@]} -gt 0 ]] || TAGS=(v7.0.20 v10.0.14)

"$GLASSBOX/build.sh" "${BUILD_TARGETS[@]}" "${TAGS[@]}"
: > "$OUT.order.txt" 2>/dev/null || { mkdir -p "$(dirname "$OUT")"; : > "$OUT.order.txt"; }
for run in $(seq 1 "$RUNS"); do
  mapfile -t order < <(printf '%s\n' "${TAGS[@]}" | shuf)
  for tag in "${order[@]}"; do
    taskset -c "$CPUS" dotnet "bin/glassbox-$tag/Runner.dll" --methods "$METHODS" --iterations "$ITERATIONS" --counter-interval-ms "$COUNTER_INTERVAL" --out "$OUT" "${REPORT[@]}"
    echo "$run $tag" >> "$OUT.order.txt"
  done
done
dotnet run --project src/Compare -c Release -- methods --traces "$OUT" --out "$OUT/methods.csv"
echo "traces: $OUT/<tag>/<scenario>-<pid>.ember, order of processes: $OUT.order.txt"
