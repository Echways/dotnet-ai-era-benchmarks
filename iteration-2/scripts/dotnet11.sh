#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET11_ROOT="$("$REPO_ROOT/scripts/ensure-sdk11.sh")"
SDK_DIR="$(ls -d "$DOTNET11_ROOT"/sdk/11.* | sort -V | tail -n 1)"
export DOTNET_ROOT="$DOTNET11_ROOT"
export DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR="$DOTNET11_ROOT"
exec "$DOTNET11_ROOT/dotnet" exec "$SDK_DIR/dotnet.dll" "$@"
