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
MULTIMODAL_QUARTILE_RATIO = 1.5
SIGNIFICANCE = 0.05
RESAMPLES = 10_000
TRACK_A_BASELINE = "e0-7"
TRACK_B_BASELINE = "b-net10"
TRACK_A_ORDER = ["e0-6", "e0-7", "e1-8", "e1-9", "e2-10", "e2-11"]
TRACK_B_ORDER = ["b-net10", "b-net11"]
SINGLE_ROUND = "single"
VERDICT_ORDER = ["faster", "slower", "mixed", "noise", "inconclusive"]
COLUMNS = [
    "scenario",
    "track",
    "scope",
    "label",
    "epoch",
    "baseline",
    "n",
    "rounds",
    "verdict",
    "primary_metric",
    "multimodal",
    "quartile_ratio",
    "median_verdict",
    "mean_verdict",
    "prespecified_verdict",
    "rounds_agree",
    "round_change_min_pct",
    "round_change_max_pct",
    "median_ms",
    "ci_low_ms",
    "ci_high_ms",
    "baseline_median_ms",
    "change_pct",
    "change_ci_low_pct",
    "change_ci_high_pct",
    "mw_p",
    "mw_p_adj",
    "mean_ms",
    "mean_ci_low_ms",
    "mean_ci_high_ms",
    "baseline_mean_ms",
    "mean_change_pct",
    "mean_change_ci_low_pct",
    "mean_change_ci_high_pct",
    "mean_mw_p",
    "mean_mw_p_adj",
]
EPOCH_BANDS = [("E0", 6, 7), ("E1", 8, 9), ("E2", 10, 11)]

SURFACE = "#fcfcfb"
TEXT_PRIMARY = "#0b0b0b"
TEXT_SECONDARY = "#52514e"
GRID = "#e4e3df"
SERIES = "#2a78d6"
MEAN_SERIES = "#eb6834"
SERIES_BAND = "#b7d3f6"
NOISE_BAND = "#f0efec"
EPOCH_FILLS = ["#f6f5f2", "#ecebe7", "#f6f5f2"]


def bootstrap_median(values: np.ndarray, rng: np.random.Generator) -> np.ndarray:
    picks = rng.integers(0, len(values), size=(RESAMPLES, len(values)))
    return np.median(values[picks], axis=1)


def across_rounds(per_round: np.ndarray, rng: np.random.Generator) -> np.ndarray:
    picks = rng.integers(0, per_round.shape[0], size=(RESAMPLES, per_round.shape[0]))
    return per_round[picks, np.arange(RESAMPLES)[:, None]].mean(axis=1)


def by_round(frame: pd.DataFrame, column: str, rounds: list[str]) -> list[np.ndarray]:
    return [frame.loc[frame["round"] == round_id, column].to_numpy(dtype=float) for round_id in rounds]


def stratified_mann_whitney(samples: list[np.ndarray], baselines: list[np.ndarray]) -> float:
    weights = [math.sqrt(len(s) * len(b) / (len(s) + len(b))) for s, b in zip(samples, baselines, strict=True)]
    one_sided = []
    for alternative in ("greater", "less"):
        pvalues = [stats.mannwhitneyu(s, b, alternative=alternative).pvalue for s, b in zip(samples, baselines, strict=True)]
        clipped = np.clip(pvalues, 1e-300, 1 - 1e-16)
        one_sided.append(stats.combine_pvalues(clipped, method="stouffer", weights=weights).pvalue)
    return min(1.0, 2 * min(one_sided))


def describe(frame: pd.DataFrame, column: str, rounds: list[str], rng: np.random.Generator) -> dict:
    log_boots = np.log([bootstrap_median(values, rng) for values in by_round(frame, column, rounds)])
    log_medians = np.log([np.median(values) for values in by_round(frame, column, rounds)])
    interval = np.exp(np.percentile(across_rounds(log_boots, rng), [2.5, 97.5]))
    return {"value": float(np.exp(log_medians.mean())), "low": float(interval[0]), "high": float(interval[1])}


def compare_metric(sample: pd.DataFrame, baseline: pd.DataFrame, column: str, rounds: list[str], rng: np.random.Generator) -> dict:
    samples = by_round(sample, column, rounds)
    baselines = by_round(baseline, column, rounds)
    log_ratios = np.log([np.median(s) / np.median(b) for s, b in zip(samples, baselines, strict=True)])
    boots = np.log([bootstrap_median(s, rng) / bootstrap_median(b, rng) for s, b in zip(samples, baselines, strict=True)])
    interval = (np.exp(np.percentile(across_rounds(boots, rng), [2.5, 97.5])) - 1) * 100
    return {
        "change": float((np.exp(log_ratios.mean()) - 1) * 100),
        "low": float(interval[0]),
        "high": float(interval[1]),
        "round_changes": (np.exp(log_ratios) - 1) * 100,
        "p": stratified_mann_whitney(samples, baselines),
    }


def metric_verdict(change: float, low: float, high: float, p_value: float, round_changes: np.ndarray) -> str:
    if low > -NOISE_PERCENT and high < NOISE_PERCENT:
        return "noise"
    same_direction = bool((round_changes > 0).all() or (round_changes < 0).all())
    clears_band = low >= NOISE_PERCENT or high <= -NOISE_PERCENT
    if same_direction and clears_band and p_value < SIGNIFICANCE:
        return "slower" if change > 0 else "faster"
    return "inconclusive"


def overall_verdict(median_verdict: str, mean_verdict: str, mean_change: float, multimodal: bool) -> str:
    if multimodal:
        return mean_verdict
    opposes_slower = median_verdict == "slower" and mean_change <= -NOISE_PERCENT
    opposes_faster = median_verdict == "faster" and mean_change >= NOISE_PERCENT
    return "mixed" if opposes_slower or opposes_faster else median_verdict


def prespecified_verdict(sample: np.ndarray, baseline: np.ndarray, rng: np.random.Generator) -> str:
    change = (np.median(sample) / np.median(baseline) - 1) * 100
    if abs(change) < NOISE_PERCENT:
        return "noise"
    low, high = np.percentile((bootstrap_median(sample, rng) / bootstrap_median(baseline, rng) - 1) * 100, [2.5, 97.5])
    if low <= 0 <= high or stats.mannwhitneyu(sample, baseline, alternative="two-sided").pvalue >= SIGNIFICANCE:
        return "inconclusive"
    return "slower" if change > 0 else "faster"


def quartile_ratio(frame: pd.DataFrame) -> float:
    return float((frame["p75_ms"] / frame["p25_ms"]).median())


def analyse(runs: pd.DataFrame, scenario: str, rng: np.random.Generator) -> list[dict]:
    rows = []
    for (track, scope), group in runs.groupby(["track", "scope"], sort=False):
        baseline_label = TRACK_B_BASELINE if track == "B" else TRACK_A_BASELINE
        baseline = group[group["label"] == baseline_label]
        for label, sample in group.groupby("label", sort=False):
            is_baseline = label == baseline_label
            rounds = sorted(set(sample["round"]) & set(baseline["round"])) if not is_baseline and not baseline.empty else sorted(set(sample["round"]))
            median = describe(sample, "median_ms", rounds, rng)
            mean = describe(sample, "mean_ms", rounds, rng)
            row = {
                "scenario": scenario,
                "track": track,
                "scope": scope,
                "label": label,
                "epoch": sample["epoch"].iloc[0],
                "baseline": baseline_label,
                "n": len(sample),
                "rounds": len(rounds),
                "quartile_ratio": quartile_ratio(sample),
                "median_ms": median["value"],
                "ci_low_ms": median["low"],
                "ci_high_ms": median["high"],
                "mean_ms": mean["value"],
                "mean_ci_low_ms": mean["low"],
                "mean_ci_high_ms": mean["high"],
            }
            if not is_baseline and not baseline.empty:
                median_change = compare_metric(sample, baseline, "median_ms", rounds, rng)
                mean_change = compare_metric(sample, baseline, "mean_ms", rounds, rng)
                row.update(
                    {
                        "baseline_median_ms": describe(baseline, "median_ms", rounds, rng)["value"],
                        "baseline_mean_ms": describe(baseline, "mean_ms", rounds, rng)["value"],
                        "change_pct": median_change["change"],
                        "change_ci_low_pct": median_change["low"],
                        "change_ci_high_pct": median_change["high"],
                        "mw_p": median_change["p"],
                        "mean_change_pct": mean_change["change"],
                        "mean_change_ci_low_pct": mean_change["low"],
                        "mean_change_ci_high_pct": mean_change["high"],
                        "mean_mw_p": mean_change["p"],
                        "multimodal": max(row["quartile_ratio"], quartile_ratio(baseline)) >= MULTIMODAL_QUARTILE_RATIO,
                        "round_changes": median_change["round_changes"],
                        "mean_round_changes": mean_change["round_changes"],
                        "prespecified_verdict": prespecified_verdict(sample["median_ms"].to_numpy(float), baseline["median_ms"].to_numpy(float), rng),
                    }
                )
            rows.append(row)
    return rows


def add_verdicts(results: pd.DataFrame) -> pd.DataFrame:
    compared = results["change_pct"].notna()
    results["mw_p_adj"] = np.nan
    results["mean_mw_p_adj"] = np.nan
    results.loc[compared, "mw_p_adj"] = stats.false_discovery_control(results.loc[compared, "mw_p"], method="bh")
    results.loc[compared, "mean_mw_p_adj"] = stats.false_discovery_control(results.loc[compared, "mean_mw_p"], method="bh")
    for column in ("verdict", "primary_metric", "median_verdict", "mean_verdict"):
        results[column] = "baseline"
    results["rounds_agree"] = np.nan
    results["round_change_min_pct"] = np.nan
    results["round_change_max_pct"] = np.nan
    for index, row in results[compared].iterrows():
        median_verdict = metric_verdict(row["change_pct"], row["change_ci_low_pct"], row["change_ci_high_pct"], row["mw_p_adj"], row["round_changes"])
        mean_verdict = metric_verdict(
            row["mean_change_pct"], row["mean_change_ci_low_pct"], row["mean_change_ci_high_pct"], row["mean_mw_p_adj"], row["mean_round_changes"]
        )
        primary_is_mean = bool(row["multimodal"])
        primary_changes = row["mean_round_changes"] if primary_is_mean else row["round_changes"]
        primary_change = row["mean_change_pct"] if primary_is_mean else row["change_pct"]
        direction = 1 if primary_change > 0 else -1
        results.loc[index, "median_verdict"] = median_verdict
        results.loc[index, "mean_verdict"] = mean_verdict
        results.loc[index, "verdict"] = overall_verdict(median_verdict, mean_verdict, row["mean_change_pct"], primary_is_mean)
        results.loc[index, "primary_metric"] = "mean" if primary_is_mean else "median"
        results.loc[index, "rounds_agree"] = int((direction * primary_changes >= NOISE_PERCENT).sum())
        results.loc[index, "round_change_min_pct"] = float(primary_changes.min())
        results.loc[index, "round_change_max_pct"] = float(primary_changes.max())
    return results


def summarise(runs_by_scenario: dict[str, pd.DataFrame], rng: np.random.Generator) -> pd.DataFrame:
    rows = []
    for scenario, runs in runs_by_scenario.items():
        filled = runs.assign(round=runs["round"].astype("string").fillna(SINGLE_ROUND).astype(str))
        rows.extend(analyse(filled, scenario, rng))
    frame = pd.DataFrame(rows)
    for column in ("change_pct", "mw_p", "mean_mw_p", "multimodal", "prespecified_verdict"):
        if column not in frame:
            frame[column] = np.nan
    return add_verdicts(frame)


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
        mean_change = frame["mean_change_pct"].fillna(0.0)
        change = frame["change_pct"].fillna(0.0)
        low = frame["change_ci_low_pct"].fillna(0.0)
        high = frame["change_ci_high_pct"].fillna(0.0)
        axis.fill_between(frame["x"], low, high, color=SERIES_BAND, alpha=0.7, zorder=3, linewidth=0)
        axis.plot(frame["x"], mean_change, color=MEAN_SERIES, linewidth=2, linestyle=(0, (3, 2)), zorder=4, label="mean")
        axis.plot(frame["x"], change, color=SERIES, linewidth=2, zorder=4, label="median")
        axis.scatter(frame["x"], change, s=36, color=SERIES, edgecolor=SURFACE, linewidth=2, zorder=5)
        judged_by_mean = frame["primary_metric"].eq("mean").any()
        axis.set_title(f"{scope} (judged by mean)" if judged_by_mean else scope, fontsize=8.5, color=TEXT_PRIMARY, loc="left")
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
        f"{scenario}: change in batch time vs {baseline} ({subtitle}); band = 95% CI of the median, grey = ±{NOISE_PERCENT:.0f}% noise",
        fontsize=10,
        color=TEXT_PRIMARY,
        x=0.01,
        ha="left",
    )
    figure.supxlabel("major version" if track == "A" else ".NET runtime", fontsize=9, color=TEXT_SECONDARY)
    handles, labels = axes[0][0].get_legend_handles_labels()
    figure.legend(handles, labels, loc="upper right", ncols=2, frameon=False, fontsize=8, labelcolor=TEXT_SECONDARY)
    figure.tight_layout(rect=(0, 0, 1, 0.96))
    out_dir.mkdir(parents=True, exist_ok=True)
    paths = [out_dir / f"{scenario}.png", out_dir / f"{scenario}.svg"]
    figure.savefig(paths[0], dpi=150, facecolor=SURFACE)
    figure.savefig(paths[1], facecolor=SURFACE)
    plt.close(figure)
    return paths


def write_markdown(results: pd.DataFrame, path: Path) -> None:
    compared = results[results["verdict"] != "baseline"]
    tally = pd.crosstab(compared["label"], compared["verdict"]).reindex(columns=VERDICT_ORDER, fill_value=0)
    lines = [
        "# Statistics",
        "",
        (
            "Unit of observation: one process. Each scope has two metrics per process: the exact median and the mean of its batch durations. "
            "Each round counts equally: changes are computed inside a round, then combined as a geometric mean. "
            f"The 95% CI resamples rounds and then processes ({RESAMPLES:,} resamples); it is only as wide as the rounds available. "
            "p is a Mann-Whitney U per round, combined with Stouffer's method and adjusted with Benjamini-Hochberg across all comparisons."
        ),
        "",
        (
            f"Metric verdict: `noise` if the CI lies inside ±{NOISE_PERCENT:.0f}%; `faster`/`slower` if the CI lies wholly beyond ±{NOISE_PERCENT:.0f}%, "
            f"no round points the other way and adjusted p < {SIGNIFICANCE}; otherwise `inconclusive`. "
            f"Overall verdict: scopes with p75/p25 >= {MULTIMODAL_QUARTILE_RATIO} at the point or the baseline are multimodal and judged by the mean; "
            f"otherwise the median decides, and `mixed` means the mean's estimate moved at least {NOISE_PERCENT:.0f}% the other way. "
            "`pre-spec` is the rule fixed before the data was seen (see `docs/methodology.md`, Deviations)."
        ),
        "",
        "## Verdicts per point",
        "",
        "| point | " + " | ".join(VERDICT_ORDER) + " |",
        "|---|" + "---:|" * len(VERDICT_ORDER),
    ]
    for label, row in tally.iterrows():
        lines.append(f"| {label} | " + " | ".join(str(row[verdict]) for verdict in VERDICT_ORDER) + " |")
    lines += ["", "## Mean moved, median did not", "", "| scope | point | median change | mean change |", "|---|---|---:|---:|"]
    median_is_flat = (compared["primary_metric"] == "median") & (compared["verdict"] == "noise")
    mean_only = compared[median_is_flat & compared["mean_verdict"].isin(["faster", "slower"])]
    for row in mean_only.itertuples(index=False):
        lines.append(f"| `{row.scope}` | {row.label} | {row.change_pct:+.1f}% | {row.mean_change_pct:+.1f}% |")
    lines += [
        "",
        "## All scopes",
        "",
        (
            "| scenario | scope | point | n (rounds) | median ms [95% CI] | median change [95% CI] | mean ms | mean change [95% CI] "
            "| p75/p25 | rounds agreeing | verdict | pre-spec |"
        ),
        "|---|---|---|---:|---|---|---:|---|---:|---:|---|---|",
    ]
    for row in results.itertuples(index=False):
        median = f"{row.median_ms:.4f} [{row.ci_low_ms:.4f}, {row.ci_high_ms:.4f}]"
        n_rounds = f"{row.n} ({row.rounds})"
        if row.verdict == "baseline":
            lines.append(
                f"| {row.scenario} | `{row.scope}` | {row.label} | {n_rounds} | {median} | - | {row.mean_ms:.4f} | - | "
                f"{row.quartile_ratio:.2f} | - | baseline | - |"
            )
            continue
        median_change = f"{row.change_pct:+.1f}% [{row.change_ci_low_pct:+.1f}, {row.change_ci_high_pct:+.1f}]"
        mean_change = f"{row.mean_change_pct:+.1f}% [{row.mean_change_ci_low_pct:+.1f}, {row.mean_change_ci_high_pct:+.1f}]"
        agreeing = f"{int(row.rounds_agree)}/{row.rounds}"
        judged = f"{row.verdict} (by mean)" if row.primary_metric == "mean" else row.verdict
        lines.append(
            f"| {row.scenario} | `{row.scope}` | {row.label} | {n_rounds} | {median} | {median_change} | {row.mean_ms:.4f} | {mean_change} | "
            f"{row.quartile_ratio:.2f} | {agreeing} | {judged} | {row.prespecified_verdict} |"
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Round-aware CIs, stratified Mann-Whitney U, verdicts and epoch plots for the matrix summary")
    parser.add_argument("--summary", type=Path, default=Path("results/summary"))
    parser.add_argument("--seed", type=int, default=20250519)
    args = parser.parse_args()
    rng = np.random.default_rng(args.seed)
    order = {label: i for i, label in enumerate(TRACK_A_ORDER + TRACK_B_ORDER)}
    runs_by_scenario = {}
    for runs_file in sorted(args.summary.glob("*-runs.csv")):
        runs = pd.read_csv(runs_file, dtype={"round": "string"})
        if not runs.empty:
            runs_by_scenario[runs_file.name.removesuffix("-runs.csv")] = runs.sort_values(by="label", key=lambda column: column.map(order).fillna(len(order)))
    if not runs_by_scenario:
        print(f"no *-runs.csv files under {args.summary}")
        return 1
    results = summarise(runs_by_scenario, rng).reindex(columns=COLUMNS)
    plots = [path for scenario, frame in results.groupby("scenario", sort=False) for path in plot(frame, scenario, args.summary / "plots")]
    results.to_csv(args.summary / "stats.csv", index=False)
    write_markdown(results, args.summary / "stats.md")
    print(f"stats: {len(results)} rows -> {args.summary / 'stats.csv'}; {len(plots)} plot files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
