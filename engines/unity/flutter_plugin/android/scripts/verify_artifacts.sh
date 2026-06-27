#!/usr/bin/env bash
# Dev/CI convenience check: verifies that the Android Unity core AAR is present
# at the expected artifact path. This is SEPARATE from the runtime probe used
# by CytoidGameCoreBridge.attachActivity (file presence != class loadable);
# the runtime probe reflects on com.unity3d.player.UnityPlayer.
#
# Exit codes:
#   0 — artifact present
#   1 — artifact missing (remediation message on stderr)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PLUGIN_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
AAR_PATH="$PLUGIN_ROOT/.cytoid_game_core/artifacts/unity/android/cytoid-unity-core.aar"

if [[ -f "$AAR_PATH" ]]; then
    echo "OK: Android Unity core artifact present at:"
    echo "  $AAR_PATH"
    echo "  ($(du -h "$AAR_PATH" | cut -f1) bytes)"
    exit 0
fi

cat >&2 <<EOF
ERROR: Android Unity core artifact missing.

Expected at:
  $AAR_PATH

Remediation:
  cd engines/unity/flutter_plugin
  export CYTOID_GAME_CORE_ARTIFACT_BASE_URL=<your-artifact-base-url>
  ./tool/setup_unity_artifacts.sh
  cd example && flutter clean
EOF
exit 1
