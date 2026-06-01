# cytoid_game_core example

Minimal playable Flutter shell for the `cytoid_game_core` plugin.

It contains:

- a Flutter level select page
- a Flutter settings page
- Unity fullscreen gameplay via `CytoidGameCoreClient`
- a Flutter result page fed by `game.play.result`

```sh
flutter pub get
flutter run
```

Built-in demo levels live under `assets/levels/`. After adding a level folder, run
`dart run tool/sync_level_assets.dart` and `flutter pub get` (see
`assets/levels/README.md`).

When Unity artifacts are absent, the plugin runs a mock fullscreen session. Install
real artifacts from `../tool/setup_unity_artifacts.sh` to launch the Unity core.
