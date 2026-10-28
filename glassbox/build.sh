#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
cd "$REPO_ROOT"
[[ $# -gt 0 ]] || { echo "usage: glassbox/build.sh <tag> [<tag>...]" >&2; exit 2; }

dotnet build glassbox/Instrumenter/Instrumenter.csproj -c Release -o bin/glassbox-instrumenter -nologo -v:q -clp:ErrorsOnly
for tag in "$@"; do
  major="${tag#v}"
  major="${major%%.*}"
  "$GLASSBOX/fetch.sh" "$tag"
  dotnet bin/glassbox-instrumenter/Instrumenter.dll --tag "$tag" --source "glassbox/src/$tag" \
    --methods glassbox/methods.txt --libraries glassbox/libraries.txt \
    --scenario src/Harness/Scenarios/DiScenario.cs src/Harness/Scenarios/DiGraph.cs src/Harness/Scenarios/SlotTypes.cs src/Harness/Scenarios/CacheScenario.cs \
    --out "glassbox/generated/$tag"
  dotnet build glassbox/Runner/Runner.csproj -c Release -p:GlassboxTag="$tag" -p:MsExt="$major" -o "bin/glassbox-$tag" -nologo -v:q -clp:ErrorsOnly
done
