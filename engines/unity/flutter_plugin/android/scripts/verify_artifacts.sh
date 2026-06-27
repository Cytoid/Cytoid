#!/usr/bin/env bash
# Dev/CI convenience check: verifies that the Android Unity core AAR is present
# AND non-empty at the canonical artifact path, and that the manifest is
# present with the v2 protocol schema. Separate from the runtime probe used
# by CytoidGameCoreBridge.attachActivity (file presence != class loadable);
# the runtime probe reflects on com.unity3d.player.UnityPlayer.
#
# Also detects the GitHub Actions artifact-nesting failure mode: if the AAR
# lands under a nested unity/android/ subdir (because the workflow downloaded
# to the leaf instead of the canonical root), the canonical check fails AND
# this script points at the misplaced file.
#
# Exit codes:
#   0 — artifact + manifest present and well-formed
#   1 — artifact missing/empty or manifest missing/malformed (remediation on stderr)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

ARTIFACT_DIR="$PLUGIN_ROOT/.cytoid_game_core/artifacts"
MANIFEST_FILE="$ARTIFACT_DIR/manifest.android.json"
AAR_FILE="$ARTIFACT_DIR/unity/android/cytoid-unity-core.aar"
# The path that appears when the CI download step preserves the upload root
# (actions/download-artifact@v4 keeps the relative structure) and the AAR
# lands one level deeper than expected.
NESTED_AAR_FILE="$ARTIFACT_DIR/unity/android/unity/android/cytoid-unity-core.aar"

fail() {
    cat >&2 <<EOF
ERROR: $1

Expected AAR at:
  $AAR_FILE
Expected manifest at:
  $MANIFEST_FILE

Remediation:
  - local: cd engines/unity/flutter_plugin
           export CYTOID_GAME_CORE_ARTIFACT_BASE_URL=<your-artifact-base-url>
           ./tool/setup_unity_artifacts.sh
           cd example && flutter clean
  - CI: check the download step downloads to the canonical artifact root
    (\`.cytoid_game_core/artifacts\`), NOT the leaf \`.../artifacts/unity/android\`.
$2
EOF
    exit 1
}

if [[ ! -s "$AAR_FILE" ]]; then
    nested_hint=""
    if [[ -s "$NESTED_AAR_FILE" ]]; then
        nested_hint="

Detected: the AAR IS present at the NESTED path:
  $NESTED_AAR_FILE
This is the actions/download-artifact path-preservation failure — fix the
workflow download path, don't move the file manually."
    fi
    fail "Android Unity core artifact missing or empty at canonical path." "$nested_hint"
fi

if [[ ! -f "$MANIFEST_FILE" ]]; then
    fail "Android artifact manifest missing at canonical path." ""
fi

if ! grep -Eq '"protocolSchema":[[:space:]]*"cytoid\.game-core\.v2"' "$MANIFEST_FILE"; then
    fail "Manifest at $MANIFEST_FILE is not a cytoid.game-core.v2 schema." ""
fi

if ! grep -Eq '"platform":[[:space:]]*"android"' "$MANIFEST_FILE"; then
    fail "Manifest at $MANIFEST_FILE does not declare platform=android." ""
fi

echo "OK: Android Unity core artifact present at:"
echo "  $AAR_FILE"
echo "  ($(du -h "$AAR_FILE" | cut -f1) bytes)"
echo "  manifest: $MANIFEST_FILE (cytoid.game-core.v2, android)"
