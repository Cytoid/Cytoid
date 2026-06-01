#!/usr/bin/env bash
# Remove the local Unity Assets/Vendor/ tree (licensed payloads are not in git).
set -eu

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_ROOT="$REPO_ROOT/engines/unity"
VENDOR_DIR="$UNITY_ROOT/Assets/Vendor"

if [ -d "$VENDOR_DIR" ]; then
  rm -rf "$VENDOR_DIR"
  echo "Removed $VENDOR_DIR"
else
  echo "Nothing to clean: $VENDOR_DIR does not exist"
fi

for meta in "$UNITY_ROOT/Assets/Vendor.meta" "$UNITY_ROOT/Assets/vendor.meta"; do
  if [ -f "$meta" ]; then
    rm -f "$meta"
    echo "Removed $meta"
  fi
done
