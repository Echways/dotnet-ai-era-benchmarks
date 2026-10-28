#!/usr/bin/env bash
set -euo pipefail
ATTRIBUTION="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LIBRARY="all"
RANGE=()
EXTRA=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --library) LIBRARY="$2"; shift 2 ;;
    --from | --to) RANGE+=("$1" "$2"); shift 2 ;;
    --offline) EXTRA+=(--offline); shift ;;
    *) echo "usage: $0 [--library DependencyInjection,Caching|all] [--from TAG] [--to TAG] [--offline]" >&2; exit 2 ;;
  esac
done
PYTHON="$(command -v python3.12 || command -v python3)"
mkdir -p "$ATTRIBUTION/../results/summary"
"$PYTHON" "$ATTRIBUTION/commits.py" --library "$LIBRARY" "${RANGE[@]}"
"$PYTHON" "$ATTRIBUTION/prs.py" --library "$LIBRARY" "${RANGE[@]}" "${EXTRA[@]}"
"$PYTHON" "$ATTRIBUTION/classify.py" --library "$LIBRARY" "${RANGE[@]}"
METHOD_LIBRARIES="$(echo "$LIBRARY" | tr ',' '\n' | grep -E '^(all|DependencyInjection|Caching)$' | paste -sd, - || true)"
if [[ -n "$METHOD_LIBRARIES" ]]; then
  [[ "$METHOD_LIBRARIES" == *all* ]] && METHOD_LIBRARIES="DependencyInjection,Caching"
  "$PYTHON" "$ATTRIBUTION/methods.py" --library "$METHOD_LIBRARIES" "${RANGE[@]}"
fi
"$PYTHON" "$ATTRIBUTION/regressions.py" --library "$LIBRARY"
"$PYTHON" "$ATTRIBUTION/report.py"
