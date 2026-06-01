# Example built-in levels

Add one unpacked folder per level under `assets/levels/`. Each folder must contain `level.json`.

## Register assets (manual)

Flutter only bundles directories listed in `pubspec.yaml`. A single `assets/levels/` line does **not** include nested folders — each level needs its own entry.

After adding or removing a folder, run from `engines/unity/flutter_plugin/example`:

```sh
dart run tool/sync_level_assets.dart
flutter pub get
```

Then fully restart the app (hot reload does not pick up new assets).

## Runtime

Levels are discovered from the asset manifest (`assets/levels/*/level.json`).
`offset_guide/` is for **Settings → Global calibration** only and is not shown on the level select screen.
