#!/usr/bin/env bash
set -euo pipefail
STATIC="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$STATIC/.." && pwd)"
cd "$REPO_ROOT"
WORK="$REPO_ROOT/.work/static"
TAGS=("$@")
[[ ${#TAGS[@]} -gt 0 ]] || TAGS=(v7.0.20 v8.0.33 v9.0.22 v10.0.14)

dotnet build static-metrics/Metrics/Metrics.csproj -c Release -o bin/static-metrics -nologo -v:q -clp:ErrorsOnly
for tag in "${TAGS[@]}"; do
  echo "== static metrics $tag"
  out="$WORK/$tag"
  rm -rf "$out"
  mkdir -p "$out"
  for repo in $(awk -F'\t' 'NR > 1 { print $2 }' glassbox/packages.tsv | sort -u); do
    ./glassbox/fetch.sh "$tag" "$repo" >/dev/null
    if [[ "$repo" == "runtime" ]]; then checkout="glassbox/src/$tag"; else checkout="glassbox/src/$repo/$tag"; fi
    while IFS=$'\t' read -r id package_repo source_dir tests_dir; do
      [[ "$package_repo" == "$repo" ]] || continue
      [[ -d "$checkout/$source_dir" ]] || { echo "$id: not present at $tag"; continue; }
      tests=()
      [[ -n "$tests_dir" && -d "$checkout/$tests_dir" ]] && tests=(--tests "$checkout/$tests_dir")
      dotnet bin/static-metrics/Metrics.dll --source "$checkout/$source_dir" "${tests[@]}" --out "$out/$id.metrics.json"
    done < <(tail -n +2 glassbox/packages.tsv)
  done
done
python3 static-metrics/aggregate.py --work "$WORK" --out static-metrics/results
