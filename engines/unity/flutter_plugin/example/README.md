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

## Running on iOS Simulator

iOS Simulator runs mock-only because the current Unity artifact is device-only.
Real-device testing is required for Unity verification.

The shipped `UnityFramework.xcframework` lacks a simulator slice, so the plugin
falls back to the mock runtime when the example app targets an iOS Simulator
destination. This is true even after `./tool/setup_unity_artifacts.sh` has
downloaded artifacts — the device slice is present, but the simulator loader
cannot link it.

To distinguish mock from Unity at a glance, the example app shows a
`MOCK ENGINE` chip at the top of the game session screen in debug builds
whenever `CytoidGameCoreClient.getEngineMode()` reports `'mock'`. The badge
is hidden in release builds and whenever the real Unity runtime is mounted.

To verify Unity startup, VFS loading, scene rendering, callbacks, and the
native lifecycle path:

1. Build and run on a physical iOS device after installing artifacts.
2. Confirm the `MOCK ENGINE` chip is absent.
3. Call `CytoidGameCoreClient.getEngineMode()` and expect `'unity'`.

Producing a simulator-capable artifact requires adding a simulator slice to
`UnityFramework.xcframework` (a Unity export configuration change tracked
outside this example app — see `docs/unity-ios-export.md` in `cytoid_flutter`).