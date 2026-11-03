#!/usr/bin/env python3
import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
from datetime import UTC, datetime
from pathlib import Path

FLAT = "https://api.nuget.org/v3-flatcontainer"
MAJORS = [6, 7, 8, 9, 10, 11]
EPOCHS = {6: "E0", 7: "E0", 8: "E1", 9: "E1", 10: "E2", 11: "E2"}
RUNTIME_TFM = {"track_a": 10, "track_b": 11}

TRACK_A_ROOTS = [
    "Microsoft.Extensions.DependencyInjection",
    "Microsoft.Extensions.Configuration",
    "Microsoft.Extensions.Configuration.Json",
    "Microsoft.Extensions.Configuration.Binder",
    "Microsoft.Extensions.Options",
    "Microsoft.Extensions.Options.ConfigurationExtensions",
    "Microsoft.Extensions.Options.DataAnnotations",
    "Microsoft.Extensions.Logging",
    "Microsoft.Extensions.Caching.Memory",
]
EF_ROOTS = [
    "Microsoft.EntityFrameworkCore",
    "Microsoft.EntityFrameworkCore.Sqlite",
    "Microsoft.EntityFrameworkCore.InMemory",
]
TRACK_B_PACKAGES = [
    "System.Text.Json",
    "System.Threading.Channels",
    "System.Collections.Immutable",
    "Microsoft.AspNetCore.App.Ref",
]
MSEXT_PREFIX = "Microsoft.Extensions."
EF_PREFIXES = ("Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite")
SQLITE_BUNDLE = "SQLitePCLRaw.bundle_e_sqlite3"


def fetch(url: str, attempts: int = 10) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": "dotnet-ai-era-benchmarks-resolver"})
    for attempt in range(1, attempts + 1):
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                return response.read()
        except urllib.error.HTTPError:
            raise
        except (urllib.error.URLError, TimeoutError, ConnectionError):
            if attempt == attempts:
                raise
            time.sleep(min(30, 3 * attempt))
    raise RuntimeError(url)


def version_key(version: str) -> tuple:
    release, _, pre = version.partition("-")
    numbers = tuple(int(part) for part in release.split("."))
    if not pre:
        return numbers, 1, ()
    identifiers = tuple((0, int(p), "") if p.isdigit() else (1, 0, p) for p in pre.split("."))
    return numbers, 0, identifiers


def all_versions(package_id: str) -> list[str]:
    data = json.loads(fetch(f"{FLAT}/{package_id.lower()}/index.json"))
    return sorted(data["versions"], key=version_key)


def pick_version(versions: list[str], major: int) -> str | None:
    stable = [v for v in versions if "-" not in v and v.split(".")[0] == str(major)]
    if stable:
        return stable[-1]
    if major == max(MAJORS):
        candidates = [v for v in versions if v.startswith(f"{major}.0.0-rc.")]
        if candidates:
            return candidates[-1]
    return None


def nuspec(package_id: str, version: str, cache_dir: Path) -> ET.Element:
    lower = package_id.lower()
    cached = cache_dir / f"{lower}.{version.lower()}.nuspec"
    if not cached.exists():
        cache_dir.mkdir(parents=True, exist_ok=True)
        cached.write_bytes(fetch(f"{FLAT}/{lower}/{version.lower()}/{lower}.nuspec"))
    return ET.fromstring(cached.read_bytes())


def strip_ns(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def dependency_groups(root: ET.Element) -> dict[str, list[tuple[str, str]]]:
    groups: dict[str, list[tuple[str, str]]] = {}
    for element in root.iter():
        if strip_ns(element.tag) != "group":
            continue
        tfm = normalize_tfm(element.get("targetFramework", "any"))
        deps = [(d.get("id", ""), d.get("version", "")) for d in element if strip_ns(d.tag) == "dependency"]
        groups[tfm] = deps
    return groups


def normalize_tfm(raw: str) -> str:
    value = raw.strip().lower()
    match = re.fullmatch(r"\.?netcoreapp(\d+)\.(\d+)", value)
    if match:
        major = int(match.group(1))
        return f"net{major}.{match.group(2)}" if major >= 5 else f"netcoreapp{major}.{match.group(2)}"
    match = re.fullmatch(r"net(\d+)\.(\d+)", value)
    if match:
        return value
    match = re.fullmatch(r"\.?netstandard(\d+)\.(\d+)", value)
    if match:
        return f"netstandard{match.group(1)}.{match.group(2)}"
    match = re.fullmatch(r"\.?netframework(\d)\.(\d)(?:\.(\d))?", value)
    if match:
        return "net" + "".join(g for g in match.groups() if g)
    return value


def nearest_group(groups: dict[str, list], runtime_major: int) -> str | None:
    def net_major(tfm: str) -> int | None:
        match = re.fullmatch(r"net(\d+)\.\d+", tfm)
        return int(match.group(1)) if match else None

    modern = [(net_major(t), t) for t in groups if net_major(t) is not None]
    compatible = [pair for pair in modern if pair[0] <= runtime_major]
    if compatible:
        return max(compatible)[1]
    for fallback in ("netcoreapp3.1", "netstandard2.1", "netstandard2.0", "any"):
        if fallback in groups:
            return fallback
    return None


def range_floor(spec: str) -> str:
    return spec.strip("[]() ").split(",")[0].strip()


class Resolver:
    def __init__(self, cache_dir: Path) -> None:
        self.cache_dir = cache_dir
        self.version_cache: dict[str, list[str]] = {}
        self.nuspec_cache: dict[tuple[str, str], dict[str, list[tuple[str, str]]]] = {}

    def versions(self, package_id: str) -> list[str]:
        if package_id not in self.version_cache:
            self.version_cache[package_id] = all_versions(package_id)
        return self.version_cache[package_id]

    def groups(self, package_id: str, version: str) -> dict[str, list[tuple[str, str]]]:
        key = (package_id, version)
        if key not in self.nuspec_cache:
            self.nuspec_cache[key] = dependency_groups(nuspec(package_id, version, self.cache_dir))
        return self.nuspec_cache[key]

    def family_closure(self, roots: list[str], major: int, runtime_major: int, in_family) -> dict[str, dict]:
        resolved: dict[str, dict] = {}
        pending = list(roots)
        while pending:
            package_id = pending.pop()
            if package_id in resolved:
                continue
            version = pick_version(self.versions(package_id), major)
            if version is None:
                resolved[package_id] = {"version": None}
                continue
            groups = self.groups(package_id, version)
            selected = nearest_group(groups, runtime_major)
            deps = groups.get(selected, []) if selected else []
            resolved[package_id] = {
                "version": version,
                "tfms": sorted(groups),
                "asset_tfm": selected,
                "dependencies": {dep_id: range_floor(spec) for dep_id, spec in deps},
            }
            pending.extend(dep_id for dep_id, _ in deps if in_family(dep_id))
        return resolved


def sqlite_minimum(ef_packages: dict[str, dict]) -> str | None:
    floors = [info["dependencies"][SQLITE_BUNDLE] for info in ef_packages.values() if info.get("version") and SQLITE_BUNDLE in info.get("dependencies", {})]
    return max(floors, key=version_key) if floors else None


def is_msext(package_id: str) -> bool:
    return package_id.startswith(MSEXT_PREFIX)


def is_ef(package_id: str) -> bool:
    return package_id.startswith(EF_PREFIXES)


def build_point(resolver: Resolver, major: int) -> dict:
    runtime = RUNTIME_TFM["track_a"]
    ef = resolver.family_closure(EF_ROOTS, major, runtime, is_ef)
    ef_main = ef["Microsoft.EntityFrameworkCore"]
    ef_track = "A" if ef_main.get("asset_tfm") else "B"
    if ef_track == "B":
        ef = resolver.family_closure(EF_ROOTS, major, RUNTIME_TFM["track_b"], is_ef)
        ef_main = ef["Microsoft.EntityFrameworkCore"]
    msext_roots = list(TRACK_A_ROOTS)
    for info in ef.values():
        msext_roots.extend(dep for dep in info.get("dependencies", {}) if is_msext(dep))
    msext = resolver.family_closure(msext_roots, major, runtime, is_msext)
    msext_versions = sorted({i["version"] for i in msext.values() if i["version"]}, key=version_key)
    track_b = {}
    for package_id in TRACK_B_PACKAGES:
        version = pick_version(resolver.versions(package_id), major)
        groups = resolver.groups(package_id, version) if version else {}
        track_b[package_id] = {"version": version, "tfms": sorted(groups)}
    merged = dict(sorted({**msext, **ef}.items()))
    return {
        "label": f"e{EPOCHS[major][1]}-{major}",
        "epoch": EPOCHS[major],
        "major": major,
        "msext": f"{major}.0.x",
        "msext_max": msext_versions[-1],
        "ef": ef_main["version"] if ef_track == "A" else None,
        "ef_package_version": ef_main["version"],
        "ef_track": ef_track,
        "ef_asset_tfm": ef_main.get("asset_tfm"),
        "sqlitepclraw_min": sqlite_minimum(ef),
        "tfms": {pid: info.get("tfms", []) for pid, info in merged.items()},
        "net10_asset_tfm": {pid: info.get("asset_tfm") for pid, info in msext.items()},
        "packages": {pid: info["version"] for pid, info in merged.items()},
        "track_b": track_b,
    }


def choose_sqlite(resolver: Resolver, points: list[dict]) -> dict:
    floors = {p["label"]: p["sqlitepclraw_min"] for p in points if p["ef_track"] == "A"}
    highest = max(floors.values(), key=version_key)
    stable = [v for v in resolver.versions(SQLITE_BUNDLE) if "-" not in v]
    return {
        "track_a_minimum_per_point": floors,
        "track_a_pinned": highest,
        "track_a_single_minor_line": len({".".join(v.split(".")[:2]) for v in floors.values()}) == 1,
        "track_b_minimum_per_point": {p["label"]: p["sqlitepclraw_min"] for p in points if p["ef_track"] == "B"},
        "latest_stable": stable[-1],
        "rule": "Track A pins the highest minimum required by any Track A EF point",
    }


def sqlite_items(version: str) -> list[str]:
    family = ["SQLitePCLRaw.bundle_e_sqlite3", "SQLitePCLRaw.core"]
    if version.startswith("2."):
        family += ["SQLitePCLRaw.provider.e_sqlite3", "SQLitePCLRaw.lib.e_sqlite3"]
    return [f'    <PackageVersion Include="{pid}" Version="{version}" />' for pid in family]


def write_props(document: dict, path: Path) -> None:
    pinned = document["sqlitepclraw"]["track_a_pinned"]
    track_a = ";".join(str(p["major"]) for p in document["points"] if p["ef_track"] == "A")
    track_b = ";".join(str(p["major"]) for p in document["points"] if p["ef_track"] == "B")
    lines = [
        "<Project>",
        "  <PropertyGroup>",
        f"    <MatrixMajors>;{';'.join(str(p['major']) for p in document['points'])};</MatrixMajors>",
        f"    <MatrixTrackAEf>;{track_a};</MatrixTrackAEf>",
        f"    <MatrixTrackBEf>;{track_b};</MatrixTrackBEf>",
        f"    <MatrixSqlitePclRawTrackA>{pinned}</MatrixSqlitePclRawTrackA>",
        "  </PropertyGroup>",
    ]
    for point in document["points"]:
        lines.append(f"  <ItemGroup Condition=\"'$(MsExt)' == '{point['major']}'\">")
        for package_id, version in point["packages"].items():
            if version and is_msext(package_id):
                lines.append(f'    <PackageVersion Include="{package_id}" Version="{version}" />')
        lines.append("  </ItemGroup>")
        lines.append(f"  <ItemGroup Condition=\"'$(Ef)' == '{point['major']}'\">")
        for package_id, version in point["packages"].items():
            if version and is_ef(package_id):
                lines.append(f'    <PackageVersion Include="{package_id}" Version="{version}" />')
        sqlite = pinned if point["ef_track"] == "A" else point["sqlitepclraw_min"]
        lines.extend(sqlite_items(sqlite))
        lines.append("  </ItemGroup>")
    lines.append("</Project>")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Resolve the NuGet version matrix")
    parser.add_argument("--out", type=Path, default=Path("matrix/versions.json"))
    parser.add_argument("--props", type=Path, default=Path("matrix/versions.props"))
    parser.add_argument("--nuspec-cache", type=Path, default=Path(".work/nuspec-cache"))
    parser.add_argument("--props-only", action="store_true", help="regenerate the props file from the JSON")
    args = parser.parse_args()
    if args.props_only:
        write_props(json.loads(args.out.read_text(encoding="utf-8")), args.props)
        return 0
    resolver = Resolver(args.nuspec_cache)
    points = [build_point(resolver, major) for major in MAJORS]
    missing = [(p["label"], pid) for p in points for pid, v in p["packages"].items() if v is None]
    document = {
        "generated_utc": datetime.now(UTC).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "source": FLAT,
        "baseline": "e0-7",
        "sqlitepclraw": choose_sqlite(resolver, points),
        "missing": [{"label": label, "package": pid} for label, pid in missing],
        "points": points,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    write_props(document, args.props)
    for point in points:
        print(f"{point['label']}: msext<= {point['msext_max']} ef={point['ef_package_version']} track={point['ef_track']} sqlite>={point['sqlitepclraw_min']}")
    print(f"sqlitepclraw track A pinned: {document['sqlitepclraw']['track_a_pinned']}")
    for label, pid in missing:
        print(f"missing: {label} {pid}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
