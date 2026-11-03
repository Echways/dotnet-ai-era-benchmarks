#!/usr/bin/env bash
set -euo pipefail
EXPERIMENTS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$EXPERIMENTS/.." && pwd)"
MIRROR="$REPO_ROOT/.work/repos/runtime.git"
BASE_POINT="$REPO_ROOT/bin/e2-11"
RC=11.0.0-rc.3.26522.148

usage() {
  echo "usage: $0 --library binder|di --commit SHA --name NAME [--revert SHA] [--ignore-check stub|no-inherit] [--binder-file-from REF]" >&2
  exit 2
}

LIBRARY=""
COMMIT=""
NAME=""
REVERT=""
IGNORE_CHECK=""
BINDER_FILE_FROM=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --library) LIBRARY="$2"; shift 2 ;;
    --commit) COMMIT="$2"; shift 2 ;;
    --name) NAME="$2"; shift 2 ;;
    --revert) REVERT="$2"; shift 2 ;;
    --ignore-check) IGNORE_CHECK="$2"; shift 2 ;;
    --binder-file-from) BINDER_FILE_FROM="$2"; shift 2 ;;
    *) usage ;;
  esac
done
[[ -n "$LIBRARY" && -n "$COMMIT" && -n "$NAME" ]] || usage

case "$LIBRARY" in
  binder) ASSEMBLY=Microsoft.Extensions.Configuration.Binder; DEPENDENCY=Microsoft.Extensions.Configuration; DEFINES=""; EXCLUDE="src/src/libraries/$ASSEMBLY/src/**/gen/**" ;;
  di) ASSEMBLY=Microsoft.Extensions.DependencyInjection; DEPENDENCY=Microsoft.Extensions.DependencyInjection.Abstractions; DEFINES="IL_EMIT"; EXCLUDE="" ;;
  *) usage ;;
esac

WORK="$REPO_ROOT/.work/experiments/$NAME"
SOURCE="src/libraries/$ASSEMBLY/src"
rm -rf "$WORK"
mkdir -p "$WORK/src"
git -C "$MIRROR" archive "$COMMIT" "$SOURCE" src/libraries/Common/src/Extensions/ParameterDefaultValue src/libraries/Common/src/Extensions/TypeNameHelper | tar -x -C "$WORK/src"
rm -f "$WORK/src/src/libraries/Common/src/Extensions/ParameterDefaultValue/ParameterDefaultValue.netstandard.cs"
if [[ -n "$REVERT" ]]; then
  git -C "$MIRROR" show --format= "$REVERT" -- "$SOURCE" | git -C "$WORK/src" apply -R --whitespace=nowarn
fi
BINDER_SOURCE="$WORK/src/$SOURCE/ConfigurationBinder.cs"
if [[ -n "$BINDER_FILE_FROM" ]]; then
  git -C "$MIRROR" fetch --quiet --filter=blob:none origin "$BINDER_FILE_FROM"
  git -C "$MIRROR" show "FETCH_HEAD:$SOURCE/ConfigurationBinder.cs" > "$BINDER_SOURCE"
fi
case "$IGNORE_CHECK" in
  stub) sed -i 's|property.IsDefined(typeof(ConfigurationIgnoreAttribute));|false;|' "$BINDER_SOURCE" ;;
  no-inherit) sed -i 's|property.IsDefined(typeof(ConfigurationIgnoreAttribute));|property.IsDefined(typeof(ConfigurationIgnoreAttribute), inherit: false);|' "$BINDER_SOURCE" ;;
esac
echo "<Project />" > "$WORK/Directory.Build.props"
echo "<Project />" > "$WORK/Directory.Build.targets"
echo "<Project />" > "$WORK/Directory.Packages.props"
python3 "$EXPERIMENTS/generate_sr.py" "$WORK/src/$SOURCE/Resources/Strings.resx" "$WORK/SR.cs"
cat > "$WORK/$ASSEMBLY.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Optimize>true</Optimize>
    <DefineConstants>\$(DefineConstants);$DEFINES</DefineConstants>
    <AssemblyVersion>11.0.0.0</AssemblyVersion>
    <InformationalVersion>$RC+$NAME</InformationalVersion>
    <NoWarn>\$(NoWarn);CS1591;CS8600;CS8601;CS8602;CS8603;CS8604;CS8618;CS8625;SYSLIB0050;IL2026;IL3050</NoWarn>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="SR.cs;src/**/*.cs" Exclude="$EXCLUDE" />
    <PackageReference Include="$DEPENDENCY" Version="$RC" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="$RC" Condition="'$LIBRARY' == 'binder'" />
  </ItemGroup>
</Project>
PROJECT
dotnet build "$WORK/$ASSEMBLY.csproj" -c Release -nologo -v:q -clp:ErrorsOnly -o "$WORK/out"
HARNESS="$REPO_ROOT/bin/experiment-$NAME"
rm -rf "$HARNESS"
cp -r "$BASE_POINT" "$HARNESS"
cp "$WORK/out/$ASSEMBLY.dll" "$HARNESS/"
echo "$NAME -> $HARNESS"
