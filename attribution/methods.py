#!/usr/bin/env python3
import argparse
import csv
import re
import subprocess
from pathlib import Path

from common import CONFIG, ROOT, SUMMARY, git, libraries

HASH = re.compile(r"^commit ([0-9a-f]{40})$", re.MULTILINE)


def locate(library, type_name: str, tag: str) -> str | None:
    pattern = rf"(class|struct|record|interface)[[:space:]]+{type_name}([^A-Za-z0-9_]|$)"
    result = subprocess.run(
        ["git", "-C", str(library.mirror), "grep", "-l", "-E", pattern, tag, "--", *library.paths],
        capture_output=True,
        text=True,
        check=False,
    )
    files = [line.split(":", 1)[1] for line in result.stdout.splitlines() if ":" in line]
    return sorted(files, key=len)[0] if files else None


MODIFIERS = r"(?:public|private|internal|protected|static|override|virtual|sealed|async|unsafe|new|extern|abstract|readonly|partial)"


def method_ranges(source: str, method: str) -> list[tuple[int, int]]:
    declaration = re.compile(rf"^[ \t]*(?:{MODIFIERS}[ \t]+)+[^=;(){{}}]*?\b{re.escape(method)}[ \t]*[(<]", re.MULTILINE)
    ranges = []
    for match in declaration.finditer(source):
        end = body_end(source, match.end())
        if end is not None:
            first = source.count("\n", 0, match.start()) + 1
            last = source.count("\n", 0, end) + 1
            ranges.append((first, last))
    return ranges


def body_end(source: str, position: int) -> int | None:
    depth = 0
    index = position
    while index < len(source):
        char = source[index]
        pair = source[index : index + 2]
        if pair == "//":
            index = source.find("\n", index)
            if index < 0:
                return None
            continue
        if pair == "/*":
            index = source.find("*/", index + 2) + 2
            continue
        if char in "\"'":
            index = skip_literal(source, index)
            continue
        if depth == 0 and pair == "=>":
            return source.find(";", index)
        if depth == 0 and char == ";":
            return None
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return index
        index += 1
    return None


def skip_literal(source: str, index: int) -> int:
    quote = source[index]
    verbatim = index > 0 and source[index - 1] == "@"
    index += 1
    while index < len(source):
        if source[index] == "\\" and not verbatim:
            index += 2
            continue
        if source[index] == quote:
            if verbatim and source[index + 1 : index + 2] == quote:
                index += 2
                continue
            return index + 1
        index += 1
    return index


def touching_commits(library, method: str, path: str, start: str, end: str) -> list[str]:
    source = git(library.mirror, "show", f"{end}:{path}")
    shas: list[str] = []
    for first, last in method_ranges(source, method):
        result = subprocess.run(
            ["git", "-C", str(library.mirror), "log", f"-L{first},{last}:{path}", "--format=commit %H", f"{start}..{end}"],
            capture_output=True,
            text=True,
            check=False,
        )
        shas.extend(sha for sha in HASH.findall(result.stdout) if sha not in shas)
    return shas


def main() -> int:
    parser = argparse.ArgumentParser(description="Find the pull requests that touched each hot method between two tags (git log -L)")
    parser.add_argument("--library", default="DependencyInjection,Caching")
    parser.add_argument("--from", dest="start", default=CONFIG["default_range"]["from"])
    parser.add_argument("--to", dest="end", default=CONFIG["default_range"]["to"])
    parser.add_argument("--methods", default=str(ROOT / "glassbox" / "methods.txt"))
    parser.add_argument("--attribution", default=str(SUMMARY / "attribution.csv"))
    parser.add_argument("--out", default=str(SUMMARY / "attribution-methods.csv"))
    args = parser.parse_args()
    attributed = {}
    if Path(args.attribution).exists():
        with open(args.attribution, encoding="utf-8") as handle:
            attributed = {row["sha"]: row for row in csv.DictReader(handle)}
    targets = [line.strip() for line in Path(args.methods).read_text(encoding="utf-8").splitlines() if line.strip()]
    selected = [library for library in libraries(args.library) if library.repo == "runtime"]
    rows = []
    for target in targets:
        type_name, method = target.split(".", 1)
        located = next(((library, path) for library in selected if (path := locate(library, type_name, args.end))), None)
        if located is None:
            print(f"warning: {type_name} not found at {args.end}")
            rows.append({"method": target, "library": "", "file": "", "sha": "", "pr_number": "", "class": "type-not-found", "cca_coauthored": ""})
            continue
        library, path = located
        commits = touching_commits(library, method, path, args.start, args.end)
        if not commits:
            rows.append({"method": target, "library": library.name, "file": path, "sha": "", "pr_number": "", "class": "untouched", "cca_coauthored": ""})
        for sha in commits:
            row = attributed.get(sha)
            rows.append(
                {
                    "method": target,
                    "library": library.name,
                    "file": path,
                    "sha": sha,
                    "pr_number": row["pr_number"] if row else "",
                    "class": row["class"] if row else "unattributed",
                    "cca_coauthored": row["cca_coauthored"] if row else "",
                }
            )
        print(f"{target}: {len(commits)} commits in {path}")
    with open(args.out, "w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=["method", "library", "file", "sha", "pr_number", "class", "cca_coauthored"])
        writer.writeheader()
        writer.writerows(rows)
    print(f"-> {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
