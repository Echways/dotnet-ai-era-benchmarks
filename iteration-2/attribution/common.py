import csv
import json
import os
import re
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ATTRIBUTION = ROOT / "attribution"
CACHE = ROOT / ".work" / "attribution-cache"
SUMMARY = ATTRIBUTION / "results"
PACKAGES = ROOT / "glassbox" / "packages.tsv"
CONFIG = json.loads((ATTRIBUTION / "config.json").read_text(encoding="utf-8"))

CCA_ACCOUNT_ID = 198982749
CCA_LOGIN = "Copilot"
COPILOT_REVIEWER_ID = 175728472
COPILOT_CLI_ID = 223556219
COAUTHOR = re.compile(r"^Co-authored-by:\s*(?P<name>.+?)\s*<(?P<email>[^>]+)>\s*$", re.IGNORECASE | re.MULTILINE)
NOREPLY_ID = re.compile(r"^(?P<id>\d+)\+[^@]+@users\.noreply\.github\.com$", re.IGNORECASE)


@dataclass(frozen=True)
class Library:
    name: str
    repo: str
    paths: tuple[str, ...]

    @property
    def owner(self) -> str:
        return CONFIG["repos"][self.repo]["owner"]

    @property
    def repo_name(self) -> str:
        return CONFIG["repos"][self.repo]["name"]

    @property
    def mirror(self) -> Path:
        return ROOT / CONFIG["repos"][self.repo]["mirror"]


@dataclass(frozen=True)
class Step:
    start: str
    end: str

    @property
    def key(self) -> str:
        return f"{self.start}..{self.end}"


def configured_libraries() -> dict[str, Library]:
    with PACKAGES.open(encoding="utf-8") as handle:
        rows = list(csv.DictReader(handle, delimiter="\t"))
    return {row["id"]: Library(row["id"], row["repo"], (row["source_dir"],)) for row in rows}


def libraries(selection: str) -> list[Library]:
    configured = configured_libraries()
    names = list(configured) if selection == "all" else [name.strip() for name in selection.split(",")]
    unknown = [name for name in names if name not in configured]
    if unknown:
        sys.exit(f"unknown libraries: {', '.join(unknown)}")
    return [configured[name] for name in names]


def steps(start: str | None, end: str | None) -> list[Step]:
    if start and end:
        return [Step(start, end)]
    return [Step(step["from"], step["to"]) for step in CONFIG["steps"]]


def add_step_arguments(parser) -> None:
    parser.add_argument("--library", default="all", help="comma-separated package ids from glassbox/packages.tsv, or 'all'")
    parser.add_argument("--from", dest="start", default=None)
    parser.add_argument("--to", dest="end", default=None)


def ensure_mirror(repo: str) -> Path:
    mirror = ROOT / CONFIG["repos"][repo]["mirror"]
    if not mirror.exists():
        mirror.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(["git", "clone", "--bare", "--filter=blob:none", CONFIG["repos"][repo]["url"], str(mirror)], check=True)
    return mirror


def git(mirror: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(mirror), *args], capture_output=True, text=True, check=True).stdout


def coauthor_ids(message: str) -> list[int]:
    ids = []
    for match in COAUTHOR.finditer(message):
        noreply = NOREPLY_ID.match(match.group("email"))
        if noreply:
            ids.append(int(noreply.group("id")))
    return ids


def read_json(path: Path, default):
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else default


def write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


class GitHub:
    def __init__(self, offline: bool = False) -> None:
        self.offline = offline
        self.gh = shutil.which("gh") is not None and self._gh_authenticated()
        self.token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
        self.requests = 0

    @property
    def authenticated(self) -> bool:
        return self.gh or bool(self.token)

    @property
    def transport(self) -> str:
        if self.gh:
            return "gh api"
        return "https with token" if self.token else "https unauthenticated (60 requests/hour)"

    @staticmethod
    def _gh_authenticated() -> bool:
        return subprocess.run(["gh", "auth", "status"], capture_output=True, check=False).returncode == 0

    def graphql(self, query: str) -> dict:
        self.requests += 1
        for attempt in range(8):
            if self.gh:
                result = subprocess.run(["gh", "api", "graphql", "-f", f"query={query}"], capture_output=True, text=True, check=False)
                if result.returncode == 0:
                    return json.loads(result.stdout)
                if "rate limit" in result.stderr.lower() or "502" in result.stderr or "timeout" in result.stderr.lower():
                    time.sleep(min(300, 30 * (attempt + 1)))
                    continue
                raise RuntimeError(f"gh api graphql: {result.stderr.strip()[:500]}")
            headers = {"Authorization": f"Bearer {self.token}", "Content-Type": "application/json", "User-Agent": "dotnet-ai-era-benchmarks-attribution"}
            request = urllib.request.Request("https://api.github.com/graphql", data=json.dumps({"query": query}).encode(), headers=headers)
            try:
                with urllib.request.urlopen(request, timeout=120) as response:
                    return json.loads(response.read())
            except (urllib.error.URLError, TimeoutError, ConnectionError):
                time.sleep(min(120, 10 * (attempt + 1)))
        raise RuntimeError("GraphQL request failed repeatedly")

    @staticmethod
    def cache_path(endpoint: str) -> Path:
        return CACHE / (re.sub(r"[^A-Za-z0-9._-]+", "_", endpoint.strip("/")) + ".json")

    def get(self, endpoint: str):
        cached = self.cache_path(endpoint)
        if cached.exists():
            return json.loads(cached.read_text(encoding="utf-8"))
        if self.offline:
            return None
        for attempt in range(5):
            result = subprocess.run(["gh", "api", endpoint], capture_output=True, text=True, check=False)
            if result.returncode == 0:
                value = json.loads(result.stdout)
                write_json(cached, value)
                self.requests += 1
                return value
            if "rate limit" in result.stderr.lower():
                time.sleep(60 * (attempt + 1))
                continue
            raise RuntimeError(f"gh api {endpoint}: {result.stderr.strip()}")
        raise RuntimeError(f"gh api {endpoint}: rate limited")
