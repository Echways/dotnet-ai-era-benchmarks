# Microsoft's .NET libraries before and after AI: report draft

Draft built from the measurements in this repository (snapshot of 30 finished rounds, 2026-10-31). Prose in sections 1 to 8 states what the data supports; **Appendix A** holds the complete tables and provenance. Numbers are copied from `results/`; claims are worded to match the evidence and every limit is in section 7.

## Summary

1. **Hot paths got faster in steady state and slower on first use.** Against .NET 7.0 (the last release written before ChatGPT), the repeated, warmed-up operations of the caches, source-generated logging, EF change tracking and several DI paths are 10% to 40% faster in 8.0, 9.0 and 10.0, while first-use costs (first resolve, first log, building a provider or logger factory, EF context creation) are 10% to 60% slower. Both statements hold at full speed and with stock power settings (section 4.1).
2. **The agent era does not look different from the AI-assistant era.** Between 9.0 (E1) and 10.0 (E2) the change of a typical scope is small: 39 of 46 scopes differ by less than 5 percentage points at full resources (34 of 46 with stock settings). The big steps (for example `di.validate-on-build100` +60%, `logging.factory-create` +45%, the `logger-message-define` gain of 36%) are already present at 8.0 or 9.0 (section 4.2).
3. **Up to 10.0 no change can be attributed to the Copilot coding agent; in 11.0 RC one can.** In `Microsoft.Extensions.DependencyInjection` and `Caching.Memory`, none of the 113 commits between v7.0.20 and v10.0.14 came from an agent pull request or carries an agent co-author trailer, and none of the 24 hot methods was touched by one. Between 10.0 and 11.0 RC the agent authored 11 of 21 DI commits, and one of them, dotnet/runtime#124331, a deadlock fix, costs +16.5% on `di.first-resolve-depth5` (section 4.6). The agent has 1,734 pull requests in dotnet/runtime overall, so this is a statement about these hot paths, not about the repository (section 5).
4. **The 11.0 release candidate has clear regressions in configuration binding and options** (`config.bind-poco50` +138%, `options.snapshot-new-scope` +162%, with +48% and +45% allocation). They come from one line added by the human-authored dotnet/runtime#126396, reported as dotnet/runtime#136759 (section 4.6). It is a release candidate, measured as the `net10.0` build of the 11.0 sources, so these may change before release (section 4.3).
5. **The machine state matters and is reported as two scenarios.** Full resources (tuned stand) and limited resources (stock `powersave`, turbo on). Several DI and Options scopes look 10% to 18% faster than 7.0 under limited resources but only 1% to 8% faster at full speed; the scenario effect is significant in 29 of 252 comparisons while a drift check on the full-resource rounds alone flags 1 (section 3, Appendix A §5.4).
6. **Allocations are a firmer signal than times.** Allocated bytes per batch are deterministic: `logger-message-define` -33%, `cache.size-limit-compact-half` -29%, EF change tracking -20% to -42%, but DI provider building +15% to +32% and EF query-string/new-context +13% to +51% (section 4.4).
7. **Static code metrics show growth in size, not in complexity.** Source lines grow 24% to 129% between 7.0 and 11.0 RC, while mean cyclomatic complexity and mean method length barely move (section 6).

## 1. Question and scope

Did the hot paths of Microsoft's .NET libraries get faster or slower across three epochs, and can any change be attributed to code written by the GitHub Copilot coding agent rather than by people?

| epoch | versions | GA | why |
|---|---|---|---|
| E0 | 6.0.x, **7.0.x (baseline)** | 2021-11-08, 2022-11-08 | 7.0 is the last release written before ChatGPT (2022-11-30) |
| E1 | 8.0.x, 9.0.x | 2023-11-14, 2024-11-12 | AI assistants, no coding agent |
| E2 | 10.0.x, 11.0 RC3 | 2025-11-11, unreleased | Copilot coding agent opens PRs in dotnet/runtime from 2025-05-19 |

Libraries: DependencyInjection, Configuration (Json, Binder), Options, Logging, Caching.Memory and EF Core with SQLite (Track A, 6 to 10; EF Core 11 only in Track B); System.Text.Json, Channels, Immutable, ASP.NET Core and EF Core 10 vs 11 (Track B). "Microsoft's code" here means these libraries, which are not representative of all of dotnet/runtime.

## 2. Method

- **Track A (primary):** the runtime is fixed at .NET 10.0.11 and only the NuGet packages change (`Microsoft.Extensions.*` 6 to 10 and 11.0.0-rc.3, EF Core 6 to 10), so library changes are separated from JIT and GC changes. A guard aborts the run if a loaded assembly has another major version.
- **Track B:** libraries that ship with the runtime are compared as net10.0 vs net11.0, so every Track B number is library and runtime together.
- **Measurements:** one EmberTrace scope per batch (typically 10,000 operations), 200 measured and 20 warm-up iterations per process, each scenario in its own process, pinned to cores 2 to 5, allocations from `GC.GetAllocatedBytesForCurrentThread`. Cold phases (first build, first resolve, first query) have their own scopes.
- **Rounds:** the matrix was repeated in 30 rounds with the order of the six Track A points shuffled each time; 111 processes per point and scope in total.
- **Statistics:** the unit is the process; every round counts equally; changes are computed inside a round against `e0-7`, then combined as a geometric mean; 95% CI by hierarchical bootstrap (rounds, then processes); Mann-Whitney U per round, combined with Stouffer, adjusted with Benjamini-Hochberg. A change under 5% is noise. A scope is `faster` or `slower` only if the whole CI lies beyond 5%, no round points the other way and adjusted p < 0.05; scopes with p75/p25 >= 1.5 are judged by the mean. These verdict rules were tightened after the first rounds were seen; the original rule is kept as `prespecified_verdict` (Appendix A, methodology deviation 22).
- **Attribution:** commits between tags are mapped to pull requests and classified as `cca` (opened by the coding agent), `msft-human`, `community`, `bot` or `no-pr`; hot methods are followed with `git log -L`. Glass-box builds of DI and Caching.Memory with scopes injected into hot methods give per-method timings for v7.0.20 and v10.0.14.
- **Static metrics:** size, cyclomatic complexity, method length, nesting and analyzer warnings per tag.

## 3. Test scenarios: full and limited resources

Applications rarely have a machine to themselves, so the measurements were taken in two machine states of the same laptop (12th Gen Intel i5-12450H, Ubuntu 22.04.5, kernel 6.8.0-138):

| | full resources | limited resources |
|---|---|---|
| CPU governor | `performance` | `powersave` (stock default) |
| turbo | off | on |
| rounds / processes per point | 19 / 78 | 11 / 33 |
| reproduce | `./scripts/setup-stand.sh --apply` | `./scripts/setup-stand.sh --restore` |

Limited resources is a CPU power policy (lower, load-dependent clock), not a cap on cores, memory or I/O. The limited rounds form one contiguous block, bracketed on both sides by full-resource rounds. A comparison of the full-resource rounds before and after that block found 1 of 252 comparisons significant after correction, against 29 of 252 for full vs limited, so the difference between scenarios is not explained by slow drift (Appendix A §5.4). Statistics are computed separately for each scenario; the pooled analysis is in Appendix A §4.

## 4. Results

Cells show the change against 7.0 (`e0-7`) of the primary metric (median, or mean for multimodal scopes) and the verdict. "full" and "limited" are the scenarios of section 3.

### 4.1 Steady-state operations got faster

| scope | full e1-9 | full e2-10 | full e2-11 | limited e1-9 | limited e2-10 | limited e2-11 |
|---|---|---|---|---|---|---|
| `cache.try-get-hit.x10k` | -17.3% faster | -14.0% faster | -15.2% inconclusive | -24.3% faster | -15.8% faster | -14.2% faster |
| `cache.try-get-miss.x10k` | -16.6% faster | -22.4% faster | -22.2% faster | -21.5% faster | -26.0% faster | -21.6% faster |
| `cache.size-limit-compact-half.x1` | -8.7% inconclusive | -21.0% faster | -20.9% faster | -8.9% inconclusive | -31.6% faster | -26.8% faster |
| `logging.logger-message-define.x10k` | -36.6% faster | -35.4% faster | -36.0% faster | -39.1% faster | -39.0% faster | -35.3% faster |
| `ef.detect-changes-10k-tracked.x10` | -40.8% faster | -29.5% faster | n/a | -44.4% faster | -33.7% faster | n/a |
| `di.build1000.x10` | -26.6% faster | -25.3% faster | -26.2% faster | -22.6% inconclusive | -22.6% faster | -17.2% inconclusive |
| `di.create-scope-dispose.x10k` | -14.5% faster | -13.7% faster | -15.7% faster | -15.4% faster | -16.1% faster | -14.9% inconclusive |
| `di.singleton.x10k` | -6.3% inconclusive | -5.9% inconclusive | -5.8% inconclusive | -20.4% faster | -19.8% faster | -16.5% faster |
| `di.transient.x10k` | -7.5% inconclusive | -6.8% inconclusive | -6.9% inconclusive | -19.0% faster | -18.1% faster | -13.4% faster |
| `di.open-generic.x10k` | -3.6% inconclusive | -2.6% inconclusive | -4.0% inconclusive | -17.0% faster | -15.2% faster | -10.7% faster |
| `options.monitor-current-value.x10k` | -5.6% inconclusive | -8.0% faster | -6.8% inconclusive | -19.2% faster | -17.2% faster | -14.5% faster |

The cache read paths, `logger-message-define` and EF change detection are faster in both scenarios at 8.0, 9.0 and 10.0 with all rounds agreeing; 11.0 RC points the same way. The DI singleton/transient/open-generic and `options.monitor-current-value` scopes are fast under limited resources, but at full resources their intervals still cross the 5% threshold. Glass-box timings for the same scopes (Appendix A §7) point to `MemoryCache.TryGetValue` (-7% per call) and `MemoryCache.SetEntry` (-14%) as the inner methods behind the cache gain (one process per tag, with scope overhead, so indicative only).

### 4.2 First-use and construction costs got slower, mostly before the agent era

| scope | full e1-9 | full e2-10 | full e2-11 | limited e1-9 | limited e2-10 | limited e2-11 |
|---|---|---|---|---|---|---|
| `di.validate-on-build100.x100` | +60.2% slower | +63.5% slower | +61.7% slower | +49.4% inconclusive | +52.3% inconclusive | +61.8% slower |
| `di.first-resolve-depth5.x100` | +57.9% slower | +60.2% slower | +84.9% slower | +46.9% inconclusive | +50.9% inconclusive | +85.6% slower |
| `logging.factory-create.x100` | +45.9% slower | +44.8% slower | +50.5% slower | +47.6% slower | +49.2% slower | +57.4% slower |
| `ef.new-context.x1k` | +23.1% slower | +39.1% slower | n/a | +15.8% inconclusive | +30.8% inconclusive | n/a |
| `ef.to-query-string.x1k` | +24.4% slower | +25.7% slower | n/a | +15.0% inconclusive | +17.1% inconclusive | n/a |
| `di.cold.first-resolve-depth5` | +14.0% slower | +18.3% slower | +27.4% slower | +5.5% inconclusive | +9.0% inconclusive | +26.4% slower |
| `logging.cold.first-log` | +16.5% slower | +24.8% slower | +24.6% slower | +10.4% inconclusive | +18.9% inconclusive | +25.9% slower |
| `options.cold.first-value` | +13.3% slower | +17.9% slower | +26.9% slower | +6.4% inconclusive | +14.0% inconclusive | +26.4% slower |

These increases are already present at 9.0 (E1); at 10.0 they are the same or somewhat larger. Between 9.0 and 10.0 the change of the primary metric is under 5 percentage points for 39 of 46 scopes at full resources (34 of 46 limited; median absolute step 1.8 and 2.2 points). The data shows that these costs changed; it does not explain why. Cold scopes include JIT and tier-up cost, which the median of a short batch only partly removes, and old packages run on a newer runtime in Track A.

### 4.3 The 11.0 release candidate

| scope | full e2-10 | full e2-11 | limited e2-10 | limited e2-11 |
|---|---|---|---|---|
| `config.bind-poco50.x1k` | -1.4% noise | +137.8% slower | -7.3% inconclusive | +133.8% slower |
| `options.snapshot-new-scope.x1k` | -2.9% noise | +162.2% slower | -6.9% inconclusive | +157.7% slower |
| `config.cold.build-json2000` | +2.7% inconclusive | +76.3% slower | -2.9% inconclusive | +75.1% slower |
| `options.validate-data-annotations.x1k` | +5.9% inconclusive | +26.2% slower | -0.8% inconclusive | +23.9% slower |
| `cache.cold.create-and-first-set` | +13.0% slower | +26.6% slower | +8.0% inconclusive | +25.8% slower |

Binding and options snapshots in the RC are far slower and allocate far more (bind-poco +48% bytes, snapshot-new-scope +45%). The RC is the `net10.0` build of the 11.0 sources on the .NET 10 runtime; an unreleased version, so treat it as a warning, not a result.

Track B (net10.0 vs net11.0, library and runtime together, all rounds): Kestrel start-up -54%, EF cold connection/schema -17% to -15%, bounded channel throughput -12% (full only), first reflection serialize -10% (full only); EF change detection +11% to +12% (and cold first query +9% at full resources). ASP.NET Core per-request service time is +81% to +90% slower, but these scopes are judged by the mean (multimodal), the fixed-rate generator is bound at 100 ms per 400-request batch (`batch400` scopes read +0.1%), and bombardier was not run, so this number needs a second tool before it is claimed.

### 4.4 Allocated bytes

Scopes with a change of 10% or more against 7.0 (allocations are deterministic, so these hold in both scenarios):

| scope | e0-7 bytes per batch | e1-9 | e2-10 | e2-11 |
|---|---|---|---|---|
| `di.build100.x100` | 2,490,400 | +15.3% | +15.3% | +15.3% |
| `di.build1000.x10` | 2,151,840 | +17.3% | +17.3% | +17.3% |
| `di.validate-on-build100.x100` | 7,656,800 | +32.4% | +32.4% | +32.4% |
| `config.bind-poco50.x1k` | 19,240,000 | -4.4% | -4.4% | +47.6% |
| `options.snapshot-new-scope.x1k` | 20,280,000 | -4.0% | -3.8% | +45.3% |
| `logging.cold.first-log` | 4,312 | +18.6% | +18.6% | +18.6% |
| `logging.factory-create.x100` | 1,796,072 | +12.1% | +12.6% | +5.0% |
| `logging.logger-message-define.x10k` | 1,471,120 | -32.6% | -32.6% | -32.6% |
| `cache.size-limit-compact-half.x1` | 918,104 | +0.0% | -28.5% | -28.5% |
| `ef.cold.first-query` | 2,337,304 | +19.4% | +19.1% | - |
| `ef.detect-changes-10k-tracked.x10` | 56,802,160 | -42.3% | -42.3% | - |
| `ef.new-context.x1k` | 33,112,176 | +9.6% | +13.3% | - |
| `ef.query-where-orderby-take.x1k` | 12,709,752 | +18.8% | +22.0% | - |
| `ef.to-query-string.x1k` | 4,968,832 | +46.5% | +51.2% | - |

### 4.5 What the two scenarios change

At full resources 65 of 252 comparisons are `slower` and 36 `faster`, against 22 and 41 under limited resources, where 11 rounds and wider intervals leave more scopes `inconclusive` (Appendix A §5.1). The conclusions in 4.1 to 4.3 are written only for effects that appear in both scenarios, or flagged where they do not.

### 4.6 Locating the 11.0 RC regressions

Each experiment replaces one assembly in the unchanged `e2-11` harness output, full-resource scenario. Scripts are in `experiments/`.

Binder (6 rounds, per call): `IsIgnoredProperty` from dotnet/runtime#126396 calls `property.IsDefined(typeof(ConfigurationIgnoreAttribute))` for each property on each bind.

| scope | 10.0 | RC3 package | RC3 sources | RC3, expression replaced by `false` |
|---|---:|---:|---:|---:|
| `config.bind-poco50.x1k` | 60.5 µs | 148.5 µs | 148.8 µs | 59.6 µs |
| `options.snapshot-new-scope.x1k` | 51.2 µs | 139.4 µs | 138.1 µs | 52.6 µs |
| `options.validate-data-annotations.x1k` | 42.9 µs | 52.8 µs | 51.2 µs | 42.7 µs |
| `config.bind-poco50.x1k` bytes per call | 18,400 | 28,400 | 28,400 | 18,400 |

`config.cold.build-json2000` (+76%) is not affected and remains unexplained. Standalone BenchmarkDotNet reproduction (`experiments/repro`, `Get<T>`, 50 properties): 10.0 29 µs and 17.15 KB; RC3 120 µs and 27 KB; RC3 with `inherit: false` 45 µs and 17.15 KB; RC3 with the stub 29 µs and 17.15 KB; the `ConfigurationBinder.cs` of `main` after dotnet/runtime#135672 45 µs and 17.16 KB; RC3 with the source generator 12 µs and 5.67 KB.

DI (8 rounds, `di.first-resolve-depth5.x100`, ms per batch): 10.0 package 0.565; 350951d, the commit before dotnet/runtime#124331, 0.554; 7603a59, dotnet/runtime#124331 opened by the agent, 0.641; RC3 package 0.653; RC3 sources 0.650; RC3 with only that commit reverted 0.569. Within rounds the commit adds +16.5% (95% CI 14.3 to 18.8, 8 of 8 rounds) and the revert leaves +1.9% against 10.0 (CI -0.6 to 4.1). The pull request fixes the deadlock dotnet/runtime#88390 with a `[ThreadStatic]` `HashSet` in `VisitRootCache`, which is not in the list of 24 hot methods, so only the bisect found it.

Release steps with the previous release as baseline (`habr/release_steps.py`, same verdict rules): full resources 19, 5, 5 and 6 confirmed shifts for 8.0, 9.0, 10.0 and 11.0 RC; limited resources 8, 7, 5 and 6, with much wider intervals. Each major of a package also brings its own TFM asset (`lib/net7.0`, `lib/net8.0`, ...), so these steps cannot separate source edits from conditional compilation.

Commit counts by author, git only, no merges (agent author / commits): 7.0 to 10.0: DI 0/80, Caching 0/33, Configuration 0/67, Options 0/37, Logging 1/52, EF Core 29/867; 10.0 to 11.0 RC: DI 11/21, Caching 1/14, Configuration 1/15, Options 0/7, Logging 3/8, EF Core 73/256.

## 5. Attribution to the coding agent

- 113 commits in `v7.0.20..v10.0.14`: 57 `msft-human`, 56 `community`, 0 `cca`, 0 `cca_coauthored`; 24 hot methods followed with `git log -L`, none touched by an agent pull request.
- Six pull requests carry `tenet-performance`; all are human (five `community`, one `msft-human`). One revert, of a human performance change.
- Servicing: no commits touching these paths in the 12 months after 7.0 GA, nor in the 352 days observed after 10.0 GA.
- Consequently the difference-in-differences design of H2 has no treated methods and no estimate is produced: the null here comes from the absence of agent changes in these paths, not from a measured absence of effect.
- Only DI and Caching have pull-request attribution for this range. Section 4.6 adds git-only commit counts for all libraries up to 11.0 RC.
- `cca` means "opened by the agent", not "written without human input"; `author_association` disagrees with `@microsoft.com` e-mails in 24 of 113 commits; AI use that leaves no trace in git or GitHub is invisible.

## 6. Static metrics

DI, DI.Abstractions, Caching.Memory and Caching.Abstractions, v6.0.36 to 11.0 RC3: source code lines grow between 7.0 and 11.0 RC by +25% (DI: 2,704 to 3,381), +24% (Caching.Memory: 1,010 to 1,251), +129% (DI.Abstractions: 1,303 to 2,983) and +61% (Caching.Abstractions: 605 to 976), while mean cyclomatic complexity stays within 1.3 to 2.5 and mean lines per method within 7 to 12; analyzer warnings change little (31 to 32 in DI, 38 to 39 in Caching.Memory, 39 to 42 in Caching.Abstractions) except in DI.Abstractions (37 to 49). These metrics describe the code base; they cannot separate agent work from human work (Appendix A §8).

## 7. Threats to validity and limits

- **Not a prepared bench.** Laptop, hybrid CPU, no core isolation, load average 1 to 2.7 at round starts. Treat absolute times as machine-specific; ratios inside a round are the evidence.
- **Scenarios are unequal and not interleaved** (78 vs 33 processes per point; one contiguous limited block). "Limited" is a power policy, not a quota.
- **Statistical:** most pooled verdicts are `inconclusive`; scopes are many, so adjusted p-values are used; rounds on one machine are not independent samples of all machines; the verdict rules were tightened after the first rounds were seen.
- **Construct:** synthetic batches of APIs present in every version, so the newest fast paths are excluded; batch medians measure throughput, not application latency.
- **Internal:** old packages run on the .NET 10 runtime (6.x on `net6.0`/`netstandard` assets) and may miss runtime fast paths; in-box dependencies are always .NET 10 copies; JIT tiering adds variance and no full `--no-tiering` control was run; cache `set` and `get-or-create` are bimodal and the cause is not established.
- **Partial cross-check:** BenchmarkDotNet ran only `BindPoco50` and `SnapshotNewScope`, once per version: 53.8, 49.6 and 138.2 µs, and 56.0, 51.2 and 143.4 µs for 7.0, 10.0 and RC3. The large regression agrees with EmberTrace; 10.0 against 7.0 reads -8% and -9% against -1% and -3%. Tracer distortion is not checked for the other 44 scopes.
- **External:** five libraries are not all of Microsoft's code; 11.0 is a release candidate.

## 8. Conclusions the data supports

- Warmed-up hot paths of these libraries are mostly faster than in 7.0 and first-use costs are slower; the allocation changes agree for the scopes where bytes changed.
- The step between the AI-assistant era and the coding-agent era is small compared with the step from 7.0 to 8.0/9.0.
- Up to 10.0 nothing measured can be attributed to the coding agent. In 11.0 RC one measured change is: dotnet/runtime#124331 costs +16.5% on first resolve as the price of a deadlock fix. The largest regression of the RC comes from a human-authored change.
- Results depend on machine state; reporting full and limited resources separately is part of the finding.

## Not claimed

That the agent made code better or worse in general; that Microsoft code as a whole changed; that Track B differences are due to library code rather than runtime; that ASP.NET Core request service time regressed (pending a second load tool).

---

# Appendix A. Collected data

Working document for the report. It gathers everything measured so far in one place and says which numbers are trustworthy and which are not. Numbers are copied from the CSVs in `results/`; nothing here is a new measurement except the per-scenario statistics in section 5, which re-run `analysis/stats.py` on subsets of the same data.

Data snapshot: accumulated summary from **30 finished rounds** (processes per point and scenario: 111). Round `20261031T180544Z` was stopped on purpose and is not included. Regenerate with `./scripts/run-rounds.sh` data plus `analysis/stats.py`.

## 1. Question and design

**Question.** Did the hot paths of Microsoft's .NET libraries get faster or slower across three epochs, and can any change be attributed to code written by the GitHub Copilot coding agent rather than by people?

| epoch | points | GA | meaning |
|---|---|---|---|
| E0 | 6.0.x (`e0-6`), **7.0.x (`e0-7`, baseline)** | 2021-11-08, 2022-11-08 | 7.0 is the last release written before ChatGPT (2022-11-30) |
| E1 | 8.0.x (`e1-8`), 9.0.x (`e1-9`) | 2023-11-14, 2024-11-12 | AI assistants, no coding agent |
| E2 | 10.0.x (`e2-10`), 11.0 RC3 (`e2-11`) | 2025-11-11, not yet released | the Copilot coding agent opens PRs in dotnet/runtime from 2025-05-19 |

Exact packages: `Microsoft.Extensions.*` 6.0.x to 10.0.14 and 11.0.0-rc.3.26522.148; EF Core 6.0.36, 7.0.20, 8.0.33, 9.0.22, 10.0.14 (EF Core 11 only targets net11.0, so it exists only in Track B). Full table in `docs/verified-facts.md` (F1).

**Hypotheses** (`docs/methodology.md`):

- **H1.** Median batch time and allocated bytes per batch differ between epochs, per scope, against `e0-7` (Track A) or `b-net10` (Track B).
- **H2.** Hot methods touched by agent PRs changed differently from methods touched only by people (difference-in-differences on glass-box timings).
- **H3.** Static metrics (size, complexity, warnings) shift between epochs; descriptive only.

**Fixed rules.** Differences under 5% are noise; a null result is acceptable; baseline fixed; full matrix specified as 10 processes per point, 200 measured and 20 warm-up iterations, pinned to cores 2-5, on a prepared machine (`docs/stand.md`).

**Tracks.**

- **Track A (primary).** Runtime fixed at .NET 10.0.11; only the NuGet packages change, so library code is separated from JIT and GC. Libraries: DI, Configuration (+Json, Binder), Options, Logging, Caching.Memory, EF Core + SQLite.
- **Track B.** `System.Text.Json`, Channels, Immutable, ASP.NET Core and EF Core 10 vs 11 ship with the runtime, so the comparison is net10.0 vs net11.0: library and runtime together, not separable.
- **Cross-check.** BenchmarkDotNet mirror in `bench/Bdn`. Only two benchmarks have been run (section 7).
- **Glass box.** DI and Caching.Memory compiled from tags `v7.0.20` and `v10.0.14` with a scope injected into each hot method.

## 2. Data inventory

| what | where | status |
|---|---|---|
| Raw traces, 30 rounds | `results/rounds/rounds/<UTC>/raw/<point>/<scenario>-<pid>.ember` + `.run-meta.json`, per-round `env.json`, `track-a-order.txt` | 5.6 GB, not committed |
| Accumulated summary (authoritative) | `results/summary/` (`stats.csv`, `stats.md`, `<scenario>.csv`, `<scenario>-runs.csv`, `plots/`, `diff/`, `traces/`) | committed; pools both machine states |
| Stopped rounds | `results/rounds/incomplete/20261031T180544Z`, `20261030T143818Z` | ignored by the summary |
| Run log | `results/rounds/run-rounds.log` | |
| Attribution, DI + Caching only | `results/summary/attribution*.csv`, `attribution.md`, `attribution-regressions.json` | committed |
| Static metrics, 4 libraries, v6 to v11 | `results/summary/static-*.csv` | committed |
| Glass box traces and hotspots | `results/glassbox/v7.0.20/`, `results/glassbox/v10.0.14/` (`di`, `cache`) | not committed |
| BenchmarkDotNet | `BindPoco50` and `SnapshotNewScope` only, one run per version; full suite not run | partial |
| Tiering control (`--no-tiering`) | none as a full run; checked on 2 processes of `e0-7` (see methodology, Deviation 22) | missing |

Scenario and scope counts in the accumulated `stats.csv` (328 rows = 6 points x scopes for Track A, 2 points x scopes for Track B):

| scenario | scopes | points |
|---|---|---|
| cache | 7 | e0-6, e0-7, e1-8, e1-9, e2-10, e2-11 |
| config | 7 | e0-6, e0-7, e1-8, e1-9, e2-10, e2-11 |
| di | 12 | e0-6, e0-7, e1-8, e1-9, e2-10, e2-11 |
| ef | 8 | e0-6, e0-7, e1-8, e1-9, e2-10 |
| logging | 7 | e0-6, e0-7, e1-8, e1-9, e2-10, e2-11 |
| options | 5 | e0-6, e0-7, e1-8, e1-9, e2-10, e2-11 |
| trackb-aspnetcore | 8 | b-net10, b-net11 |
| trackb-channels | 2 | b-net10, b-net11 |
| trackb-ef | 8 | b-net10, b-net11 |
| trackb-immutable | 4 | b-net10, b-net11 |
| trackb-stj | 8 | b-net10, b-net11 |

## 3. Collection conditions

- Machine: `a-MCLF-XX`, 12th Gen Intel Core i5-12450H (12 logical CPUs, hybrid), 16 GB RAM, Ubuntu 22.04.5, kernel 6.8.0-138, SMT on. Processes pinned with `taskset -c 2-5` (affinity mask 60). Runtime .NET 10.0.11 (Track A), `.dotnet/` runtimes 10.0.14 and 11.0.0-rc.3 (Track B).
- Rounds started 2026-10-29 18:56 UTC and run back to back; each runs the six Track A points in a freshly shuffled order plus Track B. 27 rounds have 3 processes per point and scenario, 3 rounds have 10; **111 processes per point and scenario in total**, 30 rounds.
- No core isolation; ASLR state is not recorded in `env.json`; load average at round start 0.2 to 4.2 (mostly 1 to 2.7), so the machine was not idle. It is a laptop, not the quiet bench of `docs/stand.md`.
- **Two resource scenarios.** The rounds were run in two machine states, and the report treats them as two scenarios, because real applications rarely get the whole machine to themselves:
  - **Full resources**: CPU governor `performance`, turbo off (fixed, maximum sustained frequency).
  - **Limited resources**: CPU governor `powersave`, turbo on (frequency scaled by load and power policy; lower and less predictable clock for the pinned cores).

| governor | rounds | processes per point (sum) |
|---|---|---|
| performance | 19 | 78 |
| powersave | 11 | 33 |

Rounds 8 to 18 (started 2026-10-30 16:56 to 23:10 UTC) ran in `powersave` with turbo on; all other rounds in `performance` with turbo off. The limited scenario is one contiguous block, so scenario and time of day are confounded, and it has fewer processes (33 vs 78 per point). It is a CPU frequency policy, not a cap on cores, memory or I/O: it models "constrained CPU speed", not a container with a CPU quota. `methodology.md` originally said to label such runs and not mix them with prepared-machine runs; section 5 reports them separately.

<details><summary>All rounds</summary>

| round (UTC) | governor | turbo | load avg at start | processes per point | Track A order |
|---|---|---|---|---|---|
| 20261029T185617Z | performance | off | 0.19 | 3 | e0-7,e1-9,e2-10,e0-6,e1-8,e2-11 |
| 20261029T211812Z | performance | off | 1.25 | 10 | e1-8,e2-11,e2-10,e1-9,e0-6,e0-7 |
| 20261030T005444Z | performance | off | 1.28 | 10 | e2-10,e0-6,e1-8,e0-7,e2-11,e1-9 |
| 20261030T043112Z | performance | off | 1.06 | 10 | e1-8,e0-6,e1-9,e0-7,e2-11,e2-10 |
| 20261030T111223Z | performance | off | 4.10 | 3 | e2-11,e1-8,e1-9,e0-6,e2-10,e0-7 |
| 20261030T122009Z | performance | off | 1.91 | 3 | e1-8,e0-7,e0-6,e2-10,e2-11,e1-9 |
| 20261030T132806Z | performance | off | 1.76 | 3 | e2-11,e0-6,e1-9,e0-7,e1-8,e2-10 |
| 20261030T165647Z | powersave | on | 4.20 | 3 | e2-11,e1-8,e2-10,e0-6,e1-9,e0-7 |
| 20261030T173312Z | powersave | on | 1.11 | 3 | e2-11,e0-6,e1-8,e0-7,e2-10,e1-9 |
| 20261030T180936Z | powersave | on | 1.07 | 3 | e0-6,e1-8,e2-11,e1-9,e2-10,e0-7 |
| 20261030T184609Z | powersave | on | 1.47 | 3 | e0-7,e2-11,e1-9,e0-6,e1-8,e2-10 |
| 20261030T192242Z | powersave | on | 1.08 | 3 | e0-7,e1-8,e2-11,e0-6,e2-10,e1-9 |
| 20261030T195914Z | powersave | on | 1.75 | 3 | e2-11,e0-7,e1-8,e0-6,e2-10,e1-9 |
| 20261030T203559Z | powersave | on | 1.28 | 3 | e2-10,e0-7,e2-11,e1-8,e1-9,e0-6 |
| 20261030T211316Z | powersave | on | 2.50 | 3 | e2-10,e1-8,e0-6,e1-9,e0-7,e2-11 |
| 20261030T215136Z | powersave | on | 2.36 | 3 | e0-6,e0-7,e2-10,e2-11,e1-8,e1-9 |
| 20261030T223039Z | powersave | on | 1.94 | 3 | e0-6,e2-11,e1-8,e2-10,e0-7,e1-9 |
| 20261030T231032Z | powersave | on | 2.68 | 3 | e2-10,e1-9,e1-8,e0-6,e2-11,e0-7 |
| 20261031T000753Z | performance | off | 1.82 | 3 | e1-9,e2-11,e2-10,e1-8,e0-7,e0-6 |
| 20261031T011704Z | performance | off | 1.01 | 3 | e1-8,e2-10,e0-7,e2-11,e1-9,e0-6 |
| 20261031T022612Z | performance | off | 1.44 | 3 | e2-11,e1-8,e1-9,e0-7,e0-6,e2-10 |
| 20261031T033540Z | performance | off | 1.13 | 3 | e0-7,e1-8,e1-9,e2-11,e0-6,e2-10 |
| 20261031T044506Z | performance | off | 1.19 | 3 | e1-9,e1-8,e2-11,e0-6,e2-10,e0-7 |
| 20261031T055432Z | performance | off | 1.16 | 3 | e1-8,e0-6,e2-11,e0-7,e2-10,e1-9 |
| 20261031T070411Z | performance | off | 1.46 | 3 | e2-11,e2-10,e0-6,e1-9,e1-8,e0-7 |
| 20261031T081352Z | performance | off | 1.13 | 3 | e1-8,e2-10,e1-9,e0-6,e0-7,e2-11 |
| 20261031T092341Z | performance | off | 1.22 | 3 | e1-9,e2-11,e0-7,e2-10,e1-8,e0-6 |
| 20261031T103333Z | performance | off | 1.71 | 3 | e1-9,e0-7,e1-8,e2-11,e2-10,e0-6 |
| 20261031T114411Z | performance | off | 2.53 | 3 | e1-8,e0-6,e2-10,e2-11,e0-7,e1-9 |
| 20261031T125515Z | performance | off | 1.88 | 3 | e1-9,e1-8,e0-7,e2-11,e0-6,e2-10 |

</details>

## 4. Track A, all 30 rounds pooled

Method (`analysis/stats.py`): unit is the process; each round counts equally; 95% CI by hierarchical bootstrap (10,000 resamples); stratified Mann-Whitney U with Stouffer and Benjamini-Hochberg; `faster`/`slower` need the CI wholly beyond ±5%, no round pointing the other way, adjusted p < 0.05; `noise` means the CI is inside ±5%; the rest is `inconclusive`. Scopes with p75/p25 >= 1.5 are judged by the mean (marked †). The verdict rules were changed after the first rounds were seen (Deviation 22); `stats.csv` keeps the original as `prespecified_verdict`.

### 4.1 Verdict counts per point

| epoch | point (b-net11 is Track B) | faster | slower | mixed | noise | inconclusive | scopes |
|---|---|---|---|---|---|---|---|
| E0 | e0-6 | 2 | 2 | 0 | 3 | 39 | 46 |
| E1 | e1-8 | 7 | 0 | 0 | 1 | 38 | 46 |
| E1 | e1-9 | 8 | 0 | 1 | 1 | 36 | 46 |
| E2 | b-net11 | 2 | 5 | 0 | 15 | 8 | 30 |
| E2 | e2-10 | 8 | 0 | 0 | 1 | 37 | 46 |
| E2 | e2-11 | 5 | 12 | 0 | 7 | 14 | 38 |

Reading: against the 7.0 baseline, `e1-8`, `e1-9` and `e2-10` have 7 to 8 scopes faster, none slower, and most scopes inconclusive. `e2-11` has 12 Track A scopes slower and 5 faster. `e0-6` (older than the baseline) shows 2 slower, consistent with libraries improving over time. Most verdicts are `inconclusive`: the CI reaches past ±5% (rounds disagree), and the pooled spread across sessions is wide, see section 5.

Scopes judged by the mean because p75/p25 >= 1.5 at the point or the baseline:

- `cache.set.x10k` (p75/p25 = 3.71 at the baseline)
- `cache.get-or-create.x10k` (p75/p25 = 4.26 at the baseline)
- `di.build100.x100` (p75/p25 = 1.74 at the baseline)
- `di.build1000.x10` (p75/p25 = 1.72 at the baseline)
- `di.create-scope-dispose.x10k` (p75/p25 = 1.66 at the baseline)
- `aspnetcore.hello.request` (p75/p25 = 1.68 at the baseline)
- `aspnetcore.json.request` (p75/p25 = 1.67 at the baseline)
- `aspnetcore.routes500.request` (p75/p25 = 1.65 at the baseline)

### 4.2 Change vs `e0-7` per scope (primary metric: median, or mean for †)

Cells show the change of the primary metric and the verdict. Baseline cells show absolute median time. Full CIs, p-values and `rounds_agree` are in `results/summary/stats.md` and `stats.csv`.

| scenario | scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 | multimodal |
|---|---|---|---|---|---|---|---|---|
| cache | `cache.cold.create-and-first-set` | +147.6% slower | 4.309 ms (baseline) | +6.2% inconclusive | +7.5% inconclusive | +11.1% inconclusive | +26.3% slower |  |
| cache | `cache.set.x10k` | +53.1%† inconclusive | 2.259 ms (baseline) | -11.6%† faster | -13.0%† faster | -6.7%† inconclusive | -6.6%† inconclusive | yes |
| cache | `cache.try-get-hit.x10k` | +12.8% inconclusive | 0.874 ms (baseline) | -19.2% faster | -19.9% faster | -14.7% faster | -14.8% inconclusive |  |
| cache | `cache.try-get-miss.x10k` | +0.6% noise | 0.687 ms (baseline) | -18.3% faster | -18.4% faster | -23.8% faster | -22.0% faster |  |
| cache | `cache.get-or-create.x10k` | +25.1%† inconclusive | 2.621 ms (baseline) | -14.2%† faster | -14.8%† faster | -8.5%† inconclusive | -6.8%† inconclusive | yes |
| cache | `cache.size-limit-compact-half.x1` | -6.9% inconclusive | 2.852 ms (baseline) | -7.3% inconclusive | -7.4% inconclusive | -24.3% faster | -22.7% faster |  |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | +8.2% inconclusive | 1.129 ms (baseline) | -7.3% inconclusive | -7.5% inconclusive | -6.5% inconclusive | -5.8% inconclusive |  |
| config | `config.cold.build-json2000` | -0.4% inconclusive | 11.582 ms (baseline) | +0.5% inconclusive | -0.5% inconclusive | +0.6% inconclusive | +75.9% slower |  |
| config | `config.cold.bind-poco50` | +29.6% inconclusive | 16.952 ms (baseline) | -4.9% inconclusive | -4.4% inconclusive | -1.3% inconclusive | +3.6% inconclusive |  |
| config | `config.build-json2000.x10` | -2.0% inconclusive | 7.619 ms (baseline) | +1.2% noise | +4.0% inconclusive | +5.4% inconclusive | +2.3% noise |  |
| config | `config.indexer.x10k` | -2.7% inconclusive | 0.454 ms (baseline) | -2.2% inconclusive | -2.3% inconclusive | -1.6% inconclusive | -1.2% noise |  |
| config | `config.get-section-deep.x10k` | -2.4% inconclusive | 0.316 ms (baseline) | -1.1% inconclusive | -1.6% inconclusive | -2.3% inconclusive | +0.8% noise |  |
| config | `config.bind-poco50.x1k` | +12.8% inconclusive | 48.184 ms (baseline) | -3.2% inconclusive | -5.3% inconclusive | -3.6% inconclusive | +136.3% slower |  |
| config | `config.reload-json2000.x10` | -0.4% noise | 5.815 ms (baseline) | +5.1% inconclusive | -0.0% noise | +1.7% noise | -0.5% noise |  |
| di | `di.cold.build100` | -1.0% inconclusive | 0.478 ms (baseline) | -1.5% inconclusive | -0.9% inconclusive | +1.9% inconclusive | +6.3% inconclusive |  |
| di | `di.cold.first-resolve-depth5` | -1.8% inconclusive | 6.371 ms (baseline) | +9.9% inconclusive | +10.8% inconclusive | +14.8% inconclusive | +27.0% slower |  |
| di | `di.build100.x100` | +0.2%† inconclusive | 0.720 ms (baseline) | +3.7%† inconclusive | +7.1%† inconclusive | +9.5%† inconclusive | +15.2%† inconclusive | yes |
| di | `di.build1000.x10` | +2.7%† inconclusive | 0.604 ms (baseline) | +18.5%† inconclusive | -25.2%† inconclusive | -24.3%† faster | -23.0%† inconclusive | yes |
| di | `di.validate-on-build100.x100` | -2.9% inconclusive | 3.198 ms (baseline) | +16.7% inconclusive | +56.1% inconclusive | +59.3% inconclusive | +61.7% slower |  |
| di | `di.first-resolve-depth5.x100` | -2.1% inconclusive | 0.260 ms (baseline) | +59.3% inconclusive | +53.7% inconclusive | +56.7% inconclusive | +85.2% slower |  |
| di | `di.transient.x10k` | -2.0% inconclusive | 0.207 ms (baseline) | -9.9% inconclusive | -11.9% inconclusive | -11.1% inconclusive | -9.3% inconclusive |  |
| di | `di.scoped.x10k` | -1.9% inconclusive | 0.446 ms (baseline) | -4.2% inconclusive | -4.5% inconclusive | -4.9% inconclusive | -0.6% noise |  |
| di | `di.singleton.x10k` | -2.9% inconclusive | 0.156 ms (baseline) | -9.1% inconclusive | -11.7% faster | -11.3% inconclusive | -9.9% faster |  |
| di | `di.open-generic.x10k` | -2.7% inconclusive | 0.229 ms (baseline) | -8.5% inconclusive | -8.8% inconclusive | -7.5% inconclusive | -6.5% inconclusive |  |
| di | `di.enumerable20.x10k` | -2.7% inconclusive | 3.330 ms (baseline) | -3.3% inconclusive | -3.5% inconclusive | -2.6% inconclusive | +0.1% noise |  |
| di | `di.create-scope-dispose.x10k` | -15.1%† faster | 0.631 ms (baseline) | -11.1%† inconclusive | -14.9%† faster | -14.6%† faster | -15.4%† inconclusive | yes |
| ef | `ef.cold.open-connection-create-schema` | -3.9% inconclusive | 49.484 ms (baseline) | -2.1% inconclusive | -2.0% inconclusive | -1.2% inconclusive | n/a |  |
| ef | `ef.cold.first-query` | -6.5% inconclusive | 776.2 ms (baseline) | +6.5% inconclusive | +10.8% inconclusive | +13.5% inconclusive | n/a |  |
| ef | `ef.new-context.x1k` | -5.5% inconclusive | 28.706 ms (baseline) | +4.7% inconclusive | +20.4% inconclusive | +36.0% inconclusive | n/a |  |
| ef | `ef.pooled-context.x1k` | -20.1% faster | 0.088 ms (baseline) | +11.5% inconclusive | +8.0% inconclusive | +8.7% inconclusive | n/a |  |
| ef | `ef.query-where-orderby-take.x1k` | -2.2% inconclusive | 172.8 ms (baseline) | -1.7% inconclusive | -1.2% inconclusive | -0.9% inconclusive | n/a |  |
| ef | `ef.save-changes-1000-entities.x1` | +2.3% inconclusive | 21.274 ms (baseline) | -3.1% inconclusive | -5.9% inconclusive | -0.8% inconclusive | n/a |  |
| ef | `ef.detect-changes-10k-tracked.x10` | +8.6% inconclusive | 48.729 ms (baseline) | -29.0% faster | -42.1% faster | -31.1% faster | n/a |  |
| ef | `ef.to-query-string.x1k` | -1.0% inconclusive | 9.064 ms (baseline) | -1.8% inconclusive | +20.9% inconclusive | +22.4% inconclusive | n/a |  |
| logging | `logging.cold.factory-create` | -5.4% inconclusive | 39.056 ms (baseline) | +11.1% inconclusive | +10.3% inconclusive | +14.3% inconclusive | +21.5% slower |  |
| logging | `logging.cold.first-log` | -2.9% inconclusive | 6.641 ms (baseline) | +6.5% inconclusive | +14.3% inconclusive | +22.6% inconclusive | +25.1% slower |  |
| logging | `logging.factory-create.x100` | -5.4% inconclusive | 1.902 ms (baseline) | +44.6% inconclusive | +43.9% inconclusive | +42.7% inconclusive | +49.7% slower |  |
| logging | `logging.log-information-3args-enabled.x10k` | +0.6% noise | 3.029 ms (baseline) | -10.5% inconclusive | -14.1% mixed | -12.0% inconclusive | -11.2% inconclusive |  |
| logging | `logging.log-debug-3args-disabled.x10k` | +8.9% inconclusive | 0.440 ms (baseline) | -2.6% inconclusive | -5.2% inconclusive | +0.8% inconclusive | +3.7% inconclusive |  |
| logging | `logging.logger-message-define.x10k` | -1.9% inconclusive | 2.976 ms (baseline) | -37.3% faster | -37.5% faster | -36.7% faster | -35.7% faster |  |
| logging | `logging.begin-scope.x10k` | +7.9% inconclusive | 0.634 ms (baseline) | -3.3% inconclusive | -4.1% inconclusive | +1.8% inconclusive | +3.5% inconclusive |  |
| options | `options.cold.first-value` | +13.6% inconclusive | 25.024 ms (baseline) | +11.3% inconclusive | +10.7% inconclusive | +16.5% inconclusive | +26.7% slower |  |
| options | `options.ioptions-value.x10k` | -2.2% inconclusive | 0.064 ms (baseline) | -2.0% inconclusive | -1.4% inconclusive | -1.3% inconclusive | -0.6% noise |  |
| options | `options.monitor-current-value.x10k` | +149.6% slower | 0.124 ms (baseline) | -10.5% faster | -10.9% inconclusive | -11.5% faster | -9.7% faster |  |
| options | `options.snapshot-new-scope.x1k` | +8.9% inconclusive | 40.935 ms (baseline) | -5.5% inconclusive | -6.6% inconclusive | -4.4% inconclusive | +160.5% slower |  |
| options | `options.validate-data-annotations.x1k` | +15.7% inconclusive | 31.666 ms (baseline) | +4.7% inconclusive | +2.2% inconclusive | +3.4% inconclusive | +25.4% slower |  |

### 4.3 Allocated bytes per batch

Allocation counts are essentially deterministic (they come from `GC.GetAllocatedBytesForCurrentThread`), so they do not depend on the governor. Percentages are against `e0-7`.

| scope (bytes per batch) | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 |
|---|---|---|---|---|---|---|
| `di.cold.build100` | 156,368 (-0%) | 156,376 | 160,192 (+2%) | 160,192 (+2%) | 160,192 (+2%) | 160,192 (+2%) |
| `di.cold.first-resolve-depth5` | 8,480 (+0%) | 8,480 | 8,848 (+4%) | 8,848 (+4%) | 8,848 (+4%) | 9,080 (+7%) |
| `di.build100.x100` | 2,489,600 (-0%) | 2,490,400 | 2,872,000 (+15%) | 2,872,000 (+15%) | 2,872,000 (+15%) | 2,872,000 (+15%) |
| `di.build1000.x10` | 2,151,760 (-0%) | 2,151,840 | 2,524,480 (+17%) | 2,524,480 (+17%) | 2,524,480 (+17%) | 2,524,480 (+17%) |
| `di.validate-on-build100.x100` | 7,656,000 (-0%) | 7,656,800 | 8,592,000 (+12%) | 10,134,400 (+32%) | 10,134,400 (+32%) | 10,134,400 (+32%) |
| `di.first-resolve-depth5.x100` | 319,200 (+0%) | 319,200 | 336,992 (+6%) | 336,992 (+6%) | 336,992 (+6%) | 336,992 (+6%) |
| `di.transient.x10k` | 240,000 (+0%) | 240,000 | 240,000 (+0%) | 240,000 (+0%) | 240,000 (+0%) | 240,000 (+0%) |
| `di.scoped.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `di.singleton.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `di.open-generic.x10k` | 240,000 (+0%) | 240,000 | 240,000 (+0%) | 240,000 (+0%) | 240,000 (+0%) | 240,000 (+0%) |
| `di.enumerable20.x10k` | 6,640,000 (+0%) | 6,640,000 | 6,640,000 (+0%) | 6,640,000 (+0%) | 6,640,000 (+0%) | 6,640,000 (+0%) |
| `di.create-scope-dispose.x10k` | 1,280,000 (+0%) | 1,280,000 | 1,280,000 (+0%) | 1,280,000 (+0%) | 1,280,000 (+0%) | 1,280,000 (+0%) |
| `config.cold.build-json2000` | 903,136 (+0%) | 903,112 | 903,176 (+0%) | 903,176 (+0%) | 903,176 (+0%) | 903,280 (+0%) |
| `config.cold.bind-poco50` | 318,272 (+37%) | 232,456 | 230,328 (-1%) | 231,528 (-0%) | 231,528 (-0%) | 241,624 (+4%) |
| `config.build-json2000.x10` | 6,365,248 (-0%) | 6,365,408 | 6,365,568 (+0%) | 6,365,568 (+0%) | 6,365,568 (+0%) | 6,365,568 (+0%) |
| `config.indexer.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `config.get-section-deep.x10k` | 400,000 (+0%) | 400,000 | 400,000 (+0%) | 400,000 (+0%) | 400,000 (+0%) | 400,000 (+0%) |
| `config.bind-poco50.x1k` | 22,699,940 (+18%) | 19,240,000 | 17,200,000 (-11%) | 18,400,000 (-4%) | 18,400,000 (-4%) | 28,400,000 (+48%) |
| `config.reload-json2000.x10` | 6,362,688 (+0%) | 6,362,688 | 6,362,928 (+0%) | 6,362,928 (+0%) | 6,362,928 (+0%) | 6,362,928 (+0%) |
| `options.cold.first-value` | 455,088 (+19%) | 382,264 | 379,800 (-1%) | 381,000 (-0%) | 381,024 (-0%) | 392,416 (+3%) |
| `options.ioptions-value.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `options.monitor-current-value.x10k` | 960,000 | 0 | 0 | 0 | 0 | 0 |
| `options.snapshot-new-scope.x1k` | 23,808,000 (+17%) | 20,280,000 | 18,264,000 (-10%) | 19,464,000 (-4%) | 19,504,000 (-4%) | 29,472,000 (+45%) |
| `options.validate-data-annotations.x1k` | 29,808,000 (+43%) | 20,824,000 | 21,137,536 (+2%) | 21,233,536 (+2%) | 21,273,536 (+2%) | 22,009,920 (+6%) |
| `logging.cold.factory-create` | 172,352 (-1%) | 173,928 | 176,432 (+1%) | 176,456 (+1%) | 176,576 (+2%) | 175,392 (+1%) |
| `logging.cold.first-log` | 4,288 (-1%) | 4,312 | 5,144 (+19%) | 5,112 (+19%) | 5,112 (+19%) | 5,112 (+19%) |
| `logging.factory-create.x100` | 1,710,472 (-5%) | 1,796,072 | 2,013,064 (+12%) | 2,013,064 (+12%) | 2,021,864 (+13%) | 1,886,664 (+5%) |
| `logging.log-information-3args-enabled.x10k` | 1,951,120 (+0%) | 1,951,120 | 1,951,120 (+0%) | 1,951,120 (+0%) | 1,951,120 (+0%) | 1,951,120 (+0%) |
| `logging.log-debug-3args-disabled.x10k` | 960,000 (+0%) | 960,000 | 960,000 (+0%) | 960,000 (+0%) | 960,000 (+0%) | 960,000 (+0%) |
| `logging.logger-message-define.x10k` | 1,471,120 (+0%) | 1,471,120 | 991,120 (-33%) | 991,120 (-33%) | 991,120 (-33%) | 991,120 (-33%) |
| `logging.begin-scope.x10k` | 960,000 (+0%) | 960,000 | 960,000 (+0%) | 960,000 (+0%) | 1,040,000 (+8%) | 1,040,000 (+8%) |
| `cache.cold.create-and-first-set` | 136,040 (+1%) | 134,200 | 134,432 (+0%) | 134,432 (+0%) | 134,448 (+0%) | 134,584 (+0%) |
| `cache.set.x10k` | 4,047,320 (+42%) | 2,849,504 | 2,844,992 (-0%) | 2,844,992 (-0%) | 2,844,992 (-0%) | 2,844,992 (-0%) |
| `cache.try-get-hit.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `cache.try-get-miss.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `cache.get-or-create.x10k` | 4,047,320 (+42%) | 2,849,504 | 2,844,992 (-0%) | 2,844,992 (-0%) | 2,844,992 (-0%) | 2,844,992 (-0%) |
| `cache.size-limit-compact-half.x1` | 918,288 (+0%) | 918,104 | 918,288 (+0%) | 918,288 (+0%) | 656,176 (-29%) | 656,176 (-29%) |
| `cache.parallel8-hit-miss.per-thread-x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `ef.cold.open-connection-create-schema` | 812,200 (-1%) | 820,360 | 763,120 (-7%) | 787,120 (-4%) | 787,304 (-4%) | n/a |
| `ef.cold.first-query` | 2,154,420 (-8%) | 2,337,304 | 2,677,616 (+15%) | 2,791,732 (+19%) | 2,782,860 (+19%) | n/a |
| `ef.new-context.x1k` | 32,176,176 (-3%) | 33,112,176 | 35,482,872 (+7%) | 36,284,216 (+10%) | 37,500,216 (+13%) | n/a |
| `ef.pooled-context.x1k` | 0 | 0 | 0 | 0 | 0 | n/a |
| `ef.query-where-orderby-take.x1k` | 12,685,752 (-0%) | 12,709,752 | 12,893,248 (+1%) | 15,093,248 (+19%) | 15,501,248 (+22%) | n/a |
| `ef.save-changes-1000-entities.x1` | 10,693,088 (+27%) | 8,438,344 | 8,054,136 (-5%) | 7,758,136 (-8%) | 7,862,136 (-7%) | n/a |
| `ef.detect-changes-10k-tracked.x10` | 82,402,160 (+45%) | 56,802,160 | 45,602,160 (-20%) | 32,802,160 (-42%) | 32,802,160 (-42%) | n/a |
| `ef.to-query-string.x1k` | 4,912,832 (-1%) | 4,968,832 | 5,152,664 (+4%) | 7,280,720 (+47%) | 7,512,720 (+51%) | n/a |

## 5. Two resource scenarios: full vs limited

Same code, same statistics (`analysis/stats.py`, same seed), run separately on the per-process data of each scenario: **full** = 19 rounds, 78 processes per point; **limited** = 11 rounds, 33 processes per point. The re-computed files are in the session scratchpad, not in the repository; `stats.py --summary <dir>` on a directory with `*-runs.csv` filtered by round reproduces them.

### 5.0 Scenario definitions

| | full resources | limited resources (stock settings) |
|---|---|---|
| CPU governor | `performance` | `powersave` (the Ubuntu 22.04 default of this machine) |
| turbo | off (`intel_pstate/no_turbo = 1`) | on |
| how to reproduce | `./scripts/setup-stand.sh --apply` | `./scripts/setup-stand.sh --restore` (back to the machine's defaults) |
| recorded in | `env.json` of every round (`governors`, `turbo_enabled`) | same |
| rounds / processes per point | 19 / 78 | 11 / 33 |

Same hardware, kernel, runtime, pinning (`taskset -c 2-5`) and code in both. The first capture of the machine state (`results/raw/env.json`, before any tuning) is exactly the limited state, so "limited" means "the machine as an application would meet it without tuning": lower and load-dependent clock, boost on. It does not limit cores, memory or I/O.

Why the scenarios can be compared although they were not interleaved: the full-resource rounds lie on **both sides** of the limited block (rounds 1 to 7 before it, rounds 19 to 30 after it), so a slow drift over the two days would show up as a difference between those two groups. Section 5.4 tests this directly.

### 5.1 Verdict counts per scenario

Full resources:

| epoch | point (b-net11 is Track B) | faster | slower | mixed | noise | inconclusive | scopes |
|---|---|---|---|---|---|---|---|
| E0 | e0-6 | 2 | 12 | 0 | 21 | 11 | 46 |
| E1 | e1-8 | 7 | 12 | 1 | 12 | 14 | 46 |
| E1 | e1-9 | 8 | 11 | 1 | 13 | 13 | 46 |
| E2 | b-net11 | 4 | 5 | 0 | 17 | 4 | 30 |
| E2 | e2-10 | 9 | 13 | 0 | 10 | 14 | 46 |
| E2 | e2-11 | 6 | 12 | 1 | 8 | 11 | 38 |

Limited resources:

| epoch | point (b-net11 is Track B) | faster | slower | mixed | noise | inconclusive | scopes |
|---|---|---|---|---|---|---|---|
| E0 | e0-6 | 2 | 2 | 0 | 0 | 42 | 46 |
| E1 | e1-8 | 7 | 1 | 0 | 0 | 38 | 46 |
| E1 | e1-9 | 11 | 1 | 0 | 0 | 34 | 46 |
| E2 | b-net11 | 2 | 4 | 0 | 8 | 16 | 30 |
| E2 | e2-10 | 11 | 1 | 0 | 0 | 34 | 46 |
| E2 | e2-11 | 8 | 13 | 0 | 4 | 13 | 38 |

### 5.2 How much the scenario matters

Across 252 comparisons against the baseline (Track A and Track B), the primary-metric change has the opposite sign in the two scenarios in **46** cases; the absolute gap between scenarios has median 4.9 and maximum 28.0 percentage points. Verdict cross-tabulation:

| performance-only ↓ / powersave-only → | faster | inconclusive | noise | slower |
|---|---|---|---|---|
| faster | 28 | 8 | 0 | 0 |
| inconclusive | 13 | 52 | 1 | 1 |
| mixed | 0 | 3 | 0 | 0 |
| noise | 0 | 70 | 11 | 0 |
| slower | 0 | 44 | 0 | 21 |

By point:

| point | comparisons | median abs gap (pp) | max abs gap (pp) | sign flips | median change, all rounds | median change, performance-only | median change, powersave-only |
|---|---|---|---|---|---|---|---|
| b-net11 | 30 | 0.9 | 13.1 | 3 | -1.2% | -1.4% | +0.0% |
| e0-6 | 46 | 5.1 | 28.0 | 12 | -1.4% | +1.0% | -4.5% |
| e1-8 | 46 | 6.0 | 19.4 | 7 | -2.1% | -0.2% | -6.0% |
| e1-9 | 46 | 6.8 | 14.1 | 8 | -2.9% | -0.3% | -6.4% |
| e2-10 | 46 | 5.7 | 13.9 | 13 | -1.2% | +1.3% | -5.2% |
| e2-11 | 38 | 1.3 | 10.7 | 3 | +0.4% | +1.1% | +0.2% |

Largest differences between scenarios at `e1-9` and `e2-10`:

| scope | point | all rounds | performance-only | powersave-only | verdict (all) | verdict (perf) | verdict (save) |
|---|---|---|---|---|---|---|---|
| `di.singleton.x10k` | e1-9 | -11.7% | -6.3% | -20.4% | faster | inconclusive | faster |
| `di.singleton.x10k` | e2-10 | -11.3% | -5.9% | -19.8% | inconclusive | inconclusive | faster |
| `options.monitor-current-value.x10k` | e1-9 | -10.9% | -5.6% | -19.2% | inconclusive | inconclusive | faster |
| `di.open-generic.x10k` | e1-9 | -8.8% | -3.6% | -17.0% | inconclusive | inconclusive | faster |
| `di.open-generic.x10k` | e2-10 | -7.5% | -2.6% | -15.2% | inconclusive | inconclusive | faster |
| `di.transient.x10k` | e1-9 | -11.9% | -7.5% | -19.0% | inconclusive | inconclusive | faster |
| `di.transient.x10k` | e2-10 | -11.1% | -6.8% | -18.1% | inconclusive | inconclusive | faster |
| `di.validate-on-build100.x100` | e2-10 | +59.3% | +63.5% | +52.3% | inconclusive | slower | inconclusive |
| `di.first-resolve-depth5.x100` | e1-9 | +53.7% | +57.9% | +46.9% | inconclusive | slower | inconclusive |
| `di.validate-on-build100.x100` | e1-9 | +56.1% | +60.2% | +49.4% | inconclusive | slower | inconclusive |
| `logging.log-debug-3args-disabled.x10k` | e2-10 | +0.8% | +4.8% | -5.8% | inconclusive | inconclusive | inconclusive |
| `logging.log-debug-3args-disabled.x10k` | e1-9 | -5.2% | -1.5% | -11.2% | inconclusive | noise | inconclusive |
| `cache.set.x10k` | e1-9 | -13.0% | -9.4% | -18.9% | faster | faster | faster |
| `ef.to-query-string.x1k` | e1-9 | +20.9% | +24.4% | +15.0% | inconclusive | slower | inconclusive |
| `di.first-resolve-depth5.x100` | e2-10 | +56.7% | +60.2% | +50.9% | inconclusive | slower | inconclusive |

Reading: some conclusions hold in both scenarios (for example `cache.try-get-miss`, `logging.logger-message-define` and `ef.detect-changes-10k-tracked`). Others appear only under limited resources: in the DI resolve paths (`di.singleton`, `di.transient`, `di.open-generic`) the newer versions look about 17 to 20% faster than 7.0 when the CPU is limited but only about 4 to 8% faster at full speed. Conversely `di.validate-on-build100` and `di.first-resolve-depth5.x100` get much slower in the newer versions at full resources and only inconclusive when limited, mostly because the limited set has fewer processes and wider intervals. Fewer processes widen the CI, so "inconclusive" in the limited scenario is partly a sample-size effect, not evidence of no change.

### 5.3 Track A per scenario, change vs `e0-7`

Cells as in 4.2.

#### Full resources (performance, turbo off)

| scenario | scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 | multimodal |
|---|---|---|---|---|---|---|---|---|
| cache | `cache.cold.create-and-first-set` | +151.4% slower | 5.544 ms (baseline) | +8.7% slower | +10.6% slower | +13.0% slower | +26.6% slower |  |
| cache | `cache.set.x10k` | +55.2%† slower | 2.858 ms (baseline) | -9.1%† faster | -9.4%† faster | -5.3%† inconclusive | -6.6%† inconclusive | yes |
| cache | `cache.try-get-hit.x10k` | +10.6% slower | 1.112 ms (baseline) | -16.4% faster | -17.3% faster | -14.0% faster | -15.2% inconclusive |  |
| cache | `cache.try-get-miss.x10k` | +2.5% noise | 0.880 ms (baseline) | -16.5% faster | -16.6% faster | -22.4% faster | -22.2% faster |  |
| cache | `cache.get-or-create.x10k` | +25.2%† slower | 3.352 ms (baseline) | -12.7%† faster | -13.3%† faster | -9.0%† faster | -8.5%† faster | yes |
| cache | `cache.size-limit-compact-half.x1` | -4.8% inconclusive | 3.617 ms (baseline) | -7.7% inconclusive | -8.7% inconclusive | -21.0% faster | -20.9% faster |  |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | +9.7% slower | 1.442 ms (baseline) | -5.1% inconclusive | -5.1% inconclusive | -4.4% inconclusive | -5.3% inconclusive |  |
| config | `config.cold.build-json2000` | +2.0% noise | 14.676 ms (baseline) | +2.3% inconclusive | +2.1% noise | +2.7% inconclusive | +76.3% slower |  |
| config | `config.cold.bind-poco50` | +32.8% slower | 21.708 ms (baseline) | -2.3% inconclusive | -1.7% inconclusive | +1.1% inconclusive | +3.2% inconclusive |  |
| config | `config.build-json2000.x10` | -1.1% noise | 9.543 ms (baseline) | +3.0% inconclusive | +6.3% inconclusive | +7.6% slower | +2.3% noise |  |
| config | `config.indexer.x10k` | -0.5% noise | 0.583 ms (baseline) | -0.2% noise | +0.0% noise | +0.2% noise | -1.1% noise |  |
| config | `config.get-section-deep.x10k` | -0.2% noise | 0.397 ms (baseline) | +0.6% inconclusive | -0.1% noise | -0.6% noise | +0.9% noise |  |
| config | `config.bind-poco50.x1k` | +14.4% slower | 61.085 ms (baseline) | -1.1% noise | -3.4% noise | -1.4% noise | +137.8% slower |  |
| config | `config.reload-json2000.x10` | +0.8% noise | 7.300 ms (baseline) | +7.0% slower | +2.1% noise | +3.8% noise | +0.1% noise |  |
| di | `di.cold.build100` | +1.3% inconclusive | 0.618 ms (baseline) | +0.9% noise | +1.6% noise | +4.6% inconclusive | +6.5% inconclusive |  |
| di | `di.cold.first-resolve-depth5` | +0.1% noise | 8.215 ms (baseline) | +13.2% slower | +14.0% slower | +18.3% slower | +27.4% slower |  |
| di | `di.build100.x100` | +2.6%† inconclusive | 0.885 ms (baseline) | +2.9%† inconclusive | +7.5%† inconclusive | +9.1%† inconclusive | +12.7%† inconclusive | yes |
| di | `di.build1000.x10` | +4.0%† inconclusive | 0.759 ms (baseline) | +16.8%† slower | -26.6%† faster | -25.3%† faster | -26.2%† faster | yes |
| di | `di.validate-on-build100.x100` | -1.5% noise | 4.038 ms (baseline) | +19.5% slower | +60.2% slower | +63.5% slower | +61.7% slower |  |
| di | `di.first-resolve-depth5.x100` | +0.1% noise | 0.327 ms (baseline) | +62.8% slower | +57.9% slower | +60.2% slower | +84.9% slower |  |
| di | `di.transient.x10k` | -1.1% noise | 0.250 ms (baseline) | -4.7% inconclusive | -7.5% inconclusive | -6.8% inconclusive | -6.9% inconclusive |  |
| di | `di.scoped.x10k` | +1.4% noise | 0.564 ms (baseline) | -1.3% inconclusive | -1.3% inconclusive | -3.1% inconclusive | -0.8% noise |  |
| di | `di.singleton.x10k` | -1.0% noise | 0.194 ms (baseline) | -1.4% noise | -6.3% inconclusive | -5.9% inconclusive | -5.8% inconclusive |  |
| di | `di.open-generic.x10k` | -1.5% inconclusive | 0.278 ms (baseline) | -3.3% inconclusive | -3.6% inconclusive | -2.6% inconclusive | -4.0% inconclusive |  |
| di | `di.enumerable20.x10k` | -1.0% noise | 4.218 ms (baseline) | -1.2% noise | -0.6% noise | +0.1% noise | +0.5% noise |  |
| di | `di.create-scope-dispose.x10k` | -13.6%† faster | 0.760 ms (baseline) | -11.2%† faster | -14.5%† faster | -13.7%† faster | -15.7%† faster | yes |
| ef | `ef.cold.open-connection-create-schema` | -2.6% noise | 63.052 ms (baseline) | +0.4% noise | +0.0% noise | +1.5% noise | n/a |  |
| ef | `ef.cold.first-query` | -5.7% inconclusive | 996.0 ms (baseline) | +9.1% slower | +13.4% slower | +16.6% slower | n/a |  |
| ef | `ef.new-context.x1k` | -3.6% noise | 36.642 ms (baseline) | +7.0% slower | +23.1% slower | +39.1% slower | n/a |  |
| ef | `ef.pooled-context.x1k` | -17.8% faster | 0.113 ms (baseline) | +11.9% inconclusive | +9.8% slower | +9.4% slower | n/a |  |
| ef | `ef.query-where-orderby-take.x1k` | +0.3% noise | 220.0 ms (baseline) | +0.8% noise | +1.5% noise | +1.7% noise | n/a |  |
| ef | `ef.save-changes-1000-entities.x1` | +4.3% inconclusive | 27.062 ms (baseline) | -0.7% noise | -3.2% noise | +1.7% noise | n/a |  |
| ef | `ef.detect-changes-10k-tracked.x10` | +10.7% slower | 61.211 ms (baseline) | -27.9% faster | -40.8% faster | -29.5% faster | n/a |  |
| ef | `ef.to-query-string.x1k` | +3.0% noise | 11.408 ms (baseline) | +1.1% noise | +24.4% slower | +25.7% slower | n/a |  |
| logging | `logging.cold.factory-create` | -3.8% inconclusive | 50.470 ms (baseline) | +12.7% slower | +11.9% inconclusive | +16.5% slower | +20.3% slower |  |
| logging | `logging.cold.first-log` | -0.9% noise | 8.538 ms (baseline) | +8.8% slower | +16.5% slower | +24.8% slower | +24.6% slower |  |
| logging | `logging.factory-create.x100` | -4.2% inconclusive | 2.327 ms (baseline) | +46.8% slower | +45.9% slower | +44.8% slower | +50.5% slower |  |
| logging | `logging.log-information-3args-enabled.x10k` | +1.2% noise | 3.816 ms (baseline) | -10.5% mixed | -13.1% mixed | -11.3% inconclusive | -12.0% mixed |  |
| logging | `logging.log-debug-3args-disabled.x10k` | +10.5% inconclusive | 0.524 ms (baseline) | -0.4% noise | -1.5% noise | +4.8% inconclusive | +5.3% inconclusive |  |
| logging | `logging.logger-message-define.x10k` | -0.3% noise | 3.769 ms (baseline) | -36.4% faster | -36.6% faster | -35.4% faster | -36.0% faster |  |
| logging | `logging.begin-scope.x10k` | +7.0% inconclusive | 0.774 ms (baseline) | -1.9% noise | -2.5% noise | +3.6% inconclusive | +2.6% noise |  |
| options | `options.cold.first-value` | +16.1% slower | 32.285 ms (baseline) | +13.6% slower | +13.3% slower | +17.9% slower | +26.9% slower |  |
| options | `options.ioptions-value.x10k` | -0.1% noise | 0.080 ms (baseline) | -0.2% noise | +1.8% noise | +0.2% noise | +1.2% noise |  |
| options | `options.monitor-current-value.x10k` | +139.6% slower | 0.163 ms (baseline) | -5.4% inconclusive | -5.6% inconclusive | -8.0% faster | -6.8% inconclusive |  |
| options | `options.snapshot-new-scope.x1k` | +10.4% slower | 51.704 ms (baseline) | -4.5% inconclusive | -4.3% inconclusive | -2.9% noise | +162.2% slower |  |
| options | `options.validate-data-annotations.x1k` | +17.7% slower | 39.844 ms (baseline) | +7.1% inconclusive | +5.2% inconclusive | +5.9% inconclusive | +26.2% slower |  |

#### Limited resources (powersave, turbo on)

| scenario | scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 | multimodal |
|---|---|---|---|---|---|---|---|---|
| cache | `cache.cold.create-and-first-set` | +141.0% slower | 2.788 ms (baseline) | +2.0% inconclusive | +2.3% inconclusive | +8.0% inconclusive | +25.8% slower |  |
| cache | `cache.set.x10k` | +49.6%† inconclusive | 1.505 ms (baseline) | -15.7%† faster | -18.9%† faster | -9.2%† inconclusive | -6.7%† inconclusive | yes |
| cache | `cache.try-get-hit.x10k` | +16.7% inconclusive | 0.577 ms (baseline) | -23.8% faster | -24.3% faster | -15.8% faster | -14.2% faster |  |
| cache | `cache.try-get-miss.x10k` | -2.5% inconclusive | 0.447 ms (baseline) | -21.2% faster | -21.5% faster | -26.0% faster | -21.6% faster |  |
| cache | `cache.get-or-create.x10k` | +25.1%† inconclusive | 1.713 ms (baseline) | -16.8%† faster | -17.4%† faster | -7.8%† inconclusive | -3.7%† inconclusive | yes |
| cache | `cache.size-limit-compact-half.x1` | -7.3%† inconclusive | 1.893 ms (baseline) | -8.9%† inconclusive | -8.9%† inconclusive | -31.6%† faster | -26.8%† faster | yes |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | +5.8% inconclusive | 0.739 ms (baseline) | -10.9% inconclusive | -11.6% inconclusive | -10.1% inconclusive | -6.6% inconclusive |  |
| config | `config.cold.build-json2000` | -4.5% inconclusive | 7.694 ms (baseline) | -2.6% inconclusive | -4.9% inconclusive | -2.9% inconclusive | +75.1% slower |  |
| config | `config.cold.bind-poco50` | +24.3% inconclusive | 11.059 ms (baseline) | -9.1% inconclusive | -9.0% inconclusive | -5.3% inconclusive | +4.3% inconclusive |  |
| config | `config.build-json2000.x10` | -3.5% inconclusive | 5.164 ms (baseline) | -1.9% inconclusive | +0.1% inconclusive | +1.9% inconclusive | +2.3% inconclusive |  |
| config | `config.indexer.x10k` | -6.4% inconclusive | 0.295 ms (baseline) | -5.6% inconclusive | -6.1% inconclusive | -4.5% inconclusive | -1.4% noise |  |
| config | `config.get-section-deep.x10k` | -6.2% inconclusive | 0.213 ms (baseline) | -3.9% inconclusive | -4.3% inconclusive | -5.2% inconclusive | +0.6% noise |  |
| config | `config.bind-poco50.x1k` | +10.2% inconclusive | 31.985 ms (baseline) | -6.8% inconclusive | -8.6% inconclusive | -7.3% inconclusive | +133.8% slower |  |
| config | `config.reload-json2000.x10` | -2.2% inconclusive | 3.926 ms (baseline) | +1.9% inconclusive | -3.6% inconclusive | -2.0% inconclusive | -1.7% noise |  |
| di | `di.cold.build100` | -4.8% inconclusive | 0.306 ms (baseline) | -5.6% inconclusive | -5.1% inconclusive | -2.7% inconclusive | +5.9% inconclusive |  |
| di | `di.cold.first-resolve-depth5` | -4.9% inconclusive | 4.106 ms (baseline) | +4.5% inconclusive | +5.5% inconclusive | +9.0% inconclusive | +26.4% slower |  |
| di | `di.build100.x100` | -3.8%† inconclusive | 0.503 ms (baseline) | +5.2%† inconclusive | +6.4%† inconclusive | +10.3%† inconclusive | +19.7%† slower | yes |
| di | `di.build1000.x10` | +0.6%† inconclusive | 0.407 ms (baseline) | +21.6%† inconclusive | -22.6%† inconclusive | -22.6%† faster | -17.2%† inconclusive | yes |
| di | `di.validate-on-build100.x100` | -5.3% inconclusive | 2.138 ms (baseline) | +12.2% inconclusive | +49.4% inconclusive | +52.3% inconclusive | +61.8% slower |  |
| di | `di.first-resolve-depth5.x100` | -5.7% inconclusive | 0.175 ms (baseline) | +53.6% inconclusive | +46.9% inconclusive | +50.9% inconclusive | +85.6% slower |  |
| di | `di.transient.x10k` | -3.6% inconclusive | 0.149 ms (baseline) | +4.0%† inconclusive | -19.0% faster | -18.1% faster | -13.4% faster | yes |
| di | `di.scoped.x10k` | -7.2% inconclusive | 0.298 ms (baseline) | +10.8%† inconclusive | -9.7% inconclusive | -7.8% inconclusive | -0.2% inconclusive | yes |
| di | `di.singleton.x10k` | -6.0% inconclusive | 0.107 ms (baseline) | -0.5%† inconclusive | -20.4% faster | -19.8% faster | -16.5% faster | yes |
| di | `di.open-generic.x10k` | -4.7% inconclusive | 0.164 ms (baseline) | -7.1%† inconclusive | -17.0% faster | -15.2% faster | -10.7% faster | yes |
| di | `di.enumerable20.x10k` | -5.6% inconclusive | 2.213 ms (baseline) | -6.8% inconclusive | -8.4% inconclusive | -7.2% inconclusive | -0.8% noise |  |
| di | `di.create-scope-dispose.x10k` | -17.6%† faster | 0.457 ms (baseline) | -10.9%† inconclusive | -15.4%† faster | -16.1%† faster | -14.9%† inconclusive | yes |
| ef | `ef.cold.open-connection-create-schema` | -6.1% inconclusive | 32.560 ms (baseline) | -6.2% inconclusive | -5.3% inconclusive | -5.7% inconclusive | n/a |  |
| ef | `ef.cold.first-query` | -7.9% inconclusive | 504.6 ms (baseline) | +2.2% inconclusive | +6.4% inconclusive | +8.4% inconclusive | n/a |  |
| ef | `ef.new-context.x1k` | -8.7% inconclusive | 18.831 ms (baseline) | +0.9% inconclusive | +15.8% inconclusive | +30.8% inconclusive | n/a |  |
| ef | `ef.pooled-context.x1k` | -23.9% faster | 0.058 ms (baseline) | +10.9% inconclusive | +4.9% inconclusive | +7.5% inconclusive | n/a |  |
| ef | `ef.query-where-orderby-take.x1k` | -6.5% inconclusive | 113.9 ms (baseline) | -5.8% inconclusive | -5.6% inconclusive | -5.3% inconclusive | n/a |  |
| ef | `ef.save-changes-1000-entities.x1` | -1.0% inconclusive | 14.038 ms (baseline) | -7.2% inconclusive | -10.3% inconclusive | -5.0% inconclusive | n/a |  |
| ef | `ef.detect-changes-10k-tracked.x10` | +5.0% inconclusive | 32.864 ms (baseline) | -30.9% faster | -44.4% faster | -33.7% faster | n/a |  |
| ef | `ef.to-query-string.x1k` | -7.6% inconclusive | 6.091 ms (baseline) | -6.7% inconclusive | +15.0% inconclusive | +17.1% inconclusive | n/a |  |
| logging | `logging.cold.factory-create` | -8.0% inconclusive | 25.082 ms (baseline) | +8.3% inconclusive | +7.8% inconclusive | +10.5% inconclusive | +23.7% slower |  |
| logging | `logging.cold.first-log` | -6.4% inconclusive | 4.302 ms (baseline) | +2.6% inconclusive | +10.4% inconclusive | +18.9% inconclusive | +25.9% slower |  |
| logging | `logging.factory-create.x100` | -8.8%† inconclusive | 1.341 ms (baseline) | +47.5%† slower | +47.6%† slower | +49.2%† slower | +57.4%† slower | yes |
| logging | `logging.log-information-3args-enabled.x10k` | -0.6% inconclusive | 2.032 ms (baseline) | +4.7%† inconclusive | -0.9%† inconclusive | -13.0% inconclusive | -9.9% inconclusive | yes |
| logging | `logging.log-debug-3args-disabled.x10k` | +0.8%† inconclusive | 0.325 ms (baseline) | -1.0%† inconclusive | -3.1%† inconclusive | -1.9%† inconclusive | +1.1%† inconclusive | yes |
| logging | `logging.logger-message-define.x10k` | -4.5% inconclusive | 1.980 ms (baseline) | -38.9% faster | -39.1% faster | -39.0% faster | -35.3% faster |  |
| logging | `logging.begin-scope.x10k` | +9.4% inconclusive | 0.449 ms (baseline) | -5.6% inconclusive | -6.8% inconclusive | -1.3% inconclusive | +5.2% inconclusive |  |
| options | `options.cold.first-value` | +9.5% inconclusive | 16.116 ms (baseline) | +7.5% inconclusive | +6.4% inconclusive | +14.0% inconclusive | +26.4% slower |  |
| options | `options.ioptions-value.x10k` | -5.8% inconclusive | 0.043 ms (baseline) | -5.1% inconclusive | -6.7% inconclusive | -3.8% inconclusive | -3.7% inconclusive |  |
| options | `options.monitor-current-value.x10k` | +167.7% slower | 0.077 ms (baseline) | -18.6% faster | -19.2% faster | -17.2% faster | -14.5% faster |  |
| options | `options.snapshot-new-scope.x1k` | +6.2% inconclusive | 27.346 ms (baseline) | -7.1% inconclusive | -10.4% inconclusive | -6.9% inconclusive | +157.7% slower |  |
| options | `options.validate-data-annotations.x1k` | +12.2% inconclusive | 21.295 ms (baseline) | +0.7% inconclusive | -2.7% inconclusive | -0.8% inconclusive | +23.9% slower |  |

### 5.4 Is the scenario effect real, or just drift?

For each of the 252 comparisons, per round the change of the point against the baseline is computed inside the round (same session), then the 19 full rounds are compared with the 11 limited rounds (Mann-Whitney U on per-round log ratios, Benjamini-Hochberg across all 252). As a calibration, the same test is applied to a split that is **not** a scenario change: the 7 full rounds before the limited block against the 12 full rounds after it.

| comparison | significant after BH (q < 0.05) | raw p < 0.05 | median gap between groups (pp) |
|---|---:|---:|---:|
| full vs limited | 29 of 252 | 55 | 1.4 |
| full, first 7 rounds vs last 12 rounds (drift) | 1 of 252 | 15 | 0.7 |

The scenario split separates the data much more than the time split does, so the scenario effect is not explained by slow drift on this machine. Scopes with q < 0.05 for full vs limited (largest first, at most 20 shown):

| scope | point | full | limited | full, first 7 rounds | full, last 12 rounds | q (full vs limited) | q (first vs last) |
|---|---|---|---|---|---|---|---|
| `di.singleton.x10k` | e1-8 | -1.0% | -16.5% | -0.9% | -1.3% | 0.0009 | 0.94 |
| `di.singleton.x10k` | e1-9 | -5.5% | -14.7% | -5.9% | -5.4% | 0.0009 | 0.89 |
| `di.open-generic.x10k` | e2-10 | -2.2% | -12.0% | -2.8% | -1.9% | 0.0009 | 0.99 |
| `di.singleton.x10k` | e2-10 | -6.7% | -14.6% | -6.7% | -6.6% | 0.0009 | 0.96 |
| `di.open-generic.x10k` | e1-8 | -3.9% | -13.6% | -3.3% | -4.0% | 0.0009 | 0.82 |
| `ef.to-query-string.x1k` | e0-6 | +3.2% | -3.0% | +2.4% | +3.3% | 0.0009 | 0.75 |
| `channels.bounded1024-1p4c.x100k-items` | b-net11 | -11.7% | +3.0% | -11.6% | -11.9% | 0.0009 | 0.89 |
| `options.monitor-current-value.x10k` | e1-9 | -4.3% | -13.7% | -9.2% | -3.5% | 0.0015 | 0.59 |
| `options.monitor-current-value.x10k` | e1-8 | -4.5% | -16.0% | -6.1% | -3.4% | 0.0015 | 0.70 |
| `di.open-generic.x10k` | e1-9 | -3.6% | -15.3% | -4.8% | -3.4% | 0.0021 | 0.89 |
| `di.transient.x10k` | e1-8 | -5.8% | -16.9% | -6.9% | -5.1% | 0.0021 | 0.95 |
| `di.singleton.x10k` | e2-11 | -5.1% | -17.9% | -4.9% | -5.1% | 0.0027 | 0.95 |
| `options.monitor-current-value.x10k` | e0-6 | +142.6% | +176.8% | +130.9% | +145.8% | 0.0046 | 0.59 |
| `di.enumerable20.x10k` | e2-10 | -0.2% | -2.3% | -0.2% | -0.1% | 0.0046 | 0.96 |
| `di.transient.x10k` | e2-10 | -7.5% | -14.0% | -8.9% | -7.0% | 0.0070 | 0.82 |
| `di.open-generic.x10k` | e2-11 | -4.5% | -10.5% | -2.3% | -4.7% | 0.0091 | 0.70 |
| `options.monitor-current-value.x10k` | e2-10 | -9.2% | -15.1% | -9.8% | -6.8% | 0.0117 | 0.59 |
| `cache.get-or-create.x10k` | e2-11 | -8.6% | -3.3% | -8.3% | -9.5% | 0.0129 | 0.70 |
| `cache.get-or-create.x10k` | e2-10 | -9.1% | -3.2% | -7.0% | -10.1% | 0.0142 | 0.63 |
| `ef.query-where-orderby-take.x1k` | e1-9 | +1.5% | -0.7% | +1.4% | +1.6% | 0.0202 | 0.70 |

The primary metric is taken from the pooled analysis, and the per-round value is the ratio of per-process medians. Recounted this way on the earlier 29 rounds the test gives 29 of 252 and 1 of 252; an earlier draft of this table reported 26 of 252 for those rounds, and that difference in the method was not traced. The gaps in this table use per-round medians of within-round ratios and are smaller than in 5.2, which uses the pooled bootstrap; use one definition consistently in the report. Limits that remain: unequal group sizes, one contiguous limited block, one machine, and "limited" is a power policy, not a quota. These are stated limits of the design, not something the existing data can remove.



## 6. Track B (library + runtime, net10.0 vs net11.0, all 30 rounds)

Every number here covers library and runtime together. Baseline `b-net10`.

| scenario | scope | b-net10 | b-net11 | multimodal |
|---|---|---|---|---|
| trackb-aspnetcore | `aspnetcore.cold.start-kestrel` | 193.1 ms (baseline) | -54.5% faster |  |
| trackb-aspnetcore | `aspnetcore.cold.first-request` | 1446.2 ms (baseline) | +4.7% inconclusive |  |
| trackb-aspnetcore | `aspnetcore.hello.batch400` | 100.5 ms (baseline) | +0.1% noise |  |
| trackb-aspnetcore | `aspnetcore.json.batch400` | 100.5 ms (baseline) | +0.1% noise |  |
| trackb-aspnetcore | `aspnetcore.routes500.batch400` | 100.5 ms (baseline) | +0.1% noise |  |
| trackb-aspnetcore | `aspnetcore.hello.request` | 0.064 ms (baseline) | +85.1%† slower | yes |
| trackb-aspnetcore | `aspnetcore.json.request` | 0.066 ms (baseline) | +83.9%† slower | yes |
| trackb-aspnetcore | `aspnetcore.routes500.request` | 0.069 ms (baseline) | +82.4%† slower | yes |
| trackb-channels | `channels.cold.first-pipeline` | 12.007 ms (baseline) | +0.7% noise |  |
| trackb-channels | `channels.bounded1024-1p4c.x100k-items` | 29.695 ms (baseline) | -7.0% inconclusive |  |
| trackb-ef | `ef.cold.open-connection-create-schema` | 44.204 ms (baseline) | -16.5% faster |  |
| trackb-ef | `ef.cold.first-query` | 717.3 ms (baseline) | +8.5% slower |  |
| trackb-ef | `ef.new-context.x1k` | 39.092 ms (baseline) | +2.6% noise |  |
| trackb-ef | `ef.pooled-context.x1k` | 0.095 ms (baseline) | -2.0% noise |  |
| trackb-ef | `ef.query-where-orderby-take.x1k` | 174.4 ms (baseline) | +1.2% noise |  |
| trackb-ef | `ef.save-changes-1000-entities.x1` | 21.497 ms (baseline) | +0.5% noise |  |
| trackb-ef | `ef.detect-changes-10k-tracked.x10` | 34.078 ms (baseline) | +11.6% slower |  |
| trackb-ef | `ef.to-query-string.x1k` | 11.199 ms (baseline) | -0.6% noise |  |
| trackb-immutable | `immutable.cold.first-build-10k` | 11.547 ms (baseline) | -2.0% noise |  |
| trackb-immutable | `immutable.dictionary-build-10k.x10` | 38.539 ms (baseline) | -1.9% noise |  |
| trackb-immutable | `immutable.builder-add-10k.x10` | 38.742 ms (baseline) | -3.1% noise |  |
| trackb-immutable | `immutable.dictionary-lookup.x10k` | 1.528 ms (baseline) | +1.2% noise |  |
| trackb-stj | `stj.cold.first-serialize-reflection` | 24.982 ms (baseline) | -8.6% inconclusive |  |
| trackb-stj | `stj.serialize-reflection.x10k` | 24.357 ms (baseline) | -4.0% inconclusive |  |
| trackb-stj | `stj.deserialize-reflection.x10k` | 38.337 ms (baseline) | -5.8% inconclusive |  |
| trackb-stj | `stj.serialize-sourcegen.x10k` | 15.699 ms (baseline) | -4.9% inconclusive |  |
| trackb-stj | `stj.deserialize-sourcegen.x10k` | 37.183 ms (baseline) | -5.1% inconclusive |  |
| trackb-stj | `stj.utf8jsonreader-1mb.x10` | 26.010 ms (baseline) | -2.1% noise |  |
| trackb-stj | `stj.jsondocument-parse-1mb.x10` | 41.556 ms (baseline) | -1.8% noise |  |
| trackb-stj | `stj.jsonnode-parse-read.x10k` | 34.580 ms (baseline) | -3.9% inconclusive |  |

## 7. H2: attribution (DependencyInjection and Caching.Memory only)

Range `v7.0.20..v10.0.14`. Source: `results/summary/attribution.md`, `attribution-methods.csv`, `attribution-regressions.csv`.

Classes: `cca` = pull request opened by the Copilot coding agent (GitHub app `copilot-swe-agent`, account id 198982749); `msft-human` = human author with `author_association` MEMBER/OWNER/COLLABORATOR; `community` = any other human; `bot` = any other GitHub app or bot; `no-pr` = commit not associated with a pull request; `pending` = not yet resolved through the API. Flags: `cca_coauthored` = a non-agent PR whose squash commit carries a `copilot-swe-agent` co-author trailer; `copilot_review_coauthored` (app id 175728472) and `copilot_cli_coauthored` (app id 223556219) are reported but not counted as agent work.

Range: v7.0.20..v10.0.14. Commits: 113.

| library | class | commits | PRs | additions | deletions | cca_coauthored | copilot review co-author |
|---|---|---:|---:|---:|---:|---:|---:|
| Caching | msft-human | 18 | 14 | 16265 | 19167 | 0 | 0 |
| Caching | community | 15 | 15 | 1867 | 891 | 0 | 0 |
| DependencyInjection | msft-human | 39 | 35 | 19328 | 20664 | 0 | 0 |
| DependencyInjection | community | 41 | 41 | 9362 | 2400 | 0 | 0 |

`author_association` is a weak proxy for Microsoft employment: private organisation membership is reported as CONTRIBUTOR, and non-employees can be MEMBER of the dotnet organisation. The `microsoft_email` column (commit author e-mail ends with `@microsoft.com`) disagrees with the class for {'msft-human': 17, 'community': 7} commits; treat `msft-human` vs `community` as indicative.

Line statistics source: {'git commit numstat': 113}.

#### Hot methods (git log -L)

| method | file | commits | cca | cca_coauthored | msft-human | community | other |
|---|---|---:|---:|---:|---:|---:|---:|
| `CallSiteFactory.CreateCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 1 | 0 | 0 | 0 | 1 | 0 |
| `CallSiteFactory.TryCreateExact` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 3 | 0 | 0 | 0 | 3 | 0 |
| `CallSiteFactory.TryCreateOpenGeneric` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 2 | 0 | 0 | 0 | 2 | 0 |
| `CallSiteFactory.TryCreateEnumerable` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 8 | 0 | 0 | 1 | 7 | 0 |
| `CallSiteFactory.CreateConstructorCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 3 | 0 | 0 | 1 | 2 | 0 |
| `CallSiteFactory.GetCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteFactory.cs | 1 | 0 | 0 | 0 | 1 | 0 |
| `CallSiteRuntimeResolver.Resolve` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteRuntimeResolver.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CallSiteRuntimeResolver.VisitCache` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteRuntimeResolver.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `ServiceProvider.GetService` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs | 3 | 0 | 0 | 0 | 3 | 0 |
| `ServiceProvider.CreateServiceAccessor` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs | 3 | 0 | 0 | 0 | 3 | 0 |
| `ServiceProvider.CreateScope` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceProvider.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `ServiceProviderEngineScope.GetService` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs | 1 | 0 | 0 | 0 | 1 | 0 |
| `ServiceProviderEngineScope.CaptureDisposable` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs | 1 | 0 | 0 | 1 | 0 | 0 |
| `ServiceProviderEngineScope.Dispose` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/ServiceProviderEngineScope.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `DynamicServiceProviderEngine.RealizeService` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/DynamicServiceProviderEngine.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CallSiteValidator.ValidateCallSite` | src/libraries/Microsoft.Extensions.DependencyInjection/src/ServiceLookup/CallSiteValidator.cs | 2 | 0 | 0 | 0 | 2 | 0 |
| `MemoryCache.SetEntry` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 1 | 1 | 0 |
| `MemoryCache.TryGetValue` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 3 | 0 | 0 | 1 | 2 | 0 |
| `MemoryCache.CreateEntry` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 1 | 1 | 0 |
| `MemoryCache.Remove` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 1 | 1 | 0 |
| `MemoryCache.Compact` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 2 | 0 | 0 | 0 | 2 | 0 |
| `MemoryCache.StartScanForExpiredItemsIfNeeded` | src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CacheEntry.Dispose` | src/libraries/Microsoft.Extensions.Caching.Memory/src/CacheEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 |
| `CacheEntry.CheckExpired` | src/libraries/Microsoft.Extensions.Caching.Memory/src/CacheEntry.cs | 0 | 0 | 0 | 0 | 0 | 0 |

#### Performance labels and reverts

PRs labelled `tenet-performance` or `regression-from-last-release`: 6.
- DependencyInjection #99383 (community, area-Extensions-DependencyInjection;tenet-performance): Improve perf\allocs of ActivatorUtilities.CreateInstance() (#99383)
- DependencyInjection #91290 (community, area-Extensions-DependencyInjection;tenet-performance): Improve perf of ActivatorUtilities.CreateInstance() (#91290)
- DependencyInjection #91133 (msft-human, area-Extensions-DependencyInjection;os-android;tenet-performance): [DependencyInjection] introduce feature switch to disable S.R.E (#91133)
- DependencyInjection #90119 (community, area-Extensions-DependencyInjection;tenet-performance): For perf, use the new ConstructorInvoker APIs for ActivatorUtilities.CreateFactory (#90119)
- DependencyInjection #89573 (community, area-Extensions-DependencyInjection;tenet-performance): For perf, use the new ConstructorInvoker APIs for ActivatorUtilities.CreateFactory (#89573)
- DependencyInjection #85065 (community, arch-wasm;area-System.Reflection;tenet-performance): Use emit-based invoke for all of WASM including DI (#85065)

Revert commits: 1.
- DependencyInjection #89992 (msft-human): Revert "For perf, use the new ConstructorInvoker APIs for ActivatorUtilities.CreateFactory (#89573)" (#89992)

#### Servicing churn in the 12 months after GA

| library | major | GA | window end | complete | days observed | patches | patches touching paths | commits |
|---|---:|---|---|---|---:|---:|---:|---:|
| DependencyInjection | 7 | 2022-11-08 | 2023-11-08 | True | 365 | 12 | 0 | 0 |
| DependencyInjection | 10 | 2025-11-11 | 2026-11-11 | False | 352 | 14 | 0 | 0 |
| Caching | 7 | 2022-11-08 | 2023-11-08 | True | 365 | 12 | 0 | 0 |
| Caching | 10 | 2025-11-11 | 2026-11-11 | False | 352 | 14 | 0 | 0 |

**Observations.**

- No `cca` (coding agent) pull request touched any of the 113 commits in these two libraries, and `cca_coauthored` is 0 everywhere. All 24 hot methods fall in `msft-human`, `community` or untouched classes, so there are **zero treated methods** and the difference-in-differences of H2 cannot be estimated for DI and Caching. H2 as designed is a null by construction here, not by measurement. The agent has 1,734 PRs in dotnet/runtime overall (`docs/verified-facts.md`, F6), but none in these hot paths.
- The README suggests `attribution/run.sh --library all`, but only DI and Caching were attributed; Configuration, Options, Logging and EF Core have no attribution data.
- Servicing churn: no commits touching these paths in any 12-month window after GA (0 of 12 patches for 7.0, 0 of 14 for 10.0 so far, window for 10.0 incomplete: 352 of 365 days).
- Classification caveat: `author_association` disagrees with the `@microsoft.com` e-mail in 24 of 113 commits.

### Glass box (inclusive time per call, injected scopes, relative comparison only)

Absolute times include the cost of hashing the scope name on every call. One process per tag (`DroppedEvents: 0`).

#### DependencyInjection

| scope | v7.0.20 calls | v7.0.20 incl ms | v7.0.20 incl µs/call | v10.0.14 calls | v10.0.14 incl ms | v10.0.14 incl µs/call | per-call change |
|---|---:|---:|---:|---:|---:|---:|---:|
| `ServiceProvider.GetService` | 330,602 | 125.64 | 0.38 | 330,602 | 135.85 | 0.41 | +8.1% |
| `di.validate-on-build100.x100` | 3 | 84.02 | 28005.33 | 3 | 90.31 | 30103.33 | +7.5% |
| `CallSiteFactory.TryCreateExact` | 34,242 | 76.72 | 2.24 | 34,222 | 89.23 | 2.61 | +16.4% |
| `CallSiteFactory.GetCallSite` | 32,113 | 74.69 | 2.33 | 32,113 | 82.22 | 2.56 | +10.1% |
| `di.create-scope-dispose.x10k` | 3 | 40.46 | 13487.00 | 3 | 38.63 | 12875.67 | -4.5% |
| `CallSiteFactory.CreateConstructorCallSite` | 32,131 | 32.96 | 1.03 | 32,131 | 33.40 | 1.04 | +1.4% |
| `di.enumerable20.x10k` | 3 | 28.77 | 9590.33 | 3 | 28.57 | 9522.33 | -0.7% |
| `di.scoped.x10k` | 3 | 24.70 | 8234.67 | 3 | 25.55 | 8517.67 | +3.4% |
| `di.transient.x10k` | 3 | 24.14 | 8045.00 | 3 | 22.89 | 7629.00 | -5.2% |
| `CallSiteFactory.CreateCallSite` | 2,112 | 22.19 | 10.51 | 2,112 | 27.69 | 13.11 | +24.8% |
| `di.open-generic.x10k` | 3 | 19.51 | 6502.33 | 3 | 19.51 | 6504.33 | +0.0% |
| `di.singleton.x10k` | 3 | 18.53 | 6175.33 | 3 | 18.56 | 6187.33 | +0.2% |
| `ServiceProviderEngineScope.GetService` | 30,000 | 15.83 | 0.53 | 30,000 | 16.97 | 0.57 | +7.2% |
| `di.build1000.x10` | 3 | 13.46 | 4486.67 | 3 | 14.03 | 4676.33 | +4.2% |
| `ServiceProvider.CreateServiceAccessor` | 306 | 9.78 | 31.95 | 306 | 12.07 | 39.46 | +23.5% |
| `di.first-resolve-depth5.x100` | 3 | 8.26 | 2752.33 | 3 | 8.35 | 2782.00 | +1.1% |
| `di.build100.x100` | 3 | 7.76 | 2585.67 | 3 | 8.42 | 2807.00 | +8.6% |
| `CallSiteRuntimeResolver.Resolve` | 4,336 | 7.59 | 1.75 | 4,865 | 7.22 | 1.48 | -15.3% |
| `di.cold.first-resolve-depth5` | 1 | 4.30 | 4300.00 | 1 | 5.44 | 5438.00 | +26.5% |
| `ServiceProviderEngineScope.Dispose` | 30,932 | 3.51 | 0.11 | - | - | - | - |
| `CallSiteValidator.ValidateCallSite` | - | - | - | 30,000 | 10.16 | 0.34 | - |

#### Caching.Memory

| scope | v7.0.20 calls | v7.0.20 incl ms | v7.0.20 incl µs/call | v10.0.14 calls | v10.0.14 incl ms | v10.0.14 incl µs/call | per-call change |
|---|---:|---:|---:|---:|---:|---:|---:|
| `cache.parallel8-hit-miss.per-thread-x10k` | 24 | 1131.83 | 47159.75 | 24 | 993.72 | 41404.88 | -12.2% |
| `MemoryCache.TryGetValue` | 330,000 | 957.41 | 2.90 | 330,000 | 886.50 | 2.69 | -7.4% |
| `MemoryCache.StartScanForExpiredItemsIfNeeded` | 430,001 | 214.75 | 0.50 | 430,001 | 205.86 | 0.48 | -4.1% |
| `CacheEntry.Dispose` | 100,001 | 142.92 | 1.43 | 100,001 | 126.66 | 1.27 | -11.4% |
| `MemoryCache.SetEntry` | 100,001 | 115.09 | 1.15 | 100,001 | 98.75 | 0.99 | -14.2% |
| `CacheEntry.CheckExpired` | 280,001 | 106.68 | 0.38 | 280,001 | 107.21 | 0.38 | +0.5% |
| `cache.get-or-create.x10k` | 3 | 96.08 | 32026.00 | 3 | 80.70 | 26899.00 | -16.0% |
| `MemoryCache.Compact` | 6 | 82.32 | 13720.67 | 6 | 74.10 | 12350.33 | -10.0% |
| `cache.set.x10k` | 3 | 54.47 | 18158.00 | 3 | 54.74 | 18247.00 | +0.5% |
| `cache.size-limit-compact-half.x1` | 3 | 41.89 | 13963.33 | 3 | 37.70 | 12567.00 | -10.0% |
| `cache.try-get-hit.x10k` | 3 | 36.27 | 12088.67 | 3 | 32.65 | 10884.67 | -10.0% |
| `cache.try-get-miss.x10k` | 3 | 26.04 | 8680.33 | 3 | 23.19 | 7729.00 | -11.0% |
| `MemoryCache.CreateEntry` | 100,001 | 13.45 | 0.13 | 100,001 | 21.66 | 0.22 | +61.1% |
| `cache.cold.create-and-first-set` | 1 | 3.08 | 3079.00 | 1 | 3.37 | 3368.00 | +9.4% |

## 8. H3: static metrics (committed, 4 libraries, latest tag per major)

| library | tag | epoch | src files | src code lines | test code lines | methods | CC mean | CC max | methods CC>10 | LOC/method mean | max nesting | analyzer warnings |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Caching.Abstractions | v6.0.36 | E0 | 16 | 634 | 0 | 54 | 1.611 | 4 | 0 | 8.296 | 2 | 77 |
| Caching.Abstractions | v7.0.20 | E0 | 18 | 605 | 0 | 56 | 1.321 | 4 | 0 | 7.232 | 2 | 39 |
| Caching.Abstractions | v8.0.33 | E1 | 18 | 623 | 0 | 58 | 1.328 | 4 | 0 | 7.276 | 2 | 41 |
| Caching.Abstractions | v9.0.22 | E1 | 24 | 745 | 0 | 66 | 1.47 | 4 | 0 | 7.409 | 2 | 43 |
| Caching.Abstractions | v10.0.14 | E2 | 24 | 793 | 0 | 72 | 1.514 | 6 | 0 | 7.319 | 3 | 42 |
| Caching.Abstractions | v11.0.0-rc3 | E2 | 25 | 976 | 322 | 88 | 1.534 | 6 | 0 | 7.591 | 3 | 42 |
| Caching.Memory | v6.0.36 | E0 | 10 | 1077 | 2106 | 94 | 2.447 | 26 | 1 | 9.585 | 7 | 94 |
| Caching.Memory | v7.0.20 | E0 | 8 | 1010 | 2358 | 87 | 2.402 | 17 | 1 | 10.034 | 6 | 38 |
| Caching.Memory | v8.0.33 | E1 | 8 | 1073 | 2386 | 92 | 2.424 | 17 | 1 | 9.793 | 6 | 39 |
| Caching.Memory | v9.0.22 | E1 | 8 | 1097 | 2487 | 93 | 2.441 | 17 | 1 | 9.903 | 5 | 39 |
| Caching.Memory | v10.0.14 | E2 | 8 | 1132 | 2541 | 96 | 2.396 | 17 | 1 | 9.896 | 5 | 38 |
| Caching.Memory | v11.0.0-rc3 | E2 | 8 | 1251 | 2891 | 102 | 2.422 | 17 | 3 | 10.314 | 6 | 39 |
| DependencyInjection.Abstractions | v6.0.36 | E0 | 17 | 1658 | 0 | 119 | 2.613 | 10 | 0 | 12.605 | 5 | 163 |
| DependencyInjection.Abstractions | v7.0.20 | E0 | 17 | 1303 | 0 | 122 | 1.541 | 10 | 0 | 9.369 | 5 | 37 |
| DependencyInjection.Abstractions | v8.0.33 | E1 | 25 | 2742 | 0 | 227 | 1.775 | 27 | 3 | 10.753 | 5 | 51 |
| DependencyInjection.Abstractions | v9.0.22 | E1 | 25 | 2836 | 0 | 230 | 1.791 | 27 | 3 | 11.022 | 5 | 51 |
| DependencyInjection.Abstractions | v10.0.14 | E2 | 26 | 2871 | 0 | 231 | 1.805 | 26 | 4 | 11.078 | 5 | 49 |
| DependencyInjection.Abstractions | v11.0.0-rc3 | E2 | 27 | 2983 | 0 | 245 | 1.853 | 26 | 4 | 10.796 | 5 | 49 |
| DependencyInjection | v6.0.36 | E0 | 36 | 2711 | 7337 | 187 | 2.326 | 14 | 4 | 11.791 | 7 | 132 |
| DependencyInjection | v7.0.20 | E0 | 36 | 2704 | 7430 | 187 | 2.289 | 14 | 4 | 11.62 | 7 | 31 |
| DependencyInjection | v8.0.33 | E1 | 38 | 3051 | 8819 | 225 | 2.311 | 18 | 6 | 11.089 | 7 | 29 |
| DependencyInjection | v9.0.22 | E1 | 38 | 3061 | 9073 | 226 | 2.319 | 19 | 6 | 11.093 | 7 | 30 |
| DependencyInjection | v10.0.14 | E2 | 38 | 3183 | 9238 | 235 | 2.357 | 22 | 6 | 11.379 | 7 | 29 |
| DependencyInjection | v11.0.0-rc3 | E2 | 38 | 3381 | 9977 | 241 | 2.444 | 23 | 6 | 11.954 | 8 | 32 |

Notes: lines counted from Roslyn trivia (`loc_tool = roslyn`, `cloc` absent). Warnings use `AnalysisLevel=latest-all` on renamed sources. Tests for the Abstractions libraries live in the implementing library, hence zero. `v6.0.36` has far more analyzer warnings than later tags (for example 132 vs 31 in DI); the cause was not investigated.

## 9. Gaps, risks and open items

1. **Two scenarios, unequal and not interleaved.** Full resources has 78 processes per point, limited has 33 and ran as one contiguous block, bracketed by full-resource rounds on both sides (sections 5.0 and 5.4). The scenario effect is larger than the drift between the full-resource groups, but the design stays unbalanced and "limited" is a CPU power policy, not a quota. No further runs are planned, so state this limit in the report.
2. **BenchmarkDotNet covers two operations only** and there is no full `--no-tiering` control run, so the tracer-distortion check is partial and the JIT-tiering control does not exist.
3. **Not a prepared bench.** Laptop, hybrid CPU, load average above idle, no core isolation, no ASLR change. `docs/stand.md` says such numbers are smoke-test output; the report must either say so or repeat the measurement on a prepared machine.
4. **Most pooled verdicts are `inconclusive`** (37 of 46 at `e2-10`; fewer when the scenarios are analysed separately, section 5.1). The CI is wide because round-to-round spread is large; more rounds help only if the session-state drift is controlled.
5. **Multimodal scopes** (list in section 4.1) are judged by the mean with a p75/p25 threshold of 1.5 chosen after seeing data; `methodology.md` notes that `di.build100.x100`, `di.build1000.x10` and `di.create-scope-dispose.x10k` change verdict at other thresholds; the cause of the two modes is not established (not tiering; GC plausible, unmeasured).
6. **Median vs mean disagree** at `e2-10` for cache `set` and `get-or-create` (median about +26% to +33%, mean about -7% to -9%), so the sign of the headline depends on the metric.
7. **Track A with old packages on a new runtime**: 6.x packages run their `net6.0`/`netstandard` assets on .NET 10 and may miss runtime-specific fast paths; in-box dependencies are always the .NET 10 copies. `e2-11` in Track A runs the `net10.0` build of the 11.0 RC sources.
8. **Scenario validity.** Synthetic batches of APIs present in every version; the newest fast paths (keyed services, source-generated binder) are excluded from comparisons.
9. **Attribution is weak by construction.** Only two libraries; zero agent PRs in the hot paths; `cca` means "opened by the agent", not "written without human input"; AI use that leaves no trace is invisible.
10. **The committed summary pools two machine states.** `results/summary/` covers all 30 rounds; the per-scenario tables are in section 5 of this appendix. Raw traces are untracked.
11. **11.0 is a release candidate**, and the servicing window for 10.0 ends 2026-11-11.
12. **bombardier** was never run; Track B ASP.NET Core uses the built-in fixed-rate `HttpClient` generator (service time, not corrected for coordinated omission).
13. **Aggregation difference.** `<scenario>.csv` medians are pooled over processes (longer rounds weigh more); `stats.csv` weighs rounds equally. Use `stats.csv` for claims.

## 10. Where to look next

- `results/summary/stats.md` for CIs, p-values and rounds agreeing, per scope and point.
- `results/summary/plots/*.svg` for epoch plots; `diff/` for TraceDiff texts.
- `docs/methodology.md` for the 23 deviations, `docs/verified-facts.md` for sources of every external fact.
