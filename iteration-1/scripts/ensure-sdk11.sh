#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_DOTNET="$REPO_ROOT/.dotnet"

has_sdk11() {
  "$1" --list-sdks 2>/dev/null | grep -qE '^11\.'
}

if command -v dotnet >/dev/null 2>&1 && has_sdk11 dotnet; then
  dirname "$(readlink -f "$(command -v dotnet)")"
  exit 0
fi
if [[ -x "$LOCAL_DOTNET/dotnet" ]] && has_sdk11 "$LOCAL_DOTNET/dotnet"; then
  echo "$LOCAL_DOTNET"
  exit 0
fi
mkdir -p "$LOCAL_DOTNET"
curl -sSL --retry 5 https://dot.net/v1/dotnet-install.sh -o "$LOCAL_DOTNET/dotnet-install.sh"
bash "$LOCAL_DOTNET/dotnet-install.sh" --channel 11.0 --quality preview --install-dir "$LOCAL_DOTNET" --no-path >&2
bash "$LOCAL_DOTNET/dotnet-install.sh" --channel 10.0 --runtime dotnet --install-dir "$LOCAL_DOTNET" --no-path >&2
bash "$LOCAL_DOTNET/dotnet-install.sh" --channel 10.0 --runtime aspnetcore --install-dir "$LOCAL_DOTNET" --no-path >&2
has_sdk11 "$LOCAL_DOTNET/dotnet"
echo "$LOCAL_DOTNET"
