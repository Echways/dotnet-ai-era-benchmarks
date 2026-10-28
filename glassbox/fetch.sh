#!/usr/bin/env bash
set -euo pipefail
GLASSBOX="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$GLASSBOX/.." && pwd)"
TAG="${1:?usage: glassbox/fetch.sh <tag, e.g. v7.0.20>}"
MIRROR="$REPO_ROOT/.work/repos/runtime.git"
TARGET="$GLASSBOX/src/$TAG"

if [[ ! -d "$MIRROR" ]]; then
  mkdir -p "$(dirname "$MIRROR")"
  git clone --bare --filter=blob:none https://github.com/dotnet/runtime.git "$MIRROR"
fi
if ! git -C "$MIRROR" rev-parse --verify --quiet "refs/tags/$TAG" >/dev/null; then
  git -C "$MIRROR" fetch --filter=blob:none origin "refs/tags/$TAG:refs/tags/$TAG"
fi

mapfile -t PATTERNS < <(python3 - "$MIRROR" "$TAG" "$GLASSBOX/libraries.txt" <<'PY'
import re
import subprocess
import sys

mirror, tag, libraries_file = sys.argv[1:4]
libraries = [line.strip() for line in open(libraries_file) if line.strip()]
patterns = set()
for library in libraries:
    patterns.add(f"/src/libraries/{library}/src/")
    project = f"src/libraries/{library}/src/{library}.csproj"
    text = subprocess.run(["git", "-C", mirror, "show", f"{tag}:{project}"], capture_output=True, text=True, check=True).stdout
    for match in re.finditer(r"\$\(CommonPath\)([^\"]+?\.cs)", text):
        patterns.add("/src/libraries/Common/src/" + match.group(1).replace("\\", "/"))
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
