# Iteration 2: design

Status: draft for approval. Decisions marked **[author's call]** were made without the maintainer and can be overridden. Decisions marked **[maintainer]** were made by the maintainer.

## Intent

Iteration 1 measured 46 operations from the outside and opened the inside of only two libraries (DI and `Caching.Memory`, 22 methods, two tags). That is too small to say where in widely used Microsoft libraries performance is lost, or whether the Copilot coding agent contributed.

Iteration 2 widens the studied code: many heavily used packages, every call chain unrolled from public operations down to leaf methods, across every major version. The output is the data for one large analytical article with two threads:

1. where, inside the most used Microsoft libraries, hot methods got slower or faster between releases;
2. how much of that can be attributed to agent-authored pull requests.

A problem in a library that everything depends on spreads to all code built on it, so the value of a finding grows with how widely the package is used. Selection therefore follows real usage **[maintainer]**: NuGet download counts.

## Goals

- Select packages by NuGet download counts, with the rule and the snapshot fixed and published before any measurement.
- Measure public entry operations of every selected package from the outside across all versions (breadth).
- Unroll call chains through internal methods with EmberTrace down to leaves (depth), for every major version from 7.0 on.
- Attribute changes of each traced method to pull requests classified `cca`, `msft-human`, `community`, as in iteration 1.
- Keep the verdict rules of iteration 1 so both iterations stay comparable.
- Fix the measurement defects listed under "Measurement changes" before scaling up, and report their effect.

## Non-goals

- Proving that the agent caused a change. Attribution stays descriptive.
- Third-party libraries and application-level benchmarks.
- 6.0 in the glass box.

## Anti-goals

A result that technically works but fails the purpose:

- A package list chosen by taste or convenience instead of the published download rule.
- Thresholds, metrics or subsets changed after seeing data until something is significant.
- Tracer overhead on tiny leaf methods reported as library slowdown.
- A "problem rate" computed only from chains that were opened because they looked slow.
- Findings without the number of methods, agent pull requests and processes behind them.

## Constraints

- Methodology rules of iteration 1 hold: 5% noise threshold, null results reported, fixed baseline, hierarchical bootstrap, stratified Mann–Whitney with Benjamini–Hochberg, verdict rules including multimodal and `mixed` handling. The deviations 22 to 24 of iteration 1 are inherited.
- Global instructions: no comments in code, latest stable versions of tools and libraries.
- Scenarios use only APIs present in every traced version.
- One development laptop (i5-12450H, 4 P-cores with SMT, 4 E-cores, 15 GB RAM). Machine time is the budget.

## Key correction from iteration 1 data

Iteration 1 found that in DI and `Caching.Memory` none of 113 commits between v7.0.20 and v10.0.14 came from the agent, while between 10.0 and 11.0 RC3 it has 12 of 35. In EF Core the agent already has 73 of 256 commits before 10.0. So the agent-era effect sits mostly in the step 10.0 to 11.0 for the `dotnet/runtime` libraries, and in 9.0 to 10.0 and later for EF Core. A study limited to 7.0–10.0 would have almost no treated methods outside EF Core. Therefore 11.0 is part of this iteration as a second phase **[author's call]**, collected from the final tag after the release on 2026-11-20 **[maintainer: wait for the release]**.

## Design

### 1. Package selection

**[author's call]** Done; the rule, the snapshot and every decision are in [`selection.md`](selection.md), [`nuget-snapshot.csv`](nuget-snapshot.csv) and [`package-decisions.csv`](package-decisions.csv), produced by `selection/select_packages.py`. Differences from the rule first written here, all forced by what nuget.org and the repositories contain:

1. The nuspec repository URL cannot tell `dotnet/runtime` from `dotnet/aspnetcore` any more: since 10.0 it points to `dotnet/dotnet` for every package. Source is decided by the project that exists in the three repositories at the 10.0 tag.
2. A package whose latest version is an old stand-alone release (`System.Linq` 4.3.0 and the like, 24 of the first 76 ranks) is excluded as not current: it is in-box today and the studied versions do not exist as packages.
3. Eligibility is checked on the nupkg contents (implementation assembly for a current runtime), not on a reading of the entry operations; `not traceable` packages stay in the list.
4. The cut is 30 eligible packages, which ends at rank 76. It contains no EF Core package and one ASP.NET Core package (`Microsoft.Extensions.ObjectPool`); 29 of 30 come from `dotnet/runtime`.

Downloads overcount CI restores and transitive pulls; "most used" means "most restored", and the article says so.

### 2. Two stages

**[author's call]** Opening every chain of 30 packages and testing thousands of methods against noise would bury real findings in multiple-testing correction and would overstate how common problems are. Two stages:

- **Stage 1, breadth.** Entry operations of all 30 packages, measured from the outside (Track A principle where packages can be swapped, glass-box-compiled sources otherwise), versions 6.0, 7.0, 8.0, 9.0, 10.0, later 11.0. 6.0 is kept here because it is cheap and adds a third pre-period (6→7, 7→8, 8→9) for the parallel-trends check **[author's call]**.
- **Stage 2, depth.** Full call-chain unrolling for (a) every entry operation with a non-noise verdict in Stage 1 and (b) a random 20% sample of the remaining operations, drawn with a recorded seed before looking at results. The sample (b) gives an unbiased estimate of how many chains contain a moving method; without it the article could only report problems, not their prevalence. Chains of operations in (a) are reported as selected-by-signal.

Machine budget: iteration 1 ran about 138 processes per hour (roughly 26 s each, 138 processes per one-hour round). Stage 1 at 30 scenario groups × 6 versions × 10 processes is about 1,800 processes, around 13 hours, before extra rounds; reaching iteration 1's depth is a multiple of that. This is an estimate from iteration 1 round timestamps, to be replaced by a pilot measurement.

### 3. Entry operations

**[author's call]** Per package, a list of operations of its primary documented usage plus the operations iteration 1 already had, fixed in [`entry-operations.tsv`](entry-operations.tsv) and rendered in `selection.md` before measurement: 52 operations for 29 packages. This is the one judgment step in the pipeline and is reported as a threat to validity. The harness scenarios that execute these operations are written and mapped to the operations in [`scope-operations.tsv`](scope-operations.tsv), which `tests/Operations.Tests` checks against the sources and against `entry-operations.tsv`. A scenario exercises every entry that exists in all versions from 6.0 to 10.0 (the design rule on APIs present in every traced version), so `MemoryCache.Clear` and `GetCurrentStatistics`, added in 7.0, are not called and `Caching.Memory` `maintenance` stays `partial`. Packages that can be swapped on the net10.0 runtime run in the Track A harness (`primitives`, `files`, `utilities` and extensions of `di`, `config`, `options`, `logging`, `cache`); `System.*` libraries that ship in the shared framework run in Track B as library plus runtime, net10.0 against net11.0 (`stj`, `immutable`, `diagnostics`, `text`, `metadata`), because the in-box assembly outranks an older package on the net10.0 runtime and the 6.0 to 9.0 versions of those packages need the glass-box build instead. `System.Configuration.ConfigurationManager` has no scenario yet, and `ProtectedData` and `Permissions` cannot be measured on the Linux stand.

### 4. Call-chain discovery

Iteration 1 used a hand-written `methods.txt`. Iteration 2 generates it:

- A Roslyn semantic-model pass builds a static call graph of each package from the entry operations.
- Methods reachable within the package are instrumented; calls leaving the package are boundary nodes.
- Virtual and interface calls resolve to all implementations in the package (over-approximation); methods never executed are reported with zero calls.
- Method identity is the fully qualified signature. Iteration 1 keyed methods by `Type.Method`, which merges overloads and same-named types in different namespaces.
- Generated lists and graph are committed with the results.

Implemented in `glassbox/CallGraph` (`glassbox/callgraph.sh <tag>...`), output in `callgraph/<tag>/<package>/` (`methods.tsv`, gzipped `callgraph.json` with nodes, edges, boundary calls and the operations reaching each node). The pass compiles the package together with the selected packages it references, follows invocations, constructors, property accessors, indexers, method groups, lambdas, `foreach`, operators and field initializers, and resolves virtual, abstract and interface calls to every implementation in the package. Traversal is per package and per operation, so a method knows which operations reach it. Checks: all 22 methods of the hand-written iteration 1 list are found from entries of its two libraries; 16 unit tests cover dispatch, accessors, initializers and boundaries; every entry operation of every selected package matches at the four tags 7.0.20, 8.0.33, 9.0.22 and 10.0.14. Known blind spots: calls through reflection, delegates stored in fields and DI-resolved services are invisible, and the references are the current runtime's, so a call into a framework API that changed between versions can resolve differently.

`Instrumenter --targets methods.tsv` instruments exactly that list, matching a method by library, file, line and the ordinal among declarations on that line, and uses the documentation ID signature as the scope name. It handles methods, constructors, operators, accessors and expression-bodied properties; iterators are skipped and warned about. The DI and `Caching.Memory` sources of 10.0.14 instrumented from a generated list (396 methods) build without warnings. Wiring the glass-box builds and runner for the other packages (project references, scenarios) is not done.

### 5. Tracer distortion

Instrumentation changes what is measured: a scope adds IL and a try/finally, so tiny methods the JIT would inline stop being inlined and the instrumented time describes a code shape that the real build does not have. **[author's call]**:

- cache the result of `Tracer.Id` per method instead of hashing the name per call;
- do not instrument methods whose IL is below a size threshold chosen by calibration; their cost shows up as self time of the caller. Not done: the call graph tool has `--min-size` on syntax nodes of the body as a stand-in, and the calibration is open;
- for each entry operation, report the ratio of instrumented to uninstrumented time next to every glass-box result, and drop methods whose instrumented self time is below the measured cost of one scope from verdicts (`below resolution`), keeping them in call counts.

### 6. Glass-box measurement protocol

Iteration 1's glass box ran one process per tag with the cold phase plus 5 iterations and no warm-up, tags in fixed order. That gives one number per method and tag, without an uncertainty estimate, measured largely in tier-0 code, so the difference-in-differences of H2 had no measurement noise model. Iteration 2:

- many short processes per tag (at least 10), each with cold phase, adaptive warm-up and a bounded number of measured iterations; batch sizes are reduced so the event volume stays inside the EmberTrace buffer (no dropped events is a hard check);
- order of tags shuffled per round and interleaved at process level, like the outside measurements;
- per-method inclusive and self time per process fed to the same hierarchical bootstrap as everything else.

### 7. Versions and attribution

- Stage 1: 6.0, 7.0, 8.0, 9.0, 10.0; 11.0 after the GA tag.
- Glass box and attribution: latest patch tag of 7.0, 8.0, 9.0, 10.0 (7.0.20, 8.0.33, 9.0.22, 10.0.14); 11.0 after the GA tag.

Attribution is implemented in `attribution/` (`attribution/run.sh`): commits that touch each package's source directory between the tags of consecutive steps, mapped to pull requests with batched GraphQL queries (40 commits per request), classified `cca`, `msft-human`, `community`, `bot`, `no-pr` as in iteration 1; traced methods mapped to commits with `git log -L` over the line range from the call graph of the end tag, with the blobs of the touched files fetched in one batch; performance-labelled pull requests, reverts and servicing churn. Results are in `attribution/results/`. The 11.0 step is added to `attribution/config.json` after the GA tag. Static metrics (`static-metrics/run.sh`) cover lines, tests ratio, cyclomatic complexity, method length and nesting of every package at the four tags; the analyzer-warning build of iteration 1 is not repeated because it needs a compiled glass-box project per package.
- Treated window: 10.0 to 11.0 for `dotnet/runtime` and `dotnet/aspnetcore` libraries. The first version of this design also named EF Core, but no EF Core package falls inside the selection cut, so it has no treated window here. Pre-periods: 6→7, 7→8, 8→9. The report checks parallel trends of treated and control methods in the pre-periods and says when they fail.
- The report states the number of treated methods and the smallest effect detectable with that number next to every estimate.

### 8. Measurement changes (review of iteration 1 code)

Found by reading `src/Harness.Core`, `scripts/run-matrix.sh`, `analysis/stats.py`, `glassbox/` and timing the statistics. Each change is made in iteration 2 only; the published iteration 1 stays as it is. Status of each item is given at its end.

1. **Pinning.** `taskset -c 2-5` covers logical CPUs 2,3 and 4,5, which are two SMT pairs on P-cores 1 and 2. Runtime background threads (tiered JIT, GC, the tracer drain) share physical cores with the measured thread and the scheduler can move it between all four. Pin single-threaded scenarios to one logical CPU per physical core (for example `2,4`) and keep the siblings idle; keep the 4-CPU set only for scenarios that use `MeasureParallel`. Check the effect by comparing the spread of per-round changes before and after. Done: `--cpus 2,4` for single-threaded scenarios, `--parallel-cpus 2-5` for `cache`, `channels` and `aspnetcore`; the script warns about sibling CPUs and `env.json` records the topology. Measured on the 252 comparisons of the iteration 1 scope (30 rounds in iteration 1, 11 rounds in iteration 2, same scenarios and versions): the spread of per-round changes (maximum minus minimum) fell from a median of 50.4 to 3.0 percentage points and from a 90th percentile of 70.3 to 8.3; the median width of the 95% interval of the change fell from 8.4 to 1.6 points. Of the 252 verdicts 93 are identical; 150 that were `inconclusive` in iteration 1 are decided in iteration 2 (74 `noise`, 55 `slower`, 21 `faster`), no verdict changed from `slower` to `faster` or back, and the median difference between the two estimates of the change is 2.2 points. The effect of pinning, process interleaving and building once per matrix is not separated, and the equality of the machine state in both iterations was not checked.
2. **Process order.** `run-matrix.sh` runs all processes of one point in a block, then the next point, so frequency and thermal drift on a laptop correlates with the point and is only averaged out across rounds. Interleave at process level (for each run, for each scenario, points in shuffled order) and build each point once per matrix instead of per round. Done: builds happen once per matrix (`--skip-build` after the first round), and for every run and scenario the points execute in a shuffled order, recorded in `process-order.txt`.
3. **Warm-up.** A fixed 20 iterations does not reach JIT steady state (documented in iteration 1). Replace by adaptive warm-up: continue until a rolling window of batch times is stable within 2%, with a cap, and record the iterations used. Cold phases stay separate scopes. Done: `Warmup.Run` continues until the median of the last 10 batch times is within 3% of the median of the 10 before, with minimum `--warmup` and cap `--warmup-max` (300). The window and tolerance were chosen on five scenarios (10 and 3% converged in 35 to 245 iterations; 5 and 2% never converged within 100). The iterations used and whether it converged are in `run-meta.json`.
4. **GC accounting.** Record per batch the deltas of `GC.CollectionCount` for each generation and `GC.GetTotalPauseDuration()`, outside the timed scope. Iteration 1 left the cause of the bimodal batch times open ("GC plausible but not measured"); this settles it and lets the analysis separate GC time from library time. Done: collections per generation and pause time per batch are in `run-meta.json` and in the `gc_*` columns of `*-runs.csv`. A first check on `cache.set.x10k` (20 batches) shows collections in 6 batches and 68 of 116 ms of total time spent in GC pauses, which is the likely cause of the bimodal batch times.
5. **Runtime counters.** The session samples GC and memory counters every 20 ms during measured batches. This adds a listener thread and, for process-wide batches (`GC.GetTotalAllocatedBytes`), its own allocations. Test measured batches with counters off or at 1 s; keep whichever shows no difference in the 5% band, and say which. Option added (`--counter-interval-ms`, 0 disables; accepted by `scripts/run-matrix.sh` and `glassbox/run.sh`). The A/B check is run (`analysis/counter_ab.py`, result in [`counter-ab.md`](counter-ab.md)): 5 processes per cell at 0, 20 and 1000 ms on `e0-7` and `e2-10` give no `faster` or `slower` verdict at any interval, but the power is low, so the default stays 20 ms and iteration 2 stays comparable with iteration 1.
6. **Allocations.** Allocated bytes are nearly deterministic, but `stats.py` gives no verdict for them; they appear only as medians in tables. Add an allocation verdict per scope (exact comparison across processes, any stable difference counts), reported before the noisier time verdicts. Done: `alloc_verdict` and `alloc_change_pct` in `stats.csv` and a column in `stats.md`. Threshold of 1% and non-overlapping ranges are my choice and are stated in the report text.
7. **Bootstrap.** In `across_rounds` the inner bootstrap draw has the same index in every slot of a resample, so a round drawn twice contributes the same inner draw twice, which understates inner variance; this matters most for rounds of 3 processes where the median is one of three values. Draw inner samples independently per slot. Done: inner samples are drawn independently per slot. On the iteration 1 data this changed 3 of 252 verdicts (1 `inconclusive` to `faster`, 2 `inconclusive` to `noise`); point estimates are identical.
8. **Statistics cost.** Timed on a copy of the iteration 1 data: 328 rows in 165 s, about 0.5 s per row, run after every round. At several thousand methods across versions that is hours per round. Vectorize the resampling, compute each baseline once, and parallelize over scopes. Done in part: scopes run in parallel with seeds fixed per scope, so results do not depend on the worker count. The same data now takes 137 s on one worker and about 35 s on 12 (was 165 s). Profile: 62% of the time is the bootstrap, 25% Mann-Whitney calls, 7% plots, so the speed-up is mostly from parallelism, not from the bootstrap rewrite; the "0.5 s per row" figure above is for one worker.
9. **Storage.** Iteration 1 keeps 5.8 GB of `.ember` traces in `results/rounds`, while the statistics use only per-process aggregates in the `*-runs.csv`. Keep aggregates and the traces needed for the article figures; compress or discard the rest after aggregation. Done: `Compare` stores an `.aggregate.json` per process and reads it instead of the trace afterwards, `--prune-traces` deletes traces that are not a point's representative, and the glass box runner keeps warm-up out of its traces (a first version traced the warm-up and wrote 1.7 GB per process; now 14 to 37 MB for 5 iterations).

## Feasibility gate

Before a package enters the measured set its sources compile into a renamed assembly for a tag, the scenario runs against it, and outputs agree with the real package within noise. Packages with source generators or heavy runtime-internal dependencies (likely `System.Text.Json`, parts of ASP.NET Core) may fail; a failure is recorded and the package is measured from outside only, as library plus runtime, if at all.

## Left to the builder

- Call-graph tool and file formats.
- Per-package entry operations within the rule.
- The IL-size threshold and warm-up window, within the calibration described.
- Scheduling of rounds given the machine budget.

## Risks

- The agent-era window is one release step, so treated-method counts can stay small. The agent is also not assigned tasks at random, so even a clear difference would not show cause. The article reports what the data shows and is written so that a null result is a complete outcome.
- One laptop. Key findings (the strongest few) are to be repeated on a second, dedicated machine before publication.
- Static call graphs over-approximate virtual calls; mitigated by zero-call reporting.
- 11.0 data arrives only after 2026-11-20; everything else is built and run before.
- 10.0 servicing window runs to 2026-11-11, so 10.0 attribution collected earlier is incomplete, as in iteration 1.
- Findings should be sent to the maintainers (as issues in the owning repository) before the article is published; iteration 1 already had a maintainer response.

## Glass-box runner

The runner now follows section 6 in part: every process has a cold session, an adaptive warm-up without a session, and a measured session, writes `<scenario>-<pid>.ember`, and fails with exit code 5 when events were dropped. `glassbox/run.sh` repeats processes (`--runs`, default 10), shuffles the tag order per run and pins to `2,4`. Still open: batch sizes are fixed in the shared scenarios, so the event volume per process is what the scenario produces (about 2 to 4 million events for 5 iterations of DI and Caching.Memory); per-method inclusive and self time per process is exported by `dotnet run --project src/Compare -- methods` (called at the end of `glassbox/run.sh`) to `<out>/methods.csv` with columns `tag,scenario,pid,method,calls,inclusive_ms,self_ms`; inclusive time of a recursive method counts the outermost frame only, self time is the scope duration minus the durations of its direct children. `analysis/method_stats.py` consumes it: time per call per process for every method, compared with the 7.0 baseline through the same hierarchical bootstrap, stratified Mann-Whitney and Benjamini-Hochberg code as the outside measurements (several `methods.csv` files are several rounds), plus the change of the call count and `below resolution` for methods under the measured cost of a scope, which is still to be measured. It is tested on synthetic data only; there are no glass-box results for the new packages yet.
