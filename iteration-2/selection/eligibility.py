import re
from dataclasses import dataclass

SOURCE_REPOSITORIES = {
    "dotnet/runtime": "runtime",
    "dotnet/aspnetcore": "aspnetcore",
    "dotnet/efcore": "efcore",
}
MODERN_FRAMEWORK = re.compile(r"^(net(\d+)\.\d+|netstandard2\.[01]|netcoreapp\d\.\d)(-.*)?$")
IMPLEMENTATION_DIRECTORIES = ("lib", "runtimes")


@dataclass(frozen=True)
class Eligibility:
    eligible: bool
    reason: str


def repository_key(url: str) -> str:
    match = re.search(r"github\.com[/:]([^/]+/[^/.]+)", url)
    if not match:
        return ""
    return SOURCE_REPOSITORIES.get(match.group(1).lower(), "")


def implementation_frameworks(files: list[str]) -> set[str]:
    frameworks = set()
    for name in files:
        parts = name.split("/")
        if parts[0] == "lib" and len(parts) >= 3 and parts[-1].endswith(".dll"):
            frameworks.add(parts[1])
        if parts[0] == "runtimes" and len(parts) >= 5 and parts[2] == "lib" and parts[-1].endswith(".dll"):
            frameworks.add(parts[3])
    return frameworks


def eligibility(files: list[str], package_types: list[str]) -> Eligibility:
    if "Template" in package_types or "DotnetTool" in package_types:
        return Eligibility(False, "tool or template")
    if any(name.startswith("analyzers/") for name in files) and not implementation_frameworks(files):
        return Eligibility(False, "analyzer or source generator")
    frameworks = implementation_frameworks(files)
    if not frameworks:
        if any(name.startswith("ref/") for name in files):
            return Eligibility(False, "contract only")
        return Eligibility(False, "metapackage")
    if not any(MODERN_FRAMEWORK.match(framework) for framework in frameworks):
        return Eligibility(False, "no implementation for current runtimes")
    return Eligibility(True, "")
