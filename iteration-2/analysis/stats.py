#!/usr/bin/env python3
import argparse
import math
import os
from concurrent.futures import ProcessPoolExecutor
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
ALLOCATION_PERCENT = 1.0
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
    "alloc_change_pct",
    "alloc_verdict",
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


def row_medians(rows: np.ndarray) -> np.ndarray:
    ordered = np.sort(rows, axis=1)
    middle = ordered.shape[1] // 2
    if ordered.shape[1] % 2:
        return ordered[:, middle]
    return (ordered[:, middle - 1] + ordered[:, middle]) / 2


def slot_medians(values_by_round: list[np.ndarray], picks: np.ndarray, rng: np.random.Generator) -> np.ndarray:
    medians = np.empty(picks.shape)
    for index, values in enumerate(values_by_round):
        slots = picks == index
        count = int(slots.sum())
        if count:
            draws = rng.integers(0, len(values), size=(count, len(values)))
            medians[slots] = row_medians(values[draws])
    return medians


def draw_rounds(round_count: int, rng: np.random.Generator) -> np.ndarray:
    return rng.integers(0, round_count, size=(RESAMPLES, round_count))


def stratified_mann_whitney(samples: list[np.ndarray], baselines: list[np.ndarray]) -> float:
    weights = [math.sqrt(len(s) * len(b) / (len(s) + len(b))) for s, b in zip(samples, baselines, strict=True)]
    one_sided = []
    for alternative in ("greater", "less"):
        pvalues = [stats.mannwhitneyu(s, b, alternative=alternative).pvalue for s, b in zip(samples, baselines, strict=True)]
        clipped = np.clip(pvalues, 1e-300, 1 - 1e-16)
        one_sided.append(stats.combine_pvalues(clipped, method="stouffer", weights=weights).pvalue)
    return min(1.0, 2 * min(one_sided))


def describe(values_by_round: list[np.ndarray], rng: np.random.Generator) -> dict:
    log_slots = np.log(slot_medians(values_by_round, draw_rounds(len(values_by_round), rng), rng))
    interval = np.exp(np.percentile(log_slots.mean(axis=1), [2.5, 97.5]))
    value = np.exp(np.mean([np.log(np.median(values)) for values in values_by_round]))
    return {"value": float(value), "low": float(interval[0]), "high": float(interval[1])}


def compare_metric(samples: list[np.ndarray], baselines: list[np.ndarray], rng: np.random.Generator) -> dict:
    log_ratios = np.log([np.median(s) / np.median(b) for s, b in zip(samples, baselines, strict=True)])
    picks = draw_rounds(len(samples), rng)
    log_slots = np.log(slot_medians(samples, picks, rng)) - np.log(slot_medians(baselines, picks, rng))
    interval = (np.exp(np.percentile(log_slots.mean(axis=1), [2.5, 97.5])) - 1) * 100
    return {
        "change": float((np.exp(log_ratios.mean()) - 1) * 100),
        "low": float(interval[0]),
        "high": float(interval[1]),
        "round_changes": (np.exp(log_ratios) - 1) * 100,
        "p": stratified_mann_whitney(samples, baselines),
    }


def allocation_verdict(sample: np.ndarray, baseline: np.ndarray) -> tuple[float, str]:
    sample = sample[~np.isnan(sample)]
    baseline = baseline[~np.isnan(baseline)]
    if sample.size == 0 or baseline.size == 0:
        return math.nan, "n/a"
    sample_median = float(np.median(sample))
    baseline_median = float(np.median(baseline))
    if baseline_median == 0:
        return math.nan, "same" if sample_median == 0 else "more"
    change = (sample_median / baseline_median - 1) * 100
    if abs(change) < ALLOCATION_PERCENT:
        return change, "same"
    separated = sample.min() > baseline.max() or sample.max() < baseline.min()
    if not separated:
        return change, "inconclusive"
    return change, "more" if change > 0 else "less"


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
    picks_sample = rng.integers(0, len(sample), size=(RESAMPLES, len(sample)))
    picks_baseline = rng.integers(0, len(baseline), size=(RESAMPLES, len(baseline)))
    ratios = row_medians(sample[picks_sample]) / row_medians(baseline[picks_baseline])
    low, high = np.percentile((ratios - 1) * 100, [2.5, 97.5])
    if low <= 0 <= high or stats.mannwhitneyu(sample, baseline, alternative="two-sided").pvalue >= SIGNIFICANCE:
        return "inconclusive"
    return "slower" if change > 0 else "faster"


def quartile_ratio(frame: pd.DataFrame) -> float:
    return float((frame["p75_ms"] / frame["p25_ms"]).median())


def split_by_round(frame: pd.DataFrame, column: str) -> dict[tuple[str, str], np.ndarray]:
    return {key: values.to_numpy(dtype=float) for key, values in frame.groupby(["label", "round"], sort=False)[column]}


def analyse_scope(scenario: str, track: str, scope: str, group: pd.DataFrame, seed: int) -> list[dict]:
    rng = np.random.default_rng(seed)
    baseline_label = TRACK_B_BASELINE if track == "B" else TRACK_A_BASELINE
    medians = split_by_round(group, "median_ms")
    means = split_by_round(group, "mean_ms")
    has_allocations = "alloc_bytes" in group
    baseline = group[group["label"] == baseline_label]
    baseline_rounds = set(baseline["round"])
    baseline_ratio = quartile_ratio(baseline) if not baseline.empty else math.nan
    baseline_cache: dict[tuple[str, tuple[str, ...]], dict] = {}

    def baseline_description(metric: str, rounds: list[str]) -> dict:
        key = (metric, tuple(rounds))
        if key not in baseline_cache:
            source = medians if metric == "median_ms" else means
            baseline_cache[key] = describe([source[(baseline_label, r)] for r in rounds], rng)
        return baseline_cache[key]

    rows = []
    for label, sample in group.groupby("label", sort=False):
        is_baseline = label == baseline_label
        rounds = sorted(set(sample["round"]) & baseline_rounds) if not is_baseline and not baseline.empty else sorted(set(sample["round"]))
        median_by_round = [medians[(label, r)] for r in rounds]
        mean_by_round = [means[(label, r)] for r in rounds]
        ratio = quartile_ratio(sample)
        median = describe(median_by_round, rng)
        mean = describe(mean_by_round, rng)
        row = {
            "scenario": scenario,
            "track": track,
            "scope": scope,
            "label": label,
            "epoch": sample["epoch"].iloc[0],
            "baseline": baseline_label,
            "n": len(sample),
            "rounds": len(rounds),
            "quartile_ratio": ratio,
            "median_ms": median["value"],
            "ci_low_ms": median["low"],
            "ci_high_ms": median["high"],
            "mean_ms": mean["value"],
            "mean_ci_low_ms": mean["low"],
            "mean_ci_high_ms": mean["high"],
        }
        if not is_baseline and not baseline.empty:
            baseline_median_by_round = [medians[(baseline_label, r)] for r in rounds]
            baseline_mean_by_round = [means[(baseline_label, r)] for r in rounds]
            median_change = compare_metric(median_by_round, baseline_median_by_round, rng)
            mean_change = compare_metric(mean_by_round, baseline_mean_by_round, rng)
            alloc_change, alloc_verdict = (
                allocation_verdict(sample["alloc_bytes"].to_numpy(dtype=float), baseline["alloc_bytes"].to_numpy(dtype=float))
                if has_allocations
                else (math.nan, "n/a")
            )
            row.update(
                {
                    "baseline_median_ms": baseline_description("median_ms", rounds)["value"],
                    "baseline_mean_ms": baseline_description("mean_ms", rounds)["value"],
                    "change_pct": median_change["change"],
                    "change_ci_low_pct": median_change["low"],
                    "change_ci_high_pct": median_change["high"],
                    "mw_p": median_change["p"],
                    "mean_change_pct": mean_change["change"],
                    "mean_change_ci_low_pct": mean_change["low"],
                    "mean_change_ci_high_pct": mean_change["high"],
                    "mean_mw_p": mean_change["p"],
                    "multimodal": max(ratio, baseline_ratio) >= MULTIMODAL_QUARTILE_RATIO,
                    "round_changes": median_change["round_changes"],
                    "mean_round_changes": mean_change["round_changes"],
                    "prespecified_verdict": prespecified_verdict(sample["median_ms"].to_numpy(float), baseline["median_ms"].to_numpy(float), rng),
                    "alloc_change_pct": alloc_change,
                    "alloc_verdict": alloc_verdict,
                }
            )
        rows.append(row)
    return rows


def analyse_scope_job(job: tuple) -> list[dict]:
    return analyse_scope(*job)


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


def summarise(runs_by_scenario: dict[str, pd.DataFrame], rng: np.random.Generator, workers: int = 1) -> pd.DataFrame:
    keyed = []
    for scenario, runs in runs_by_scenario.items():
        filled = runs.assign(round=runs["round"].astype("string").fillna(SINGLE_ROUND).astype(str))
        keyed.extend((scenario, track, scope, group) for (track, scope), group in filled.groupby(["track", "scope"], sort=False))
    seeds = rng.integers(0, 2**63 - 1, size=len(keyed))
    jobs = [(*key, int(seed)) for key, seed in zip(keyed, seeds, strict=True)]
    if workers > 1 and len(jobs) > 1:
        with ProcessPoolExecutor(max_workers=workers) as pool:
            results = list(pool.map(analyse_scope_job, jobs, chunksize=max(1, len(jobs) // (workers * 4))))
    else:
        results = [analyse_scope_job(job) for job in jobs]
    frame = pd.DataFrame([row for rows in results for row in rows])
    for column in ("change_pct", "mw_p", "mean_mw_p", "multimodal", "prespecified_verdict", "alloc_change_pct", "alloc_verdict"):
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


def allocation_cell(row) -> str:
    if row.alloc_verdict in ("n/a", "same") or pd.isna(row.alloc_change_pct):
        return str(row.alloc_verdict)
    return f"{row.alloc_verdict} ({row.alloc_change_pct:+.1f}%)"


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
        (
            "Allocations: allocated bytes per batch are near-deterministic, so they get their own verdict, pooled over all processes of a point. "
            f"`same` if the medians differ by less than {ALLOCATION_PERCENT:.0f}%; "
            "`more`/`less` if they differ by at least that and the ranges of the two points do not overlap; "
            "`inconclusive` if they differ but the ranges overlap; `n/a` if no allocation data exists."
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
            "| p75/p25 | rounds agreeing | verdict | pre-spec | allocations |"
        ),
        "|---|---|---|---:|---|---|---:|---|---:|---:|---|---|---|",
    ]
    for row in results.itertuples(index=False):
        median = f"{row.median_ms:.4f} [{row.ci_low_ms:.4f}, {row.ci_high_ms:.4f}]"
        n_rounds = f"{row.n} ({row.rounds})"
        if row.verdict == "baseline":
            lines.append(
                f"| {row.scenario} | `{row.scope}` | {row.label} | {n_rounds} | {median} | - | {row.mean_ms:.4f} | - | "
                f"{row.quartile_ratio:.2f} | - | baseline | - | - |"
            )
            continue
        median_change = f"{row.change_pct:+.1f}% [{row.change_ci_low_pct:+.1f}, {row.change_ci_high_pct:+.1f}]"
        mean_change = f"{row.mean_change_pct:+.1f}% [{row.mean_change_ci_low_pct:+.1f}, {row.mean_change_ci_high_pct:+.1f}]"
        agreeing = f"{int(row.rounds_agree)}/{row.rounds}"
        judged = f"{row.verdict} (by mean)" if row.primary_metric == "mean" else row.verdict
        lines.append(
            f"| {row.scenario} | `{row.scope}` | {row.label} | {n_rounds} | {median} | {median_change} | {row.mean_ms:.4f} | {mean_change} | "
            f"{row.quartile_ratio:.2f} | {agreeing} | {judged} | {row.prespecified_verdict} | {allocation_cell(row)} |"
        )
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Round-aware CIs, stratified Mann-Whitney U, verdicts and epoch plots for the matrix summary")
    parser.add_argument("--summary", type=Path, default=Path("results/summary"))
    parser.add_argument("--seed", type=int, default=20250519)
    parser.add_argument("--workers", type=int, default=os.cpu_count() or 1)
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
    results = summarise(runs_by_scenario, rng, args.workers).reindex(columns=COLUMNS)
    plots = [path for scenario, frame in results.groupby("scenario", sort=False) for path in plot(frame, scenario, args.summary / "plots")]
    results.to_csv(args.summary / "stats.csv", index=False)
    write_markdown(results, args.summary / "stats.md")
    print(f"stats: {len(results)} rows -> {args.summary / 'stats.csv'}; {len(plots)} plot files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
