# cytoid_game_core example

Minimal playable Flutter shell for the `cytoid_game_core` plugin.

It contains:

- a Flutter level select page
- a Flutter settings page
- Unity fullscreen gameplay via `CytoidGameCoreClient`
- a Flutter result page fed by `game.play.result`

## Before you run

The plugin's Android side fails fast at runtime if the Unity core AAR is not
loaded (`IllegalStateException: Unity artifacts not loaded. Run
setup_unity_artifacts.sh then flutter clean.`). Verify artifacts before the
first launch and after every plugin upgrade:

```sh
# 1. Confirm the AAR is present (exits 0 when installed, non-zero with remediation)
bash ../android/scripts/verify_artifacts.sh

# 2. Install artifacts if missing
cd ..
export CYTOID_GAME_CORE_ARTIFACT_BASE_URL=<your-artifact-base-url>
./tool/setup_unity_artifacts.sh
cd example

# 3. Always flutter clean after installing or refreshing artifacts — the Gradle
#    build caches the AAR's classes and a stale cache will defeat the runtime probe.
flutter clean
```

## Running

```sh
flutter pub get
flutter run
```

Built-in demo levels live under `assets/levels/`. After adding a level folder, run
`dart run tool/sync_level_assets.dart` and `flutter pub get` (see
`assets/levels/README.md`).

When Unity artifacts are absent, the plugin runs a mock fullscreen session. Install
real artifacts from `../tool/setup_unity_artifacts.sh` to launch the Unity core.
