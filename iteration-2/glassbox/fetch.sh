#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
TAG="${1:?usage: glassbox/fetch.sh <tag, e.g. v7.0.20> [runtime|aspnetcore|efcore] [packages.tsv]}"
REPO="${2:-runtime}"
PACKAGES="${3:-$GLASSBOX/packages.tsv}"
MIRROR="$REPO_ROOT/.work/repos/$REPO.git"
if [[ "$REPO" == "runtime" ]]; then TARGET="$GLASSBOX/src/$TAG"; else TARGET="$GLASSBOX/src/$REPO/$TAG"; fi

if [[ ! -d "$MIRROR" ]]; then
  mkdir -p "$(dirname "$MIRROR")"
  git clone --bare --filter=blob:none "https://github.com/dotnet/$REPO.git" "$MIRROR"
fi
if ! git -C "$MIRROR" rev-parse --verify --quiet "refs/tags/$TAG" >/dev/null; then
  git -C "$MIRROR" fetch --filter=blob:none origin "refs/tags/$TAG:refs/tags/$TAG"
fi

mapfile -t PATTERNS < <(python3 - "$MIRROR" "$TAG" "$PACKAGES" "$REPO" <<'PY'
import csv
import re
import subprocess
import sys

mirror, tag, packages_file, repo = sys.argv[1:5]
with open(packages_file, encoding="utf-8") as handle:
    rows = [row for row in csv.DictReader(handle, delimiter="\t") if row["repo"] == repo]
patterns = set()
for row in rows:
    source_dir = row["source_dir"]
    patterns.add(f"/{source_dir}/")
    if row["tests_dir"]:
        patterns.add(f"/{row['tests_dir']}/")
    if repo != "runtime":
        continue
    text = subprocess.run(["git", "-C", mirror, "show", f"{tag}:{source_dir}/{row['id']}.csproj"], capture_output=True, text=True, check=False).stdout
    for match in re.finditer(r"\$\(CommonPath\)([^\"]+?\.cs)", text):
        patterns.add("/src/libraries/Common/src/" + match.group(1).replace("\\", "/").lstrip("/"))
for pattern in sorted(patterns):
    print(pattern)
PY
)

if [[ ! -d "$TARGET" ]]; then
  mkdir -p "$(dirname "$TARGET")"
  git -C "$MIRROR" worktree prune
  git -C "$MIRROR" worktree add --no-checkout --detach "$TARGET" "$TAG" >/dev/null
fi
git -C "$MIRROR" config extensions.worktreeConfig true
git -C "$TARGET" -c core.bare=false config --worktree core.bare false
git -C "$TARGET" sparse-checkout set --no-cone "${PATTERNS[@]}"
git -C "$TARGET" checkout --quiet --detach "$TAG"
git -C "$TARGET" reset --quiet --hard "$TAG"
echo "$TAG -> $TARGET ($(find "$TARGET/src" -type f | wc -l) files checked out)"
