#!/usr/bin/env python3
import argparse
import csv
import json
from collections import Counter, defaultdict
from pathlib import Path

from common import CCA_ACCOUNT_ID, COPILOT_CLI_ID, COPILOT_REVIEWER_ID, SUMMARY

CLASSES = ["cca", "msft-human", "community", "bot", "no-pr", "pending"]


def read_csv(path: Path) -> list[dict]:
    if not path.exists():
        return []
    with open(path, encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def number(value: str) -> int:
    return int(value) if value not in ("", None) else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Render attribution.md from the attribution CSV/JSON outputs")
    parser.add_argument("--summary", default=str(SUMMARY))
    args = parser.parse_args()
    summary = Path(args.summary)
    rows = read_csv(summary / "attribution.csv")
    methods = read_csv(summary / "attribution-methods.csv")
    regressions_path = summary / "attribution-regressions.json"
    regressions = json.loads(regressions_path.read_text(encoding="utf-8")) if regressions_path.exists() else None
    lines = ["# Attribution", ""]
    lines.append(
        f"Classes: `cca` = pull request opened by the Copilot coding agent (GitHub app `copilot-swe-agent`, account id {CCA_ACCOUNT_ID}); "
        "`msft-human` = human author with `author_association` MEMBER/OWNER/COLLABORATOR; `community` = any other human; "
        "`bot` = any other GitHub app or bot; `no-pr` = commit not associated with a pull request; `pending` = not yet resolved through the API. "
        f"Flags: `cca_coauthored` = a non-agent PR whose squash commit carries a `copilot-swe-agent` co-author trailer; "
        f"`copilot_review_coauthored` (app id {COPILOT_REVIEWER_ID}) and `copilot_cli_coauthored` (app id {COPILOT_CLI_ID}) "
        "are reported but not counted as agent work."
    )
    lines.append("")
    if rows:
        ranges = sorted({row["range"] for row in rows})
        lines += [f"Range: {', '.join(ranges)}. Commits: {len(rows)}.", ""]
        lines += [
            "| library | class | commits | PRs | additions | deletions | cca_coauthored | copilot review co-author |",
            "|---|---|---:|---:|---:|---:|---:|---:|",
        ]
        grouped: dict[tuple[str, str], list[dict]] = defaultdict(list)
        for row in rows:
            grouped[(row["library"], row["class"])].append(row)
        for library in sorted({row["library"] for row in rows}):
            for klass in CLASSES:
                members = grouped.get((library, klass), [])
                if not members:
                    continue
                prs = {row["pr_number"] for row in members if row["pr_number"]}
                lines.append(
                    f"| {library} | {klass} | {len(members)} | {len(prs)} | {sum(number(r['additions']) for r in members)} | "
                    f"{sum(number(r['deletions']) for r in members)} | {sum(r['cca_coauthored'] == 'True' for r in members)} | "
                    f"{sum(r['copilot_review_coauthored'] == 'True' for r in members)} |"
                )
        disagree = Counter(
            row["class"] for row in rows if row["class"] in ("msft-human", "community") and (row["class"] == "msft-human") != (row["microsoft_email"] == "True")
        )
        lines += [
            "",
            (
                "`author_association` is a weak proxy for Microsoft employment: private organisation membership is reported as CONTRIBUTOR, "
                "and non-employees can be MEMBER of the dotnet organisation. The `microsoft_email` column (commit author e-mail ends with "
                f"`@microsoft.com`) disagrees with the class for {dict(disagree)} commits; treat `msft-human` vs `community` as indicative."
            ),
        ]
        sources = Counter(row["stats_source"] for row in rows)
        lines += ["", f"Line statistics source: {dict(sources)}.", ""]
    if methods:
        lines += [
            "## Hot methods (git log -L)",
            "",
            "| method | file | commits | cca | cca_coauthored | msft-human | community | other |",
            "|---|---|---:|---:|---:|---:|---:|---:|",
        ]
        by_method: dict[str, list[dict]] = defaultdict(list)
        for row in methods:
            by_method[row["method"]].append(row)
        for method, entries in by_method.items():
            touched = [e for e in entries if e["sha"]]
            classes = Counter(e["class"] for e in touched)
            other = len(touched) - classes["cca"] - classes["msft-human"] - classes["community"]
            coauthored = sum(e["cca_coauthored"] == "True" for e in touched)
            lines.append(
                f"| `{method}` | {entries[0]['file'] or entries[0]['class']} | {len(touched)} | {classes['cca']} | {coauthored} | "
                f"{classes['msft-human']} | {classes['community']} | {other} |"
            )
        lines.append("")
    if regressions:
        lines += ["## Performance labels and reverts", ""]
        lines.append(f"PRs labelled `tenet-performance` or `regression-from-last-release`: {len(regressions['perf_labelled'])}.")
        for entry in regressions["perf_labelled"]:
            lines.append(f"- {entry['library']} #{entry['pr_number']} ({entry['class']}, {entry['labels']}): {entry['subject']}")
        lines.append(f"\nRevert commits: {len(regressions['reverts'])}.")
        for entry in regressions["reverts"]:
            lines.append(f"- {entry['library']} #{entry['pr_number']} ({entry['class']}): {entry['subject']}")
        lines += ["", "## Servicing churn in the 12 months after GA", ""]
        lines += [
            "| library | major | GA | window end | complete | days observed | patches | patches touching paths | commits |",
            "|---|---:|---|---|---|---:|---:|---:|---:|",
        ]
        for entry in regressions["servicing"]:
            lines.append(
                f"| {entry['library']} | {entry['major']} | {entry['ga_date']} | {entry['window_end']} | "
                f"{entry['window_complete']} | {entry['days_observed']} | "
                f"{entry['patches_in_window']} | {entry['patches_touching_paths']} | {entry['commits_in_window']} |"
            )
        lines.append("")
    (summary / "attribution.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"-> {summary / 'attribution.md'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
