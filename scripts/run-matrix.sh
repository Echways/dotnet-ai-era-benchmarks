#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

RUNS=10
ITERATIONS=200
WARMUP=20
POINTS=""
TRACK="all"
CPUS="2-5"
NO_TIERING=0
INCLUDE_NEW=0
SKIP_ANALYSIS=0
RAW_OVERRIDE=""
SUMMARY_OVERRIDE=""
TRACK_A_SCENARIOS=(di config options logging cache ef)
TRACK_B_SCENARIOS=(stj channels immutable aspnetcore ef)

usage() {
  cat <<USAGE
usage: $0 [--quick] [--runs N] [--points e0-7,e2-10] [--iterations N] [--warmup N]
          [--track a|b|all] [--cpus 2-5] [--no-tiering] [--include-new] [--skip-analysis]
          [--raw DIR] [--summary DIR]
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --quick) RUNS=2; ITERATIONS=20; WARMUP=5; POINTS="${POINTS:-e0-7,e2-10}"; shift ;;
    --runs) RUNS="$2"; shift 2 ;;
    --points) POINTS="$2"; shift 2 ;;
    --iterations) ITERATIONS="$2"; shift 2 ;;
    --warmup) WARMUP="$2"; shift 2 ;;
    --track) TRACK="$2"; shift 2 ;;
    --cpus) CPUS="$2"; shift 2 ;;
    --no-tiering) NO_TIERING=1; shift ;;
    --include-new) INCLUDE_NEW=1; shift ;;
    --skip-analysis) SKIP_ANALYSIS=1; shift ;;
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

PIN=()
if command -v taskset >/dev/null 2>&1; then
  last_cpu="${CPUS##*[-,]}"
  if (( last_cpu < $(nproc) )); then
    PIN=(taskset -c "$CPUS")
  else
    echo "warning: CPU set $CPUS exceeds $(nproc) CPUs; running unpinned" >&2
  fi
else
  echo "warning: taskset not found; running unpinned" >&2
fi

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
  set +e
  "${PIN[@]}" "$host" "$dll" --scenario "$scenario" --label "$label" --iterations "$ITERATIONS" --warmup "$WARMUP" --out "$RAW" "${extra[@]}" > "$RAW/$label/$scenario-run.log" 2>&1
  local status=$?
  set -e
  case "$status" in
    0) ;;
    4) echo "  $scenario: not available for $label" ;;
    3) echo "error: loaded assembly versions do not match $label; see $RAW/$label/$scenario-run.log" >&2; exit 3 ;;
    *) echo "error: $scenario on $label exited with $status; see $RAW/$label/$scenario-run.log" >&2; exit "$status" ;;
  esac
}

if [[ "$TRACK" == "a" || "$TRACK" == "all" ]]; then
  for entry in "${MATRIX[@]}"; do
    read -r label major _ _ <<<"$entry"
    echo "== Track A $label (Microsoft.Extensions $major, EF Core $major)"
    dotnet build src/Harness/Harness.csproj -c Release -p:MsExt="$major" -p:Ef="$major" -o "bin/$label" -nologo -v:q -clp:ErrorsOnly
    mkdir -p "$RAW/$label"
    for run in $(seq 1 "$RUNS"); do
      for scenario in "${TRACK_A_SCENARIOS[@]}"; do
        run_harness "bin/$label/Harness.dll" "$label" "$scenario" dotnet
      done
      if [[ "$INCLUDE_NEW" == 1 ]]; then
        for scenario in keyed-di binder-sourcegen; do
          run_harness "bin/$label/Harness.dll" "$label" "$scenario" dotnet
        done
      fi
      echo "  run $run/$RUNS done"
    done
  done
fi

if [[ "$TRACK" == "b" || "$TRACK" == "all" ]]; then
  DOTNET11_ROOT="$(./scripts/ensure-sdk11.sh)"
  for tfm in net10.0 net11.0; do
    label="b-${tfm%.0}"
    echo "== Track B $label (in-box libraries of $tfm)"
    ./scripts/dotnet11.sh build src/Harness.TrackB/Harness.TrackB.csproj -c Release -f "$tfm" -o "bin/$label" -nologo -v:q -clp:ErrorsOnly
    mkdir -p "$RAW/$label"
    for run in $(seq 1 "$RUNS"); do
      for scenario in "${TRACK_B_SCENARIOS[@]}"; do
        DOTNET_ROOT="$DOTNET11_ROOT" run_harness "bin/$label/Harness.TrackB.dll" "$label" "$scenario" "$DOTNET11_ROOT/dotnet"
      done
      echo "  run $run/$RUNS done"
    done
  done
fi

echo "== Compare"
dotnet run --project src/Compare/Compare.csproj -c Release -- --raw "$RAW" --summary "$SUMMARY" --matrix matrix/versions.json

if [[ "$SKIP_ANALYSIS" == 0 ]]; then
  echo "== Statistics"
  PYTHON="$(./scripts/ensure-python.sh)"
  "$PYTHON" analysis/stats.py --summary "$SUMMARY"
fi
echo "done: $SUMMARY"
