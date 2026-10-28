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

## F5. Tag formats of dotnet/runtime and dotnet/efcore

Source: `git ls-remote --tags https://github.com/dotnet/runtime` and `git ls-remote --tags https://github.com/dotnet/efcore` (and the blobless mirrors in `.work/repos/`).

- Releases are tagged `v<major>.<minor>.<patch>`, previews and release candidates `v<version>-<label>.<n>.<build>`, e.g. `v7.0.20`, `v10.0.14`, `v11.0.0-rc.3.26522.148`. The same tag names exist in both repositories.
- Latest tag per major (runtime = efcore): `v6.0.36`, `v7.0.20`, `v8.0.33`, `v9.0.22`, `v10.0.14`, `v11.0.0-rc.3.26522.148`.
- `v11.0.0-rc.2.26504.105` and `v11.0.0-rc.3.26522.148` point at the same commit: `474ed963699fc29809015392738a15231aaae2de` in runtime, `f05a97f83d8fe43366d92d561cce59f02c6dd102` in efcore.
- dotnet/efcore additionally carries `v10.0.104`, `v10.0.105` (same commit `9dae88ee8afe88ac03286758b568d628fe05e5c6`) and `v11.0.100`; they are SDK-band style names that do not follow the library tag scheme and are not used.
- `v7.0.20` in runtime is `0fb6ac59fb1edbe4ed3ad62661df0eb7eacd7903`, `v10.0.14` is `6aa72daa24f66c01c3d7b6a32519e9d21396deec`.

## F6. How Copilot coding agent pull requests are detected

Sources and commands:

1. `GET https://api.github.com/search/issues?q=repo:dotnet/runtime+is:pr+author:app/copilot-swe-agent&sort=created&order=asc` (the `gh search prs --repo dotnet/runtime --author app/copilot-swe-agent` qualifier works). Total: 1,734 PRs, 1,205 merged (`+is:merged`). The five oldest:
   | PR | created | title |
   |---|---|---|
   | [#115730](https://github.com/dotnet/runtime/pull/115730) | 2025-05-19T17:16:02Z | Fix CancellationTokenRegistration leak in AsyncOperation.TrySetCanceled |
   | [#115732](https://github.com/dotnet/runtime/pull/115732) | 2025-05-19T17:53:56Z | Fix DataTable.Compute throws exception on "true NOT= false" |
   | [#115733](https://github.com/dotnet/runtime/pull/115733) | 2025-05-19T17:58:31Z | Fix IndexOutOfRangeException in RegexInterpreter.Backtrack method |
   | [#115735](https://github.com/dotnet/runtime/pull/115735) | 2025-05-19T18:10:26Z | Fix HTTP/2 pings not working with zero connection lifetime |
   | [#115736](https://github.com/dotnet/runtime/pull/115736) | 2025-05-19T19:13:29Z | [Failed] Port System.Text documentation for .NET 8.0 APIs |
2. On these PRs the author is `user.login = "Copilot"`, `user.type = "Bot"`, `user.id = 198982749`, `author_association = "CONTRIBUTOR"`.
3. `GET https://api.github.com/user/198982749` resolves to `https://github.com/apps/copilot-swe-agent`.
4. dotnet/efcore: the same query returns 302 PRs, the first being [#36136](https://github.com/dotnet/efcore/pull/36136) (2025-05-24).
5. In git, the squash commit of an agent PR has author `Copilot <198982749+Copilot@users.noreply.github.com>`, committer `GitHub <noreply@github.com>`, and the trailer `Co-authored-by: copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>` (e.g. `3c5f74af89e331a5474025ce56d146ee180e1887`, #115735). Command: `git log -1 --format='%an <%ae>%n%B' 3c5f74af89e3`.
6. Other Copilot co-author trailers exist and are **not** the coding agent: `175728472+Copilot@users.noreply.github.com` = `https://github.com/apps/copilot-pull-request-reviewer` (accepted review suggestions, often "Copilot Autofix powered by AI"), `223556219+Copilot@users.noreply.github.com` = `https://github.com/apps/github-copilot-cli`. Sources: `GET https://api.github.com/user/175728472`, `GET https://api.github.com/user/223556219`; counts across all branches since 2025-05-01: `git log --all --since=2025-05-01 --format=%b | grep -i '^co-authored-by:.*copilot' | sort | uniq -c`.

Detection rule used by `attribution/classify.py`: class `cca` when the PR author id is 198982749 (login `Copilot`, type `Bot`), or, for a commit without a PR, when the commit author e-mail is `198982749+Copilot@users.noreply.github.com`. Flag `cca_coauthored` when a non-agent commit carries a co-author trailer with id 198982749. Ids 175728472 and 223556219 produce the separate flags `copilot_review_coauthored` and `copilot_cli_coauthored`.

## F7. Labels

Source: `GET https://api.github.com/repos/dotnet/runtime/labels/tenet-performance` ("Performance related issue") and `.../labels/regression-from-last-release` (no description). Both exist.

## F8. Release dates and support phase

Source: `https://builds.dotnet.microsoft.com/dotnet/release-metadata/<major>.0/releases.json` (fields `release-date`, `latest-release`, `support-phase`).

| major | GA (`<major>.0.0` release-date) | latest | support phase |
|---|---|---|---|
| 6.0 | 2021-11-08 | 6.0.36 | eol |
| 7.0 | 2022-11-08 | 7.0.20 (2024-05-28) | eol |
| 8.0 | 2023-11-14 | 8.0.33 (2026-10-25) | maintenance |
| 9.0 | 2024-12-03 in the feed (the public launch was 2024-11-12) | 9.0.22 (2026-10-25) | maintenance |
| 10.0 | 2025-11-11 | 10.0.14 (2026-10-25) | active |
| 11.0 | not released | 11.0.0-rc.3 (2026-10-25) | go-live |

`attribution/regressions.py` uses these dates for the 12-month servicing windows (7.0: 2022-11-08 → 2023-11-08; 10.0: 2025-11-11 → 2026-11-11, still open at collection time).

## F9. Toolchain and package versions

| item | version | source |
|---|---|---|
| .NET SDK (Track A, `global.json` 10.0.100 + latestFeature) | 10.0.111 | `dotnet --list-sdks` |
| .NET runtime used by Track A here | 10.0.11 | `dotnet --list-runtimes`, `run-meta.json` |
| .NET 11 SDK installed by `scripts/ensure-sdk11.sh` | 11.0.100-rc.3.26522.148 (runtime 11.0.0-rc.3.26522.148) | `dotnet-install.sh --channel 11.0 --quality preview` |
| runtimes for BenchmarkDotNet Track B | 6.0.36, 7.0.20, 8.0.33, 9.0.22, 10.0.14 (+ ASP.NET Core) | `scripts/ensure-runtimes.sh`, `.dotnet/dotnet --list-runtimes` |
| BenchmarkDotNet | 0.15.8 (no net11.0 moniker; its CLI accepts up to `net10_0`) | `https://api.nuget.org/v3-flatcontainer/benchmarkdotnet/index.json`; error message of `--runtimes net11.0` |
| Microsoft.CodeAnalysis.CSharp | 5.9.0 | NuGet flat container |
| System.CommandLine | 2.0.14 | NuGet flat container |
| numpy / pandas / scipy / matplotlib | 2.5.4 / 3.1.0 / 1.18.1 / 3.11.2 | `https://pypi.org/pypi/<name>/json` |
| GitHub Actions | checkout v7 (7.0.1), setup-dotnet v6 (6.0.0), setup-python v7 (7.1.0), upload-artifact v7 (7.0.2) | `git ls-remote --tags https://github.com/actions/<name>` |
| bombardier | v2.0.2 is the latest release, not installed | `GET https://api.github.com/repos/codesenberg/bombardier/releases/latest` |
| git | 2.34.1 | `git --version` |

## F10. Facts found while building that affect the design

- On a net10.0 consumer the 6.x packages resolve to a mix of `net6.0`, `netstandard2.1` and `netstandard2.0` assets; 7.x to `net7.0`; 8.x to `net8.0`; 9.x to `net9.0`; 10.x and 11.0-rc to `net10.0` (`net10_asset_tfm` in `matrix/versions.json`).
- `Microsoft.Extensions.Configuration.Binder` 8.0.x's source generator emits file-path `InterceptsLocation`, which the .NET 10 SDK compiler rejects with CS9270 (observed when building `Harness.NewOnly` with `-p:MsExt=8`).
- BenchmarkDotNet 0.15.8 depends on `Microsoft.CodeAnalysis.CSharp` 4.14.0 (→ `System.Collections.Immutable` 9.0.0, `System.Reflection.Metadata` 9.0.0) and `Microsoft.Diagnostics.Tracing.TraceEvent` 3.1.21 (→ `System.Text.Json` 8.0.5); on net6.0–net8.0 these replace the in-box assemblies unless excluded (`project.assets.json` of `bench/Bdn/Bdn.TrackB`).
- EmberTrace: names registered with `Tracer.Id` resolve in a session only with `SessionOptions.EnableRuntimeMetadata = true` (XML docs of `EmberTrace` 0.4.0); `Tracer.Id` hashes the string on every call.
