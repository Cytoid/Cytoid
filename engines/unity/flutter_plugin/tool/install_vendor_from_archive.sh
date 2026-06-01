#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VENDOR_ROOT="$PROJECT_ROOT/Assets/Vendor"
ARCHIVE="${1:-${CYTOID_VENDOR_ARCHIVE:-}}"
EXPECTED_SHA256="${CYTOID_VENDOR_ARCHIVE_SHA256:-}"

if [[ -z "$ARCHIVE" ]]; then
  echo "No vendor archive configured; continuing with in-repo fallback storyboard effects."
  exit 0
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

if [[ "$ARCHIVE" =~ ^https?:// ]]; then
  echo "Downloading vendor archive from $ARCHIVE"
  curl -fL "$ARCHIVE" -o "$TMP_DIR/vendor.zip"
  ARCHIVE="$TMP_DIR/vendor.zip"
fi

if [[ ! -f "$ARCHIVE" ]]; then
  echo "Vendor archive not found: $ARCHIVE" >&2
  exit 2
fi

if [[ -n "$EXPECTED_SHA256" ]]; then
  echo "$EXPECTED_SHA256  $ARCHIVE" | shasum -a 256 -c -
fi

rm -rf "$VENDOR_ROOT"
mkdir -p "$VENDOR_ROOT"
unzip -q "$ARCHIVE" -d "$TMP_DIR/vendor"

if [[ -d "$TMP_DIR/vendor/Assets/Vendor" ]]; then
  cp -R "$TMP_DIR/vendor/Assets/Vendor/." "$VENDOR_ROOT/"
elif [[ -d "$TMP_DIR/vendor/Vendor" ]]; then
  cp -R "$TMP_DIR/vendor/Vendor/." "$VENDOR_ROOT/"
elif [[ -d "$TMP_DIR/vendor/StoryboardFilters" ]]; then
  cp -R "$TMP_DIR/vendor/StoryboardFilters" "$VENDOR_ROOT/StoryboardFilters"
else
  cp -R "$TMP_DIR/vendor/." "$VENDOR_ROOT/"
fi

if [[ -d "$VENDOR_ROOT/StoryboardFilters" ]]; then
  touch "$VENDOR_ROOT/StoryboardFilters/.vendor-installed"
  echo "Installed vendor storyboard filters to $VENDOR_ROOT/StoryboardFilters"
else
  echo "Vendor archive did not install Assets/Vendor/StoryboardFilters." >&2
  exit 3
fi
