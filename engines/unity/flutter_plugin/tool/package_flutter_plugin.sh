#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${CYTOID_GAME_CORE_PACKAGE_VERSION:-$(sed -n 's/^version:[[:space:]]*//p' "$ROOT_DIR/pubspec.yaml" | head -n 1)}"
DIST_DIR="$ROOT_DIR/.cytoid_game_core/dist"
STAGE_DIR="$DIST_DIR/cytoid_game_core-$VERSION"
ZIP_PATH="$DIST_DIR/cytoid_game_core-$VERSION.zip"

rm -rf "$STAGE_DIR" "$ZIP_PATH"
mkdir -p "$STAGE_DIR"

rsync -a "$ROOT_DIR/" "$STAGE_DIR/" \
  --exclude '.cytoid_game_core/build/' \
  --exclude '.cytoid_game_core/dist/' \
  --exclude '.cytoid_game_core/exports/' \
  --exclude '.dart_tool/' \
  --exclude 'build/' \
  --exclude 'example/.dart_tool/' \
  --exclude 'example/build/' \
  --exclude 'example/android/build/' \
  --exclude 'example/android/unityLibrary/' \
  --exclude 'example/ios/UnityLibrary/' \
  --exclude 'pubspec.lock'

(cd "$DIST_DIR" && zip -qr "$(basename "$ZIP_PATH")" "$(basename "$STAGE_DIR")")

echo "Packaged Flutter plugin: $ZIP_PATH"
