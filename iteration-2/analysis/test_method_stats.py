import zlib

import counter_ab
import method_stats
import numpy as np
import pandas as pd


def methods_frame(rows):
    return pd.DataFrame(rows, columns=["tag", "scenario", "pid", "method", "calls", "inclusive_ms", "self_ms"])


def steady(tag, per_call, pids=range(8), method="M:A.Run", calls=1000):
    rng = np.random.default_rng(zlib.crc32(f"{tag}{method}".encode()))
    return [(tag, "di", pid, method, calls, per_call * calls * (1 + rng.normal(0, 0.005)), per_call * calls * (1 + rng.normal(0, 0.005))) for pid in pids]


def test_labels_follow_the_matrix_points():
    assert method_stats.label_of("v7.0.20") == "e0-7"
    assert method_stats.label_of("v10.0.14") == "e2-10"


def test_a_method_twice_as_slow_is_reported_slower_and_an_unchanged_one_as_noise():
    rows = steady("v7.0.20", 0.001) + steady("v10.0.14", 0.002) + steady("v7.0.20", 0.001, method="M:A.Same") + steady("v10.0.14", 0.001, method="M:A.Same")
    result = method_stats.analyse(methods_frame(rows), "self", 0.0, 1, 1)
    verdicts = result[result["label"] == "e2-10"].set_index("method")["verdict"]
    assert verdicts["M:A.Run"] == "slower"
    assert verdicts["M:A.Same"] == "noise"


def test_methods_below_the_scope_cost_are_not_given_a_verdict():
    rows = steady("v7.0.20", 0.0001) + steady("v10.0.14", 0.0002)
    result = method_stats.analyse(methods_frame(rows), "self", 0.001, 1, 1)
    assert result[result["label"] == "e2-10"]["verdict"].iloc[0] == "below resolution"


def test_call_count_change_is_reported_next_to_the_time():
    rows = steady("v7.0.20", 0.001, calls=1000) + steady("v10.0.14", 0.001, calls=2000)
    result = method_stats.analyse(methods_frame(rows), "self", 0.0, 1, 1)
    assert result[result["label"] == "e2-10"]["calls_change_pct"].iloc[0] == 100.0


def test_several_files_are_treated_as_rounds(tmp_path):
    paths = []
    for index in range(3):
        path = tmp_path / f"methods-{index}.csv"
        methods_frame(
            steady("v7.0.20", 0.001, pids=range(index * 10, index * 10 + 4)) + steady("v10.0.14", 0.002, pids=range(index * 10 + 100, index * 10 + 104))
        ).to_csv(path, index=False)
        paths.append(path)
    rounds = method_stats.read_rounds(paths)
    assert set(rounds["round"]) == {"r0", "r1", "r2"}
    result = method_stats.analyse(rounds, "self", 0.0, 1, 1)
    assert (result[result["label"] == "e2-10"]["rounds"] == 3).all()


def test_counter_comparison_flags_a_scope_that_moved_and_leaves_a_flat_one():
    rng = np.random.default_rng(3)

    def runs(median):
        return pd.DataFrame(
            {"scenario": "di", "label": "e0-7", "scope": ["moves"] * 6 + ["flat"] * 6, "pid": range(12), "median_ms": [median * 1.0] * 6 + [2.0] * 6}
        ).assign(median_ms=lambda frame: frame["median_ms"] * (1 + rng.normal(0, 0.002, len(frame))))

    reference = runs(1.0)
    candidate = runs(1.0)
    candidate.loc[candidate["scope"] == "moves", "median_ms"] *= 1.5
    result = counter_ab.compare(reference, candidate, 20, 0, np.random.default_rng(1)).set_index("scope")
    assert result.loc["moves", "change_pct"] > 40
    assert abs(result.loc["flat", "change_pct"]) < 2
