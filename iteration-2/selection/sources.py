import re
import subprocess
from pathlib import Path

EXCLUDED_SEGMENTS = {"test", "tests", "ref", "gen", "tools", "perf", "samples", "sample", "benchmarks", "testassets", "testing"}
PATTERNS = {
    "runtime": re.compile(r"^src/libraries/(?P<name>[^/]+)/src/(?P=name)\.csproj$"),
    "aspnetcore": re.compile(r"^src/(?:.+/)?src/(?P<name>[^/]+)\.csproj$"),
    "efcore": re.compile(r"^src/(?P<name>[^/]+)/(?P=name)\.csproj$"),
}


def project_index(mirror: Path, repository: str, tag: str) -> dict[str, str]:
    listing = subprocess.run(["git", "-C", str(mirror), "ls-tree", "-r", "--name-only", tag], capture_output=True, text=True, check=True).stdout.splitlines()
    pattern = PATTERNS[repository]
    index: dict[str, str] = {}
    for path in listing:
        match = pattern.match(path)
        if not match:
            continue
        segments = {segment.lower() for segment in path.split("/")[:-1]}
        if segments & EXCLUDED_SEGMENTS - {"src"}:
            continue
        index.setdefault(match.group("name").lower(), path)
    return index


def locate(indexes: dict[str, dict[str, str]], package_id: str) -> tuple[str, str] | None:
    for repository, index in indexes.items():
        path = index.get(package_id.lower())
        if path:
            return repository, path
    return None


def latest_tag(mirror: Path, major: int) -> str:
    tags = subprocess.run(["git", "-C", str(mirror), "tag", "--list", f"v{major}.0.*"], capture_output=True, text=True, check=True).stdout.split()
    stable = [tag for tag in tags if re.fullmatch(rf"v{major}\.0\.\d+", tag)]
    return max(stable, key=lambda tag: int(tag.rsplit(".", 1)[1]))
