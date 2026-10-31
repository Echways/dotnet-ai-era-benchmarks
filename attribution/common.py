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
CACHE = ATTRIBUTION / "cache"
SUMMARY = ROOT / "results" / "summary"
CONFIG = json.loads((ATTRIBUTION / "libraries.json").read_text(encoding="utf-8"))

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


def libraries(selection: str) -> list[Library]:
    configured = CONFIG["libraries"]
    names = list(configured) if selection == "all" else [name.strip() for name in selection.split(",")]
    unknown = [name for name in names if name not in configured]
    if unknown:
        sys.exit(f"unknown libraries: {', '.join(unknown)}; known: {', '.join(configured)}")
    return [Library(name, configured[name]["repo"], tuple(configured[name]["paths"])) for name in names]


def ensure_mirror(repo: str) -> Path:
    mirror = ROOT / CONFIG["repos"][repo]["mirror"]
    if not mirror.exists():
        mirror.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(["git", "clone", "--bare", "--filter=blob:none", CONFIG["repos"][repo]["url"], str(mirror)], check=True)
    else:
        subprocess.run(["git", "-C", str(mirror), "fetch", "--quiet", "--tags", "--filter=blob:none", "origin"], check=False)
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


def range_key(start: str, end: str) -> str:
    return f"{start}..{end}"


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

    @staticmethod
    def cache_path(endpoint: str) -> Path:
        return CACHE / (re.sub(r"[^A-Za-z0-9._-]+", "_", endpoint.strip("/")) + ".json")

    def get(self, endpoint: str):
        cached = self.cache_path(endpoint)
        if cached.exists():
            return json.loads(cached.read_text(encoding="utf-8"))
        if self.offline:
            return None
        value = self._gh(endpoint) if self.gh else self._https(endpoint)
        write_json(cached, value)
        self.requests += 1
        return value

    def _gh(self, endpoint: str):
        for attempt in range(5):
            result = subprocess.run(["gh", "api", endpoint], capture_output=True, text=True, check=False)
            if result.returncode == 0:
                return json.loads(result.stdout)
            if "rate limit" in result.stderr.lower():
                time.sleep(60 * (attempt + 1))
                continue
            raise RuntimeError(f"gh api {endpoint}: {result.stderr.strip()}")
        raise RuntimeError(f"gh api {endpoint}: rate limited")

    def _https(self, endpoint: str):
        headers = {"Accept": "application/vnd.github+json", "X-GitHub-Api-Version": "2022-11-28", "User-Agent": "dotnet-ai-era-benchmarks-attribution"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        for attempt in range(8):
            request = urllib.request.Request(f"https://api.github.com/{endpoint.lstrip('/')}", headers=headers)
            try:
                with urllib.request.urlopen(request, timeout=60) as response:
                    return json.loads(response.read())
            except urllib.error.HTTPError as error:
                remaining = error.headers.get("X-RateLimit-Remaining")
                if error.code in (403, 429) and (remaining == "0" or error.headers.get("Retry-After")):
                    reset = int(error.headers.get("X-RateLimit-Reset", time.time() + 60))
                    wait = max(int(error.headers.get("Retry-After") or 0), reset - int(time.time()) + 2, 5)
                    print(f"rate limited; waiting {wait}s (until {time.strftime('%H:%M:%S', time.localtime(time.time() + wait))})", flush=True)
                    time.sleep(wait)
                    continue
                if error.code == 404:
                    return None
                raise
            except (urllib.error.URLError, TimeoutError, ConnectionError):
                time.sleep(min(60, 5 * (attempt + 1)))
        raise RuntimeError(f"GET {endpoint} failed repeatedly")
