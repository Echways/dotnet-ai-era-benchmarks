#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

RUNS=10
ITERATIONS=200
WARMUP=20
WARMUP_MAX=300
COUNTER_INTERVAL=20
SKIP_BUILD=0
PRUNE_TRACES=0
POINTS=""
TRACK="all"
CPUS="2,4"
PARALLEL_CPUS="2-5"
NO_TIERING=0
INCLUDE_NEW=0
SKIP_ANALYSIS=0
RAW_OVERRIDE=""
SUMMARY_OVERRIDE=""
TRACK_A_SCENARIOS=(di config options logging cache ef)
TRACK_B_SCENARIOS=(stj channels immutable aspnetcore ef)
MULTI_THREADED_SCENARIOS=(cache channels aspnetcore)

usage() {
  cat <<USAGE
usage: $0 [--quick] [--runs N] [--points e0-7,e2-10] [--iterations N] [--warmup N]
          [--warmup-max N] [--counter-interval-ms N] [--track a|b|all] [--cpus 2,4] [--parallel-cpus 2-5]
          [--no-tiering] [--include-new] [--skip-build] [--skip-analysis] [--prune-traces] [--raw DIR] [--summary DIR]
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --quick) RUNS=2; ITERATIONS=20; WARMUP=5; WARMUP_MAX=5; POINTS="${POINTS:-e0-7,e2-10}"; shift ;;
    --runs) RUNS="$2"; shift 2 ;;
    --points) POINTS="$2"; shift 2 ;;
    --iterations) ITERATIONS="$2"; shift 2 ;;
    --warmup) WARMUP="$2"; shift 2 ;;
    --warmup-max) WARMUP_MAX="$2"; shift 2 ;;
    --counter-interval-ms) COUNTER_INTERVAL="$2"; shift 2 ;;
    --track) TRACK="$2"; shift 2 ;;
    --cpus) CPUS="$2"; shift 2 ;;
    --parallel-cpus) PARALLEL_CPUS="$2"; shift 2 ;;
    --no-tiering) NO_TIERING=1; shift ;;
    --include-new) INCLUDE_NEW=1; shift ;;
    --skip-build) SKIP_BUILD=1; shift ;;
    --skip-analysis) SKIP_ANALYSIS=1; shift ;;
    --prune-traces) PRUNE_TRACES=1; shift ;;
    --raw) RAW_OVERRIDE="$2"; shift 2 ;;
    --summary) SUMMARY_OVERRIDE="$2"; shift 2 ;;
    -h | --help) usage; exit 0 ;;
    *) echo "unknown option: $1" >&2; usage >&2; exit 2 ;;
  esac
done

RAW="results/raw"
SUMMARY="results/summary"
if [[ "$NO_TIERING" == 1 ]]; then
  export DOTNET_TieredCompilation=0
  RAW="results/raw-notier"
  SUMMARY="results/summary/notier"
fi
RAW="${RAW_OVERRIDE:-$RAW}"
SUMMARY="${SUMMARY_OVERRIDE:-$SUMMARY}"
mkdir -p "$RAW" "$SUMMARY"

pin_for() {
  local cpus="$1"
  if ! command -v taskset >/dev/null 2>&1; then
    echo "warning: taskset not found; running unpinned" >&2
    return
  fi
  local last_cpu="${cpus##*[-,]}"
  if (( last_cpu >= $(nproc) )); then
    echo "warning: CPU set $cpus exceeds $(nproc) CPUs; running unpinned" >&2
    return
  fi
  echo "taskset -c $cpus"
}

warn_on_smt_siblings() {
  local cpus="$1" seen="" cpu siblings
  for cpu in $(python3 -c "import sys
for part in sys.argv[1].split(','):
    low, _, high = part.partition('-')
    print(*range(int(low), int(high or low) + 1))" "$cpus"); do
    siblings="$(cat "/sys/devices/system/cpu/cpu$cpu/topology/thread_siblings_list" 2>/dev/null || true)"
    if [[ -n "$siblings" && " $seen " == *" $siblings "* ]]; then
      echo "warning: CPUs in $cpus share a physical core ($siblings); single-threaded scenarios will compete with their own SMT sibling" >&2
    fi
    seen="$seen $siblings"
  done
}

warn_on_smt_siblings "$CPUS"
PIN_SINGLE="$(pin_for "$CPUS")"
PIN_PARALLEL="$(pin_for "$PARALLEL_CPUS")"

pin_command_for() {
  local scenario="$1" scenario_name
  for scenario_name in "${MULTI_THREADED_SCENARIOS[@]}"; do
    if [[ "$scenario_name" == "$scenario" ]]; then
      echo "$PIN_PARALLEL"
      return
    fi
  done
  echo "$PIN_SINGLE"
}

DOTNET=dotnet ./scripts/capture-env.sh "$RAW/env.json"

mapfile -t MATRIX < <(python3 - "$POINTS" <<'PY'
import json, sys
wanted = [p for p in sys.argv[1].split(",") if p]
document = json.load(open("matrix/versions.json"))
labels = [p["label"] for p in document["points"]]
unknown = [w for w in wanted if w not in labels]
if unknown:
    sys.exit(f"unknown points: {', '.join(unknown)}; known: {', '.join(labels)}")
by_label = {p["label"]: p for p in document["points"]}
for label in wanted or labels:
    point = by_label[label]
    print(f"{point['label']} {point['major']} {point['ef_package_version']} {point['ef_track']}")
PY
)

run_harness() {
  local dll="$1" label="$2" scenario="$3" host="$4"
  local extra=()
  [[ "$INCLUDE_NEW" == 1 ]] && extra+=(--include-new)
  local pin
  read -r -a pin <<<"$(pin_command_for "$scenario")"
  set +e
  "${pin[@]}" "$host" "$dll" --scenario "$scenario" --label "$label" --iterations "$ITERATIONS" --warmup "$WARMUP" --warmup-max "$WARMUP_MAX" --counter-interval-ms "$COUNTER_INTERVAL" --out "$RAW" "${extra[@]}" > "$RAW/$label/$scenario-run.log" 2>&1
  local status=$?
  set -e
  case "$status" in
    0) ;;
    4) echo "  $scenario: not available for $label" ;;
    3) echo "error: loaded assembly versions do not match $label; see $RAW/$label/$scenario-run.log" >&2; exit 3 ;;
    *) echo "error: $scenario on $label exited with $status; see $RAW/$label/$scenario-run.log" >&2; exit "$status" ;;
  esac
  echo "$run $scenario $label" >> "$RAW/process-order.txt"
}

shuffled() {
  printf '%s\n' "$@" | shuf
}

build_unless_skipped() {
  [[ "$SKIP_BUILD" == 1 && -f "$1/$2" ]] && return
  shift 2
  "$@"
}

: > "$RAW/process-order.txt"

if [[ "$TRACK" == "a" || "$TRACK" == "all" ]]; then
  labels=()
  for entry in "${MATRIX[@]}"; do
    read -r label major _ _ <<<"$entry"
    labels+=("$label")
    echo "== Track A build $label (Microsoft.Extensions $major, EF Core $major)"
    build_unless_skipped "bin/$label" Harness.dll dotnet build src/Harness/Harness.csproj -c Release -p:MsExt="$major" -p:Ef="$major" -o "bin/$label" -nologo -v:q -clp:ErrorsOnly
    mkdir -p "$RAW/$label"
  done
  for run in $(seq 1 "$RUNS"); do
    scenarios=("${TRACK_A_SCENARIOS[@]}")
    [[ "$INCLUDE_NEW" == 1 ]] && scenarios+=(keyed-di binder-sourcegen)
    for scenario in "${scenarios[@]}"; do
      mapfile -t order < <(shuffled "${labels[@]}")
      for label in "${order[@]}"; do
        run_harness "bin/$label/Harness.dll" "$label" "$scenario" dotnet
      done
    done
    echo "  Track A run $run/$RUNS done"
  done
fi

if [[ "$TRACK" == "b" || "$TRACK" == "all" ]]; then
  DOTNET11_ROOT="$(./scripts/ensure-sdk11.sh)"
  labels=()
  for tfm in net10.0 net11.0; do
    label="b-${tfm%.0}"
    labels+=("$label")
    echo "== Track B build $label (in-box libraries of $tfm)"
    build_unless_skipped "bin/$label" Harness.TrackB.dll ./scripts/dotnet11.sh build src/Harness.TrackB/Harness.TrackB.csproj -c Release -f "$tfm" -o "bin/$label" -nologo -v:q -clp:ErrorsOnly
    mkdir -p "$RAW/$label"
  done
  for run in $(seq 1 "$RUNS"); do
    for scenario in "${TRACK_B_SCENARIOS[@]}"; do
      mapfile -t order < <(shuffled "${labels[@]}")
      for label in "${order[@]}"; do
        DOTNET_ROOT="$DOTNET11_ROOT" run_harness "bin/$label/Harness.TrackB.dll" "$label" "$scenario" "$DOTNET11_ROOT/dotnet"
      done
    done
    echo "  Track B run $run/$RUNS done"
  done
fi

echo "== Compare"
COMPARE_ARGS=()
[[ "$PRUNE_TRACES" == 1 ]] && COMPARE_ARGS+=(--prune-traces)
dotnet run --project src/Compare/Compare.csproj -c Release -- --raw "$RAW" --summary "$SUMMARY" --matrix matrix/versions.json "${COMPARE_ARGS[@]}"

if [[ "$SKIP_ANALYSIS" == 0 ]]; then
  echo "== Statistics"
  PYTHON="$(./scripts/ensure-python.sh)"
  "$PYTHON" analysis/stats.py --summary "$SUMMARY"
fi
echo "done: $SUMMARY"
