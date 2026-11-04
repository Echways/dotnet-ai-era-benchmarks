#!/usr/bin/env python3
import argparse

from common import CACHE, add_step_arguments, coauthor_ids, ensure_mirror, git, libraries, steps, write_json

SEPARATOR = "\x1e"
FIELD = "\x1f"
FORMAT = FIELD.join(["%H", "%an", "%ae", "%aI", "%cn", "%cI", "%s", "%B"]) + SEPARATOR


def collect(library, start: str, end: str) -> list[dict]:
    output = git(library.mirror, "log", "--no-merges", f"--format={FORMAT}", f"{start}..{end}", "--", *library.paths)
    commits = []
    for record in output.split(SEPARATOR):
        record = record.strip("\n")
        if not record:
            continue
        sha, author, email, authored, committer, committed, subject, body = record.split(FIELD)
        commits.append(
            {
                "sha": sha,
                "author_name": author,
                "author_email": email,
                "authored_at": authored,
                "committer_name": committer,
                "committed_at": committed,
                "subject": subject,
                "coauthor_ids": coauthor_ids(body),
                "body": body,
            }
        )
    return commits


def main() -> int:
    parser = argparse.ArgumentParser(description="List the commits between two tags that touch each library")
    add_step_arguments(parser)
    args = parser.parse_args()
    for repo in sorted({library.repo for library in libraries(args.library)}):
        ensure_mirror(repo)
    for step in steps(args.start, args.end):
        for library in libraries(args.library):
            commits = collect(library, step.start, step.end)
            path = CACHE / f"commits-{library.name}-{step.key}.json"
            write_json(
                path, {"library": library.name, "repo": library.repo, "from": step.start, "to": step.end, "paths": list(library.paths), "commits": commits}
            )
            print(f"{library.name}: {len(commits)} commits {step.key}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
