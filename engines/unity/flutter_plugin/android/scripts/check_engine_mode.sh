#!/usr/bin/env bash
# Info-only probe: prints "unity" or "mock" depending on whether the Android
# Unity core artifact AND the v2 manifest are present on disk.
#
# This script is the CI smoke distinguisher. Every smoke step that prints
# ENGINE_MODE=<value> sources this script. It NEVER fails the build:
#   - "unity"  → manifest present AND protocolSchema = "cytoid.game-core.v2"
#                AND cytoid-unity-core.aar present.
#   - "mock"   → any of the above missing. The Flutter example app falls back
#                to the mock engine at build/runtime (host protocol still works).
#
# Exit code is ALWAYS 0. The runtime probe in CytoidGameCoreBridge.attachActivity
# is the authoritative fail-fast path (T1); this script is informational only.
#
# Output: stdout = single lower-case word "unity" or "mock".
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

ARTIFACT_DIR="$PLUGIN_ROOT/.cytoid_game_core/artifacts"
MANIFEST_FILE="$ARTIFACT_DIR/manifest.android.json"
AAR_FILE="$ARTIFACT_DIR/unity/android/cytoid-unity-core.aar"

is_unity_mode() {
    [[ -f "$MANIFEST_FILE" ]] || return 1
    # -s (not -f): an empty/zero-byte AAR is a broken artifact, not a usable
    # one. The build-level smoke (flutter build apk) would fail with a link
    # error on such a file; the engine-mode probe must report "mock" so the
    # CI log distinguishes a real artifact from a pipeline-corrupted one.
    [[ -s "$AAR_FILE" ]] || return 1
    # write_manifest.sh emits pretty-printed JSON with protocolSchema on its
    # own line. grep avoids a jq dependency on minimal CI runners.
    grep -Eq '"protocolSchema":[[:space:]]*"cytoid\.game-core\.v2"' "$MANIFEST_FILE" || return 1
    return 0
}

if is_unity_mode; then
    echo "unity"
else
    echo "mock"
fi
exit 0
