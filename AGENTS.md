# AGENTS.md — Cytoid Core Unity

> **IMPORTANT: Keep this file up-to-date.** When build paths, integration targets, or the ecosystem layout change, update this document. An outdated AGENTS.md is worse than none.

## Project Overview

**Cytoid** is a monorepo. The Unity gameplay core lives in `engines/unity/` and handles note rendering, audio, hit detection, scoring, and the storyboard engine. It is extracted/evolved from the legacy Unity-only client and is designed to embed in a **Bridge-embedded** via an engine-agnostic JSON protocol.

| Item | Value |
|------|-------|
| Unity editor | **6000.0.75f1** (`engines/unity/ProjectSettings/ProjectVersion.txt`) |
| Language | C# |
| Remote | `origin` → `https://github.com/Cytoid/cytoid-core-unity.git` |
| Upstream reference | `upstream` → `https://github.com/Cytoid/Cytoid-private.git` (legacy production client) |
| Unity project root | `engines/unity/` |

### Runtime modes

`GameEmbedMode` (`engines/unity/Assets/Scripts/Host/GameEmbedMode.cs`) selects behavior at compile/runtime:

| Mode | Define / trigger | Behavior |
|------|------------------|----------|
| **Standalone debug** | Default in Editor (no `CYTOID_FLUTTER_HOST`) | Navigation + Game for in-editor Play Mode |
| **Bridge-embedded** | `CYTOID_FLUTTER_HOST` on plugin export builds | `CoreHostBootstrap` + `bridge.play.start` → Game; results via `GameBridge` / host protocol |

### Scenes (build order)

| Scene | Path | Role |
|-------|------|------|
| Bootstrapper | `engines/unity/Assets/Scenes/Bootstrapper.unity` | Splash (distribution-specific), loads Navigation |
| Navigation | `engines/unity/Assets/Scenes/Navigation.unity` | Menus, `ScreenManager`, local DB init |
| CoreHostBootstrap | `engines/unity/Assets/Scenes/CoreHostBootstrap.unity` | Minimal Bridge-embedded runtime bootstrap; no Navigation UI |
| Game | `engines/unity/Assets/Scenes/Game.unity` | Gameplay |

Navigation scenes remain for in-editor debugging with Play Mode. **Plugin exports** use **CoreHostBootstrap + Game** only (`CytoidCoreBuild.PluginBuildScenes`).

## Repository Structure

```
Cytoid/
├── engines/
│   └── unity/
│       ├── Assets/             # Unity scenes, scripts, plugins, shaders, resources
│       ├── Packages/           # UPM manifest (UniTask, unity-mcp, ...)
│       ├── ProjectSettings/
│       └── flutter_plugin/     # cytoid_game_core Flutter plugin, examples, artifact tools
├── tools/                      # Monorepo-level maintainer scripts
├── docs/                       # Monorepo-level docs
├── .github/workflows/          # GameCI + Flutter plugin artifact packaging
└── README.md                   # Human-oriented; may lag AGENTS.md on paths
```

### Canonical payload types (host protocol)

Dart models in `engines/unity/flutter_plugin/lib/src/models/` mirror C#:

- `engines/unity/Assets/Scripts/Game/GameLaunchPayload.cs`
- `engines/unity/Assets/Scripts/Game/GameResultPayload.cs`
- `engines/unity/Assets/Scripts/Game/GameLaunchBridge.cs` / `GameResultBridge.cs`

Protocol spec: `docs/host-protocol-v2.md` (v1 doc at `engines/unity/flutter_plugin/example/docs/host-protocol.md` is DEPRECATED; keep `cytoid_flutter/docs/host-protocol.md` in sync).

**JNI / native callback (Android):** Unity C# → `NativeHostMessenger` → `org.cytoid.gamecore.UnityHostCallback.onMessage` (implemented in `engines/unity/flutter_plugin/android/`). Not the legacy `com.example.cytoid_flutter.host.UnityHostCallback` string in older README text.

**Native send failures (Android `sendToUnity` / `returnToFlutterActivity`):** when the reflective `UnitySendMessage` call or the `startActivity` back to the Flutter Activity throws, the failure is NOT surfaced as a synchronous `PlatformException` from `send()`. `send()` returns `Future<void>` and callers have no wait point to convert the exception. Instead, the bridge routes the failure asynchronously via the v2 § Active-Session Runtime Failure contract:

- `activeSessionId == null` → `engine.error` envelope via the EventChannel, `error.code = "runtime_exception"`, message sanitized to `"<ExceptionClassSimpleName>: <first message line>"` (no raw stack trace).
- `activeSessionId != null` → synthesized `session.failed` with `error.code = "runtime_unreachable"` via the T4 primitive. Never both — active-session failures use `session.failed` ONLY; `engine.error` is for non-session failures.

**Android Unity Activity lifecycle (warm-resident):** the exclusive Unity Activity (`me.tigerhix.cytoid.CytoidPluginActivity`) is NOT `finish()`-ed on session end. `hideGameSurface()` brings the Flutter Activity to front via `FLAG_ACTIVITY_REORDER_TO_FRONT | FLAG_ACTIVITY_SINGLE_TOP` and leaves the Unity Activity warm-resident in the back stack. This keeps Unity/IL2CPP/GL state alive across the typical play loop (select level → play → result → select next), eliminating the 5–15s cold-start tax on every session.

Activity lifecycle callbacks drive the T3 state machine:
- `onActivityPaused` (Unity Activity) → `runtimeState.onSuspend()` (READY|BUSY → SUSPENDED; single-slot prior state preserved).
- `onActivityResumed` → `runtimeState.onResume()` (SUSPENDED → prior state).
- `onActivityDestroyed` → if `activeSessionId != null`, T4's `synthesizeRuntimeFailure(SURFACE_LOST, sessionId)` fires (emits `session.failed` with `error.code = "runtime_surface_lost"`, transitions to FAILED); otherwise `runtimeState.reset()` returns to UNAVAILABLE (caller must `startRuntime()` again).

A `@VisibleForTesting unityActivityInstanceCount` counter tracks live Unity Activity instances. In the warm-resident policy this counter MUST stay at 0 or 1 across arbitrary session cycles; a value > 1 indicates Activity accumulation (memory leak). The memory regression test runs 10 sequential session cycles via low-level primitives and asserts the counter never exceeds 1.

The Unity Activity source lives in the AAR and is NOT editable from this repo. Lifecycle integration is exclusively via `Application.ActivityLifecycleCallbacks` registered in `CytoidGameCoreBridge.attachActivity`.

---

## Build & Debug

Plugin builds: **Cytoid → Build Android/iOS Plugin Artifacts** (`engines/unity/Assets/Scripts/Editor/CytoidCoreBuild.cs`). Storyboard vendor check: **Cytoid → Log Storyboard Effects Backend**.

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
PROJECT="/path/to/Cytoid/engines/unity"

"$UNITY" -batchmode -quit \
  -projectPath "$PROJECT" \
  -executeMethod CytoidCoreBuild.ExportAndroidLibraryForFlutter \
  -logFile "$PROJECT/flutter_plugin/.cytoid_game_core/build/unity-android.log"
```

Exports to `engines/unity/flutter_plugin/.cytoid_game_core/exports/android/unityLibrary`, then runs `engines/unity/flutter_plugin/tool/build_unity_aar.sh` → `engines/unity/flutter_plugin/.cytoid_game_core/artifacts/unity/android/cytoid-unity-core.aar` (+ dependency AARs).

Re-package only (after manual export edits): `cd engines/unity/flutter_plugin && ./tool/build_unity_aar.sh`

IL2CPP ARM64 export typically takes **5–15+ minutes** on a clean machine.

Run example:

```bash
cd engines/unity/flutter_plugin/example && flutter pub get && flutter run
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

> **iOS Simulator runs mock-only because the current Unity artifact is device-only. Real-device testing is required for Unity verification.**

- Batch or **Cytoid → Build iOS Plugin Artifacts**: `CytoidCoreBuild.ExportIOSLibraryForFlutter`
  exports to `engines/unity/flutter_plugin/.cytoid_game_core/exports/ios/UnityLibrary`, then runs
  `engines/unity/flutter_plugin/tool/build_unity_ios_framework.sh` (requires macOS + Xcode) to write
  `engines/unity/flutter_plugin/.cytoid_game_core/artifacts/unity/ios/UnityFramework.framework` and
  `UnityFramework.xcframework`; Swift Package Manager uses the `.xcframework`.
- CI can call `CytoidCoreBuild.ExportIOSLibraryForFlutterWithoutPackaging` to
  export the UnityLibrary Xcode project on GameCI/Linux and package it later on
  macOS.
- Re-package only: `cd engines/unity/flutter_plugin && ./tool/build_unity_ios_framework.sh`
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
- Match existing patterns in `engines/unity/Assets/Scripts/` (UniTask, Newtonsoft JSON, namespace layout).
- **Minimize scope** — prefer focused changes; do not refactor unrelated Unity or Flutter code.

### Licensing & assets

- Optional paid packages under **`engines/unity/Assets/Vendor/<Package>/`** (gitignored), installed via maintainer zip (`engines/unity/flutter_plugin/tool/install_vendor_from_archive.sh`). Example: `engines/unity/Assets/Vendor/StoryboardFilters/`. Open-source clones use fallbacks in `engines/unity/Assets/Shaders/Storyboard/`. See `docs/vendor.md`.
- Some other commercial plugins and art are **not** in git (see upstream `Cytoid-private`). Do not commit licensed third-party assets or secrets.
- `Builds/`, `Library/`, `engines/unity/flutter_plugin/.cytoid_game_core/exports/`, and artifact binaries are **local-only** (gitignored).

### CytoidGameCore protocol changes

If you change envelope types or payloads:

1. Update C# (`engines/unity/Assets/Scripts/Host/`, `GameLaunchPayload`, `GameResultBridge`, …).
2. Update Dart models in `engines/unity/flutter_plugin/lib/`.
3. Update `docs/host-protocol-v2.md` and keep `cytoid_flutter/docs/host-protocol.md` in sync if the contract changed.

### Testing on device

- Prefer **plugin artifact builds** and `engines/unity/flutter_plugin/example` for Flutter ↔ Unity messaging (`bridge.play.start`, `game.play.result`, `game.ready`, `bridge.ping`).
- Android logcat: `adb logcat -s Unity ActivityManager`

### Unity MCP

`engines/unity/Packages/com.coplaydev.unity-mcp` is present for editor automation; optional for agents with Unity Editor access.

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
| Build menu / batchmode | `engines/unity/Assets/Scripts/Editor/CytoidCoreBuild.cs` |
| CI plugin artifacts | `.github/workflows/flutter-plugin-artifacts.yml` |
| Vendor asset install | `engines/unity/flutter_plugin/tool/install_vendor_from_archive.sh`, `docs/vendor.md` |
| Game bridge | `engines/unity/Assets/Scripts/Host/GameBridge.cs` |
| Wire envelope (C#) | `engines/unity/Assets/Scripts/Host/CytoidGameCoreEnvelope.cs` |
| Game log forwarding | `engines/unity/Assets/Scripts/Host/GameLogBridge.cs` |
| Embed mode | `engines/unity/Assets/Scripts/Host/GameEmbedMode.cs` |
| Native outbound (game → bridge) | `engines/unity/Assets/Scripts/Host/NativeBridgeMessenger.cs` |
| Flutter plugin (Kotlin) | `engines/unity/flutter_plugin/android/src/main/kotlin/org/cytoid/gamecore/` |
| Flutter plugin (iOS Swift / SPM) | `engines/unity/flutter_plugin/ios/cytoid_game_core/` |
| Flutter Dart API | `engines/unity/flutter_plugin/lib/src/cytoid_game_core_client.dart` |
| Protocol doc | `docs/host-protocol-v2.md` (v1 doc at engines/unity/flutter_plugin/example/docs/host-protocol.md is DEPRECATED) |
| Legacy architecture notes | `engines/unity/flutter_plugin/example/docs/old-architecture/` |
