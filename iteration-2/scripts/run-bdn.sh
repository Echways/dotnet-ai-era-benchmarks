#!/usr/bin/env bash
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

TRACK="all"
POINTS=""
FILTER="*"
JOB=""
OUT="results/bdn"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --quick) JOB="short"; POINTS="${POINTS:-e0-7,e2-10}"; shift ;;
    --track) TRACK="$2"; shift 2 ;;
    --points) POINTS="$2"; shift 2 ;;
    --filter) FILTER="$2"; shift 2 ;;
    --job) JOB="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    -h | --help) echo "usage: $0 [--quick] [--track a|b|all] [--points e0-7,e2-10] [--filter GLOB] [--job dry|short|medium|long] [--out DIR]"; exit 0 ;;
    *) echo "unknown option: $1" >&2; exit 2 ;;
  esac
done
JOB_ARGS=()
[[ -n "$JOB" ]] && JOB_ARGS=(--job "$JOB")

if [[ "$TRACK" == "a" || "$TRACK" == "all" ]]; then
  mapfile -t MATRIX < <(python3 - "$POINTS" <<'PY'
import json, sys
wanted = [p for p in sys.argv[1].split(",") if p]
for point in json.load(open("matrix/versions.json"))["points"]:
    if not wanted or point["label"] in wanted:
        print(point["label"], point["major"])
PY
  )
  for entry in "${MATRIX[@]}"; do
    read -r label major <<<"$entry"
    echo "== BenchmarkDotNet Track A $label (in-process, net10.0 runtime)"
    dotnet build bench/Bdn/Bdn.TrackA/Bdn.TrackA.csproj -c Release -p:MsExt="$major" -p:Ef="$major" -o "bin/bdn-a-$label" -nologo -v:q -clp:ErrorsOnly
    dotnet "bin/bdn-a-$label/Bdn.TrackA.dll" --filter "$FILTER" --artifacts "$OUT/track-a/$label" "${JOB_ARGS[@]}"
  done
fi

if [[ "$TRACK" == "b" || "$TRACK" == "all" ]]; then
  ROOT="$(./scripts/ensure-runtimes.sh)"
  echo "== BenchmarkDotNet Track B net6.0-net10.0 (out-of-process, library + JIT)"
  "$ROOT/dotnet" build bench/Bdn/Bdn.TrackB/Bdn.TrackB.csproj -c Release -f net10.0 -nologo -v:q -clp:ErrorsOnly
  "$ROOT/dotnet" bench/Bdn/Bdn.TrackB/bin/Release/net10.0/Bdn.TrackB.dll --runtimes net6.0 net7.0 net8.0 net9.0 net10.0 --cli "$ROOT/dotnet" \
    --filter "$FILTER" --artifacts "$OUT/track-b/out-of-process" "${JOB_ARGS[@]}"
  echo "== BenchmarkDotNet Track B net10.0 vs net11.0 (in-process, library + JIT)"
  ./scripts/dotnet11.sh build bench/Bdn/Bdn.TrackB/Bdn.TrackB.csproj -c Release -f net11.0 -nologo -v:q -clp:ErrorsOnly
  for tfm in net10.0 net11.0; do
    "$ROOT/dotnet" "bench/Bdn/Bdn.TrackB/bin/Release/$tfm/Bdn.TrackB.dll" --inProcess --filter "$FILTER" --artifacts "$OUT/track-b/in-process-$tfm" "${JOB_ARGS[@]}"
  done
fi
