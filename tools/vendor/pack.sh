#!/usr/bin/env bash
# Zip engines/unity/Assets/Vendor/ for distribution. Extract at Unity project root to restore the same path.
set -eu

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_ROOT="$REPO_ROOT/engines/unity"
VENDOR_ROOT="$UNITY_ROOT/Assets/Vendor"
PKG_DIR="$VENDOR_ROOT/StoryboardFilters"
OUT_DIR="$REPO_ROOT/Builds/vendor-bundles"
STAMP="$(date -u +%Y%m%d)"
OUT_ZIP="${OUT_DIR}/cytoid-core-unity-vendor-${STAMP}.zip"

if [ ! -d "$PKG_DIR" ]; then
  echo "error: missing $PKG_DIR" >&2
  echo "Install or restore the vendor zip first (see docs/vendor.md)." >&2
  exit 1
fi

BOOTSTRAP="$PKG_DIR/VendorStoryboardEffectsBootstrap.cs"
if [ ! -f "$BOOTSTRAP" ]; then
  echo "error: incomplete StoryboardFilters (missing VendorStoryboardEffectsBootstrap.cs)" >&2
  exit 1
fi

for dir in "Camera Filter Pack" "Sleek Render"; do
  if [ ! -d "$PKG_DIR/$dir" ]; then
    echo "error: incomplete StoryboardFilters (missing $dir)" >&2
    exit 1
  fi
done

mkdir -p "$OUT_DIR"
rm -f "$OUT_ZIP"

(
  cd "$UNITY_ROOT"
  zip -r "$OUT_ZIP" Assets/Vendor -x "*.DS_Store" -x "*__MACOSX*"
)

echo "Wrote $OUT_ZIP"
echo "Install: cd <repo>/engines/unity && unzip -o \"$OUT_ZIP\""
