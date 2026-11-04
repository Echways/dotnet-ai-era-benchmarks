import io
import json
import time
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from pathlib import Path
from xml.etree import ElementTree

SEARCH = "https://azuresearch-usnc.nuget.org/query"
FLAT = "https://api.nuget.org/v3-flatcontainer"
PAGE = 1000
SKIP_LIMIT = 3000
ATTEMPTS = 5


def fetch(url: str) -> bytes:
    for attempt in range(ATTEMPTS):
        try:
            with urllib.request.urlopen(url, timeout=60) as response:
                return response.read()
        except urllib.error.HTTPError as error:
            if error.code == 404:
                raise
            time.sleep(2**attempt)
        except urllib.error.URLError:
            time.sleep(2**attempt)
    raise RuntimeError(f"failed to fetch {url}")


def search_owner(owner: str) -> list[dict]:
    found = []
    for skip in range(0, SKIP_LIMIT, PAGE):
        query = urllib.parse.urlencode({"q": f"owner:{owner}", "take": PAGE, "skip": skip, "prerelease": "false", "sortBy": "totalDownloads-desc"})
        data = json.loads(fetch(f"{SEARCH}?{query}"))["data"]
        found.extend(data)
        if len(data) < PAGE:
            break
    return found


def cached(path: Path, url: str) -> bytes:
    if path.exists():
        return path.read_bytes()
    content = fetch(url)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(content)
    return content


def nuspec(package_id: str, version: str, cache: Path) -> ElementTree.Element:
    lowered = package_id.lower()
    content = cached(cache / f"{lowered}.{version}.nuspec", f"{FLAT}/{lowered}/{version}/{lowered}.nuspec")
    root = ElementTree.fromstring(content)
    for element in root.iter():
        element.tag = element.tag.rpartition("}")[2]
    return root


def repository_url(spec: ElementTree.Element) -> str:
    repository = spec.find("./metadata/repository")
    return repository.get("url", "") if repository is not None else ""


def package_files(package_id: str, version: str, cache: Path) -> list[str]:
    lowered = package_id.lower()
    content = cached(cache / f"{lowered}.{version}.nupkg", f"{FLAT}/{lowered}/{version}/{lowered}.{version}.nupkg")
    with zipfile.ZipFile(io.BytesIO(content)) as archive:
        return archive.namelist()
