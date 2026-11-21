# Iteration 2

Status: in preparation; the measurement changes are validated on the iteration 1 scope with 11 full rounds (section 8 of the design). The design is in [`docs/design.md`](docs/design.md); the bench machine checklist is in [`docs/stand.md`](docs/stand.md).

The code in this directory started as a copy of the harness, statistics and glass box of [iteration 1](../iteration-1/README.md), with the measurement changes of section 8 of the design applied. Iteration 1 is left as published.

## Layout

| directory | content |
| --- | --- |
| `src/Harness.Core`, `src/Harness`, `src/Harness.TrackB` | runner, recorder with GC accounting, adaptive warm-up, scenarios |
| `src/Compare` | per-process aggregates, summaries, trace pruning, per-method glass-box times |
| `selection/` | package selection by NuGet downloads: `select_packages.py` writes the snapshot and decisions, `write_selection.py` writes `docs/selection.md` and `glassbox/packages.tsv` |
| `glassbox/` | `CallGraph` (static call graph from entry operations), `Instrumenter` (instrumented builds), `Runner`, `callgraph.sh`, `fetch.sh` |
| `callgraph/` | generated call graphs and method lists per tag and package |
| `attribution/` | commits, pull requests, classes (`cca`, `msft-human`, `community`), traced methods and servicing churn per package and release step |
| `static-metrics/` | lines, complexity, method length and tests ratio per package and tag |
| `analysis/` | statistics: verdicts, allocation verdicts, parallel bootstrap, per-method bootstrap, counter interval A/B |
| `scripts/` | matrix and round runners, stand setup |
| `tests/` | `dotnet run --project tests/<name>`; `analysis`, `attribution`, `selection`: `pytest` |

## Run

```bash
python3 selection/select_packages.py && python3 selection/write_selection.py   # snapshot, decisions, glassbox/packages.tsv, docs/selection.md
./glassbox/callgraph.sh v7.0.20 v8.0.33 v9.0.22 v10.0.14                       # callgraph/<tag>/<package>/
./attribution/run.sh                                                           # attribution/results/
./static-metrics/run.sh                                                        # static-metrics/results/static-metrics.csv
python3 analysis/counter_ab.py                                                 # docs/counter-ab.md
python3 analysis/method_stats.py --methods results/glassbox/methods.csv        # per-method verdicts
```

`attribution/run.sh` needs `gh` logged in (GraphQL, 5,000 points per hour); `.work/repos/{runtime,aspnetcore,efcore}.git` are partial clones made on first use.

## Collect .NET 11 GA

Run these commands from `iteration-2`:

```bash
./scripts/run-matrix.sh --net11-only --quick
./scripts/run-rounds.sh --net11-only --rounds 11 --runs 10 --prune-traces
./scripts/overnight.sh --net11-only --hours 8 --skip-ab
```

`--net11-only` selects `e2-11` (Microsoft.Extensions 11.0.0 on .NET 10) and `b-net11` (the .NET and ASP.NET Core 11.0.0 shared frameworks and EF Core 11.0.0). Use `--track a` or `--track b` to run one track. Track A and the summary tools use the .NET 10 SDK on `PATH`. Track B builds with SDK 11.0.100 and runs with runtime 11.0.0.

The quick run writes to `results/net11-ga/single/`. Repeated rounds write to `results/net11-ga/rounds/` and `results/net11-ga/summary/`. Keep the stand settings and per-round options aligned with the earlier collection. The .NET 11-only summary describes the two points. Comparisons in the existing statistics pipeline require baseline samples in the same round.

The overnight runner applies the stand settings and restores them on exit. It logs to `results/net11-ga/overnight/`. Omit `--skip-ab` to run counter interval A/B on `e2-11` first, with outputs in `results/net11-ga/ab-counter/`. Pass `--allow-dirty` to run with uncommitted changes.

Collect source data for the GA tag and attribution for the step from 10.0.14:

```bash
./glassbox/callgraph.sh v11.0.0
./static-metrics/run.sh --out results/net11-ga/static-metrics v11.0.0
./glassbox/run.sh --targets Microsoft.Extensions.DependencyInjection,Microsoft.Extensions.Caching.Memory \
  --out results/net11-ga/glassbox v11.0.0
(
  for stage in commits prs classify methods report; do
    ./attribution/run.sh --from v10.0.14 --to v11.0.0 \
      --out results/net11-ga/attribution --stage "$stage" || exit
  done
)
```

## Status

Done: package selection (30 packages, rank 1 to 76, snapshot of 2026-11-04), call graphs of all selected packages at 7.0.20, 8.0.33, 9.0.22 and 10.0.14, instrumentation from the generated list, attribution of commits and traced methods for the three steps 7.0 to 10.0, static metrics, the per-method bootstrap, the counter interval A/B.

Not done:

- harness scenarios for the 52 entry operations and Stage 1 measurements;
- glass-box builds and runs for the packages other than `DependencyInjection` and `Caching.Memory` (project references per package, runner scenarios), and the feasibility gate; three selected packages are already marked `not traceable`;
- the calibration of the cost of one scope and of the IL-size threshold;
- .NET 11 GA measurements and attribution of the 10.0 to 11.0 step;
- the second machine for the strongest findings.
