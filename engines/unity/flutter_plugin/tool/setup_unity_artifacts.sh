#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACT_ROOT="$ROOT_DIR/.cytoid_game_core/artifacts/unity"
VERSION_FILE="$ROOT_DIR/.cytoid_game_core/artifacts/VERSION"

VERSION="${CYTOID_GAME_CORE_ARTIFACT_VERSION:-0.0.1}"
BASE_URL="${CYTOID_GAME_CORE_ARTIFACT_BASE_URL:-}"
CHECKSUM_FILE="${CYTOID_GAME_CORE_ARTIFACT_CHECKSUM_FILE:-}"

if [[ -z "$BASE_URL" ]]; then
  cat <<EOF >&2
CYTOID_GAME_CORE_ARTIFACT_BASE_URL is not set.

Expected files:
  \$CYTOID_GAME_CORE_ARTIFACT_BASE_URL/$VERSION/cytoid-unity-core.aar
  \$CYTOID_GAME_CORE_ARTIFACT_BASE_URL/$VERSION/*.aar
  \$CYTOID_GAME_CORE_ARTIFACT_BASE_URL/$VERSION/UnityFramework.xcframework.zip

Example:
  export CYTOID_GAME_CORE_ARTIFACT_BASE_URL=https://github.com/cytoid/cytoid-core-unity/releases/download
  CYTOID_GAME_CORE_ARTIFACT_VERSION=$VERSION ./tool/setup_unity_artifacts.sh
EOF
  exit 2
fi

download() {
  local url="$1"
  local output="$2"
  mkdir -p "$(dirname "$output")"
  echo "Downloading $url"
  curl -fL "$url" -o "$output"
}

download_first_available() {
  local output="$1"
  shift

  local url
  for url in "$@"; do
    if curl -fIL "$url" >/dev/null 2>&1; then
      download "$url" "$output"
      return 0
    fi
  done

  echo "None of these artifact URLs were available:" >&2
  for url in "$@"; do
    echo "  $url" >&2
  done
  exit 4
}

verify_checksum() {
  local file="$1"
  if [[ -z "$CHECKSUM_FILE" ]]; then
    return 0
  fi
  if [[ ! -f "$CHECKSUM_FILE" ]]; then
    echo "Checksum file not found: $CHECKSUM_FILE" >&2
    exit 3
  fi
  (cd "$(dirname "$CHECKSUM_FILE")" && shasum -a 256 -c "$(basename "$CHECKSUM_FILE")")
}

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

mkdir -p "$ARTIFACT_ROOT/android" "$ARTIFACT_ROOT/ios"

download_first_available \
  "$ARTIFACT_ROOT/android/cytoid-unity-core.aar" \
  "$BASE_URL/$VERSION/cytoid-unity-core.aar" \
  "$BASE_URL/$VERSION/android/cytoid-unity-core.aar"

for aar in NativeAudio.aar IngameDebugConsole.aar CytoidPlugin.aar; do
  if curl -fIL "$BASE_URL/$VERSION/$aar" >/dev/null 2>&1; then
    download "$BASE_URL/$VERSION/$aar" "$ARTIFACT_ROOT/android/$aar"
  elif curl -fIL "$BASE_URL/$VERSION/android/$aar" >/dev/null 2>&1; then
    download "$BASE_URL/$VERSION/android/$aar" "$ARTIFACT_ROOT/android/$aar"
  fi
done

download_first_available \
  "$TMP_DIR/UnityFramework.xcframework.zip" \
  "$BASE_URL/$VERSION/UnityFramework.xcframework.zip" \
  "$BASE_URL/$VERSION/ios/UnityFramework.xcframework.zip"
rm -rf "$ARTIFACT_ROOT/ios/UnityFramework.xcframework"
unzip -q "$TMP_DIR/UnityFramework.xcframework.zip" -d "$ARTIFACT_ROOT/ios"

verify_checksum "$ARTIFACT_ROOT/android/cytoid-unity-core.aar"
printf '%s\n' "$VERSION" > "$VERSION_FILE"

# Write typed manifests alongside the legacy VERSION file. Both android and
# ios manifests are emitted because setup_unity_artifacts.sh installs both
# platform artifacts in one pass.
MANIFEST_PLATFORM=android \
MANIFEST_VERSION="$VERSION" \
  bash "$ROOT_DIR/tool/write_manifest.sh"
MANIFEST_PLATFORM=ios \
MANIFEST_VERSION="$VERSION" \
  bash "$ROOT_DIR/tool/write_manifest.sh"

echo "Cytoid game core Unity artifacts installed:"
echo "  Android: $ARTIFACT_ROOT/android"
echo "  iOS:     $ARTIFACT_ROOT/ios/UnityFramework.xcframework"
