#!/usr/bin/env bash
# Emit a cytoid-game-core Unity artifact manifest as JSON.
#
# Output: <plugin>/.cytoid_game_core/artifacts/manifest.<platform>.json
#
# Required environment:
#   MANIFEST_PLATFORM  Either "android" or "ios".
#   MANIFEST_VERSION   Artifact version (typically matches the plugin version
#                       in pubspec.yaml, but is an independent field).
#
# Optional environment:
#   MANIFEST_PLUGIN_VERSION   Defaults to $MANIFEST_VERSION.
#   MANIFEST_UNITY_VERSION    Defaults to 6000.0.75f1 (matches AGENTS.md).
#   MANIFEST_COMMIT_SHA       Defaults to `git rev-parse HEAD`.
#   MANIFEST_BUILD_DATE       Defaults to current UTC time (ISO 8601).
#
# This script never prompts and never modifies the working tree beyond the
# generated manifest file. It is safe to call from CI and from local
# `setup_unity_artifacts.sh`.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACT_DIR="$ROOT_DIR/.cytoid_game_core/artifacts"

if [[ -z "${MANIFEST_PLATFORM:-}" ]]; then
  echo "MANIFEST_PLATFORM is required (android|ios)" >&2
  exit 2
fi
case "$MANIFEST_PLATFORM" in
  android|ios) ;;
  *)
    echo "MANIFEST_PLATFORM must be 'android' or 'ios', got: $MANIFEST_PLATFORM" >&2
    exit 2
    ;;
esac

if [[ -z "${MANIFEST_VERSION:-}" ]]; then
  echo "MANIFEST_VERSION is required (artifact version)" >&2
  exit 2
fi

PLUGIN_VERSION="${MANIFEST_PLUGIN_VERSION:-$MANIFEST_VERSION}"
UNITY_VERSION="${MANIFEST_UNITY_VERSION:-6000.0.75f1}"
COMMIT_SHA="${MANIFEST_COMMIT_SHA:-$(git -C "$ROOT_DIR" rev-parse HEAD 2>/dev/null || echo unknown)}"
BUILD_DATE="${MANIFEST_BUILD_DATE:-$(date -u +%Y-%m-%dT%H:%M:%SZ)}"

# Native deps currently bundled in the Unity export. Per AGENTS.md,
# NativeAudio is free since 2025-09-22. Do not list paid vendor packages here.
UNITY_DEPENDENCIES_JSON='["NativeAudio"]'

mkdir -p "$ARTIFACT_DIR"
OUT_FILE="$ARTIFACT_DIR/manifest.$MANIFEST_PLATFORM.json"

# Emit JSON manually to avoid a jq dependency on minimal CI runners.
cat > "$OUT_FILE" <<JSON
{
  "pluginVersion": "$PLUGIN_VERSION",
  "unityVersion": "$UNITY_VERSION",
  "commitSha": "$COMMIT_SHA",
  "artifactVersion": "$MANIFEST_VERSION",
  "platform": "$MANIFEST_PLATFORM",
  "buildDate": "$BUILD_DATE",
  "unityDependencies": $UNITY_DEPENDENCIES_JSON,
  "protocolSchema": "cytoid.game-core.v2"
}
JSON

echo "Wrote $OUT_FILE"
