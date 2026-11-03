# Iteration 1

Status: complete. The published write-ups ([`habr/article.md`](../habr/article.md), [`medium/article.md`](../medium/article.md)) and [`docs/report.md`](report.md) describe this iteration.

## Question

Did the hot paths of Microsoft's .NET libraries get faster or slower across three epochs, and can any change be traced to the Copilot coding agent? See [`methodology.md`](methodology.md).

## Scope of the code

| area | libraries | versions | what was done |
| --- | --- | --- | --- |
| Track A | DI, configuration, options, logging, `Caching.Memory`, EF Core with SQLite | 6.0, 7.0, 8.0, 9.0, 10.0 | 46 operations, packages swapped on a fixed .NET 10 runtime |
| Track B | `System.Text.Json`, `Channels`, `Immutable`, ASP.NET Core, EF Core | net10.0 against net11.0 (11.0 RC3) | library and runtime together |
| Glass box | DI, `Caching.Memory` | v7.0.20, v10.0.14 | scopes injected into internal methods, hotspots, flame graphs |
| Attribution | DI, `Caching.Memory`, EF Core | tags between 7.0 and 11.0 RC3 | commits mapped to pull requests, classified `cca`, `msft-human`, `community` |
| Static metrics | the same libraries | latest tag of every major | size, complexity, nesting, analyzer warnings |
| Regression search | configuration binder, DI | 11.0 RC3 | `experiments/`: rebuild one library, revert one commit |

## Results

Findings, limits and result files are in the [README](../README.md).

## Known gaps handed to iteration 2

- EF Core has the most agent commits (73 of 256 between v7.0.20 and v10.0.14) and was not examined for their effect.
- Glass-box attribution (H2) covers only DI and `Caching.Memory`, too few methods and agent pull requests for an estimate.
- Track B has no attribution and no glass box.
- Tracer distortion is unestimated for 44 of 46 operations.
- 11.0 is a release candidate.
