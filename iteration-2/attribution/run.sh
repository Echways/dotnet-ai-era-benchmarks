#!/usr/bin/env bash
set -euo pipefail
ATTRIBUTION="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LIBRARY="all"
OUT="$ATTRIBUTION/results"
RANGE=()
EXTRA=()
STAGES="all"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --library) LIBRARY="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    --from | --to) RANGE+=("$1" "$2"); shift 2 ;;
    --offline) EXTRA+=(--offline); shift ;;
    --stage) STAGES="$2"; shift 2 ;;
    -h | --help) echo "usage: $0 [--library ID,ID|all] [--from TAG --to TAG] [--out DIR] [--offline] [--stage commits|prs|classify|methods|regressions|report|all]"; exit 0 ;;
    *) echo "unknown option: $1" >&2; exit 2 ;;
  esac
done
PYTHON="$(command -v python3.12 || command -v python3)"
mkdir -p "$OUT"
stage() { [[ "$STAGES" == "all" || "$STAGES" == "$1" ]]; }
stage commits && "$PYTHON" "$ATTRIBUTION/commits.py" --library "$LIBRARY" "${RANGE[@]}"
stage prs && "$PYTHON" "$ATTRIBUTION/prs.py" --library "$LIBRARY" "${RANGE[@]}" "${EXTRA[@]}"
stage classify && "$PYTHON" "$ATTRIBUTION/classify.py" --library "$LIBRARY" "${RANGE[@]}" --out "$OUT/attribution.csv"
stage methods && "$PYTHON" "$ATTRIBUTION/methods.py" --library "$LIBRARY" "${RANGE[@]}" --attribution "$OUT/attribution.csv" --out "$OUT/attribution-methods.csv"
stage regressions && "$PYTHON" "$ATTRIBUTION/regressions.py" --library "$LIBRARY" --attribution "$OUT/attribution.csv" --out "$OUT/attribution-regressions.json"
stage report && "$PYTHON" "$ATTRIBUTION/report.py" --summary "$OUT"
exit 0
