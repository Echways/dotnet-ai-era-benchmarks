# Iteration 2

Status: planned. Goal: widen the range of studied code beyond the libraries of [iteration 1](../iteration-1/docs/scope.md).

## Scope candidates

Not chosen yet. Each candidate lists what iteration 1 already provides.

| candidate | already in place | missing |
| --- | --- | --- |
| EF Core | Track A and B scenarios, attribution | glass box, effect of the 73 agent commits |
| `System.Text.Json`, `Channels`, `Immutable`, ASP.NET Core | Track B scenarios | attribution, glass box, older versions on Track A |
| other `dotnet/runtime` libraries | none | scenarios, attribution entry, static metrics |
| 11.0 final | Track B point `b-net11` | rerun on the release instead of RC3 |

## Where a new library plugs in

| step | files |
| --- | --- |
| versions and packages | `iteration-1/matrix/versions.json`, `iteration-1/matrix/versions.props`, `iteration-1/scripts/resolve-versions.py` |
| scenarios | `iteration-1/src/Harness/Scenarios/` (Track A), `iteration-1/src/Harness.TrackB/Scenarios/` (Track B) |
| scope and verdict statistics | `iteration-1/src/Compare`, `iteration-1/analysis/` |
| commit and pull request attribution | `iteration-1/attribution/libraries.json` |
| glass box | `iteration-1/glassbox/libraries.txt`, `iteration-1/glassbox/methods.txt` |
| static metrics | `iteration-1/static-metrics/` |
| BenchmarkDotNet cross-check | `iteration-1/bench/Bdn/` |

## Open decisions

- Which libraries enter the scope, and in what order.
- Whether iteration 2 keeps the verdict rules of iteration 1 unchanged, so both iterations stay comparable.
- Whether results of iteration 2 get their own directory under `iteration-2/results/` or extend `iteration-1/results/summary/`.
- Whether to repeat the measurements of iteration 1 on 11.0 final.
