#!/usr/bin/env python3
import argparse
import csv
import json
import urllib.request
from datetime import date, timedelta
from pathlib import Path

from common import CACHE, CONFIG, SUMMARY, git, libraries, read_json, write_json

PERF_LABELS = {"tenet-performance", "regression-from-last-release"}
RELEASES = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/{major}.0/releases.json"


def releases(major: int) -> list[dict]:
    cached = CACHE / f"releases-{major}.0.json"
    document = read_json(cached, None)
    if document is None:
        with urllib.request.urlopen(RELEASES.format(major=major), timeout=60) as response:
            document = json.loads(response.read())
        write_json(cached, document)
    return document["releases"]


def servicing(library, major: int, window_days: int) -> dict:
    stable = [r for r in releases(major) if r["release-version"].startswith(f"{major}.0.") and "-" not in r["release-version"]]
    ga = next(r for r in stable if r["release-version"] == f"{major}.0.0")
    ga_date = date.fromisoformat(ga["release-date"])
    window_end = ga_date + timedelta(days=window_days)
    patches = sorted(
        (r for r in stable if r["release-version"] != f"{major}.0.0" and date.fromisoformat(r["release-date"]) <= window_end),
        key=lambda r: int(r["release-version"].split(".")[2]),
    )
    touched = 0
    commits = 0
    previous = f"v{major}.0.0"
    details = []
    for patch in patches:
        tag = f"v{patch['release-version']}"
        try:
            count = len(git(library.mirror, "rev-list", "--no-merges", f"{previous}..{tag}", "--", *library.paths).split())
        except Exception:
            details.append({"tag": tag, "commits": None})
            continue
        commits += count
        touched += count > 0
        details.append({"tag": tag, "date": patch["release-date"], "commits": count})
        previous = tag
    return {
        "library": library.name,
        "major": major,
        "ga_date": ga_date.isoformat(),
        "window_end": window_end.isoformat(),
        "window_complete": window_end <= date.today(),
        "days_observed": min(window_days, (date.today() - ga_date).days),
        "patches_in_window": len(patches),
        "patches_touching_paths": touched,
        "commits_in_window": commits,
        "patches": details,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Performance-labelled PRs, reverts and servicing churn per library")
    parser.add_argument("--library", default="all")
    parser.add_argument("--attribution", default=str(SUMMARY / "attribution.csv"))
    parser.add_argument("--out", default=str(SUMMARY / "attribution-regressions.json"))
    args = parser.parse_args()
    with open(args.attribution, encoding="utf-8") as handle:
        rows = list(csv.DictReader(handle))
    selected = libraries(args.library)
    names = {library.name for library in selected}
    rows = [row for row in rows if row["library"] in names]
    labelled = [row for row in rows if PERF_LABELS & set(filter(None, row["labels"].split(";")))]
    reverts = [row for row in rows if row["subject"].startswith('Revert "')]
    settings = CONFIG["servicing"]
    service = [servicing(library, major, settings["window_days"]) for library in selected for major in (settings["baseline_major"], settings["current_major"])]
    document = {
        "perf_labelled": [{k: row[k] for k in ("library", "sha", "pr_number", "class", "labels", "subject")} for row in labelled],
        "reverts": [{k: row[k] for k in ("library", "sha", "pr_number", "class", "subject")} for row in reverts],
        "servicing": service,
    }
    write_json(Path(args.out), document)
    with open(Path(args.out).with_suffix(".csv"), "w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            [
                "library",
                "major",
                "ga_date",
                "window_end",
                "window_complete",
                "days_observed",
                "patches_in_window",
                "patches_touching_paths",
                "commits_in_window",
            ]
        )
        for entry in service:
            writer.writerow(
                [
                    entry[k]
                    for k in (
                        "library",
                        "major",
                        "ga_date",
                        "window_end",
                        "window_complete",
                        "days_observed",
                        "patches_in_window",
                        "patches_touching_paths",
                        "commits_in_window",
                    )
                ]
            )
    print(f"{len(labelled)} perf-labelled, {len(reverts)} reverts, servicing rows {len(service)} -> {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
