# Verified facts

Every fact below was checked over the network on 2026-10-29 (UTC+3 local) from the machine that built this repository. Each entry names the source URL or the command that reproduces it. Anything not listed here is a design decision, not a fact, and is described in `docs/methodology.md`.

## F1. Version matrix (Track A and Track B)

Source: `python3.12 scripts/resolve-versions.py`, which reads
`https://api.nuget.org/v3-flatcontainer/{id-lowercase}/index.json` for the version list and
`https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.nuspec` for TFMs and dependencies.
The full result, including every transitive `Microsoft.Extensions.*` and EF Core package, is committed in `matrix/versions.json`; `matrix/versions.props` is generated from it.

Rule: latest stable patch of each major 6–10; for 11 the latest `11.0.0-rc.*`, because no stable `11.0.0` was published at the time of the check.

| point | epoch | DI | Configuration | Configuration.Json | Configuration.Binder | Options | Logging | Caching.Memory | EF Core | EF Core.Sqlite |
|---|---|---|---|---|---|---|---|---|---|---|
| e0-6 | E0 | 6.0.2 | 6.0.2 | 6.0.1 | 6.0.1 | 6.0.1 | 6.0.1 | 6.0.3 | 6.0.36 | 6.0.36 |
| e0-7 | E0 (baseline) | 7.0.0 | 7.0.0 | 7.0.0 | 7.0.4 | 7.0.1 | 7.0.0 | 7.0.0 | 7.0.20 | 7.0.20 |
| e1-8 | E1 | 8.0.1 | 8.0.0 | 8.0.1 | 8.0.2 | 8.0.2 | 8.0.1 | 8.0.1 | 8.0.33 | 8.0.33 |
| e1-9 | E1 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 |
| e2-10 | E2 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 |
| e2-11 | E2 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 (Track B) | 11.0.0-rc.3.26522.148 (Track B) |

Notes:
- In 6.x, 7.x and 8.x, `Microsoft.Extensions.*` packages are only re-published when they change, so the latest patch differs per package (DI 7 is still 7.0.0 while Binder 7 is 7.0.4). From 9.x every package ships with every patch.
- The task brief said 11.0 is at RC2. NuGet already serves `11.0.0-rc.3.26522.148`, and in both `dotnet/runtime` and `dotnet/efcore` the tags `v11.0.0-rc.2.26504.105` and `v11.0.0-rc.3.26522.148` point at the same commit (see F5). The matrix uses rc.3.
- Asset TFM chosen by NuGet for a net10.0 consumer (`net10_asset_tfm` in `matrix/versions.json`): 6.x uses a mix of `net6.0`, `netstandard2.1` and `netstandard2.0` assets (e.g. `Microsoft.Extensions.Primitives` 6.0.0 has a `net6.0` asset, `Microsoft.Extensions.FileSystemGlobbing` 6.0.0 only `netstandard2.0`); 7.x uses `net7.0`; 8.x `net8.0`; 9.x `net9.0`; 10.x and 11.0-rc `net10.0`. So the 11.0-rc point in Track A runs the `net10.0` build of the 11 sources, not the `net11.0` build.

Track B package versions (informational; Track B uses the in-box copies of these assemblies):

| point | System.Text.Json | System.Threading.Channels | System.Collections.Immutable | Microsoft.AspNetCore.App.Ref |
|---|---|---|---|---|
| e0-6 | 6.0.11 | 6.0.0 | 6.0.1 | 6.0.36 |
| e0-7 | 7.0.4 | 7.0.0 | 7.0.0 | 7.0.20 |
| e1-8 | 8.0.6 | 8.0.0 | 8.0.0 | 8.0.33 |
| e1-9 | 9.0.22 | 9.0.22 | 9.0.22 | 9.0.22 |
| e2-10 | 10.0.14 | 10.0.14 | 10.0.14 | 10.0.14 |
| e2-11 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 | 11.0.0-rc.3.26522.148 |

## F2. EF Core target frameworks and whether they run on net10.0

Source: dependency groups of `https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore/{version}/microsoft.entityframeworkcore.nuspec` (recorded under `tfms` in `matrix/versions.json`), plus an actual run of `Harness --scenario ef` on .NET 10.0.11 for every Track A point.

| EF Core | TFMs in nuspec | Runs on net10.0 | Track |
|---|---|---|---|
| 6.0.36 | net6.0 | yes (smoke run passed) | A |
| 7.0.20 | net6.0 | yes (smoke run passed) | A |
| 8.0.33 | net8.0 | yes (smoke run passed) | A |
| 9.0.22 | net8.0 | yes (smoke run passed) | A |
| 10.0.14 | net10.0 | yes (smoke run passed) | A |
| 11.0.0-rc.3.26522.148 | net11.0 | no: no compatible asset for net10.0 | moved to B |

## F3. SQLitePCLRaw pulled in by each EF Core version

Source: `dependency id="SQLitePCLRaw.bundle_e_sqlite3"` in `https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/{version}/microsoft.entityframeworkcore.sqlite.nuspec` (the minimum of the declared range; field `sqlitepclraw_min` in `matrix/versions.json`). Version list: `https://api.nuget.org/v3-flatcontainer/sqlitepclraw.bundle_e_sqlite3/index.json`.

| EF Core.Sqlite | SQLitePCLRaw.bundle_e_sqlite3 (minimum) |
|---|---|
| 6.0.36 | 2.1.2 |
| 7.0.20 | 2.1.4 |
| 8.0.33 | 2.1.13 |
| 9.0.22 | 2.1.13 |
| 10.0.14 | 2.1.13 |
| 11.0.0-rc.3.26522.148 | 3.0.5 (the 3.x bundle depends on `SQLitePCLRaw.config.e_sqlite3` and `SQLite` 3.53.4 instead of `lib.e_sqlite3`) |

Chosen pin for Track A: **2.1.13** for `SQLitePCLRaw.bundle_e_sqlite3`, `SQLitePCLRaw.core`, `SQLitePCLRaw.provider.e_sqlite3` and `SQLitePCLRaw.lib.e_sqlite3`. It is the highest minimum of any Track A point and lies in the same 2.1 line as all of them, so every point starts with it (verified by the EF smoke run of every Track A point). The InMemory fallback was not needed. EF Core 11 in Track B uses its own minimum, 3.0.5.

## F4. EmberTrace

Source: `https://api.nuget.org/v3-flatcontainer/embertrace/index.json` (and the same for every EmberTrace package). Latest stable: **0.4.0**, pinned as `$(EmberTrace)` in `Directory.Packages.props`. `EmberTrace.Format` and `EmberTrace.Testing` exist only from 0.3.0. Every EmberTrace package has only a `net10.0` lib folder (`EmberTrace.Abstractions` also `netstandard2.0`), checked in `~/.nuget/packages/embertrace*/0.4.0/lib/`. `EmberTrace.Extensions.Hosting` and `EmberTrace.All` are published but deliberately not referenced.
