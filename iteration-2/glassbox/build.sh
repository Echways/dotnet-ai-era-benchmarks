#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
cd "$REPO_ROOT"
TARGET_PACKAGES=""
TAGS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --targets) TARGET_PACKAGES="$2"; shift 2 ;;
    *) TAGS+=("$1"); shift ;;
  esac
done
[[ ${#TAGS[@]} -gt 0 ]] || { echo "usage: glassbox/build.sh [--targets Package,Package] <tag> [<tag>...]" >&2; exit 2; }

dotnet build glassbox/Instrumenter/Instrumenter.csproj -c Release -o bin/glassbox-instrumenter -nologo -v:q -clp:ErrorsOnly
mkdir -p glassbox/generated
EMPTY_METHODS="glassbox/generated/empty-methods.txt"
: > "$EMPTY_METHODS"
if [[ -n "$TARGET_PACKAGES" ]]; then
  IDS="glassbox/generated/target-ids.txt"
  : > "$IDS"
  for tag in "${TAGS[@]}"; do
    targets="glassbox/generated/$tag.targets.tsv"
    head -n 1 "callgraph/$tag/${TARGET_PACKAGES%%,*}/methods.tsv" > "$targets"
    for package in ${TARGET_PACKAGES//,/ }; do
      tail -n +2 "callgraph/$tag/$package/methods.tsv" >> "$targets"
    done
    tail -n +2 "$targets" | cut -f1 >> "$IDS"
  done
  sort -u -o "$IDS" "$IDS"
fi

for tag in "${TAGS[@]}"; do
  major="${tag#v}"
  major="${major%%.*}"
  "$GLASSBOX/fetch.sh" "$tag" >/dev/null
  if [[ -n "$TARGET_PACKAGES" ]]; then
    selection=(--targets "glassbox/generated/$tag.targets.tsv" --methods "$EMPTY_METHODS")
  else
    selection=(--methods glassbox/methods.txt)
  fi
  dotnet bin/glassbox-instrumenter/Instrumenter.dll --tag "$tag" --source "glassbox/src/$tag" \
    "${selection[@]}" --libraries glassbox/libraries.txt \
    --scenario src/Harness/Scenarios/DiScenario.cs src/Harness/Scenarios/DiGraph.cs src/Harness/Scenarios/SlotTypes.cs src/Harness/Scenarios/CacheScenario.cs \
    --out "glassbox/generated/$tag"
  dotnet build glassbox/Runner/Runner.csproj -c Release -p:GlassboxTag="$tag" -p:MsExt="$major" -o "bin/glassbox-$tag" -nologo -v:q -clp:ErrorsOnly
done
