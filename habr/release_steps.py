import json
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "analysis"))

import numpy as np
import pandas as pd
import stats

SUMMARY = ROOT / "results" / "rounds" / "summary"
ROUNDS = ROOT / "results" / "rounds" / "rounds"
OUT = ROOT / "habr" / "data" / "release-steps.csv"
SCENARIOS = ["di", "config", "options", "logging", "cache", "ef"]
STEPS = [("e0-7", "e1-8"), ("e1-8", "e1-9"), ("e1-9", "e2-10"), ("e2-10", "e2-11")]


def stand_of(round_id):
    environment = json.loads((ROUNDS / round_id / "raw" / "env.json").read_text(encoding="utf8"))
    return "stand" if "performance" in environment.get("governors", {}) and not environment.get("turbo_enabled") else "stock"


def analyse_mode(runs, rounds, mode):
    rng = np.random.default_rng(20250519)
    rows = []
    for before, after in STEPS:
        stats.TRACK_A_BASELINE = before
        for scenario, frame in runs.items():
            selected = frame[frame["round"].astype(str).isin(rounds) & frame["label"].isin([before, after]) & (frame["track"] == "A")]
            selected = selected.groupby("scope").filter(lambda group: set(group["label"]) == {before, after})
            if selected.empty:
                continue
            for row in stats.analyse(selected.assign(round=selected["round"].astype(str)), scenario, rng):
                if row["label"] == after:
                    row["step"] = f"{before}>{after}"
                    rows.append(row)
    results = stats.add_verdicts(pd.DataFrame(rows))
    multimodal = results["multimodal"].astype(bool)
    return pd.DataFrame(
        {
            "mode": mode,
            "step": results["step"],
            "scope": results["scope"],
            "change_pct": np.where(multimodal, results["mean_change_pct"], results["change_pct"]).round(2),
            "low_pct": np.where(multimodal, results["mean_change_ci_low_pct"], results["change_ci_low_pct"]).round(2),
            "high_pct": np.where(multimodal, results["mean_change_ci_high_pct"], results["change_ci_high_pct"]).round(2),
            "verdict": results["verdict"],
        }
    )


def main():
    runs = {scenario: pd.read_csv(SUMMARY / f"{scenario}-runs.csv") for scenario in SCENARIOS}
    reported = sorted(os.listdir(ROUNDS))
    frames = [analyse_mode(runs, [r for r in reported if stand_of(r) == mode], mode) for mode in ("stand", "stock")]
    OUT.parent.mkdir(exist_ok=True)
    pd.concat(frames).to_csv(OUT, index=False)


if __name__ == "__main__":
    main()
