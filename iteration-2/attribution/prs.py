#!/usr/bin/env python3
import argparse
import re

from common import CACHE, CONFIG, GitHub, add_step_arguments, git, libraries, read_json, steps, write_json

PR_SUFFIX = re.compile(r"\(#(\d+)\)\s*$")
BATCH = 40
FIELDS = """
associatedPullRequests(first: 5) {
  nodes {
    number title url authorAssociation createdAt mergedAt baseRefName
    mergeCommit { oid }
    additions deletions changedFiles
    labels(first: 20) { nodes { name } }
    author { __typename login ... on User { databaseId } ... on Bot { databaseId } }
  }
}
"""


def query_for(owner: str, name: str, shas: list[str]) -> str:
    aliases = "\n".join(f'c{index}: object(oid: "{sha}") {{ ... on Commit {{ {FIELDS} }} }}' for index, sha in enumerate(shas))
    return f'query {{ repository(owner: "{owner}", name: "{name}") {{ {aliases} }} }}'


def fetch_pull_requests(github: GitHub, repo: str, shas: list[str]) -> None:
    store_path = CACHE / f"pulls-{repo}.json"
    store = read_json(store_path, {})
    missing = [sha for sha in shas if sha not in store]
    settings = CONFIG["repos"][repo]
    for offset in range(0, len(missing), BATCH):
        batch = missing[offset : offset + BATCH]
        response = github.graphql(query_for(settings["owner"], settings["name"], batch))
        data = (response.get("data") or {}).get("repository")
        if data is None:
            raise RuntimeError(f"GraphQL returned no repository data: {str(response)[:300]}")
        for index, sha in enumerate(batch):
            commit = data.get(f"c{index}")
            store[sha] = [] if commit is None else commit["associatedPullRequests"]["nodes"]
        write_json(store_path, store)
        print(f"{repo}: {min(offset + BATCH, len(missing))}/{len(missing)} commits resolved", flush=True)


def pick(pulls: list[dict] | None, sha: str) -> dict | None:
    if not pulls:
        return None
    for pull in pulls:
        if (pull.get("mergeCommit") or {}).get("oid") == sha:
            return pull
    merged = [pull for pull in pulls if pull.get("mergedAt")]
    return (merged or pulls)[0]


def git_stats(mirror, sha: str) -> dict:
    output = git(mirror, "show", "--numstat", "--format=", sha)
    additions = deletions = files = 0
    for line in output.splitlines():
        parts = line.split("\t")
        if len(parts) == 3:
            files += 1
            additions += int(parts[0]) if parts[0].isdigit() else 0
            deletions += int(parts[1]) if parts[1].isdigit() else 0
    return {"additions": additions, "deletions": deletions, "changed_files": files, "stats_source": "git commit numstat"}


def describe(store: dict, library, commit: dict) -> dict:
    sha = commit["sha"]
    if sha not in store:
        return {"sha": sha, "status": "pending"}
    pull = pick(store[sha], sha)
    if pull is None:
        suffix = PR_SUFFIX.search(commit["subject"])
        return {"sha": sha, "status": "no-pr", "pr_number_from_subject": int(suffix.group(1)) if suffix else None, **git_stats(library.mirror, sha)}
    author = pull.get("author") or {}
    return {
        "sha": sha,
        "status": "ok",
        "pr_number": pull["number"],
        "title": pull["title"],
        "html_url": pull["url"],
        "author_login": author.get("login", "ghost"),
        "author_id": author.get("databaseId"),
        "author_type": "Bot" if author.get("__typename") == "Bot" else "User",
        "author_association": pull.get("authorAssociation"),
        "labels": sorted(label["name"] for label in pull["labels"]["nodes"]),
        "created_at": pull.get("createdAt"),
        "merged_at": pull.get("mergedAt"),
        "base_ref": pull.get("baseRefName"),
        "additions": pull["additions"],
        "deletions": pull["deletions"],
        "changed_files": pull["changedFiles"],
        "stats_source": "github pull request",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Map every commit to its pull request through batched GraphQL queries (cached)")
    add_step_arguments(parser)
    parser.add_argument("--offline", action="store_true", help="use cached responses only")
    args = parser.parse_args()
    github = GitHub(offline=args.offline)
    print(f"GitHub transport: {github.transport}")
    sources = {}
    for step in steps(args.start, args.end):
        for library in libraries(args.library):
            source = read_json(CACHE / f"commits-{library.name}-{step.key}.json", None)
            if source is None:
                raise SystemExit(f"run commits.py for {library.name} {step.key} first")
            sources[(library.name, step.key)] = (library, source)
    if not args.offline:
        for repo in sorted({library.repo for library, _ in sources.values()}):
            shas = sorted({commit["sha"] for library, source in sources.values() if library.repo == repo for commit in source["commits"]})
            fetch_pull_requests(github, repo, shas)
    for (name, key), (library, source) in sources.items():
        store = read_json(CACHE / f"pulls-{library.repo}.json", {})
        records = [describe(store, library, commit) for commit in source["commits"]]
        write_json(CACHE / f"prs-{name}-{key}.json", {"library": name, "repo": library.repo, "range": key, "records": records})
        pending = sum(1 for record in records if record["status"] == "pending")
        print(f"{name} {key}: {len(records)} commits, {pending} pending")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
