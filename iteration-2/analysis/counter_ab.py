#!/usr/bin/env python3
import argparse
from pathlib import Path

import numpy as np
import pandas as pd
import stats
from scipy import stats as scipy_stats

REFERENCE_INTERVAL = 0
CURRENT_INTERVAL = 20
CANDIDATE_INTERVAL = 1000


def read_intervals(root: Path) -> dict[int, pd.DataFrame]:
    intervals = {}
    for directory in sorted(root.iterdir(), key=lambda path: int(path.name) if path.name.isdigit() else -1):
        if not directory.name.isdigit():
            continue
        frames = []
        for runs_file in sorted((directory / "summary").glob("*-runs.csv")):
            frame = pd.read_csv(runs_file, dtype={"round": "string"})
            frame["scenario"] = runs_file.name.removesuffix("-runs.csv")
            frames.append(frame)
        intervals[int(directory.name)] = pd.concat(frames, ignore_index=True)
    return intervals


def compare(reference: pd.DataFrame, candidate: pd.DataFrame, interval: int, reference_ms: int, rng: np.random.Generator) -> pd.DataFrame:
    rows = []
    keys = ["scenario", "label", "scope"]
    candidate_groups = {key: group for key, group in candidate.groupby(keys, sort=False)}
    for key, base in reference.groupby(keys, sort=False):
        group = candidate_groups.get(key)
        if group is None or len(base) < 2 or len(group) < 2:
            continue
        sample = group["median_ms"].to_numpy(float)
        baseline = base["median_ms"].to_numpy(float)
        change = stats.compare_metric([sample], [baseline], rng)
        alloc_change, alloc_verdict = (
            stats.allocation_verdict(group["alloc_bytes"].to_numpy(float), base["alloc_bytes"].to_numpy(float)) if "alloc_bytes" in group else (np.nan, "n/a")
        )
        rows.append(
            {
                "interval_ms": interval,
                "reference_ms": reference_ms,
                "scenario": key[0],
                "label": key[1],
                "scope": key[2],
                "n": len(sample),
                "baseline_median_ms": float(np.median(baseline)),
                "median_ms": float(np.median(sample)),
                "change_pct": change["change"],
                "ci_low_pct": change["low"],
                "ci_high_pct": change["high"],
                "p": change["p"],
                "alloc_change_pct": alloc_change,
                "alloc_verdict": alloc_verdict,
            }
        )
    frame = pd.DataFrame(rows)
    frame["p_adj"] = scipy_stats.false_discovery_control(frame["p"], method="bh")
    frame["verdict"] = [
        stats.metric_verdict(row.change_pct, row.ci_low_pct, row.ci_high_pct, row.p_adj, np.array([row.change_pct])) for row in frame.itertuples(index=False)
    ]
    return frame


def write_markdown(results: pd.DataFrame, path: Path) -> None:
    lines = [
        "# Counter interval A/B",
        "",
        (
            f"Every interval is compared with interval {REFERENCE_INTERVAL} ms (session counters off), "
            f"and the candidate default of {CANDIDATE_INTERVAL} ms with the current default of {CURRENT_INTERVAL} ms, on the same points and scopes. "
            "Per scope: median change of per-process medians, bootstrap interval, "
            "Mann-Whitney p with Benjamini-Hochberg correction across the scopes of one interval, "
            f"verdict by the rules of `stats.py` (noise band ±{stats.NOISE_PERCENT:g}%). "
            "Allocation verdicts compare bytes per batch exactly."
        ),
        "",
        "| interval | reference | scopes | noise | faster | slower | inconclusive | allocations same | allocations differ |",
        "|---:|---:|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for (interval, reference), group in results.groupby(["interval_ms", "reference_ms"]):
        counts = group["verdict"].value_counts()
        differ = int(group["alloc_verdict"].isin(["more", "less", "inconclusive"]).sum())
        lines.append(
            f"| {interval} | {reference} | {len(group)} | {counts.get('noise', 0)} | {counts.get('faster', 0)} | {counts.get('slower', 0)} | "
            f"{counts.get('inconclusive', 0)} | {int((group['alloc_verdict'] == 'same').sum())} | {differ} |"
        )
    decisive = results[results["verdict"].isin(["faster", "slower"])]
    per_cell = int(results["n"].min())
    if decisive.empty:
        lines += [
            "",
            "## Conclusion",
            "",
            (
                "No scope at any interval has a `faster` or `slower` verdict against its reference, "
                "so the data do not show that the counter listener changes measured batch times beyond the noise band. "
                f"Each cell has only {per_cell} processes, so the `inconclusive` rows are lack of power, not evidence of equality. "
                f"The default stays at {CURRENT_INTERVAL} ms, which keeps iteration 2 comparable with iteration 1; "
                "a longer interval is not shown to be better."
            ),
        ]
    else:
        lines += [
            "",
            "## Conclusion",
            "",
            (
                f"{len(decisive)} comparisons have a `faster` or `slower` verdict, "
                "so the counter listener does change measured batch times at some interval. "
                "Choose the interval with the fewest such scopes and state it in the report."
            ),
        ]
    moved = results[results["verdict"] != "noise"].sort_values("change_pct", key=lambda column: column.abs(), ascending=False)
    lines += [
        "",
        "## Scopes outside the noise band",
        "",
        "| interval | reference | scenario | point | scope | change | 95% CI | adjusted p | verdict |",
        "|---:|---:|---|---|---|---:|---|---:|---|",
    ]
    for row in moved.itertuples(index=False):
        interval = f"[{row.ci_low_pct:+.1f}, {row.ci_high_pct:+.1f}]"
        lines.append(
            f"| {row.interval_ms} | {row.reference_ms} | {row.scenario} | {row.label} | `{row.scope}` | "
            f"{row.change_pct:+.1f}% | {interval} | {row.p_adj:.3g} | {row.verdict} |"
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Compare measured batches at several counter sampling intervals against counters off")
    parser.add_argument("--root", type=Path, default=Path("results/ab-counter"))
    parser.add_argument("--out", type=Path, default=Path("docs"))
    parser.add_argument("--seed", type=int, default=20250519)
    args = parser.parse_args()
    intervals = read_intervals(args.root)
    rng = np.random.default_rng(args.seed)
    pairs = [(interval, REFERENCE_INTERVAL) for interval in intervals if interval != REFERENCE_INTERVAL] + [(CANDIDATE_INTERVAL, CURRENT_INTERVAL)]
    results = pd.concat([compare(intervals[reference], intervals[interval], interval, reference, rng) for interval, reference in pairs], ignore_index=True)
    args.out.mkdir(parents=True, exist_ok=True)
    results.to_csv(args.out / "counter-ab.csv", index=False)
    write_markdown(results, args.out / "counter-ab.md")
    print(f"{len(results)} comparisons -> {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
