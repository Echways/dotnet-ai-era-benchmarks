import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "analysis"))

import numpy as np
import pandas as pd
import stats

SUMMARY = ROOT / "results" / "rounds" / "summary"
OUT = ROOT / "articles-iteration-1-rewrite" / "data" / "release-steps.csv"
SCENARIOS = ["di", "config", "options", "logging", "cache", "ef"]
STEPS = [("e0-7", "e1-8"), ("e1-8", "e1-9"), ("e1-9", "e2-10"), ("e2-10", "e2-11")]
SEED = 20250519


def analyse_step(runs, before, after):
    stats.TRACK_A_BASELINE = before
    selected = {}
    for scenario, frame in runs.items():
        part = frame[(frame["track"] == "A") & frame["label"].isin([before, after])]
        part = part.groupby("scope").filter(lambda group: set(group["label"]) == {before, after})
        if not part.empty:
            selected[scenario] = part
    results = stats.summarise(selected, np.random.default_rng(SEED), os.cpu_count() or 1)
    results = results[results["label"] == after]
    multimodal = results["multimodal"].astype(bool)
    return pd.DataFrame(
        {
            "mode": "stand",
            "step": f"{before}>{after}",
            "scope": results["scope"],
            "change_pct": np.where(multimodal, results["mean_change_pct"], results["change_pct"]).round(2),
            "low_pct": np.where(multimodal, results["mean_change_ci_low_pct"], results["change_ci_low_pct"]).round(2),
            "high_pct": np.where(multimodal, results["mean_change_ci_high_pct"], results["change_ci_high_pct"]).round(2),
            "verdict": results["verdict"],
            "alloc_verdict": results["alloc_verdict"],
        }
    )


def main():
    runs = {scenario: pd.read_csv(SUMMARY / f"{scenario}-runs.csv") for scenario in SCENARIOS}
    OUT.parent.mkdir(exist_ok=True)
    pd.concat([analyse_step(runs, before, after) for before, after in STEPS]).to_csv(OUT, index=False)


if __name__ == "__main__":
    main()
