#!/usr/bin/env python3
import argparse
import csv

from common import CACHE, CCA_ACCOUNT_ID, CCA_LOGIN, CONFIG, COPILOT_CLI_ID, COPILOT_REVIEWER_ID, SUMMARY, libraries, range_key, read_json

HUMAN_MSFT = {"MEMBER", "OWNER", "COLLABORATOR"}
CCA_EMAIL = f"{CCA_ACCOUNT_ID}+copilot@users.noreply.github.com"
COLUMNS = [
    "library",
    "repo",
    "range",
    "sha",
    "authored_at",
    "subject",
    "status",
    "pr_number",
    "pr_url",
    "author_login",
    "author_type",
    "author_association",
    "class",
    "cca_coauthored",
    "copilot_review_coauthored",
    "copilot_cli_coauthored",
    "labels",
    "created_at",
    "merged_at",
    "additions",
    "deletions",
    "changed_files",
    "stats_source",
]


def author_class(record: dict, commit: dict) -> str:
    if record["status"] == "pending":
        return "pending"
    if record["status"] == "no-pr":
        return "cca" if commit["author_email"].lower() == CCA_EMAIL else "no-pr"
    if record["author_id"] == CCA_ACCOUNT_ID or (record["author_login"] == CCA_LOGIN and record["author_type"] == "Bot"):
        return "cca"
    if record["author_type"] == "Bot":
        return "bot"
    return "msft-human" if record["author_association"] in HUMAN_MSFT else "community"


def rows_for(library, start: str, end: str) -> list[dict]:
    commits = read_json(CACHE / f"commits-{library.name}-{range_key(start, end)}.json", None)
    prs = read_json(CACHE / f"prs-{library.name}-{range_key(start, end)}.json", None)
    if commits is None or prs is None:
        raise SystemExit(f"run commits.py and prs.py for {library.name} {start}..{end} first")
    by_sha = {record["sha"]: record for record in prs["records"]}
    rows = []
    for commit in commits["commits"]:
        record = by_sha.get(commit["sha"], {"sha": commit["sha"], "status": "pending"})
        klass = author_class(record, commit)
        coauthors = set(commit["coauthor_ids"])
        authored_by_agent = commit["author_email"].lower() == CCA_EMAIL
        rows.append(
            {
                "library": library.name,
                "repo": library.repo,
                "range": range_key(start, end),
                "sha": commit["sha"],
                "authored_at": commit["authored_at"],
                "subject": commit["subject"],
                "status": record["status"],
                "pr_number": record.get("pr_number", ""),
                "pr_url": record.get("html_url", ""),
                "author_login": record.get("author_login", commit["author_name"]),
                "author_type": record.get("author_type", ""),
                "author_association": record.get("author_association", ""),
                "class": klass,
                "cca_coauthored": klass != "cca" and (CCA_ACCOUNT_ID in coauthors or authored_by_agent),
                "copilot_review_coauthored": COPILOT_REVIEWER_ID in coauthors,
                "copilot_cli_coauthored": COPILOT_CLI_ID in coauthors,
                "labels": ";".join(record.get("labels", [])),
                "created_at": record.get("created_at", ""),
                "merged_at": record.get("merged_at", ""),
                "additions": record.get("additions", ""),
                "deletions": record.get("deletions", ""),
                "changed_files": record.get("changed_files", ""),
                "stats_source": record.get("stats_source", ""),
            }
        )
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description="Classify each commit's pull request as cca, msft-human, community or bot")
    parser.add_argument("--library", default="all")
    parser.add_argument("--from", dest="start", default=CONFIG["default_range"]["from"])
    parser.add_argument("--to", dest="end", default=CONFIG["default_range"]["to"])
    parser.add_argument("--out", default=str(SUMMARY / "attribution.csv"))
    args = parser.parse_args()
    rows = [row for library in libraries(args.library) for row in rows_for(library, args.start, args.end)]
    with open(args.out, "w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=COLUMNS)
        writer.writeheader()
        writer.writerows(rows)
    counts: dict[str, int] = {}
    for row in rows:
        counts[row["class"]] = counts.get(row["class"], 0) + 1
    print(f"{len(rows)} commits -> {args.out}: {counts}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
