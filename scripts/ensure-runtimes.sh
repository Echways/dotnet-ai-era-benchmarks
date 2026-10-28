#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_DOTNET="$REPO_ROOT/.dotnet"
"$REPO_ROOT/scripts/ensure-sdk11.sh" >/dev/null
INSTALL="$LOCAL_DOTNET/dotnet-install.sh"
if ! "$LOCAL_DOTNET/dotnet" --list-sdks | grep -q '^10\.'; then
  bash "$INSTALL" --channel 10.0 --install-dir "$LOCAL_DOTNET" --no-path >&2
fi
for channel in 6.0 7.0 8.0 9.0 10.0; do
  for runtime in dotnet aspnetcore; do
    framework="Microsoft.NETCore.App"
    [[ "$runtime" == "aspnetcore" ]] && framework="Microsoft.AspNetCore.App"
    if ! "$LOCAL_DOTNET/dotnet" --list-runtimes | grep -q "^$framework ${channel%.0}\."; then
      bash "$INSTALL" --channel "$channel" --runtime "$runtime" --install-dir "$LOCAL_DOTNET" --no-path >&2
    fi
  done
done
"$LOCAL_DOTNET/dotnet" --list-runtimes >&2
echo "$LOCAL_DOTNET"
