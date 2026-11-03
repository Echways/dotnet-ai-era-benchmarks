#!/usr/bin/env bash
set -euo pipefail
OUT="${1:-results/raw/env.json}"
DOTNET="${DOTNET:-dotnet}"
mkdir -p "$(dirname "$OUT")"

read_file() { [[ -r "$1" ]] && tr -d '\n' < "$1" || echo ""; }

governors="$(cat /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor 2>/dev/null | sort | uniq -c | awk '{print $2"="$1}' | paste -sd, - || true)"
driver="$(read_file /sys/devices/system/cpu/cpu0/cpufreq/scaling_driver)"
no_turbo="$(read_file /sys/devices/system/cpu/intel_pstate/no_turbo)"
boost="$(read_file /sys/devices/system/cpu/cpufreq/boost)"
smt="$(read_file /sys/devices/system/cpu/smt/control)"
cpu_model="$(grep -m1 'model name' /proc/cpuinfo 2>/dev/null | cut -d: -f2- | sed 's/^ *//' || true)"
dotnet_info="$("$DOTNET" --info 2>&1 || true)"
git_sha="$(git rev-parse HEAD 2>/dev/null || true)"

export governors driver no_turbo boost smt cpu_model dotnet_info git_sha OUT
python3 - <<'PY'
import json, os, platform, subprocess
from datetime import datetime, timezone

def run(*args):
    try:
        return subprocess.run(args, capture_output=True, text=True, check=False).stdout.strip()
    except FileNotFoundError:
        return ""

governors = dict(item.split("=") for item in os.environ["governors"].split(",") if "=" in item)
turbo_enabled = None
if os.environ["no_turbo"] in ("0", "1"):
    turbo_enabled = os.environ["no_turbo"] == "0"
elif os.environ["boost"] in ("0", "1"):
    turbo_enabled = os.environ["boost"] == "1"
document = {
    "captured_utc": datetime.now(timezone.utc).isoformat(),
    "hostname": platform.node(),
    "kernel": platform.release(),
    "kernel_cmdline": open("/proc/cmdline").read().strip() if os.path.exists("/proc/cmdline") else "",
    "os_release": run("sh", "-c", "grep PRETTY_NAME /etc/os-release | cut -d= -f2- | tr -d '\"'"),
    "cpu_model": os.environ["cpu_model"],
    "logical_cpus": os.cpu_count(),
    "online_cpus": run("sh", "-c", "cat /sys/devices/system/cpu/online 2>/dev/null"),
    "scaling_driver": os.environ["driver"] or None,
    "governors": governors,
    "governor_is_performance": bool(governors) and set(governors) == {"performance"},
    "turbo_enabled": turbo_enabled,
    "smt_control": os.environ["smt"] or None,
    "memory_kb": run("sh", "-c", "grep MemTotal /proc/meminfo | awk '{print $2}'"),
    "load_average": open("/proc/loadavg").read().strip() if os.path.exists("/proc/loadavg") else "",
    "taskset_available": bool(run("sh", "-c", "command -v taskset")),
    "git_sha": os.environ["git_sha"] or None,
    "dotnet_info": os.environ["dotnet_info"],
}
with open(os.environ["OUT"], "w", encoding="utf-8") as handle:
    json.dump(document, handle, indent=2)
    handle.write("\n")
if not document["governor_is_performance"]:
    print(f"warning: CPU governor is {governors or 'unknown'}, not 'performance'; see docs/stand.md", flush=True)
if document["turbo_enabled"]:
    print("warning: turbo/boost is enabled; see docs/stand.md", flush=True)
PY
