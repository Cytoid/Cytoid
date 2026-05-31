# AGENTS.md — Cytoid Core Unity

> **IMPORTANT: Keep this file up-to-date.** When build paths, integration targets, or the ecosystem layout change, update this document. An outdated AGENTS.md is worse than none.

## Project Overview

**cytoid-core-unity** is the Unity 6 gameplay core for [Cytoid](https://cytoid.io): note rendering, audio, hit detection, scoring, and the storyboard engine. It is extracted/evolved from the legacy Unity-only client and is designed to embed in a **Bridge-embedded** via an engine-agnostic JSON protocol.

| Item | Value |
|------|-------|
| Unity editor | **6000.0.75f1** (`ProjectSettings/ProjectVersion.txt`) |
| Language | C# |
| Remote | `origin` → `https://github.com/Cytoid/cytoid-core-unity.git` |
| Upstream reference | `upstream` → `https://github.com/Cytoid/Cytoid-private.git` (legacy production client) |

### Runtime modes

`GameEmbedMode` (`Assets/Scripts/Host/GameEmbedMode.cs`) selects behavior at compile/runtime:

| Mode | Define / trigger | Behavior |
|------|------------------|----------|
| **Standalone debug** | Default in Editor (no `CYTOID_FLUTTER_HOST`) | Navigation + Game for in-editor Play Mode |
| **Bridge-embedded** | `CYTOID_FLUTTER_HOST` on plugin export builds | `CoreHostBootstrap` + `bridge.play.start` → Game; results via `GameBridge` / host protocol |

### Scenes (build order)

| Scene | Path | Role |
|-------|------|------|
| Bootstrapper | `Assets/Scenes/Bootstrapper.unity` | Splash (distribution-specific), loads Navigation |
| Navigation | `Assets/Scenes/Navigation.unity` | Menus, `ScreenManager`, local DB init |
| CoreHostBootstrap | `Assets/Scenes/CoreHostBootstrap.unity` | Minimal Bridge-embedded runtime bootstrap; no Navigation UI |
| Game | `Assets/Scenes/Game.unity` | Gameplay |

Navigation scenes remain for in-editor debugging with Play Mode. **Plugin exports** use **CoreHostBootstrap + Game** only (`CytoidCoreBuild.PluginBuildScenes`).

## Repository Structure

```
cytoid-core-unity/
├── Assets/
│   ├── Scenes/                 # Bootstrapper, CoreHostBootstrap, Navigation, Game
│   ├── Scripts/
│   │   ├── Game/               # Gameplay engine, GameLaunch/Result bridges
│   │   ├── Host/               # Game-side bridge wire adapter (GameBridge, CytoidGameCoreEnvelope)
│   │   ├── Navigation/         # Menu UI (standalone / debug)
│   │   ├── Storyboard/         # Chart background engine
│   │   ├── Online/             # API models
│   │   └── Editor/
│   │       └── CytoidCoreBuild.cs   # Cytoid menu plugin builds + batchmode entry points
│   └── Plugins/
│       ├── Android/AndroidManifest.xml   # CytoidPluginActivity
│       └── iOS/CytoidHostNativeBridge.mm
├── flutter_plugin/             # cytoid_game_core Flutter plugin (in-repo)
│   ├── lib/                    # Dart: CytoidGameCoreClient, envelopes, payloads
│   ├── android/                # org.cytoid.gamecore — loads *.aar artifacts
│   ├── example/                # Minimal Bridge-embedded demo
│   ├── tool/                   # build/package artifact scripts + vendor install helper
│   └── .cytoid_game_core/      # gitignored exports + artifacts (local)
├── .github/workflows/          # GameCI + Flutter plugin artifact packaging
├── Packages/                   # UPM manifest (UniTask, unity-mcp, …)
├── ProjectSettings/
└── README.md                   # Human-oriented; may lag AGENTS.md on paths
```

### Canonical payload types (host protocol)

Dart models in `flutter_plugin/lib/src/models/` mirror C#:

- `Assets/Scripts/Game/GameLaunchPayload.cs`
- `Assets/Scripts/Game/GameResultPayload.cs`
- `Assets/Scripts/Game/GameLaunchBridge.cs` / `GameResultBridge.cs`

Protocol spec: `flutter_plugin/example/docs/host-protocol.md` (shared with `cytoid_flutter/docs/host-protocol.md`).

**JNI / native callback (Android):** Unity C# → `NativeHostMessenger` → `org.cytoid.gamecore.UnityHostCallback.onMessage` (implemented in `flutter_plugin/android/`). Not the legacy `com.example.cytoid_flutter.host.UnityHostCallback` string in older README text.

---

## Build & Debug

Plugin builds: **Cytoid → Build Android/iOS Plugin Artifacts** (`Assets/Scripts/Editor/CytoidCoreBuild.cs`). Storyboard vendor check: **Cytoid → Log Storyboard Effects Backend**.

### Package identifiers

| Build | Android `applicationId` |
|-------|-------------------------|
| Production (ProjectSettings) | `me.tigerhix.cytoid` |
| Flutter Unity library export | `com.example.cytoid_flutter.unity` |
| Flutter example app | `com.example.cytoid_flutter` |
| Flutter plugin namespace | `org.cytoid.gamecore` |

### Flutter plugin artifacts (primary)

Editor: **Cytoid → Build Android/iOS Plugin Artifacts** (export + package in one step).

Android batchmode:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.0.75f1/Unity.app/Contents/MacOS/Unity"
PROJECT="/path/to/cytoid-core-unity"

"$UNITY" -batchmode -quit \
  -projectPath "$PROJECT" \
  -executeMethod CytoidCoreBuild.ExportAndroidLibraryForFlutter \
  -logFile "$PROJECT/flutter_plugin/.cytoid_game_core/build/unity-android.log"
```

Exports to `flutter_plugin/.cytoid_game_core/exports/android/unityLibrary`, then runs `flutter_plugin/tool/build_unity_aar.sh` → `flutter_plugin/.cytoid_game_core/artifacts/unity/android/cytoid-unity-core.aar` (+ dependency AARs).

Re-package only (after manual export edits): `cd flutter_plugin && ./tool/build_unity_aar.sh`

IL2CPP ARM64 export typically takes **5–15+ minutes** on a clean machine.

Run example:

```bash
cd flutter_plugin/example && flutter pub get && flutter run
```

Without artifacts, the plugin uses a **mock engine** (host protocol still works).

Optional download: `CYTOID_GAME_CORE_ARTIFACT_BASE_URL` + `./tool/setup_unity_artifacts.sh`.

CI: `.github/workflows/flutter-plugin-artifacts.yml` is manual-only
(`workflow_dispatch`). It uses GameCI `unity-builder@v4` for Android AAR
export/package and, when `build_ios` is enabled, iOS UnityLibrary export. iOS
`UnityFramework.xcframework` packaging runs afterward on macOS/Xcode from the
exported Xcode project. The workflow uploads GitHub Actions artifacts only; it
does not create release PRs, tags, GitHub Releases, or dart.dev publications.

### cytoid_flutter (full app)

Documented in sibling repo: `cytoid_flutter/docs/unity-android-export.md`, `scripts/export_unity_android.sh`.

Requires Unity export at `cytoid_flutter/android/unityLibrary/` (set `Cytoid.FlutterUnityLibraryRelativePath` in Unity EditorPrefs if batch export uses the plugin default path).

```bash
cd cytoid_flutter
export UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.75f1/Unity.app/Contents/MacOS/Unity"
./scripts/export_unity_android.sh
flutter run
```

### iOS Bridge-embedded

- Batch or **Cytoid → Build iOS Plugin Artifacts**: `CytoidCoreBuild.ExportIOSLibraryForFlutter`
  exports to `flutter_plugin/.cytoid_game_core/exports/ios/UnityLibrary`, then runs
  `flutter_plugin/tool/build_unity_ios_framework.sh` (requires macOS + Xcode) to write
  `flutter_plugin/.cytoid_game_core/artifacts/unity/ios/UnityFramework.framework` and
  `UnityFramework.xcframework`; Swift Package Manager uses the `.xcframework`.
- CI can call `CytoidCoreBuild.ExportIOSLibraryForFlutterWithoutPackaging` to
  export the UnityLibrary Xcode project on GameCI/Linux and package it later on
  macOS.
- Re-package only: `cd flutter_plugin && ./tool/build_unity_ios_framework.sh`
- The current Unity iOS export is device-only. Simulator builds need a simulator
  slice in `UnityFramework.xcframework`, or no mounted Unity artifact so the
  plugin falls back to the mock runtime.
- The Flutter plugin example iOS app is Swift Package Manager only; do not
  re-add `Podfile`, `Podfile.lock`, Pods xcconfig includes, or manual
  UnityFramework link/embed entries to `Runner.xcodeproj`.
- `cytoid_flutter`: see `docs/unity-ios-export.md` and `scripts/export_unity_ios.sh`

### Batch entry points

| Method | Purpose |
|--------|---------|
| `CytoidCoreBuild.ExportAndroidLibraryForFlutter` | Export Gradle library + AAR artifacts |
| `CytoidCoreBuild.ExportIOSLibraryForFlutter` | Export Xcode project + UnityFramework.xcframework |
| `CytoidCoreBuild.ExportIOSLibraryForFlutterWithoutPackaging` | Export iOS Xcode project only; CI packages on macOS |

---

## Conventions for Agents

### Language & style

- **English** for code, comments, commit messages, and user-visible debug strings unless a feature explicitly requires localization.
- Match existing patterns in `Assets/Scripts/` (UniTask, Newtonsoft JSON, namespace layout).
- **Minimize scope** — prefer focused changes; do not refactor unrelated Unity or Flutter code.

### Licensing & assets

- Optional paid packages under **`Assets/Vendor/<Package>/`** (gitignored), installed via maintainer zip (`flutter_plugin/tool/install_vendor_from_archive.sh`). Example: `Assets/Vendor/StoryboardFilters/`. Open-source clones use fallbacks in `Assets/Shaders/Storyboard/`. See `docs/vendor.md`.
- Some other commercial plugins and art are **not** in git (see upstream `Cytoid-private`). Do not commit licensed third-party assets or secrets.
- `Builds/`, `Library/`, `flutter_plugin/.cytoid_game_core/exports/`, and artifact binaries are **local-only** (gitignored).

### CytoidGameCore protocol changes

If you change envelope types or payloads:

1. Update C# (`Assets/Scripts/Host/`, `GameLaunchPayload`, `GameResultBridge`, …).
2. Update Dart models in `flutter_plugin/lib/`.
3. Update `flutter_plugin/example/docs/host-protocol.md` and keep `cytoid_flutter/docs/host-protocol.md` in sync if the contract changed.

### Testing on device

- Prefer **plugin artifact builds** and `flutter_plugin/example` for Flutter ↔ Unity messaging (`bridge.play.start`, `game.play.result`, `game.ready`, `bridge.ping`).
- Android logcat: `adb logcat -s Unity ActivityManager`

### Unity MCP

`Packages/com.coplaydev.unity-mcp` is present for editor automation; optional for agents with Unity Editor access.

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-05 | Unity 6 (6000.0.75f1) core fork | IL2CPP / mobile toolchain modernization |
| 2026-05 | Engine-agnostic host protocol | Flutter shell; future Godot adapter |
| 2026-05 | `cytoid_game_core` plugin + versioned AAR artifacts | Example app and CI can avoid committing `unityLibrary` trees |
| 2026-05 | Dual export targets (plugin `.cytoid_game_core/exports` vs `cytoid_flutter/android/unityLibrary`) | Plugin packaging vs full-app Gradle embed |
| 2026-05 | Tier = single `bridge.play.start` session + `tierPlay` launch/result | Flutter orchestrates multi-stage runs; core keeps `tierHpMods` only |
| 2026-05 | No Unity disk persistence (LiteDB removed) | Settings/records/scores live in Flutter; core uses in-memory `LocalPlayerSettings`, `LevelRecord`, `GameLaunchSettings` |
| 2026-05 | Lunar Console removed; `game.log` forwarding | Bridge-embedded receives Unity logs; Graphy retained for in-engine profiler overlay |
| 2026-05 | Cytoid top-level menu = plugin builds only | Flat `Cytoid/…` items; no Core Build submenu; exports use `PluginBuildScenes` + `CYTOID_FLUTTER_HOST` |

Append new rows when architecture or default paths change.

---

## Key File Index

| Topic | Location |
|-------|----------|
| External dependencies inventory | `DEPENDENCIES.md` |
| Build menu / batchmode | `Assets/Scripts/Editor/CytoidCoreBuild.cs` |
| CI plugin artifacts | `.github/workflows/flutter-plugin-artifacts.yml` |
| Vendor asset install | `flutter_plugin/tool/install_vendor_from_archive.sh`, `docs/vendor.md` |
| Game bridge | `Assets/Scripts/Host/GameBridge.cs` |
| Wire envelope (C#) | `Assets/Scripts/Host/CytoidGameCoreEnvelope.cs` |
| Game log forwarding | `Assets/Scripts/Host/GameLogBridge.cs` |
| Embed mode | `Assets/Scripts/Host/GameEmbedMode.cs` |
| Native outbound (game → bridge) | `Assets/Scripts/Host/NativeBridgeMessenger.cs` |
| Flutter plugin (Kotlin) | `flutter_plugin/android/src/main/kotlin/org/cytoid/gamecore/` |
| Flutter plugin (iOS Swift / SPM) | `flutter_plugin/ios/cytoid_game_core/` |
| Flutter Dart API | `flutter_plugin/lib/src/cytoid_game_core_client.dart` |
| Protocol doc | `flutter_plugin/example/docs/host-protocol.md` |
| Legacy architecture notes | `flutter_plugin/example/docs/old-architecture/` |
