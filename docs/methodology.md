# Methodology

## Question

Did the hot paths of Microsoft's .NET libraries get faster or slower across three epochs, and can any change be attributed to code written by the GitHub Copilot coding agent rather than by people?

| epoch | points | meaning |
|---|---|---|
| E0 | 6.0.x, **7.0.x (baseline)** | written before ChatGPT (7.0 GA 2022-11-08) |
| E1 | 8.0.x, 9.0.x | the LLM-assistant era, before the coding agent |
| E2 | 10.0.x, 11.0 (RC) | the coding agent opens pull requests in dotnet/runtime from 2025-05-19 |

Exact versions and their sources are in `docs/verified-facts.md` and `matrix/versions.json`.

## Hypotheses

**H1 (observation).** For the measured scopes, the median batch time and the allocated bytes per batch differ between the epochs. H1 is evaluated per scope against the baseline `e0-7` (Track A) or `b-net10` (Track B):

- unit of observation: one process; its value is the median of its batch durations (EmberTrace `P50Ms` of the scope);
- 95% bootstrap confidence interval of the median over processes (10,000 resamples) and of the relative change against the baseline;
- two-sided Mann–Whitney U test against the baseline;
- verdicts: `noise` when |change| < 5%, `inconclusive` when the CI of the change crosses 0 or p ≥ 0.05, otherwise `faster`/`slower` (`analysis/stats.py`, `results/summary/stats.md`).

**H2 (attribution).** Hot methods changed by agent-authored pull requests changed in performance differently from hot methods changed only by people. The design is a difference-in-differences over the hot methods of `glassbox/methods.txt`:

- *treated*: methods touched in the window by at least one `cca` pull request or by a pull request with the `cca_coauthored` flag (`results/summary/attribution-methods.csv`);
- *control*: methods touched in the same window only by `msft-human` or `community` pull requests;
- *pre* period: 7.0 → 9.0 (no coding agent); *post* period: 9.0 → 10.0/11.0 (agent active);
- outcome: the glass-box inclusive time per call of the method (`results/glassbox/<tag>/*.ember`), from separate glass-box builds of each tag;
- estimate: (Δpost − Δpre)treated − (Δpost − Δpre)control, with a permutation test over method labels.

With a few dozen hot methods and few agent PRs in these paths, H2 is expected to be descriptive at best; the report must state the number of treated methods next to any estimate.

**H3 (static metrics).** The per-tag static metrics of the libraries (`results/summary/static-*.csv`: lines in src and tests, method count, cyclomatic complexity, method length, nesting depth, analyzer warnings with `AnalysisLevel=latest-all`) shift between the epochs. Agent-era changes are not separable from human ones here; H3 describes the code base, it does not attribute.

## Rules fixed before looking at the data

- A difference under **5%** counts as noise, whatever its p-value. This threshold applies to every table and plot (grey band in the plots).
- **A null result is an acceptable outcome.** "No difference beyond 5% that survives the CI and the test" is a finding and is reported as such; the study does not go looking for a different metric, subset or threshold until something becomes significant.
- The baseline is fixed: `e0-7` for Track A, `b-net10` for Track B.
- The full matrix is 10 independent processes per point and scenario, 200 measured and 20 warm-up iterations, pinned with `taskset -c 2-5`, on a machine prepared with `docs/stand.md`.
- Results from a run whose `env.json` shows a non-`performance` governor or enabled turbo are labelled as such and not mixed with prepared-machine runs.

## Measurement design

### Track A: package version varies, runtime fixed

The harness (`src/Harness`) always runs on the .NET 10 runtime; only the NuGet packages change (`-p:MsExt=<major> -p:Ef=<major>`). This isolates library code from JIT and GC changes. Every `Microsoft.Extensions.*` package of a point, direct or transitive, has the same major version (central package management with transitive pinning; the full closure is generated into `matrix/versions.props`). At start-up the harness loads the closure of `Microsoft.Extensions.*`, EF Core and `Microsoft.Data.Sqlite` assemblies, prints `AssemblyName.Version` and `AssemblyInformationalVersion` of each, and exits with code 3 if a major differs from the requested one.

Scopes: one EmberTrace scope per batch (10,000 operations for cheap calls, fewer for expensive ones; the scope name carries the batch size, e.g. `di.transient.x10k`), never per call, because a scope costs about as much as two `Stopwatch.GetTimestamp` calls. Each cold phase (first build, first resolve, first EF query, ...) has a single scope, recorded before the warm-up. Allocations per batch are the delta of `GC.GetAllocatedBytesForCurrentThread()` (or of `GC.GetTotalAllocatedBytes(true)` for multi-threaded batches) and are stored in `run-meta.json` next to the scope id. Each scenario runs in its own process, so its cold phases are really cold.

### Track B: library + runtime

`System.Text.Json`, `System.Threading.Channels`, `System.Collections.Immutable` and ASP.NET Core ship in the shared framework, and an older NuGet package loses to the in-box assembly, so their versions cannot be swapped on a fixed runtime. Track B therefore compares net10.0 with net11.0 (`src/Harness.TrackB`), and every Track B number is a statement about library and runtime together. The BenchmarkDotNet suite extends this to net6.0–net9.0 and is labelled "library + JIT" in every table.

### Cross-check

`bench/Bdn` mirrors the operations with BenchmarkDotNet (no EmberTrace). Agreement between the EmberTrace ratios and the BenchmarkDotNet ratios for the same point pair shows that the tracer does not distort the comparison; absolute values are not expected to match (batch vs per-operation timing, different warm-up policy).

### Glass box

`glassbox/` compiles the DI and Caching.Memory sources of two tags (default `v7.0.20` and `v10.0.14`) into renamed assemblies with a scope injected at the start of each hot method, and runs the same DI and cache scenarios against them. It shows where inside the library the time goes, and is the outcome source for H2. The injected scope (`Tracer.Scope(Tracer.Id("Type.Method"))`) hashes the name on every call; this overhead is identical in both builds but inflates absolute times, so only relative comparisons of the glass-box numbers are meaningful.

## Threats to validity

- **Construct.** Batch medians measure throughput of a synthetic workload, not application latency. Scenarios use only APIs present in every version, which excludes the newest fast paths (they are in `--include-new` scenarios and are not compared with E0).
- **Internal.** Track A runs old library builds on a new runtime: 6.x/7.x packages execute their `net6.0`/`net7.0`/`netstandard2.0` assets on .NET 10, so a library may miss runtime-specific fast paths it would use when built for net10; in-box dependencies (`System.Text.Json`, `System.Diagnostics.DiagnosticSource`) are the .NET 10 copies at every point. JIT tiering and PGO add process-to-process variance; `--no-tiering` is a control run. The laptop that produced the smoke data used the `powersave` governor with turbo on; its numbers are not data.
- **External.** Four libraries and EF Core are not "Microsoft's code"; dotnet/runtime has hundreds of libraries and the agent's share of changes differs between them.
- **Attribution.** `author_association` MEMBER means membership of the dotnet GitHub organisation, not Microsoft employment. Agent PRs are reviewed, rebased and finished by people; `cca` means "opened by the agent", not "written without human input". Human PRs may use other AI tools that leave no trace; the Copilot review and CLI co-author trailers are recorded as flags but are not counted as agent work. Squash merges hide intermediate commits; the co-author trailers of the squash commit are the only commit-level signal.
- **Statistical.** Many scopes are tested; no multiplicity correction is applied to the per-scope p-values, which is why the 5% practical threshold and the CI are part of every verdict.

## Deviations

Each deviation from the task brief, with the reason and what was done instead.

1. **EF Core 11 is in Track B, not Track A.** `Microsoft.EntityFrameworkCore` 11.0.0-rc.3.26522.148 only targets `net11.0`, so it cannot run on the net10.0 Track A runtime. Track A has EF Core for 6–10; the `e2-11` point skips the EF scenario (the harness says so and exits 4 when it is requested explicitly). The same EF scenario runs in Track B as EF Core 10 on net10.0 vs EF Core 11 on net11.0, i.e. library + runtime.
2. **11.0 is RC3, not RC2.** NuGet serves `11.0.0-rc.3.26522.148`; in dotnet/runtime and dotnet/efcore the rc.2 and rc.3 tags point at the same commit. The matrix uses rc.3.
3. **SQLitePCLRaw is pinned to 2.1.13 in Track A; EF Core 11 uses 3.0.5.** 2.1.13 is the highest minimum of the Track A EF points (6.0 needs ≥ 2.1.2, 7.0 ≥ 2.1.4, 8.0–10.0 ≥ 2.1.13) and every Track A EF point starts with it. EF Core 11 requires the 3.x line, which restructures the bundle, so Track B uses each EF version's own minimum. The native SQLite in 3.0.5 is newer, which is part of the Track B "library + runtime" confound. The InMemory provider was not needed.
4. **Per-package patch versions.** In 6.x–8.x only changed packages are republished, so "latest patch of the major" differs per package (DI 7.0.0, Binder 7.0.4). The same-major rule holds; `matrix/versions.props` lists every package of every point.
5. **The source-generated configuration binder is measured from 9.0 on.** The 8.0.x binder generator emits file-path `InterceptsLocation` attributes that the C# 14 compiler of the .NET 10 SDK rejects (CS9270). The `binder-sourcegen` scenario is reported as unavailable for 8.0; keyed services are measured from 8.0.
6. **Track B needs the .NET 11 SDK although `global.json` pins 10.0.x.** The muxer would refuse SDK 11 under that `global.json`, so `scripts/dotnet11.sh` runs the RC SDK through `dotnet exec <sdk>/dotnet.dll`, and `Harness.TrackB` adds the `net11.0` target only when the SDK in use is 11 or later (so the solution still builds on SDK 10). Track B runs on `.dotnet/` (runtime 10.0.14 and 11.0.0-rc.3); Track A runs on the `dotnet` on `PATH` (10.0.11 on the development machine). `run-meta.json` records the exact runtime of every process.
7. **BenchmarkDotNet 0.15.8 (latest stable) has no net11.0 moniker** and fails on an unknown runtime even with a custom job or as an out-of-process host (`GetRuntimeVersion not implemented for NotRecognized`). Track B therefore runs net6.0–net10.0 out of process with `--runtimes`, and net11.0 in-process next to an in-process net10.0 bridge run; the net11 comparison uses the bridge, not the out-of-process net10 numbers. Track A BenchmarkDotNet runs in-process, because the child-project rebuild of the out-of-process toolchain would drop `-p:MsExt` and restore the default package versions.
8. **BenchmarkDotNet's own dependencies replace in-box libraries on old runtimes.** Through Roslyn and TraceEvent it pulls `System.Collections.Immutable` 9.0.0 (net6.0–net8.0) and `System.Text.Json` 8.0.5 (net6.0, net7.0). Below net9.0 these packages are excluded (`bench/Bdn/BdnInBox.props`, imported through `Directory.Build.props` because BenchmarkDotNet's generated projects do not import `Directory.Build.targets`), their source generators are removed, and every Track B benchmark process checks that the loaded assemblies are the in-box copies of its runtime (exit 3 otherwise). An in-process run on net6.0–net8.0 is impossible under this rule because BenchmarkDotNet itself needs Immutable 9.
9. **Build settings for deliberately old packages.** `NuGetAudit` is off (6.x/7.x packages have known advisories and are used on purpose in an isolated bench); `CheckEolTargetFramework` is off and `SuppressTfmSupportBuildWarnings` is on (net6.0/net7.0 targets of the BenchmarkDotNet suite).
10. **bombardier.** It is not installed on the development machine and downloading the binary was not permitted there, so the built-in fixed-rate `HttpClient` generator was used and tested (400 requests per batch at 4,000 RPS, at most 32 in flight, one async scope per request = service time, not corrected for coordinated omission; late sends are counted in `*.load.json`). The bombardier path (`-n 400 -r 4000 -c 32 -l -o json` per batch, results in `*.load.json`) is implemented but has not been run.
11. **The "no-op" logging provider formats the message.** It does no I/O but calls the formatter, so the measured path includes `Microsoft.Extensions.Logging`'s template formatting, as any real provider would.
12. **Configuration reload uses a temporary JSON file.** Stream-based providers cannot be reloaded (they throw on a second load), so `config.reload-json2000.x10` reloads the same 2,000-key document from a file in the page cache.
13. **Extra projects.** `src/Harness.Core` (shared runner, recorder, assembly guard, run metadata) and `src/Harness/Scenarios/NewOnly/Harness.NewOnly.csproj` (version-gated scenarios; a separate assembly so the binder generator cannot intercept the reflection-binder scenario) are not in the brief's layout. Track B summaries are named `trackb-<scenario>.*` so that the Track A and Track B EF results do not collide.
14. **Aggregation.** `median_ms` is the median over processes of each process's median batch time; `p95_ms` is the median over processes of each process's 95th percentile. Per-process values are in `<scenario>-runs.csv`, which `stats.py` uses. Chrome JSON exports are written next to the hotspots and collapsed stacks but are not committed (`.gitignore`), because the per-request ASP.NET Core traces are large.
15. **Glass box.** Each library becomes its own renamed assembly (`Glassbox.V7.DependencyInjection.Abstractions`, `Glassbox.V7.DependencyInjection`, `Glassbox.V7.Caching.Abstractions`, `Glassbox.V7.Caching.Memory`) instead of one, because each library has its own `System.SR` and shares Common files. `Options`, `Logging.Abstractions` and `Primitives` come from the NuGet packages of the same major. `MemoryCacheServiceCollectionExtensions.cs` (it bridges to the real `IServiceCollection`) and the type-forward files are excluded. The source set is the `**/*.cs` of the library plus the `$(CommonPath)` items of its project file, evaluated for net10.0 by a small condition evaluator instead of the dotnet/runtime build. Iterator methods are not instrumented (a sync scope would span the whole enumeration); async methods get `ScopeAsync`. Sessions use `EnableRuntimeMetadata = true` so that names registered with `Tracer.Id` resolve. The scope statement calls `Tracer.Id` on every invocation, as specified; EmberTrace recommends caching it.
16. **Attribution without GitHub credentials.** The development machine has no `gh` and no token, so `prs.py` used the unauthenticated REST API (60 requests per hour, cached, waiting for the reset) and took the line statistics of each pull request from `git show --numstat` of its merged commit instead of `pulls/{n}` (`stats_source` says which). With `gh` authenticated or `GH_TOKEN` set, `pulls/{n}` is used automatically.
17. **Attribution classes.** Besides `cca`, `msft-human` (`author_association` MEMBER, OWNER or COLLABORATOR) and `community`, other GitHub apps (backport and dependency bots) get the class `bot`, and commits without a pull request get `no-pr`, rather than being forced into one of the three.
18. **`git log -L :Method:file` does not work for these sources.** Git's built-in C# function-name pattern (git 2.34) rejects declarations with nullable return types such as `ServiceCallSite?`, and `:` inside a POSIX class collides with the `-L` syntax. `methods.py` finds each declaration (all overloads) at the end tag with a regular expression and brace matching and runs `git log -L <first>,<last>:<file>` per overload; `-L` follows the line range back through history but not across file renames.
19. **Servicing window.** 10.0 GA was 2025-11-11, so its 12-month window ends 2026-11-11, after the data was collected. `attribution-regressions.csv` marks the window incomplete and records the days observed; compare rates per month, not totals.
20. **`cloc` was not installed.** `static-metrics/run.sh` uses `cloc` when it is on `PATH` and otherwise counts code, comment and blank lines from Roslyn trivia; the `loc_tool` column records which.
21. **Warnings in static metrics** are counted on the renamed sources compiled for net10.0 against packages of the same major, with `NoWarn` cleared, nullable enabled and `AnalysisLevel=latest-all`, as distinct (file, line, column, id) tuples of the library's own project.
