#!/usr/bin/env python3
import argparse
import csv
import os
import re
import subprocess
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from common import CACHE, ROOT, SUMMARY, add_step_arguments, git, libraries, read_json, steps, write_json

HASH = re.compile(r"^commit ([0-9a-f]{40})$", re.MULTILINE)
COLUMNS = ["library", "range", "method", "file", "sha", "pr_number", "class", "cca_coauthored"]
WORKERS = 8


def repository_path(library, relative: str) -> str:
    if relative.startswith("Common/"):
        return "src/libraries/Common/src/" + relative[len("Common/") :]
    return f"{library.paths[0]}/{relative}"


def changed_files(library, start: str, end: str) -> set[str]:
    return set(git(library.mirror, "diff", "--name-only", f"{start}..{end}", "--", "src/libraries/Common/src", *library.paths).splitlines())


ZERO = "0" * 40
BLOB_CHUNK = 300


def blob_ids(library, start: str, end: str, paths: list[str]) -> set[str]:
    ids: set[str] = set()
    for offset in range(0, len(paths), 200):
        output = git(library.mirror, "log", "--raw", "--no-abbrev", "--no-merges", "--format=", f"{start}..{end}", "--", *paths[offset : offset + 200])
        for line in output.splitlines():
            fields = line.split()
            if len(fields) >= 5 and fields[0].startswith(":"):
                ids.update(oid for oid in fields[2:4] if oid != ZERO)
    return ids


def prefetch_blobs(library, ids: set[str]) -> None:
    ordered = sorted(ids)
    check = subprocess.run(
        ["git", "-C", str(library.mirror), "cat-file", "--batch-check"],
        input="\n".join(ordered) + "\n",
        capture_output=True,
        text=True,
        check=False,
        env={**os.environ, "GIT_NO_LAZY_FETCH": "1"},
    )
    missing = [oid for oid, line in zip(ordered, check.stdout.splitlines(), strict=False) if line.endswith("missing")]
    for offset in range(0, len(missing), BLOB_CHUNK):
        chunk = missing[offset : offset + BLOB_CHUNK]
        fetch_chunk(library, chunk)


def fetch_chunk(library, chunk: list[str]) -> None:
    for attempt in range(6):
        result = subprocess.run(
            [
                "git",
                "-C",
                str(library.mirror),
                "-c",
                "fetch.negotiationAlgorithm=noop",
                "fetch",
                "origin",
                "--no-tags",
                "--no-write-fetch-head",
                "--recurse-submodules=no",
                "--filter=blob:none",
                "--stdin",
            ],
            input="\n".join(chunk) + "\n",
            text=True,
            check=False,
        )
        if result.returncode == 0:
            return
        time.sleep(min(120, 15 * (attempt + 1)))
    raise RuntimeError(f"could not fetch {len(chunk)} blobs for {library.name}")


def touching_commits(library, path: str, first: int, last: int, start: str, end: str) -> list[str]:
    result = subprocess.run(
        ["git", "-C", str(library.mirror), "log", f"-L{first},{last}:{path}", "--format=commit %H", f"{start}..{end}"],
        capture_output=True,
        text=True,
        check=False,
    )
    return list(dict.fromkeys(HASH.findall(result.stdout)))


def touching_by_method(library, step, methods: list[dict], changed: set[str]) -> dict[str, list[str]]:
    jobs = [(method, repository_path(library, method["file"])) for method in methods]
    touched_paths = sorted({path for _, path in jobs if path in changed})
    prefetch_blobs(library, blob_ids(library, step.start, step.end, touched_paths))
    with ThreadPoolExecutor(WORKERS) as pool:
        futures = {
            method["id"]: pool.submit(touching_commits, library, path, int(method["line"]), int(method["end_line"]), step.start, step.end)
            for method, path in jobs
            if path in changed
        }
    return {method["id"]: futures[method["id"]].result() if method["id"] in futures else [] for method, _ in jobs}


def graph_methods(tag: str, package: str) -> list[dict]:
    path = ROOT / "callgraph" / tag / package / "methods.tsv"
    if not path.exists():
        return []
    with path.open(encoding="utf-8") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def main() -> int:
    parser = argparse.ArgumentParser(description="Find the commits that touched every traced method between two tags (git log -L)")
    add_step_arguments(parser)
    parser.add_argument("--attribution", default=str(SUMMARY / "attribution.csv"))
    parser.add_argument("--out", default=str(SUMMARY / "attribution-methods.csv"))
    args = parser.parse_args()
    attributed = {}
    if Path(args.attribution).exists():
        with open(args.attribution, encoding="utf-8") as handle:
            attributed = {(row["library"], row["range"], row["sha"]): row for row in csv.DictReader(handle)}
    rows = []
    for step in steps(args.start, args.end):
        for library in libraries(args.library):
            methods = graph_methods(step.end, library.name)
            if not methods:
                continue
            changed = changed_files(library, step.start, step.end)
            cache_path = CACHE / f"methods-{library.name}-{step.key}.json"
            shas_by_method = read_json(cache_path, None)
            if shas_by_method is None:
                shas_by_method = touching_by_method(library, step, methods, changed)
                write_json(cache_path, shas_by_method)
            for method in methods:
                path = repository_path(library, method["file"])
                shas = shas_by_method[method["id"]]
                base = {"library": library.name, "range": step.key, "method": method["id"], "file": path}
                if not shas:
                    rows.append({**base, "sha": "", "pr_number": "", "class": "untouched", "cca_coauthored": ""})
                for sha in shas:
                    row = attributed.get((library.name, step.key, sha))
                    rows.append(
                        {
                            **base,
                            "sha": sha,
                            "pr_number": row["pr_number"] if row else "",
                            "class": row["class"] if row else "unattributed",
                            "cca_coauthored": row["cca_coauthored"] if row else "",
                        }
                    )
            print(f"{library.name} {step.key}: {len(methods)} methods, {len(changed)} changed files", flush=True)
    SUMMARY.mkdir(parents=True, exist_ok=True)
    with open(args.out, "w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=COLUMNS, lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
    print(f"-> {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
