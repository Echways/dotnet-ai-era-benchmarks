import argparse
import csv
from pathlib import Path

PACKAGE_COLUMNS = ["id", "repo", "source_dir", "tests_dir"]
RULE = Path(__file__).with_name("rule.md")
OPERATIONS_INTRO = Path(__file__).with_name("operations-intro.md")


def selected(decisions: Path) -> list[dict]:
    with decisions.open(encoding="utf-8") as handle:
        return [row for row in csv.DictReader(handle) if row["decision"] == "selected"]


def source_directory(project: str) -> str:
    return project.rpartition("/")[0]


def tests_directory(source_dir: str) -> str:
    parent, _, leaf = source_dir.rpartition("/")
    return f"{parent}/tests" if leaf == "src" else ""


def write_packages(rows: list[dict], path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.writer(handle, delimiter="\t", lineterminator="\n")
        writer.writerow(PACKAGE_COLUMNS)
        for row in rows:
            source_dir = source_directory(row["project"])
            writer.writerow([row["id"], row["source"], source_dir, tests_directory(source_dir)])


def read_tsv(path: Path) -> list[dict]:
    with path.open(encoding="utf-8") as handle:
        return list(csv.DictReader(handle, delimiter="\t"))


def graph_size(graphs: Path, package: str) -> str:
    path = graphs / package / "methods.tsv"
    if not path.exists():
        return ""
    with path.open(encoding="utf-8") as handle:
        return str(sum(1 for _ in handle) - 1)


def render(decisions: Path, operations: Path, not_traceable: Path, snapshot_date: str, snapshot_rows: int, graphs: Path, graph_tag: str) -> str:
    with decisions.open(encoding="utf-8") as handle:
        rows = list(csv.DictReader(handle))
    chosen = [row for row in rows if row["decision"] == "selected"]
    excluded = [row for row in rows if row["decision"] == "excluded"]
    flagged = {row["package"]: row["reason"] for row in read_tsv(not_traceable)}
    by_package: dict[str, list[dict]] = {}
    for row in read_tsv(operations):
        by_package.setdefault(row["package"], []).append(row)
    last_rank = rows[-1]["rank"]
    lines = (
        RULE.read_text(encoding="utf-8").format(snapshot_date=snapshot_date, snapshot_rows=snapshot_rows, last_rank=last_rank, graph_tag=graph_tag).splitlines()
    )
    lines += [
        "| rank | package | version | downloads | repository | operations | methods | note |",
        "|---:|---|---|---:|---|---:|---:|---|",
    ]
    for row in chosen:
        package = row["id"]
        note = f"not traceable: {flagged[package]}" if package in flagged else ""
        cells = [
            row["rank"],
            package,
            row["version"],
            f"{int(row['downloads']):,}",
            row["source"],
            len(by_package.get(package, [])),
            graph_size(graphs, package),
            note,
        ]
        lines.append("| " + " | ".join(str(cell) for cell in cells) + " |")
    lines += ["", "## Excluded before the cut", "", "| rank | package | version | downloads | reason |", "|---:|---|---|---:|---|"]
    for row in excluded:
        lines.append(f"| {row['rank']} | {row['id']} | {row['version']} | {int(row['downloads']):,} | {row['reason']} |")
    lines += OPERATIONS_INTRO.read_text(encoding="utf-8").splitlines()
    for row in chosen:
        for operation in by_package.get(row["id"], []):
            entries = ", ".join(f"`{entry}`" for entry in operation["entries"].split(";"))
            lines.append(f"| {row['id']} | {operation['operation']} | {entries} | {operation['description']} |")
    return "\n".join(lines) + "\n"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--decisions", type=Path, default=Path("docs/package-decisions.csv"))
    parser.add_argument("--packages", type=Path, default=Path("glassbox/packages.tsv"))
    parser.add_argument("--operations", type=Path, default=Path("docs/entry-operations.tsv"))
    parser.add_argument("--not-traceable", type=Path, default=Path("selection/not-traceable.tsv"))
    parser.add_argument("--snapshot", type=Path, default=Path("docs/nuget-snapshot.csv"))
    parser.add_argument("--graphs", type=Path, default=Path("callgraph"))
    parser.add_argument("--graph-tag", default="v10.0.14")
    parser.add_argument("--out", type=Path, default=Path("docs/selection.md"))
    arguments = parser.parse_args()
    write_packages(selected(arguments.decisions), arguments.packages)
    snapshot_date = arguments.snapshot.with_suffix(".date").read_text(encoding="utf-8").strip()
    snapshot_rows = sum(1 for _ in arguments.snapshot.open(encoding="utf-8")) - 1
    text = render(
        arguments.decisions,
        arguments.operations,
        arguments.not_traceable,
        snapshot_date,
        snapshot_rows,
        arguments.graphs / arguments.graph_tag,
        arguments.graph_tag,
    )
    arguments.out.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
