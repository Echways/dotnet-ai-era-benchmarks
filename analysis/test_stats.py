import math

import numpy as np
import pandas as pd
import pytest
import stats


def runs_for(label, values, round_id="r1", mean=None, quartile_ratio=1.02, scope="scope"):
    medians = np.asarray(values, dtype=float)
    means = medians if mean is None else np.asarray(mean, dtype=float)
    return pd.DataFrame(
        {
            "label": label,
            "epoch": "E",
            "track": "A",
            "round": round_id,
            "scope": scope,
            "median_ms": medians,
            "mean_ms": means,
            "p25_ms": medians / math.sqrt(quartile_ratio),
            "p75_ms": medians * math.sqrt(quartile_ratio),
        }
    )


def around(value, count=10):
    return value * (1 + np.linspace(-0.002, 0.002, count))


def comparison(*frames):
    results = stats.summarise({"scenario": pd.concat(frames, ignore_index=True)}, np.random.default_rng(1))
    return results[results["label"] == "e2-10"].iloc[0]


def rounds_with_changes(changes, base=1.0, **point_options):
    frames = []
    for index, change in enumerate(changes):
        frames.append(runs_for("e0-7", around(base), f"r{index}"))
        frames.append(runs_for("e2-10", around(base * (1 + change / 100)), f"r{index}", **point_options))
    return frames


def test_slower_when_every_round_clears_the_noise_band():
    row = comparison(*rounds_with_changes([20, 21, 19]))

    assert row["verdict"] == "slower"
    assert row["change_pct"] == pytest.approx(20, abs=0.1)
    assert row["rounds_agree"] == 3


def test_noise_when_every_round_stays_inside_the_band():
    assert comparison(*rounds_with_changes([1, -1, 0.5]))["verdict"] == "noise"


def test_inconclusive_when_rounds_straddle_the_band_edge():
    row = comparison(*rounds_with_changes([4.5, 19, 11]))

    assert row["verdict"] == "inconclusive"
    assert row["rounds_agree"] == 2


def test_inconclusive_when_rounds_disagree_on_direction():
    assert comparison(*rounds_with_changes([-12, 12, 12]))["verdict"] == "inconclusive"


def test_rounds_count_equally_whatever_their_run_count():
    short_baseline = runs_for("e0-7", around(1.0, 3), "short")
    short_point = runs_for("e2-10", around(1.2, 3), "short")
    long_baseline = runs_for("e0-7", around(1.0, 10), "long")
    long_point = runs_for("e2-10", around(1.4, 10), "long")

    row = comparison(short_baseline, short_point, long_baseline, long_point)

    assert row["change_pct"] == pytest.approx((math.sqrt(1.2 * 1.4) - 1) * 100, abs=0.01)


def test_blank_round_is_treated_as_one_round():
    baseline = runs_for("e0-7", around(1.0), None)
    point = runs_for("e2-10", around(1.2), None)

    row = comparison(baseline, point)

    assert row["rounds"] == 1
    assert row["verdict"] == "slower"


def test_three_runs_alone_cannot_reach_significance():
    baseline = runs_for("e0-7", around(1.0, 3))
    point = runs_for("e2-10", around(2.0, 3))

    assert comparison(baseline, point)["verdict"] == "inconclusive"


def test_mixed_when_median_and_mean_move_in_opposite_directions():
    frames = []
    for round_id in ("r0", "r1", "r2"):
        frames.append(runs_for("e0-7", around(1.0), round_id))
        frames.append(runs_for("e2-10", around(0.86), round_id, mean=around(1.11)))

    row = comparison(*frames)

    assert row["median_verdict"] == "faster"
    assert row["mean_verdict"] == "slower"
    assert row["verdict"] == "mixed"


def test_multimodal_point_is_judged_by_its_mean():
    frames = []
    for round_id in ("r0", "r1", "r2"):
        frames.append(runs_for("e0-7", around(3.2), round_id, mean=around(8.1), quartile_ratio=4))
        frames.append(runs_for("e2-10", around(4.7), round_id, mean=around(7.4), quartile_ratio=4))

    row = comparison(*frames)

    assert row["multimodal"]
    assert row["primary_metric"] == "mean"
    assert row["median_verdict"] == "slower"
    assert row["verdict"] == "faster"


def test_multimodal_baseline_alone_is_enough_to_judge_by_mean():
    frames = []
    for round_id in ("r0", "r1", "r2"):
        frames.append(runs_for("e0-7", around(0.87), round_id, mean=around(1.25), quartile_ratio=1.7))
        frames.append(runs_for("e2-10", around(1.08), round_id, mean=around(1.27)))

    row = comparison(*frames)

    assert row["multimodal"]
    assert row["verdict"] == "noise"


def test_p_values_are_adjusted_across_all_comparisons():
    strong_baseline = runs_for("e0-7", around(1.0), scope="strong")
    strong_point = runs_for("e2-10", around(1.5), scope="strong")
    weak_baseline = runs_for("e0-7", [1.00, 1.01, 1.02, 1.03, 1.04, 1.05, 1.06, 1.07, 1.08, 1.09], scope="weak")
    weak_point = runs_for("e2-10", [1.015, 1.035, 1.055, 1.075, 1.095, 1.10, 1.11, 1.12, 1.13, 1.14], scope="weak")
    frames = pd.concat([strong_baseline, strong_point, weak_baseline, weak_point], ignore_index=True)

    results = stats.summarise({"scenario": frames}, np.random.default_rng(1))
    strong = results[(results["scope"] == "strong") & (results["label"] == "e2-10")].iloc[0]

    assert strong["mw_p_adj"] == pytest.approx(2 * strong["mw_p"])


def test_baseline_row_carries_no_change():
    row = stats.summarise({"scenario": pd.concat(rounds_with_changes([20, 21]), ignore_index=True)}, np.random.default_rng(1))
    baseline = row[row["label"] == "e0-7"].iloc[0]

    assert baseline["verdict"] == "baseline"
    assert math.isnan(baseline["change_pct"])


def test_prespecified_verdict_keeps_the_original_median_rule():
    frames = []
    for round_id in ("r0", "r1", "r2"):
        frames.append(runs_for("e0-7", around(3.2), round_id, mean=around(8.1), quartile_ratio=4))
        frames.append(runs_for("e2-10", around(4.7), round_id, mean=around(7.4), quartile_ratio=4))

    row = comparison(*frames)

    assert row["prespecified_verdict"] == "slower"
    assert row["verdict"] == "faster"


def test_prespecified_verdict_ignores_rounds():
    assert comparison(*rounds_with_changes([4.5, 19, 11]))["prespecified_verdict"] == "slower"


def test_mixed_when_mean_estimate_opposes_a_decided_median_despite_a_wide_mean_interval():
    frames = []
    for round_id, mean_change in (("r0", 1), ("r1", 7), ("r2", 13)):
        frames.append(runs_for("e0-7", around(1.0), round_id))
        frames.append(runs_for("e2-10", around(0.87), round_id, mean=around(1 + mean_change / 100)))

    row = comparison(*frames)

    assert row["median_verdict"] == "faster"
    assert row["mean_verdict"] == "inconclusive"
    assert row["verdict"] == "mixed"


def test_small_opposite_mean_does_not_make_a_decided_median_mixed():
    frames = []
    for round_id in ("r0", "r1", "r2"):
        frames.append(runs_for("e0-7", around(1.0), round_id))
        frames.append(runs_for("e2-10", around(0.87), round_id, mean=around(1.02)))

    assert comparison(*frames)["verdict"] == "faster"


def test_markdown_lists_scopes_whose_mean_moved_while_the_median_did_not(tmp_path):
    frames = []
    for round_id in ("r0", "r1", "r2"):
        frames.append(runs_for("e0-7", around(1.0), round_id))
        frames.append(runs_for("e2-10", around(1.0), round_id, mean=around(0.84)))
    results = stats.summarise({"scenario": pd.concat(frames, ignore_index=True)}, np.random.default_rng(1)).reindex(columns=stats.COLUMNS)

    stats.write_markdown(results, tmp_path / "stats.md")

    section = (tmp_path / "stats.md").read_text(encoding="utf-8").split("## Mean moved, median did not")[1].split("## All scopes")[0]
    assert "`scope`" in section
    assert "-16.0%" in section
