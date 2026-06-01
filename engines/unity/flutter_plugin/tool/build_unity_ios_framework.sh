#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
EXPORT_ROOT="$ROOT_DIR/.cytoid_game_core/exports/ios/UnityLibrary"
ARTIFACT_DIR="$ROOT_DIR/.cytoid_game_core/artifacts/unity/ios"
PACKAGE_ARTIFACT_DIR="$ROOT_DIR/ios/cytoid_game_core/Artifacts"
DERIVED_DATA="$ROOT_DIR/.cytoid_game_core/build/ios-derived-data"
BUILD_LOG="$ROOT_DIR/.cytoid_game_core/build/ios-unity-framework.log"
PROJECT_FILE="$EXPORT_ROOT/Unity-iPhone.xcodeproj/project.pbxproj"

if [[ ! -d "$EXPORT_ROOT/Unity-iPhone.xcodeproj" ]]; then
  echo "Unity iOS export missing at $EXPORT_ROOT" >&2
  echo "Run: Unity -batchmode -quit -projectPath <repo> -executeMethod CytoidCoreBuild.ExportIOSLibraryForFlutter" >&2
  exit 1
fi

rm -rf "$DERIVED_DATA" "$BUILD_LOG"
mkdir -p "$ARTIFACT_DIR" "$(dirname "$BUILD_LOG")"

# Unity 6 produces both libGameAssembly.a and il2cpp.a. UnityFramework still
# needs the il2cpp.a runtime archive for the _il2cpp_* symbols.
perl -0pi -e 's/"\$CONFIGURATION_BUILD_DIR\/libGameAssembly\.a"/"\$CONFIGURATION_BUILD_DIR\/il2cpp.a"/g' "$PROJECT_FILE"

# The generated GameAssembly script can otherwise hide an IL2CPP/Bee failure
# behind its final cleanup command, which later appears as a missing archive.
perl -0pi -e 's/shellScript = "export BEE_CACHE_BEHAVIOUR=/shellScript = "set -e\\nexport BEE_CACHE_BEHAVIOUR=/g' "$PROJECT_FILE"

COMMON_XCODEBUILD_ARGS=(
  -project "$EXPORT_ROOT/Unity-iPhone.xcodeproj" \
  -configuration Release \
  -sdk iphoneos \
  -destination generic/platform=iOS \
  -derivedDataPath "$DERIVED_DATA" \
  CODE_SIGNING_ALLOWED=NO \
  CODE_SIGNING_REQUIRED=NO \
  CODE_SIGN_IDENTITY=
)

if ! xcodebuild \
  -quiet \
  "${COMMON_XCODEBUILD_ARGS[@]}" \
  -scheme UnityFramework \
  build >"$BUILD_LOG" 2>&1; then
  tail -n 200 "$BUILD_LOG" >&2
  exit 3
fi

FRAMEWORK_PATH="$(find "$DERIVED_DATA/Build/Products" -path '*/UnityFramework.framework' -type d | head -n 1)"
if [[ -z "$FRAMEWORK_PATH" ]]; then
  echo "UnityFramework.framework was not produced by xcodebuild." >&2
  exit 2
fi

rm -rf "$ARTIFACT_DIR/UnityFramework.framework" "$ARTIFACT_DIR/UnityFramework.xcframework"
cp -R "$FRAMEWORK_PATH" "$ARTIFACT_DIR/UnityFramework.framework"
rm -rf "$ARTIFACT_DIR/UnityFramework.framework/Data"
cp -R "$EXPORT_ROOT/Data" "$ARTIFACT_DIR/UnityFramework.framework/Data"

xcodebuild \
  -create-xcframework \
  -framework "$ARTIFACT_DIR/UnityFramework.framework" \
  -output "$ARTIFACT_DIR/UnityFramework.xcframework" >>"$BUILD_LOG" 2>&1

mkdir -p "$PACKAGE_ARTIFACT_DIR"
ln -sfn "../../../.cytoid_game_core/artifacts/unity/ios/UnityFramework.xcframework" \
  "$PACKAGE_ARTIFACT_DIR/UnityFramework.xcframework"

echo "Installed UnityFramework.framework and UnityFramework.xcframework to $ARTIFACT_DIR"
