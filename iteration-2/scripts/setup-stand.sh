#!/usr/bin/env bash
set -euo pipefail
MODE="print"
case "${1:-}" in
  --apply) MODE="apply" ;;
  --restore) MODE="restore" ;;
  "" ) ;;
  *) echo "usage: $0 [--apply|--restore]" >&2; exit 2 ;;
esac

commands=()
if [[ "$MODE" == "restore" ]]; then
  commands+=("for g in /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor; do echo schedutil | sudo tee \$g >/dev/null || echo powersave | sudo tee \$g >/dev/null; done")
  [[ -e /sys/devices/system/cpu/intel_pstate/no_turbo ]] && commands+=("echo 0 | sudo tee /sys/devices/system/cpu/intel_pstate/no_turbo")
  [[ -e /sys/devices/system/cpu/cpufreq/boost ]] && commands+=("echo 1 | sudo tee /sys/devices/system/cpu/cpufreq/boost")
  [[ -e /sys/devices/system/cpu/amd_pstate/status ]] && commands+=("echo active | sudo tee /sys/devices/system/cpu/amd_pstate/status")
  commands+=("sudo sysctl -w kernel.randomize_va_space=2")
else
  commands+=("for g in /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor; do echo performance | sudo tee \$g >/dev/null; done")
  if [[ -e /sys/devices/system/cpu/intel_pstate/no_turbo ]]; then
    commands+=("echo 1 | sudo tee /sys/devices/system/cpu/intel_pstate/no_turbo")
  fi
  if [[ -e /sys/devices/system/cpu/amd_pstate/status ]]; then
    commands+=("echo passive | sudo tee /sys/devices/system/cpu/amd_pstate/status")
  fi
  if [[ -e /sys/devices/system/cpu/cpufreq/boost ]]; then
    commands+=("echo 0 | sudo tee /sys/devices/system/cpu/cpufreq/boost")
  fi
  commands+=("sudo sysctl -w kernel.randomize_va_space=0")
fi

if [[ "$MODE" == "print" ]]; then
  echo "# Bench setup commands (not applied). Run '$0 --apply' to apply, '$0 --restore' to undo."
  printf '%s\n' "${commands[@]}"
  echo "# Optional, needs a reboot: add 'isolcpus=2-5 nohz_full=2-5 rcu_nocbs=2-5' to the kernel command line."
  echo "# Optional: echo off | sudo tee /sys/devices/system/cpu/smt/control"
  exit 0
fi

for command in "${commands[@]}"; do
  echo "+ $command"
  bash -c "$command"
done
