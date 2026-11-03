# Locating the 11.0 RC regressions

Each experiment changes one assembly in `bin/e2-11` and reruns the unchanged harness. `./scripts/run-matrix.sh --points e2-11` builds that directory, and `.work/repos/runtime.git` is created by `glassbox/fetch.sh` or `attribution/run.sh`.

## Binder: `IsDefined` per property (dotnet/runtime#136759)

```bash
RC=v11.0.0-rc.3.26522.148
./experiments/build-library.sh --library binder --commit $RC --name binder-source
./experiments/build-library.sh --library binder --commit $RC --name binder-stub --ignore-check stub
./experiments/build-library.sh --library binder --commit $RC --name binder-no-inherit --ignore-check no-inherit
./experiments/run-variants.sh config,options 6 results/experiments/binder e2-11 experiment-binder-source experiment-binder-stub experiment-binder-no-inherit
```

Standalone BenchmarkDotNet reproduction, without EmberTrace, is in `repro/`:

```bash
cd experiments/repro
dotnet build -c Release -p:Ext=11.0.0-rc.3.26522.148 -o out-11
dotnet out-11/BinderRepro.dll --inProcess --filter '*'
```

`trace/` runs a binder with EmberTrace scopes added to `BindProperties`, `BindProperty`, `GetAllProperties` and `IsIgnoredProperty`. Pass the instrumented assembly with `-p:BinderDll=<path>`.

## DI: `first-resolve` and #124331

```bash
RC=v11.0.0-rc.3.26522.148
./experiments/build-library.sh --library di --commit 350951d2a94 --name di-before
./experiments/build-library.sh --library di --commit 7603a59257f --name di-after
./experiments/build-library.sh --library di --commit $RC --name di-rc3
./experiments/build-library.sh --library di --commit $RC --name di-rc3-reverted --revert 7603a59257f
./experiments/run-variants.sh di 8 results/experiments/di experiment-di-before experiment-di-after experiment-di-rc3 experiment-di-rc3-reverted
```

## Release steps

`habr/release_steps.py` reruns `analysis/stats.py` with the previous release as the baseline and writes `habr/data/release-steps.csv`.
