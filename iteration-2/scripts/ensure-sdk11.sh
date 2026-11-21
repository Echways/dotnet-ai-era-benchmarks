#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_DOTNET="$REPO_ROOT/.dotnet"
SDK_VERSION="11.0.100"
RUNTIME_VERSION="11.0.0"

has_runtime() {
  "$1" --list-runtimes | awk -v framework="$2" -v version="$3" '$1 == framework && $2 == version { found = 1 } END { exit !found }'
}

has_sdk11() {
  [[ -x "$1/dotnet" && -f "$1/sdk/$SDK_VERSION/dotnet.dll" ]] &&
    has_runtime "$1/dotnet" Microsoft.NETCore.App "$RUNTIME_VERSION" &&
    has_runtime "$1/dotnet" Microsoft.AspNetCore.App "$RUNTIME_VERSION"
}

root="$LOCAL_DOTNET"
if ! has_sdk11 "$root"; then
  system_dotnet="$(command -v dotnet || true)"
  if [[ -n "$system_dotnet" ]] && has_sdk11 "$(dirname "$(readlink -f "$system_dotnet")")"; then
    root="$(dirname "$(readlink -f "$system_dotnet")")"
  else
    mkdir -p "$root"
    curl -fsSL --retry 5 https://dot.net/v1/dotnet-install.sh -o "$root/dotnet-install.sh"
    bash "$root/dotnet-install.sh" --version "$SDK_VERSION" --install-dir "$root" --no-path >&2
    has_sdk11 "$root"
  fi
fi
echo "$root"
