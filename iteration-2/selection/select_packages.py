import argparse
import csv
from datetime import date
from pathlib import Path

from eligibility import eligibility
from nuget import package_files, search_owner
from sources import latest_tag, locate, project_index

OWNERS = ("dotnetframework", "aspnet", "EntityFramework")
CUT = 30
SNAPSHOT_COLUMNS = ["rank", "id", "version", "downloads", "owners"]
DECISION_COLUMNS = ["rank", "id", "version", "downloads", "source", "project", "decision", "reason"]
CURRENT_MAJOR = 10
REPOSITORIES = ("runtime", "aspnetcore", "efcore")


def merged_candidates() -> list[dict]:
    by_id: dict[str, dict] = {}
    for owner in OWNERS:
        for entry in search_owner(owner):
            by_id[entry["id"].lower()] = entry
    ranked = sorted(by_id.values(), key=lambda entry: entry["totalDownloads"], reverse=True)
    return ranked


def decide(candidates: list[dict], indexes: dict[str, dict[str, str]], cache: Path, cut: int) -> list[dict]:
    decisions = []
    selected = 0
    for rank, entry in enumerate(candidates, start=1):
        if selected >= cut:
            break
        located = locate(indexes, entry["id"])
        row = {
            "rank": rank,
            "id": entry["id"],
            "version": entry["version"],
            "downloads": entry["totalDownloads"],
            "source": located[0] if located else "",
            "project": located[1] if located else "",
            "decision": "excluded",
            "reason": "",
        }
        if not located:
            row["reason"] = "project not in dotnet/runtime, aspnetcore or efcore at the 10.0 tag"
        elif int(entry["version"].split(".")[0]) < CURRENT_MAJOR:
            row["reason"] = "latest version is not from the current line"
        else:
            files = package_files(entry["id"], entry["version"], cache)
            package_types = [item["name"] for item in entry.get("packageTypes", [])]
            verdict = eligibility(files, package_types)
            if verdict.eligible:
                row["decision"] = "selected"
                selected += 1
            else:
                row["reason"] = verdict.reason
        decisions.append(row)
    return decisions


def write_csv(path: Path, columns: list[str], rows: list[dict]) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=columns, extrasaction="ignore", lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, default=Path("docs"))
    parser.add_argument("--cache", type=Path, default=Path(".work/nuget"))
    parser.add_argument("--cut", type=int, default=CUT)
    parser.add_argument("--mirrors", type=Path, default=Path(".work/repos"))
    arguments = parser.parse_args()
    arguments.out.mkdir(parents=True, exist_ok=True)
    candidates = merged_candidates()
    snapshot = [
        {
            "rank": rank,
            "id": entry["id"],
            "version": entry["version"],
            "downloads": entry["totalDownloads"],
            "owners": ";".join(entry.get("owners", [])),
        }
        for rank, entry in enumerate(candidates, start=1)
    ]
    write_csv(arguments.out / "nuget-snapshot.csv", SNAPSHOT_COLUMNS, snapshot)
    indexes = {
        repository: project_index(
            arguments.mirrors / f"{repository}.git",
            repository,
            latest_tag(arguments.mirrors / f"{repository}.git", CURRENT_MAJOR),
        )
        for repository in REPOSITORIES
    }
    decisions = decide(candidates, indexes, arguments.cache, arguments.cut)
    write_csv(arguments.out / "package-decisions.csv", DECISION_COLUMNS, decisions)
    (arguments.out / "nuget-snapshot.date").write_text(date.today().isoformat() + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
