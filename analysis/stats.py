#!/usr/bin/env python3
import argparse
import math
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from scipy import stats

NOISE_PERCENT = 5.0
RESAMPLES = 10_000
TRACK_A_BASELINE = "e0-7"
TRACK_B_BASELINE = "b-net10"
TRACK_A_ORDER = ["e0-6", "e0-7", "e1-8", "e1-9", "e2-10", "e2-11"]
TRACK_B_ORDER = ["b-net10", "b-net11"]
COLUMNS = [
    "scenario",
    "track",
    "scope",
    "label",
    "epoch",
    "baseline",
    "n",
    "median_ms",
    "ci_low_ms",
    "ci_high_ms",
    "baseline_median_ms",
    "change_pct",
    "change_ci_low_pct",
    "change_ci_high_pct",
    "mw_u",
    "mw_p",
    "verdict",
]
EPOCH_BANDS = [("E0", 6, 7), ("E1", 8, 9), ("E2", 10, 11)]

SURFACE = "#fcfcfb"
TEXT_PRIMARY = "#0b0b0b"
TEXT_SECONDARY = "#52514e"
GRID = "#e4e3df"
SERIES = "#2a78d6"
SERIES_BAND = "#b7d3f6"
NOISE_BAND = "#f0efec"
EPOCH_FILLS = ["#f6f5f2", "#ecebe7", "#f6f5f2"]


def bootstrap_median(values: np.ndarray, rng: np.random.Generator) -> np.ndarray:
    picks = rng.integers(0, len(values), size=(RESAMPLES, len(values)))
    return np.median(values[picks], axis=1)


def compare(sample: np.ndarray, baseline: np.ndarray, rng: np.random.Generator) -> dict:
    point_boot = bootstrap_median(sample, rng)
    result = {
        "n": len(sample),
        "median_ms": float(np.median(sample)),
        "ci_low_ms": float(np.percentile(point_boot, 2.5)),
        "ci_high_ms": float(np.percentile(point_boot, 97.5)),
    }
    if baseline.size == 0:
        return result
    base_boot = bootstrap_median(baseline, rng)
    with np.errstate(divide="ignore", invalid="ignore"):
        change_boot = (point_boot / base_boot - 1.0) * 100.0
    base_median = float(np.median(baseline))
    change = (result["median_ms"] / base_median - 1.0) * 100.0 if base_median > 0 else math.nan
    result.update(
        {
            "baseline_median_ms": base_median,
            "change_pct": change,
            "change_ci_low_pct": float(np.nanpercentile(change_boot, 2.5)),
            "change_ci_high_pct": float(np.nanpercentile(change_boot, 97.5)),
        }
    )
    if len(sample) >= 2 and len(baseline) >= 2:
        test = stats.mannwhitneyu(sample, baseline, alternative="two-sided")
        result["mw_u"] = float(test.statistic)
        result["mw_p"] = float(test.pvalue)
    return result


def verdict(row: dict) -> str:
    change = row.get("change_pct")
    if change is None or math.isnan(change):
        return "baseline"
    if abs(change) < NOISE_PERCENT:
        return "noise"
    low, high = row["change_ci_low_pct"], row["change_ci_high_pct"]
    if low <= 0 <= high or row.get("mw_p", 1.0) >= 0.05:
        return "inconclusive"
    return "slower" if change > 0 else "faster"


def analyse(runs: pd.DataFrame, scenario: str, rng: np.random.Generator) -> list[dict]:
    rows = []
    for (track, scope), group in runs.groupby(["track", "scope"], sort=False):
        baseline_label = TRACK_B_BASELINE if track == "B" else TRACK_A_BASELINE
        baseline = group.loc[group["label"] == baseline_label, "median_ms"].to_numpy(dtype=float)
        for label, sample in group.groupby("label", sort=False):
            values = sample["median_ms"].to_numpy(dtype=float)
            result = compare(values, baseline if label != baseline_label else np.array([]), rng)
            result.update(
                {
                    "scenario": scenario,
                    "track": track,
                    "scope": scope,
                    "label": label,
                    "epoch": sample["epoch"].iloc[0],
                    "baseline": baseline_label,
                }
            )
            result["verdict"] = verdict(result) if label != baseline_label else "baseline"
            rows.append(result)
    return rows


def point_x(label: str) -> float:
    if label.startswith("b-net"):
        return float(label.removeprefix("b-net"))
    return float(label.split("-")[1])


def style_axes(axis: plt.Axes) -> None:
    axis.set_facecolor(SURFACE)
    axis.grid(axis="y", color=GRID, linewidth=0.8)
    axis.set_axisbelow(True)
    for side in ("top", "right"):
        axis.spines[side].set_visible(False)
    for side in ("left", "bottom"):
        axis.spines[side].set_color(GRID)
    axis.tick_params(colors=TEXT_SECONDARY, labelsize=8)


def plot(results: pd.DataFrame, scenario: str, out_dir: Path) -> list[Path]:
    data = results.reindex(columns=COLUMNS)
    if data["change_pct"].notna().sum() == 0:
        return []
    data["x"] = data["label"].map(point_x)
    scopes = list(dict.fromkeys(data.sort_values("x")["scope"]))
    if not scopes:
        return []
    columns = min(3, len(scopes))
    rows = math.ceil(len(scopes) / columns)
    figure, axes = plt.subplots(rows, columns, figsize=(4.2 * columns, 2.8 * rows), squeeze=False, facecolor=SURFACE)
    xs_all = sorted(data["x"].unique())
    for index, scope in enumerate(scopes):
        axis = axes[index // columns][index % columns]
        style_axes(axis)
        frame = data[data["scope"] == scope].sort_values("x")
        for (name, start, end), fill in zip(EPOCH_BANDS, EPOCH_FILLS, strict=True):
            if any(start <= x <= end for x in xs_all):
                axis.axvspan(start - 0.5, end + 0.5, color=fill, zorder=0, linewidth=0)
                axis.text((start + end) / 2, 0.02, name, transform=axis.get_xaxis_transform(), ha="center", va="bottom", fontsize=7, color=TEXT_SECONDARY)
        axis.axhspan(-NOISE_PERCENT, NOISE_PERCENT, color=NOISE_BAND, zorder=1, linewidth=0)
        axis.axhline(0, color=TEXT_SECONDARY, linewidth=0.8, zorder=2)
        change = frame["change_pct"].fillna(0.0)
        low = frame["change_ci_low_pct"].fillna(0.0)
        high = frame["change_ci_high_pct"].fillna(0.0)
        axis.fill_between(frame["x"], low, high, color=SERIES_BAND, alpha=0.7, zorder=3, linewidth=0)
        axis.plot(frame["x"], change, color=SERIES, linewidth=2, zorder=4)
        axis.scatter(frame["x"], change, s=36, color=SERIES, edgecolor=SURFACE, linewidth=2, zorder=5)
        axis.set_title(scope, fontsize=8.5, color=TEXT_PRIMARY, loc="left")
        axis.set_xticks(xs_all)
        axis.set_xticklabels([f"{int(x)}" for x in xs_all])
        axis.set_xlim(min(xs_all) - 0.6, max(xs_all) + 0.6)
        axis.yaxis.set_major_formatter(matplotlib.ticker.FuncFormatter(lambda value, _: f"{value:+.0f}%"))
    for index in range(len(scopes), rows * columns):
        axes[index // columns][index % columns].set_visible(False)
    track = data["track"].iloc[0]
    baseline = TRACK_B_BASELINE if track == "B" else TRACK_A_BASELINE
    subtitle = "library + runtime" if track == "B" else "net10.0 runtime fixed, package version varies"
    figure.suptitle(
        f"{scenario}: change in median batch time vs {baseline} ({subtitle}); band = 95% bootstrap CI, grey = ±{NOISE_PERCENT:.0f}% noise",
        fontsize=10,
        color=TEXT_PRIMARY,
        x=0.01,
        ha="left",
    )
    figure.supxlabel("major version" if track == "A" else ".NET runtime", fontsize=9, color=TEXT_SECONDARY)
    figure.tight_layout(rect=(0, 0, 1, 0.96))
    out_dir.mkdir(parents=True, exist_ok=True)
    paths = [out_dir / f"{scenario}.png", out_dir / f"{scenario}.svg"]
    figure.savefig(paths[0], dpi=150, facecolor=SURFACE)
    figure.savefig(paths[1], facecolor=SURFACE)
    plt.close(figure)
    return paths


def write_markdown(results: pd.DataFrame, path: Path) -> None:
    lines = [
        "# Statistics",
        "",
        (
            f"Unit of observation: one process (its median batch time). 95% bootstrap CI of the median with {RESAMPLES:,} resamples; "
            f"two-sided Mann-Whitney U against the baseline. |change| < {NOISE_PERCENT:.0f}% is reported as `noise` regardless of p; "
            "`inconclusive` means the CI of the change crosses 0 or p >= 0.05."
        ),
        "",
        "| scenario | scope | point | n | median ms [95% CI] | change vs baseline [95% CI] | MW p | verdict |",
        "|---|---|---|---:|---|---|---:|---|",
    ]
    for row in results.itertuples(index=False):
        change = "-" if pd.isna(row.change_pct) else f"{row.change_pct:+.1f}% [{row.change_ci_low_pct:+.1f}, {row.change_ci_high_pct:+.1f}]"
        p_value = "-" if pd.isna(row.mw_p) else f"{row.mw_p:.3g}"
        median = f"{row.median_ms:.4f} [{row.ci_low_ms:.4f}, {row.ci_high_ms:.4f}]"
        lines.append(f"| {row.scenario} | `{row.scope}` | {row.label} | {row.n} | {median} | {change} | {p_value} | {row.verdict} |")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Bootstrap CIs, Mann-Whitney U and epoch plots for the matrix summary")
    parser.add_argument("--summary", type=Path, default=Path("results/summary"))
    parser.add_argument("--seed", type=int, default=20250519)
    args = parser.parse_args()
    rng = np.random.default_rng(args.seed)
    all_rows: list[dict] = []
    plots: list[Path] = []
    for runs_file in sorted(args.summary.glob("*-runs.csv")):
        scenario = runs_file.name.removesuffix("-runs.csv")
        runs = pd.read_csv(runs_file)
        if runs.empty:
            continue
        order = {label: i for i, label in enumerate(TRACK_A_ORDER + TRACK_B_ORDER)}
        runs = runs.sort_values(by="label", key=lambda column: column.map(order).fillna(len(order)))
        rows = analyse(runs, scenario, rng)
        all_rows.extend(rows)
        plots.extend(plot(pd.DataFrame(rows), scenario, args.summary / "plots"))
    if not all_rows:
        print(f"no *-runs.csv files under {args.summary}")
        return 1
    results = pd.DataFrame(all_rows).reindex(columns=COLUMNS)
    results.to_csv(args.summary / "stats.csv", index=False)
    write_markdown(results, args.summary / "stats.md")
    print(f"stats: {len(results)} rows -> {args.summary / 'stats.csv'}; {len(plots)} plot files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
