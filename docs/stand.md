# Bench machine checklist

The numbers are only comparable when every run happens on the same, quiet, fixed-frequency machine. `scripts/run-matrix.sh` records the state of the machine in `results/raw/env.json` before it starts and warns (but continues) when the governor is not `performance` or turbo is on.

## Before a full run

1. **Hardware is fixed.** Same machine, same BIOS settings, on AC power, lid open, no thermal throttling (`sensors` shows headroom).
2. **Frequency is fixed.** Print the commands with `./scripts/setup-stand.sh`, apply them with `./scripts/setup-stand.sh --apply`, undo them with `./scripts/setup-stand.sh --restore`. They:
   - set every core's `scaling_governor` to `performance`;
   - disable turbo: `intel_pstate/no_turbo = 1` on Intel, `cpufreq/boost = 0` on AMD/acpi-cpufreq (and `amd_pstate` to `passive` so the governor applies);
   - disable ASLR (`kernel.randomize_va_space = 0`) to reduce layout noise between processes.
3. **Measurement cores are isolated** (optional, needs a reboot): add `isolcpus=2-5 nohz_full=2-5 rcu_nocbs=2-5` to the kernel command line. The runner pins every measured process with `taskset -c 2-5` (change with `--cpus`). On hybrid CPUs (Intel 12th gen and later) make sure the pinned CPUs are P-cores: `lscpu --extended` shows the `MAXMHZ` of each CPU.
4. **SMT** (optional): `echo off | sudo tee /sys/devices/system/cpu/smt/control`, or pin to one hardware thread per core.
5. **The machine is quiet.** No browser, IDE, indexer (`tracker-miner`, `updatedb`), backup or package manager running; `uptime` load average below 0.2 before the start; network not used by anything else. On Ubuntu, also stop the update timers: `sudo systemctl stop apt-daily-upgrade.timer apt-daily.timer unattended-upgrades`.
6. **Toolchain is fixed.** The .NET 10 SDK and runtime used for Track A are the ones on `PATH` (`dotnet --info` is captured in `env.json`); Track B and BenchmarkDotNet use `.dotnet/`, which the scripts populate. Do not update them in the middle of a matrix.
7. **Repository is clean.** `git status` shows nothing; every `run-meta.json` records the commit and whether the tree was dirty.

## Running

- Smoke test: `./scripts/run-matrix.sh --quick`
- Full matrix: `./scripts/run-matrix.sh --runs 10`
- Tiering control: `./scripts/run-matrix.sh --runs 10 --no-tiering` (writes to `results/raw-notier/` and `results/summary/notier/`)
- Cross-check with BenchmarkDotNet: `./scripts/run-bdn.sh` (add `--quick` for a short job)

## After a run

- Check `results/raw/env.json`: `governor_is_performance` must be `true` and `turbo_enabled` `false`.
- Check that no `results/raw/*/*-run.log` contains a `[guard]` or `[trace]` line (version mismatch, dropped events).
- Run `./scripts/setup-stand.sh --restore`.
