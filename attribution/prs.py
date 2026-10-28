#!/usr/bin/env python3
import argparse
import re

from common import CACHE, CONFIG, GitHub, git, libraries, range_key, read_json, write_json

PR_SUFFIX = re.compile(r"\(#(\d+)\)\s*$")


def pick(pulls: list[dict] | None, sha: str) -> dict | None:
    if not pulls:
        return None
    for pull in pulls:
        if pull.get("merge_commit_sha") == sha:
            return pull
    merged = [pull for pull in pulls if pull.get("merged_at")]
    return (merged or pulls)[0]


def git_stats(library, sha: str) -> dict:
    output = git(library.mirror, "show", "--numstat", "--format=", sha)
    additions = deletions = files = 0
    for line in output.splitlines():
        parts = line.split("\t")
        if len(parts) == 3:
            files += 1
            additions += int(parts[0]) if parts[0].isdigit() else 0
            deletions += int(parts[1]) if parts[1].isdigit() else 0
    return {"additions": additions, "deletions": deletions, "changed_files": files, "stats_source": "git commit numstat"}


def describe(github: GitHub, library, commit: dict, details: bool) -> dict:
    sha = commit["sha"]
    pulls = github.get(f"repos/{library.owner}/{library.repo_name}/commits/{sha}/pulls")
    if pulls is None and github.offline:
        return {"sha": sha, "status": "pending"}
    pull = pick(pulls, sha)
    if pull is None:
        suffix = PR_SUFFIX.search(commit["subject"])
        return {"sha": sha, "status": "no-pr", "pr_number_from_subject": int(suffix.group(1)) if suffix else None, **git_stats(library, sha)}
    record = {
        "sha": sha,
        "status": "ok",
        "pr_number": pull["number"],
        "title": pull["title"],
        "html_url": pull["html_url"],
        "author_login": pull["user"]["login"],
        "author_id": pull["user"]["id"],
        "author_type": pull["user"]["type"],
        "author_association": pull.get("author_association"),
        "labels": sorted(label["name"] for label in pull.get("labels", [])),
        "created_at": pull.get("created_at"),
        "merged_at": pull.get("merged_at"),
        "base_ref": pull.get("base", {}).get("ref"),
    }
    full = github.get(f"repos/{library.owner}/{library.repo_name}/pulls/{pull['number']}") if details else None
    if full:
        record.update(
            {"additions": full["additions"], "deletions": full["deletions"], "changed_files": full["changed_files"], "stats_source": "github pull request"}
        )
    else:
        record.update(git_stats(library, sha))
    return record


def main() -> int:
    parser = argparse.ArgumentParser(description="Map every commit to its pull request through the GitHub API (cached)")
    parser.add_argument("--library", default="all")
    parser.add_argument("--from", dest="start", default=CONFIG["default_range"]["from"])
    parser.add_argument("--to", dest="end", default=CONFIG["default_range"]["to"])
    parser.add_argument("--details", choices=["auto", "always", "never"], default="auto", help="fetch pulls/{n} for additions/deletions/changed_files")
    parser.add_argument("--offline", action="store_true", help="use cached responses only")
    args = parser.parse_args()
    github = GitHub(offline=args.offline)
    details = args.details == "always" or (args.details == "auto" and github.authenticated)
    print(f"GitHub transport: {github.transport}; PR details: {'pulls/{n}' if details else 'git numstat of the merged commit'}")
    for library in libraries(args.library):
        source = read_json(CACHE / f"commits-{library.name}-{range_key(args.start, args.end)}.json", None)
        if source is None:
            raise SystemExit(f"run commits.py --library {library.name} --from {args.start} --to {args.end} first")
        records = [describe(github, library, commit, details) for commit in source["commits"]]
        path = CACHE / f"prs-{library.name}-{range_key(args.start, args.end)}.json"
        write_json(path, {"library": library.name, "repo": library.repo, "from": args.start, "to": args.end, "records": records})
        pending = sum(1 for record in records if record["status"] == "pending")
        print(f"{library.name}: {len(records)} commits, {pending} pending, {github.requests} API requests so far")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
