#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
cd "$REPO_ROOT"
ITERATIONS=5
TAGS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --iterations) ITERATIONS="$2"; shift 2 ;;
    *) TAGS+=("$1"); shift ;;
  esac
done
[[ ${#TAGS[@]} -gt 0 ]] || TAGS=(v7.0.20 v10.0.14)

"$GLASSBOX/build.sh" "${TAGS[@]}"
for tag in "${TAGS[@]}"; do
  dotnet "bin/glassbox-$tag/Runner.dll" --methods glassbox/methods.txt --iterations "$ITERATIONS" --out results/glassbox
done
echo "hotspots, collapsed stacks and traces: results/glassbox/<tag>/"
