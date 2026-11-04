#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
cd "$REPO_ROOT"
OUT="callgraph"
TAGS=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --out) OUT="$2"; shift 2 ;;
    *) TAGS+=("$1"); shift ;;
  esac
done
[[ ${#TAGS[@]} -gt 0 ]] || { echo "usage: glassbox/callgraph.sh [--out DIR] <tag> [<tag>...]" >&2; exit 2; }

dotnet build glassbox/CallGraph/CallGraph.csproj -c Release -o bin/glassbox-callgraph -nologo -v:q -clp:ErrorsOnly
for tag in "${TAGS[@]}"; do
  for repo in $(awk -F'\t' 'NR > 1 { print $2 }' glassbox/packages.tsv | sort -u); do
    "$GLASSBOX/fetch.sh" "$tag" "$repo" >/dev/null
    if [[ "$repo" == "runtime" ]]; then checkout="glassbox/src/$tag"; else checkout="glassbox/src/$repo/$tag"; fi
    dotnet bin/glassbox-callgraph/CallGraph.dll --tag "$tag" --source "$checkout" --repo "$repo" \
      --packages glassbox/packages.tsv --operations docs/entry-operations.tsv --out "$OUT/$tag" || true
  done
  find "$OUT/$tag" -name callgraph.json -exec gzip -nf {} +
done
