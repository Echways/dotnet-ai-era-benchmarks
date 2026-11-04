#!/usr/bin/env python3
import argparse
import csv
import json
from pathlib import Path

EPOCHS = {6: "E0", 7: "E0", 8: "E1", 9: "E1", 10: "E2", 11: "E2"}
METRIC_COLUMNS = [
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
]
COLUMNS = ["package", "tag", "major", "epoch", *METRIC_COLUMNS]


def row(path: Path) -> dict:
    metrics = json.loads(path.read_text(encoding="utf-8"))
    source = metrics["source"]
    tests = metrics.get("tests") or {}
    tag = path.parent.name
    major = int(tag.lstrip("v").split(".")[0])
    code_src = source["code_lines"]
    code_tests = tests.get("code_lines", 0)
    return {
        "package": path.name.removesuffix(".metrics.json"),
        "tag": tag,
        "major": major,
        "epoch": EPOCHS.get(major, ""),
        "files_src": source["files"],
        "code_lines_src": code_src,
        "comment_lines_src": source["comment_lines"],
        "blank_lines_src": source["blank_lines"],
        "code_lines_tests": code_tests,
        "tests_to_src_ratio": round(code_tests / code_src, 3) if code_src else "",
        **{key: source[key] for key in METRIC_COLUMNS if key in source},
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Collect per-tag static metrics of every selected package into one CSV")
    parser.add_argument("--work", type=Path, default=Path(".work/static"))
    parser.add_argument("--out", type=Path, default=Path("static-metrics/results"))
    args = parser.parse_args()
    paths = sorted(args.work.glob("*/*.metrics.json"), key=lambda p: (p.name, int(p.parent.name.lstrip("v").split(".")[0])))
    args.out.mkdir(parents=True, exist_ok=True)
    target = args.out / "static-metrics.csv"
    with open(target, "w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=COLUMNS, lineterminator="\n")
        writer.writeheader()
        writer.writerows(row(path) for path in paths)
    print(f"{target}: {len(paths)} rows")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
