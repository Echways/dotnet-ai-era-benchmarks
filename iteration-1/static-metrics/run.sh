#!/usr/bin/env bash
set -euo pipefail
STATIC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$STATIC/.." && pwd)"
cd "$REPO_ROOT"
WORK="$REPO_ROOT/.work/static"
MIRROR="$REPO_ROOT/.work/repos/runtime.git"

TAGS=("$@")
if [[ ${#TAGS[@]} -eq 0 ]]; then
  [[ -d "$MIRROR" ]] || git clone --bare --filter=blob:none https://github.com/dotnet/runtime.git "$MIRROR"
  mapfile -t TAGS < <(python3 - "$MIRROR" <<'PY'
import json, re, subprocess, sys
tags = subprocess.run(["git", "-C", sys.argv[1], "tag", "--list", "v*"], capture_output=True, text=True, check=True).stdout.split()
def key(tag):
    release, _, pre = tag[1:].partition("-")
    return tuple(int(x) for x in release.split(".")), pre == "", [int(p) if p.isdigit() else p for p in re.split(r"[.]", pre)] if pre else []
for point in json.load(open("matrix/versions.json"))["points"]:
    major = point["major"]
    stable = [t for t in tags if re.fullmatch(rf"v{major}\.0\.\d+", t)]
    candidates = stable or [t for t in tags if re.fullmatch(rf"v{major}\.0\.0-rc\.[0-9.]+", t)]
    print(max(candidates, key=key))
PY
  )
fi

dotnet build static-metrics/Metrics/Metrics.csproj -c Release -o bin/static-metrics -nologo -v:q -clp:ErrorsOnly
dotnet build glassbox/Instrumenter/Instrumenter.csproj -c Release -o bin/glassbox-instrumenter -nologo -v:q -clp:ErrorsOnly
EMPTY_METHODS="$(mktemp)"
trap 'rm -f "$EMPTY_METHODS"' EXIT
LOC_TOOL="roslyn"
command -v cloc >/dev/null 2>&1 && LOC_TOOL="cloc"

for tag in "${TAGS[@]}"; do
  major="${tag#v}"
  major="${major%%.*}"
  echo "== static metrics $tag"
  ./glassbox/fetch.sh "$tag"
  out="$WORK/$tag"
  rm -rf "$out"
  mkdir -p "$out"
  checkout="glassbox/src/$tag/src/libraries"
  while read -r library; do
    [[ -z "$library" ]] && continue
    dotnet bin/static-metrics/Metrics.dll --source "$checkout/$library/src" --tests "$checkout/$library/tests" --out "$out/$library.metrics.json"
    if [[ "$LOC_TOOL" == "cloc" ]]; then
      cloc --json --quiet "$checkout/$library/src" > "$out/$library.cloc-src.json"
      if [[ -d "$checkout/$library/tests" ]]; then
        cloc --json --quiet "$checkout/$library/tests" > "$out/$library.cloc-tests.json"
      fi
    fi
  done < glassbox/libraries.txt

  dotnet bin/glassbox-instrumenter/Instrumenter.dll --tag "$tag" --source "glassbox/src/$tag" --methods "$EMPTY_METHODS" \
    --libraries glassbox/libraries.txt --out "$out/build" > "$out/instrumenter.log"
  cp static-metrics/.editorconfig "$out/build/.editorconfig"
  for project in "$out"/build/*/*.csproj; do
    name="$(basename "$project" .csproj)"
    set +e
    dotnet build "$project" -c Release -p:MsExt="$major" -p:RunAnalyzers=true -p:AnalysisLevel=latest-all -p:WarningLevel=9999 \
      -p:NoWarn= -p:Nullable=enable -p:TreatWarningsAsErrors=false -p:MSBuildTreatWarningsAsErrors=false --no-incremental -nologo -clp:NoSummary > "$out/$name.build.log" 2>&1
    echo "$?" > "$out/$name.build.status"
    set -e
  done
  echo "$LOC_TOOL" > "$out/loc-tool"
done

python3 static-metrics/aggregate.py --work "$WORK" --out results/summary
