#!/usr/bin/env python3
import argparse
import csv
import json
import re
from pathlib import Path

WARNING = re.compile(r"^(?P<file>.+?)\((?P<line>\d+),(?P<column>\d+)\): warning (?P<code>[A-Za-z]+\d+): .*\[(?P<project>[^\]]+)\]$")
EPOCHS = {6: "E0", 7: "E0", 8: "E1", 9: "E1", 10: "E2", 11: "E2"}
COLUMNS = [
    "tag",
    "major",
    "epoch",
    "loc_tool",
    "files_src",
    "code_lines_src",
    "comment_lines_src",
    "blank_lines_src",
    "code_lines_tests",
    "tests_to_src_ratio",
    "methods",
    "cc_mean",
    "cc_median",
    "cc_p90",
    "cc_max",
    "cc_over_10",
    "loc_per_method_mean",
    "loc_per_method_median",
    "loc_per_method_p90",
    "nesting_mean",
    "nesting_max",
    "build_status",
    "warnings",
    "warning_ids",
]


def cloc_code(path: Path) -> int | None:
    if not path.exists():
        return None
    document = json.loads(path.read_text(encoding="utf-8"))
    return document.get("C#", {}).get("code")


def warnings(log: Path, project_name: str) -> tuple[int, int]:
    if not log.exists():
        return 0, 0
    distinct = set()
    for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
        match = WARNING.match(line.strip())
        if match and Path(match.group("project")).stem == project_name:
            distinct.add((match.group("file"), match.group("line"), match.group("column"), match.group("code")))
    return len(distinct), len({entry[3] for entry in distinct})


def row(tag_dir: Path, library: str) -> dict | None:
    metrics_path = tag_dir / f"{library}.metrics.json"
    if not metrics_path.exists():
        return None
    metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
    source = metrics["source"]
    tests = metrics.get("tests") or {}
    tag = tag_dir.name
    major = int(tag.lstrip("v").split(".")[0])
    loc_tool = (tag_dir / "loc-tool").read_text().strip() if (tag_dir / "loc-tool").exists() else "roslyn"
    code_src = source["code_lines"]
    code_tests = tests.get("code_lines", 0)
    if loc_tool == "cloc":
        code_src = cloc_code(tag_dir / f"{library}.cloc-src.json") or code_src
        code_tests = cloc_code(tag_dir / f"{library}.cloc-tests.json") or code_tests
    project = f"Glassbox.V{major}.{library.removeprefix('Microsoft.Extensions.')}"
    status_path = tag_dir / f"{project}.build.status"
    status = status_path.read_text().strip() if status_path.exists() else ""
    count, ids = warnings(tag_dir / f"{project}.build.log", project)
    return {
        "tag": tag,
        "major": major,
        "epoch": EPOCHS.get(major, ""),
        "loc_tool": loc_tool,
        "files_src": source["files"],
        "code_lines_src": code_src,
        "comment_lines_src": source["comment_lines"],
        "blank_lines_src": source["blank_lines"],
        "code_lines_tests": code_tests,
        "tests_to_src_ratio": round(code_tests / code_src, 3) if code_src else "",
        **{key: source[key] for key in COLUMNS if key in source and key not in ("code_lines",)},
        "build_status": "ok" if status == "0" else f"failed ({status})" if status else "not built",
        "warnings": count if status == "0" else "",
        "warning_ids": ids if status == "0" else "",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Collect per-tag static metrics into results/summary/static-<lib>.csv")
    parser.add_argument("--work", type=Path, default=Path(".work/static"))
    parser.add_argument("--out", type=Path, default=Path("results/summary"))
    parser.add_argument("--libraries", type=Path, default=Path("glassbox/libraries.txt"))
    args = parser.parse_args()
    libraries = [line.strip() for line in args.libraries.read_text(encoding="utf-8").splitlines() if line.strip()]
    tags = sorted((d for d in args.work.iterdir() if d.is_dir()), key=lambda d: int(d.name.lstrip("v").split(".")[0]))
    args.out.mkdir(parents=True, exist_ok=True)
    for library in libraries:
        rows = [r for tag_dir in tags if (r := row(tag_dir, library))]
        short = library.removeprefix("Microsoft.Extensions.")
        path = args.out / f"static-{short}.csv"
        with open(path, "w", newline="", encoding="utf-8") as handle:
            writer = csv.DictWriter(handle, fieldnames=COLUMNS)
            writer.writeheader()
            writer.writerows(rows)
        print(f"{path}: {len(rows)} tags")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
