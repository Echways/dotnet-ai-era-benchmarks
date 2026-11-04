#!/usr/bin/env python3
import argparse
import math
from pathlib import Path

import numpy as np
import pandas as pd
import stats

POINT_LABELS = {6: "e0-6", 7: "e0-7", 8: "e1-8", 9: "e1-9", 10: "e2-10", 11: "e2-11"}
EPOCHS = {label: epoch for epoch, low, high in stats.EPOCH_BANDS for major, label in POINT_LABELS.items() if low <= major <= high}
OUTPUT_COLUMNS = [
    "scenario",
    "method",
    "label",
    "epoch",
    "n",
    "rounds",
    "calls_median",
    "calls_change_pct",
    "value_ms",
    "ci_low_ms",
    "ci_high_ms",
    "baseline_value_ms",
    "change_pct",
    "change_ci_low_pct",
    "change_ci_high_pct",
    "mw_p",
    "mw_p_adj",
    "verdict",
    "below_resolution",
    "rounds_agree",
]


def label_of(tag: str) -> str:
    return POINT_LABELS[int(tag.lstrip("v").split(".")[0])]


def read_rounds(paths: list[Path]) -> pd.DataFrame:
    frames = []
    for index, path in enumerate(paths):
        frame = pd.read_csv(path)
        frame["round"] = f"r{index}" if len(paths) > 1 else np.nan
        frames.append(frame)
    return pd.concat(frames, ignore_index=True)


def as_runs(methods: pd.DataFrame, metric: str) -> pd.DataFrame:
    column = "self_ms" if metric == "self" else "inclusive_ms"
    frame = methods[methods["calls"] > 0].copy()
    if "round" not in frame:
        frame["round"] = np.nan
    value = frame[column] / frame["calls"]
    frame["value"] = value
    runs = pd.DataFrame(
        {
            "label": frame["tag"].map(label_of),
            "track": "A",
            "round": frame["round"],
            "scope": frame["method"],
            "pid": frame["pid"],
            "median_ms": value,
            "mean_ms": value,
            "p25_ms": value,
            "p75_ms": value,
            "calls": frame["calls"],
        }
    )
    runs["epoch"] = runs["label"].map(EPOCHS)
    return runs


def calls_by_point(runs: pd.DataFrame) -> dict[tuple[str, str], float]:
    return {key: float(value) for key, value in runs.groupby(["scope", "label"])["calls"].median().items()}


def analyse(methods: pd.DataFrame, metric: str, scope_cost_ms: float, seed: int, workers: int) -> pd.DataFrame:
    parts = []
    for scenario, frame in methods.groupby("scenario", sort=False):
        runs = as_runs(frame, metric)
        if runs.empty:
            continue
        results = stats.summarise({scenario: runs}, np.random.default_rng(seed), workers).reindex(columns=stats.COLUMNS)
        calls = calls_by_point(runs)
        results["method"] = results["scope"]
        results["calls_median"] = [calls.get((method, label), math.nan) for method, label in zip(results["method"], results["label"], strict=True)]
        baseline_calls = [calls.get((method, stats.TRACK_A_BASELINE), math.nan) for method in results["method"]]
        results["calls_change_pct"] = (results["calls_median"] / pd.Series(baseline_calls, index=results.index) - 1) * 100
        results["value_ms"] = results["median_ms"]
        results["ci_low_ms"] = results["ci_low_ms"]
        results["baseline_value_ms"] = results["baseline_median_ms"]
        results["below_resolution"] = results["value_ms"] < scope_cost_ms
        parts.append(results)
    if not parts:
        return pd.DataFrame(columns=OUTPUT_COLUMNS)
    combined = pd.concat(parts, ignore_index=True)
    combined.loc[combined["below_resolution"] & (combined["verdict"] != "baseline"), "verdict"] = "below resolution"
    return combined.reindex(columns=OUTPUT_COLUMNS)


def write_markdown(results: pd.DataFrame, path: Path, metric: str, scope_cost_ms: float) -> None:
    compared = results[~results["verdict"].isin(["baseline"])]
    tally = pd.crosstab(compared["label"], compared["verdict"])
    lines = [
        "# Per-method glass-box statistics",
        "",
        (
            f"Metric: {metric} time per call per process. "
            "Verdict rules, noise band, bootstrap and Benjamini-Hochberg correction are those of `stats.py`. "
            f"Methods whose time per call is below the measured cost of one scope ({scope_cost_ms:g} ms) are reported as `below resolution`. "
            "`calls_change_pct` compares the median number of calls per process with the baseline: "
            "a call count that moved means the code path changed, not only its speed."
        ),
        "",
        "## Verdicts per point",
        "",
        "| point | " + " | ".join(tally.columns) + " |",
        "|---|" + "---:|" * len(tally.columns),
    ]
    lines += [f"| {label} | " + " | ".join(str(int(count)) for count in row) + " |" for label, row in tally.iterrows()]
    movers = compared[compared["verdict"].isin(["faster", "slower", "mixed"])].sort_values("change_pct", key=lambda column: column.abs(), ascending=False)
    lines += [
        "",
        "## Methods that moved",
        "",
        "| scenario | method | point | change | 95% CI | adjusted p | calls change |",
        "|---|---|---|---:|---|---:|---:|",
    ]
    for row in movers.itertuples(index=False):
        lines.append(
            f"| {row.scenario} | `{row.method}` | {row.label} | {row.change_pct:+.1f}% | [{row.change_ci_low_pct:+.1f}, {row.change_ci_high_pct:+.1f}] | "
            f"{row.mw_p_adj:.3g} | {row.calls_change_pct:+.1f}% |"
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Hierarchical bootstrap and verdicts over per-method glass-box times")
    parser.add_argument("--methods", type=Path, nargs="+", required=True, help="methods.csv files; several files are treated as rounds")
    parser.add_argument("--out", type=Path, default=Path("results/glassbox"))
    parser.add_argument("--metric", choices=["self", "inclusive"], default="self")
    parser.add_argument("--scope-cost-ms", type=float, default=0.0)
    parser.add_argument("--seed", type=int, default=20250519)
    parser.add_argument("--workers", type=int, default=1)
    args = parser.parse_args()
    results = analyse(read_rounds(args.methods), args.metric, args.scope_cost_ms, args.seed, args.workers)
    args.out.mkdir(parents=True, exist_ok=True)
    results.to_csv(args.out / f"methods-stats-{args.metric}.csv", index=False)
    write_markdown(results, args.out / f"methods-stats-{args.metric}.md", args.metric, args.scope_cost_ms)
    print(f"{len(results)} rows -> {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
