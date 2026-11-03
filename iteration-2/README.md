# Iteration 2

Status: in preparation. The design is in [`docs/design.md`](docs/design.md); the bench machine checklist is in [`docs/stand.md`](docs/stand.md).

The code in this directory started as a copy of the harness, statistics and glass box of [iteration 1](../iteration-1/README.md), with the measurement changes of section 8 of the design applied. Iteration 1 is left as published.

## Layout

| directory | content |
| --- | --- |
| `src/Harness.Core` | runner, recorder with GC accounting, adaptive warm-up |
| `src/Harness`, `src/Harness.TrackB` | scenarios |
| `src/Compare` | per-process aggregates, summaries, trace pruning |
| `analysis/` | statistics: verdicts, allocation verdicts, parallel bootstrap |
| `glassbox/` | instrumented builds of library sources and the runner |
| `scripts/` | matrix and round runners, stand setup |
| `tests/` | `dotnet run --project tests/<name>`; `analysis`: `pytest` |

## Not here yet

Package selection by NuGet downloads, call-graph generation, per-method aggregates of glass-box traces, attribution and static metrics for the new packages.
