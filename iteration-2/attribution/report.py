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
        lines += [f"Ranges: {', '.join(ranges)}. Commits: {len(rows)}.", ""]
        lines += [
            "| range | class | commits | PRs | additions | deletions | cca_coauthored | copilot review co-author |",
            "|---|---|---:|---:|---:|---:|---:|---:|",
        ]
        grouped: dict[tuple[str, str], list[dict]] = defaultdict(list)
        for row in rows:
            grouped[(row["range"], row["class"])].append(row)
        for step in ranges:
            for klass in CLASSES:
                members = grouped.get((step, klass), [])
                if not members:
                    continue
                prs = {(row["repo"], row["pr_number"]) for row in members if row["pr_number"]}
                lines.append(
                    f"| {step} | {klass} | {len(members)} | {len(prs)} | {sum(number(r['additions']) for r in members)} | "
                    f"{sum(number(r['deletions']) for r in members)} | {sum(r['cca_coauthored'] == 'True' for r in members)} | "
                    f"{sum(r['copilot_review_coauthored'] == 'True' for r in members)} |"
                )
        lines += ["", "Commits touching several packages are counted once per package. Per package:", ""]
        lines += ["| package | range | commits | cca | msft-human | community | other |", "|---|---|---:|---:|---:|---:|---:|"]
        per_package: dict[tuple[str, str], Counter] = defaultdict(Counter)
        for row in rows:
            per_package[(row["library"], row["range"])][row["class"]] += 1
        for (library, step), counts in sorted(per_package.items()):
            other = sum(counts.values()) - counts["cca"] - counts["msft-human"] - counts["community"]
            lines.append(f"| {library} | {step} | {sum(counts.values())} | {counts['cca']} | {counts['msft-human']} | {counts['community']} | {other} |")
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
            "## Traced methods touched in each step (git log -L)",
            "",
            "A method counts as touched by a class when at least one commit of that class changed its line range between the two tags.",
            "",
            "| package | range | traced methods | touched | by cca | by cca co-author | by msft-human | by community |",
            "|---|---|---:|---:|---:|---:|---:|---:|",
        ]
        by_key: dict[tuple[str, str], dict[str, list[dict]]] = defaultdict(lambda: defaultdict(list))
        for row in methods:
            by_key[(row["library"], row["range"])][row["method"]].append(row)
        agent_methods = []
        for (library, step), per_method in sorted(by_key.items()):

            def has(entries, klass):
                return any(e["class"] == klass for e in entries)

            touched = {m: [e for e in entries if e["sha"]] for m, entries in per_method.items()}
            touched = {m: entries for m, entries in touched.items() if entries}
            by_cca = [m for m, entries in touched.items() if has(entries, "cca")]
            agent_methods += [(library, step, m) for m in by_cca]
            lines.append(
                f"| {library} | {step} | {len(per_method)} | {len(touched)} | {len(by_cca)} | "
                f"{sum(any(e['cca_coauthored'] == 'True' for e in entries) for entries in touched.values())} | "
                f"{sum(has(entries, 'msft-human') for entries in touched.values())} | {sum(has(entries, 'community') for entries in touched.values())} |"
            )
        if agent_methods:
            lines += ["", "Methods touched by a `cca` pull request:", ""]
            lines += [f"- {library} {step}: `{method}`" for library, step, method in agent_methods]
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
