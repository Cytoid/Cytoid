#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
AGENTS_MD="$REPO_ROOT/AGENTS.md"

STALE_PATHS=$(sed -n '/^## Key File Index/,$p' "$AGENTS_MD" | awk -F'|' '/^\| [A-Z]/{gsub(/^[[:space:]`]+|[[:space:]`]+$/,"",$3); print $3}' | tr ',' '\n' | while read p; do
  p=$(echo "$p" | sed 's/^[[:space:]`]*//;s/[[:space:]`]*$//')
  [ -z "$p" ] && continue
  [[ "$p" == "Location" ]] && continue
  test -e "$REPO_ROOT/$p" || echo "$p"
done)

if [ -n "$STALE_PATHS" ]; then
  echo "STALE paths found in AGENTS.md Key File Index:"
  echo "$STALE_PATHS"
  exit 1
fi