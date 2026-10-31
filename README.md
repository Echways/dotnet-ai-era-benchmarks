# Microsoft's .NET libraries before and after AI

Did Microsoft's code get worse with AI? This repository measures performance, the part of that question that can be measured: 46 operations in DI, configuration, options, logging, caching and EF Core, across six major versions from 6.0 to 11.0 RC3, over 30 rounds. Git history shows who changed what, and the two main regressions in 11.0 RC3 are traced to specific commits.

Timing is recorded with [EmberTrace](https://github.com/Echways/EmberTrace), a .NET tracer by the same author.

The write-up is in [`habr/article.md`](habr/article.md) (in Russian). The full tables are in [`docs/report.md`](docs/report.md).

## Findings

Unless noted, the numbers come from the tuned machine state described below and compare against 7.0, the last release written before ChatGPT.

**Warmed-up code got faster, first use got slower.** Of the 46 operations at 10.0, 9 are faster, 13 slower, 10 within noise and 14 have no verdict.

| operation | 7.0 | 10.0 |
| --- | --- | --- |
| `LoggerMessage.Define`, one call | 377 ns | 244 ns |
| `MemoryCache.TryGetValue`, hit | 111 ns | 96 ns |
| EF `DetectChanges`, 10,000 entities | 6.1 ms | 4.3 ms |
| DI, first resolve of a depth-5 graph | 3.3 µs | 5.2 µs |
| provider build with `ValidateOnBuild`, 100 registrations | 40 µs | 66 µs |
| new `DbContext` | 37 µs | 51 µs |

**The biggest shift came at 8.0.** Confirmed changes per release step: 19 of 46 at 8.0, 5 at 9.0, 5 at 10.0 and 6 of 38 at 11.0 RC3. This does not say why: each package major also brings its own target framework build.

**The largest regression in 11.0 RC3 is one line, in a pull request by a person.** The `IsDefined` check added for `[ConfigurationIgnore]` in [#126396](https://github.com/dotnet/runtime/pull/126396) runs for every property on every bind. Binding a 50-property POCO went from 61 to 145 µs, and `IOptionsSnapshot` in a new scope from 52 to 136 µs. Replacing that expression with `false` brings both back to the 10.0 level. Reported as [#136759](https://github.com/dotnet/runtime/issues/136759).

**The second regression came from the Copilot coding agent, and it is a trade-off.** [#124331](https://github.com/dotnet/runtime/pull/124331) fixes a deadlock on circular factory registrations and adds about 0.9 µs to the first resolve in a new provider (+16.5%, 8 rounds). Reverting only that commit on RC3 returns the time to the 10.0 level.

**The agent shows up in these paths only after 10.0.** In DI and `Caching.Memory`, none of the 113 commits between v7.0.20 and v10.0.14 came from the agent; between 10.0 and RC3 it has 12 of 35. EF Core has the most agent commits (73 of 256 in the same window) and has not been examined for their effect.

**The tuned machine state was almost twice as slow.** With `no_turbo` the cores stay at their base 2 GHz, and all 46 baseline operations run 1.6 to 2.1 times slower than on stock settings. Relative changes move too: DI and `IOptionsMonitor` at 10.0 are 3–8% faster when tuned and 15–20% faster on stock settings.

## Method

| epoch | versions | why |
| --- | --- | --- |
| E0 | 6.0, **7.0** (baseline) | written before ChatGPT (2022-11-30) |
| E1 | 8.0, 9.0 | AI assistants, no coding agent |
| E2 | 10.0, 11.0 RC3 | the coding agent opens pull requests in dotnet/runtime from 2025-05-19 |

- **Track A (primary): packages change, the runtime does not.** `src/Harness` runs on .NET 10 and swaps the NuGet packages of `Microsoft.Extensions.*` and EF Core with SQLite. EF Core stops at 10, because EF Core 11 requires net11.0. The harness exits with code 3 if a loaded assembly has another major version.
- **Track B: library and runtime together.** `System.Text.Json`, `Channels`, `Immutable`, ASP.NET Core and EF Core are compared as net10.0 against net11.0.
- **Measurement.** One EmberTrace scope per batch, 20 warm-up and 200 measured iterations, each scenario in its own process pinned to cores 2–5, 111 processes per point. The order of the versions is shuffled every round.
- **Verdicts.** Changes are computed inside a round and every round counts equally. A change is `faster` or `slower` only if its whole 95% CI lies beyond ±5%, no round points the other way and the adjusted p is below 0.05. If the whole CI lies inside ±5% it is noise; anything else has no verdict.
- **Machine.** One laptop (Intel Core i5-12450H, Ubuntu 22.04) in two states: tuned (`performance` governor, turbo off, 19 rounds) and stock (`powersave`, turbo on, 11 rounds).

The design, hypotheses and every deviation from the plan are in [`docs/methodology.md`](docs/methodology.md). External facts and their sources are in [`docs/verified-facts.md`](docs/verified-facts.md).

## Limits

- It is a laptop with a hybrid CPU and no core isolation. Ratios inside a round are the evidence, not absolute times.
- EmberTrace is the author's own tool. BenchmarkDotNet was run on two operations only and confirms the RC regression, but on small changes it differs (10.0 against 7.0: −8% and −9% against −1% and −3%). For the other 44 operations tracer distortion has not been estimated.
- The verdict rules were tightened after the first rounds were seen. The original rule is kept as `prespecified_verdict` in `stats.csv`.
- The scenarios are synthetic batches of APIs available in every version, so the newest fast paths are excluded. Old packages run on a new runtime and may miss its optimisations.
- `cca` means "pull request opened by the agent", not "written without a person". An assistant in an IDE leaves no trace in git, so the data says nothing about it.
- 11.0 RC3 is unreleased, and Track A runs its `net10.0` build. Numbers may differ on net11.0 and in the final release.

## Repository layout

| path | contents |
| --- | --- |
| `src/Harness`, `src/Harness.TrackB` | the measured scenarios of both tracks |
| `src/Compare` | turns `.ember` traces into per-scope statistics, diffs, hotspots and collapsed stacks |
| `analysis/` | confidence intervals, Mann–Whitney U, verdicts, plots |
| `experiments/` | scripts that locate the 11.0 RC regressions by rebuilding a single library, see its [README](experiments/README.md) |
| `glassbox/` | DI and Caching.Memory compiled from two tags, with scopes injected into internal methods |
| `attribution/` | maps commits between tags to pull requests and classifies them as `cca` (agent), `msft-human` or `community` |
| `static-metrics/` | per-tag line counts, cyclomatic complexity, method length, nesting, analyzer warnings |
| `bench/Bdn` | BenchmarkDotNet mirror of both tracks, used only for the two-operation check |
| `habr/` | the article, its figures and the scripts that build them |
| `results/summary/` | collected results |

## Running

You need Linux, the .NET 10 SDK on `PATH`, Python 3.12 and git. The scripts install the .NET 11 SDK and the older runtimes into `.dotnet/`, and the Python packages into `.venv/`.

```bash
./scripts/run-matrix.sh --quick          # smoke test: e0-7 and e2-10, 2 processes, 20 iterations, plus Track B
./scripts/setup-stand.sh                 # print the machine setup commands; --apply runs them, --restore undoes them
./scripts/run-matrix.sh --runs 10        # full matrix: 6 Track A points and Track B, 10 processes, 200 iterations
./glassbox/run.sh v7.0.20 v10.0.14       # hotspots and flame graphs for two tags
./attribution/run.sh --library all       # set GH_TOKEN or log in with gh for 5,000 requests/hour
./static-metrics/run.sh                  # static metrics for the latest tag of every major
```

`run-matrix.sh` also accepts `--points e0-7,e2-10`, `--iterations`, `--warmup`, `--track a|b|all`, `--cpus 2-5`, `--no-tiering`, `--include-new` (keyed services and the source-generated binder, not comparable with E0), `--raw DIR` and `--summary DIR`. The machine checklist is in [`docs/stand.md`](docs/stand.md).

The published results were collected with `scripts/run-rounds.sh`, which repeats the matrix until stopped, shuffles the order of the Track A points every round and rebuilds the accumulated summary after each one:

```bash
mkdir -p results/rounds
setsid nohup ./scripts/run-rounds.sh --runs 3 >> results/rounds/run-rounds.log 2>&1 &
```

The same loop runs in Docker with `docker compose up -d --build`, keeping its data in the `bench-data` volume. A container cannot apply `docs/stand.md`, so check `env.json` and the spread between rounds before mixing these numbers with data from a prepared machine.

Tests:

```bash
dotnet run --project tests/Compare.Tests/Compare.Tests.csproj -c Release
.venv/bin/python -m pip install -r analysis/requirements-dev.txt && .venv/bin/python -m pytest analysis
```

## Result files

`results/summary/stats.md` pools both machine states, so its numbers differ from the tuned numbers above. The per-state tables are in section 5 of the appendix in [`docs/report.md`](docs/report.md).

| path in `results/summary/` | contents |
| --- | --- |
| `stats.md`, `stats.csv` | verdicts, confidence intervals and p-values for every scope and point |
| `<scenario>.md`, `<scenario>.csv` | median, mean, p95 and allocated bytes per point and scope |
| `<scenario>-runs.csv` | per-process values used by the statistics |
| `plots/` | change against the baseline per scenario |
| `diff/`, `traces/` | TraceDiff texts, hotspots and collapsed stacks of a representative run per point |
| `attribution*`, `static-*.csv` | attribution of commits and pull requests, static metrics per tag |

Raw `.ember` traces (about 6 GB), per-process metadata and Chrome JSON exports are not committed.

## License

[MIT](LICENSE)
