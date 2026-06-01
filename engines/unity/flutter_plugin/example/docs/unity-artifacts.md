# Unity Artifacts

The example no longer embeds Unity export trees directly. The `cytoid_game_core`
plugin loads versioned prebuilt artifacts from:

```text
engines/unity/flutter_plugin/.cytoid_game_core/artifacts/unity/android/
engines/unity/flutter_plugin/.cytoid_game_core/artifacts/unity/ios/
```

Install artifacts from a release endpoint:

```sh
cd engines/unity/flutter_plugin
export CYTOID_GAME_CORE_ARTIFACT_BASE_URL=https://example.com/cytoid-game-core
export CYTOID_GAME_CORE_ARTIFACT_VERSION=0.0.1
./tool/setup_unity_artifacts.sh
```

Without artifacts, the plugin uses its mock engine and still exercises the Dart and
native host protocol.
