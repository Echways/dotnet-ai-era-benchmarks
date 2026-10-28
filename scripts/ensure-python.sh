#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VENV="$REPO_ROOT/.venv"
REQUIREMENTS="$REPO_ROOT/analysis/requirements.txt"
STAMP="$VENV/.requirements.sha256"
if [[ ! -x "$VENV/bin/python" ]]; then
  if command -v uv >/dev/null 2>&1; then
    uv venv --quiet --python 3.12 "$VENV" >&2
  else
    python3.12 -m venv "$VENV" >&2
  fi
fi
digest="$(sha256sum "$REQUIREMENTS" | cut -d' ' -f1)"
if [[ ! -f "$STAMP" || "$(cat "$STAMP")" != "$digest" ]]; then
  if command -v uv >/dev/null 2>&1; then
    uv pip install --quiet --python "$VENV/bin/python" -r "$REQUIREMENTS" >&2
  else
    "$VENV/bin/python" -m pip install --quiet -r "$REQUIREMENTS" >&2
  fi
  echo "$digest" > "$STAMP"
fi
echo "$VENV/bin/python"
