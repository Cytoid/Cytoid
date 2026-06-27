# Flutter Client Launch Architecture Review

This document records a pre-launch architecture and implementation review for
starting the formal Flutter client work around `cytoid_game_core` and the Unity
bridge runtime.

The review focuses on:

1. Overdesigned or duplicated architecture that can be simplified before the
   Flutter client depends on it.
2. Unity/gameplay configuration that is not yet exposed through the Flutter
   plugin.
3. Issues that may look small now but will become expensive once the Flutter
   client has multiple gameplay entry points, settings screens, download flows,
   release artifacts, and real device testing.

Important assumption: Flutter and Unity do not need protocol backward or forward
compatibility at this stage. Breaking protocol changes are acceptable as long as
both sides are changed together.

## Executive Summary

The highest-risk problems are not isolated implementation bugs. They are state
and session semantics that are currently split across Dart, native Android/iOS,
and Unity.

Before the formal Flutter client starts depending on this bridge, the project
should make one breaking cleanup pass:

- Every gameplay session should end with one explicit, typed result message.
- Runtime readiness should come from Unity acknowledgement, not artifact
  presence or native guesses.
- The plugin should expose a higher-level session API so the Flutter app does
  not copy fragile sequencing from the example app.
- The plugin should expose the gameplay settings Flutter will own, especially
  language, telemetry recording, typed settings enums, and mod validation.
- Artifact builds should have a manifest/version contract and at least one
  real host smoke test.

## v2 Disposition

This section was added after protocol v2 (`docs/host-protocol-v2.md`) was
drafted and its Open Decisions were resolved. It maps each finding in this
review to its current status so engineers know which items are still active.

Statuses:

- **Resolved by v2**: the protocol spec now prevents this issue; no further
  protocol work is needed. Implementation still has to follow the spec.
- **Tracked outside protocol**: real issue, but cannot be solved at the
  protocol layer. Belongs to native plugin, Flutter client, CI, or release
  work. Track in a separate task list, not here.
- **Superseded**: the original recommendation no longer applies because of an
  Open Decision that took a different direction.

| Finding | Status | Note |
|---|---|---|
| P0-1 Not every session emits `game.play.result` | Resolved by v2 | One session → one `session.result` with typed outcome. |
| P0-2 `game.play.result` and `game.play.ended` overlap | Resolved by v2 | Only `session.result` is terminal. |
| P0-3 Artifact presence treated as "starting" | Resolved by v2 | Runtime states; `ready` requires engine ack. |
| P0-4 Android Unity Activity lifecycle not explicit | Tracked outside protocol | OD3 committed to warm-resident runtime; specific Activity/window lifecycle is native impl. |
| P0-5 Session orchestration in example-app | Partially resolved by v2 | Public concepts (`PlaySession`, `waitForReady`, `GameSession`) are listed as required in Dart API Expectations, but specific method names/signatures are suggested, not normative. Plugin implementation still has to provide them. |
| P0-6 CI does not prove Flutter hosts Unity | Tracked outside protocol | CI/release work; protocol spec cannot address this. |
| P1-1 `levelMetaJson` double-encoded | Resolved by v2 | `level.meta` structured; `LevelMetaPayload` schema defined. |
| P1-2 Pending settings leak across sessions | Resolved by v2 | `settings.apply` separated from `session.start.settings`; no implicit merge. |
| P1-3 Settings always ack as applied | Resolved by v2 | `settings.applied` returns `appliedFields` / `deferredFields` / `rejectedFields` / `errors`. |
| P1-4 Play events always sent in result | Resolved by v2 | Opt-in `recordPlayEvents` + separate `session.telemetry` message; result carries summary only. |
| P1-5 Mods too stringly typed | Resolved by v2 | Typed mod ids; deterministic conflict table. |
| P1-6 Note type / quality not public | Resolved by v2 | Typed enums in plugin surface; `NoteStyle` requires all 8 keys. |
| P1-7 Unity settings missing from plugin | Resolved by v2 | Profile/Runtime/Visual/Audio/NoteStyle groups. |
| P1-8 Android send failures not reported | Tracked outside protocol | v2 provides `engine.error` channel; native bridge still has to surface JNI failures through it. |
| P1-9 iOS `ensureRuntimeStarted` not ready | Resolved by v2 | `waitForReady()` contract; runtime states. |
| P1-10 Artifact and plugin versions not coupled | Tracked outside protocol | Release/CI work; outside protocol scope. |
| O1 Too many health/status channels | Resolved by v2 | Single `health.check` / `health.ok` path. |
| O2 Two terminal message types | Resolved by v2 | See P0-2. |
| O3 Double handoff UI | Tracked outside protocol | UX implementation; spec says native lifecycle is transport, not gameplay. |
| O4 Manual cross-language model sync | Partially resolved by v2 | v2 adds explicit field tables (LevelMeta, telemetry, logs); golden fixtures are listed in checklist item 6 but not yet implemented. Full codegen/single-source-of-truth is still a separate task. |
| O5 Protocol version field without compat strategy | Resolved by v2 | `schema` is a permanent fail-fast marker, not compat negotiation. |
| M1 VFS materializer cache does not refresh | Tracked outside protocol | Plugin/client implementation. |
| M2 Mock runtime does not match expected role | Resolved by v2 | `docs/mock-engine.md` rewritten for v2; pure-Dart mock client in implementation plan. |
| M3 Android artifact availability at config time | Tracked outside protocol | Gradle/build config. |
| M4 iOS device-only Unity artifact | Tracked outside protocol | Unity build configuration. |
| M5 Hard-coded Android class names | Tracked outside protocol | Native build configuration. |
| M6 Tooling and documentation drift | Tracked outside protocol | Maintenance work; this v2 disposition itself partially addresses the docs-drift concern. |

Items marked **Tracked outside protocol** should be moved into a host
implementation tracking document before the formal Flutter client work begins;
they are real and necessary, just not protocol-shape problems.

## Review Scope

Primary files and areas reviewed:

- `engines/unity/flutter_plugin/lib/`
- `engines/unity/flutter_plugin/example/`
- `engines/unity/flutter_plugin/android/`
- `engines/unity/flutter_plugin/ios/`
- `engines/unity/Assets/Scripts/Host/`
- `engines/unity/Assets/Scripts/Game/GameLaunchPayload.cs`
- `engines/unity/Assets/Scripts/Game/GameResultPayload.cs`
- `engines/unity/Assets/Scripts/Game/GameResultBridge.cs`
- `engines/unity/Assets/Scripts/Game/ExternalGameContentProvider.cs`
- `engines/unity/Assets/Scripts/Context.cs`
- `engines/unity/Assets/Scripts/Editor/CytoidCoreBuild.cs`
- `.github/workflows/flutter-plugin-artifacts.yml`
- Flutter plugin artifact setup and packaging scripts
- Host protocol documentation

This was a static architecture review. Unity batch builds, Flutter real-device
example runs, iOS packaging, and Android/iOS integration tests were not executed.

## P0 Findings

### P0-1: Not Every Gameplay Session Emits `game.play.result`

Relevant files:

- `engines/unity/Assets/Scripts/Context.cs`
- `engines/unity/Assets/Scripts/Game/Game.cs`
- `engines/unity/Assets/Scripts/Game/GameResultBridge.cs`
- `engines/unity/flutter_plugin/lib/src/cytoid_game_core_client.dart`

`Context.EmitExternalGameResult()` currently delegates to
`SaveLastCompletedGameResult()`. That method still contains debug navigation
semantics: cache the last completed standard result, but ignore some other
outcomes.

Problematic behavior:

- Completed `Standard` sessions with Auto mods can be dropped.
- `Practice` and future non-standard modes can be dropped.
- Failed sessions often go through `game.play.ended`, not a score/result
  payload.
- Flutter's `startPlay()` waits for `game.play.result`; if Unity emits no result,
  the client eventually reports the engine as lost instead of receiving a clear
  terminal outcome.

Why this will scale badly:

The formal Flutter client will need reliable behavior for standard play,
practice, calibration, tier stages, retry flows, failed runs, cancelled runs,
analytics, result screens, and eventual replay upload. If "no result" is used
for both user navigation and missing/filtered outcomes, every caller has to
guess what happened.

Recommended fix:

- Stop reusing debug navigation result caching for the bridge protocol.
- Make the bridge path always emit one explicit terminal outcome.
- Replace the current overlapping result fields with a discriminated outcome,
  for example:
  - `completed`
  - `failed`
  - `cancelled`
  - `tierRetry`
  - `calibration`
- Keep debug navigation persistence separate from host protocol emission.

### P0-2: `game.play.result` and `game.play.ended` Have Overlapping Semantics

Relevant files:

- `engines/unity/Assets/Scripts/Host/WireMessageTypes.cs`
- `engines/unity/Assets/Scripts/Host/GameBridge.cs`
- `engines/unity/Assets/Scripts/Host/GameBridgeRouter.cs`
- `engines/unity/flutter_plugin/lib/src/cytoid_game_core_client.dart`
- `engines/unity/flutter_plugin/example/docs/host-protocol.md`

The protocol currently has two terminal-looking messages:

- `game.play.result`
- `game.play.ended`

The documentation says failures should be reported through `game.play.result`,
but some user actions and failed/tier routes produce `game.play.ended`. Dart
treats `game.play.ended` as an exceptional route-ending condition.

Why this will scale badly:

The Flutter client needs a single source of truth for result routing, failed
score display, retry UX, analytics, and upload behavior. Splitting terminal
semantics across two message types will lead to duplicated handling and edge
case bugs.

Recommended fix:

- Use `game.play.result` for every gameplay terminal state.
- Remove `game.play.ended`, or reduce it to a native/surface acknowledgement
  that does not describe gameplay outcome.
- Update Dart so `startPlay()` resolves with a typed outcome instead of throwing
  for expected user-cancelled or route-ended cases.

### P0-3: Runtime Status Treats Artifact Presence as "Starting"

Relevant files:

- `engines/unity/flutter_plugin/android/src/main/kotlin/org/cytoid/gamecore/CytoidGameCoreBridge.kt`
- `engines/unity/flutter_plugin/ios/cytoid_game_core/Sources/cytoid_game_core/CytoidGameCoreBridge.swift`
- `engines/unity/flutter_plugin/lib/src/game_runtime_status.dart`
- `engines/unity/flutter_plugin/lib/src/cytoid_game_core_client.dart`

On Android and iOS, native runtime status can report `starting` just because the
Unity artifact/framework is present. This means "artifact exists", "runtime is
launching", "runtime is stuck", and "runtime has never acknowledged ready" are
not clearly separated.

Why this will scale badly:

The Flutter client will use runtime status for route decisions, loading UI,
health checks, and diagnostics. A device with broken Unity startup can look like
it is merely starting forever.

Recommended fix:

- `starting` should mean startup was explicitly requested and is in progress.
- `ready` should only come from Unity `game.ready` or an equivalent ack.
- Add a `failed` or structured error state for launch failures.
- `ensureRuntimeStarted()` should either wait until a meaningful state is
  reached or document that callers must call `waitForReady()`.

### P0-4: Android Unity Activity Lifecycle Is Not Explicit Enough

Relevant file:

- `engines/unity/flutter_plugin/android/src/main/kotlin/org/cytoid/gamecore/CytoidGameCoreBridge.kt`

`hideGameSurface()` currently restores display refresh rate and brings the
Flutter Activity to the front, but it does not finish the Unity Activity. This
can leave Unity, IL2CPP, GL state, and memory resident behind the Flutter UI.

Why this will scale badly:

The core app loop is likely to be:

1. Select level.
2. Start Unity.
3. Return to Flutter result screen.
4. Select another level.
5. Repeat.

If the lifecycle strategy is implicit, memory pressure, back stack behavior,
surface visibility, and refresh rate state can become device-specific bugs.

Recommended fix:

- Decide and document the lifecycle policy:
  - warm runtime kept resident; or
  - tear down Unity activity after a session; or
  - explicit mode chosen by the host.
- If warm runtime is desired, expose that as intentional configuration.
- If teardown is desired, finish or unload Unity consistently.
- Keep Android and iOS lifecycle semantics as close as practical.

### P0-5: Session Orchestration Is Mostly an Example-App Pattern

Relevant files:

- `engines/unity/flutter_plugin/lib/src/cytoid_game_core_client.dart`
- `engines/unity/flutter_plugin/example/lib/src/screens/game_session_screen.dart`

The correct sequence is currently spread across the example app:

1. `ensureRuntimeStarted()`
2. `showGameSurface()`
3. wait for `game.ready` or status `ready`
4. update settings
5. `startPlay()`
6. hide the surface
7. restore Flutter presentation

The example's ready wait can time out silently and still continue to `startPlay`.
Several cleanup calls swallow errors.

Why this will scale badly:

The formal Flutter client will likely have several launch paths: normal play,
practice, calibration, tier stages, retries, settings-driven preview, and
possibly debug tooling. If each path copies this sequence, race conditions and
silent failures will spread into app code.

Recommended fix:

- Add a public `waitForReady()` API.
- Prefer adding a higher-level `PlaySession` or `runPlaySession()` API that owns:
  - runtime startup
  - surface presentation
  - ready wait
  - launch
  - cancellation
  - cleanup
- Update the example to fail loudly when readiness is not achieved.

### P0-6: CI Does Not Prove Flutter Can Host Unity

Relevant file:

- `.github/workflows/flutter-plugin-artifacts.yml`

The current artifact workflow exports/packages Unity artifacts and runs Dart
analysis/tests. It does not prove that a Flutter host can link the artifacts,
start Unity, communicate over the bridge, load VFS assets, or receive callbacks.

Why this will scale badly:

Artifact export success is not the same as host integration success. The first
true failure may appear only when the formal Flutter client tries to run on a
real device.

Recommended fix:

- Add at least one Android Flutter build using the produced AAR.
- Add an example smoke test where possible:
  - start runtime
  - show surface
  - wait for ready
  - ping/pong
  - launch a tiny built-in chart
  - receive a terminal result
- Add iOS `flutter build ios --no-codesign` or a package-level SPM link check
  when iOS artifacts are produced.
- Write artifact metadata into a manifest: Unity version, plugin version,
  commit SHA, artifact version, platform, build date.

## P1 Findings

### P1-1: `levelMetaJson` Is Double-Encoded JSON

Relevant files:

- `engines/unity/Assets/Scripts/Game/GameLaunchPayload.cs`
- `engines/unity/flutter_plugin/lib/src/models/game_launch_payload.dart`

`levelMetaJson` is a string containing serialized JSON inside the outer launch
payload JSON.

Why this will scale badly:

Flutter download, level discovery, difficulty selection, cover resolution, and
Unity launch validation will all need to understand the same level schema. A
string field hides that schema from Dart's type system and forces late runtime
errors.

Recommended fix:

- Replace `levelMetaJson: string` with `levelMeta: object`.
- Add a shared Dart `LevelMeta` model, generated schema, or another single
  source of truth.
- Validate `selectedDifficulty` against `levelMeta` before sending the launch
  payload.

### P1-2: Pending Settings Leak Across Session Boundaries

Relevant file:

- `engines/unity/Assets/Scripts/Host/GameBridgeRouter.cs`

`bridge.settings.update` stores `pendingSettings`. On the next
`bridge.play.start`, if the launch payload has no `settings`, the router inserts
the previous pending settings into the new launch.

Why this will scale badly:

Settings can be changed from a global settings page, a pre-play screen, a
calibration screen, or during gameplay. Implicitly carrying settings across
sessions can make "why did this chart use that setting?" very hard to debug.

Recommended fix:

- Remove implicit merge into the next launch payload.
- Either:
  - require launch settings to be explicit per session; or
  - define persistent runtime/profile settings separately from launch settings.
- Clear pending settings at session end if the concept remains.

### P1-3: Settings Updates Always Ack as Applied

Relevant file:

- `engines/unity/Assets/Scripts/Host/GameBridgeRouter.cs`

`HandleSettingsUpdate()` replies with `{"applied": true}` even when the payload
is null or when individual values are invalid and ignored by `Enum.TryParse` or
dictionary parsing.

Why this will scale badly:

The Flutter settings UI will assume a setting took effect even if Unity ignored
it. Audio, offset, graphics, hitbox, and color issues become silent state drift.

Recommended fix:

- Return structured acknowledgement:
  - `applied: true/false`
  - `appliedFields`
  - `ignoredFields`
  - `errors`
- Fail launch or settings updates for invalid enum strings and invalid note type
  keys.

### P1-4: Play Events Are Always Sent in Result Payloads

Relevant files:

- `engines/unity/Assets/Scripts/Game/GameResultBridge.cs`
- `engines/unity/Assets/Scripts/Game/GameResultPayload.cs`
- `engines/unity/flutter_plugin/lib/src/models/game_result_payload.dart`

`GameResultBridge` snapshots `GamePlayEventRecorder` for result payloads.
Flutter lazily encodes the events into binary, but the JSON payload has already
crossed the bridge.

Why this will scale badly:

Long charts and dense input can produce large JSON payloads. Result screen
navigation and ranked upload do not need the same payload shape.

Recommended fix:

- Add `recordPlayEvents` to launch settings or a dedicated telemetry config.
- Default it to false unless the session needs ranked upload, anti-cheat replay,
  or debugging.
- Consider splitting:
  - lightweight `game.play.result`
  - optional `game.play.telemetry`

### P1-5: Mod Handling Is Too Stringly Typed

Relevant files:

- `engines/unity/flutter_plugin/lib/src/models/game_mod.dart`
- `engines/unity/flutter_plugin/lib/src/models/game_launch_payload.dart`
- `engines/unity/Assets/Scripts/Game/ExternalGameContentProvider.cs`

Dart exposes `GameMod.wireName`, but `GameLaunchPayload.mods` is still
`List<String>`. Unity parses with case-insensitive `Enum.TryParse` and silently
ignores invalid values.

Why this will scale badly:

Mod choice affects score validity, gameplay behavior, UI display, and upload
eligibility. Silent ignore behavior can make the Flutter UI show one state while
Unity plays another.

Recommended fix:

- Make `mods` a typed `List<GameMod>` in Dart.
- Add `GameMod.fromWireName`.
- Validate conflicts and mutually exclusive choices in the plugin:
  - `Fast` vs `Slow`
  - `FlipAll` vs `FlipX`/`FlipY` semantics
  - `Auto`/`AutoDrag`/`AutoHold`/`AutoFlick`
  - ranked-incompatible mods
- Make Unity reject invalid mod strings rather than ignore them.

### P1-6: Note Type and Quality Settings Are Not Public Plugin Types

Relevant files:

- `engines/unity/flutter_plugin/lib/src/models/game_launch_settings.dart`
- `engines/unity/flutter_plugin/example/lib/src/models/note_type_wire.dart`
- `engines/unity/flutter_plugin/example/lib/src/models/example_settings.dart`
- `engines/unity/Assets/Scripts/Game/GameLaunchPayload.cs`

`holdHitSoundTiming`, `graphicsQuality`, `hitSound`, and note type maps are
plain strings/maps in the public plugin API. Some typed helpers exist only in
the example app.

Why this will scale badly:

The formal settings UI will need these exact mappings. Keeping them in the
example app encourages the production app to duplicate protocol constants.

Recommended fix:

- Move `NoteTypeWire`, `HoldHitSoundTiming`, and `GraphicsQuality` into the
  plugin public API.
- Prefer typed fields over string fields in `GameLaunchSettings`.
- Provide builders/helpers for full note type color and hitbox maps.
- Validate all note type keys `0` through `7`.

### P1-7: Unity Settings Missing From Plugin Surface

Relevant files:

- `engines/unity/Assets/Scripts/Player/LocalPlayerSettings.cs`
- `engines/unity/Assets/Scripts/Context.cs`
- `engines/unity/Assets/Scripts/Game/GameLaunchPayload.cs`
- `engines/unity/flutter_plugin/lib/src/models/game_launch_settings.dart`

Unity has settings that affect gameplay or bridge-embedded presentation but are
not exposed through the Flutter plugin.

Important gaps:

- `Language`
- `PlayRanked`
- `MenuTapticFeedback`
- `EnabledMods` semantics, although launch mods mostly override this
- telemetry/play event recording
- lifecycle/runtime mode policy

Why this will scale badly:

Flutter owns user profile, language, ranked mode, settings UI, and platform UX.
If Unity derives some of these from defaults or system language, the app can end
up with inconsistent UI and gameplay behavior.

Recommended fix:

- Add `language` to the launch or runtime settings protocol. Prefer BCP-47
  strings or a shared enum with clear mapping.
- Add explicit ranked/session intent.
- Add clear separation:
  - profile settings
  - launch settings
  - runtime settings
  - debug settings

### P1-8: Android Send Failures Are Logged but Not Reported to Dart

Relevant file:

- `engines/unity/flutter_plugin/android/src/main/kotlin/org/cytoid/gamecore/CytoidGameCoreBridge.kt`

`sendToUnity()` catches `UnitySendMessage` failures and logs them, but Dart sees
the `send()` call as successful.

Why this will scale badly:

If Unity is not loaded, the GameObject name changes, ProGuard breaks a class, or
the runtime is in a bad state, Flutter will wait for a response that cannot
arrive.

Recommended fix:

- Make send failures visible to Dart:
  - throw `PlatformException`; or
  - emit a structured bridge error event.
- Pair this with explicit ready gating.

### P1-9: iOS `ensureRuntimeStarted()` Does Not Mean Unity Is Ready

Relevant files:

- `engines/unity/flutter_plugin/ios/cytoid_game_core/Sources/cytoid_game_core/CytoidGameCoreBridge.swift`
- `engines/unity/flutter_plugin/ios/cytoid_game_core/Sources/cytoid_game_core/UnityGameCoreRuntime.swift`

`ensureRuntimeStarted()` schedules Unity loading on the main queue and returns.
Messages can be buffered before Unity is embedded, but the host still lacks a
clear ready contract.

Recommended fix:

- Make `ensureRuntimeStarted()` complete when a meaningful native state is
  reached, or keep it as a fire-and-forget method and add `waitForReady()`.
- Ensure `showGameSurface()` either starts and presents Unity atomically or
  returns a structured failure.

### P1-10: Artifact and Plugin Versions Are Not Coupled

Relevant files:

- `engines/unity/flutter_plugin/pubspec.yaml`
- `engines/unity/flutter_plugin/CHANGELOG.md`
- `engines/unity/flutter_plugin/tool/setup_unity_artifacts.sh`
- `engines/unity/flutter_plugin/tool/package_flutter_plugin.sh`

The plugin version is `0.0.1`, the changelog is a placeholder, and artifacts are
downloaded by an external version variable. The packaged Flutter plugin does not
contain Unity binaries.

Why this will scale badly:

The formal Flutter client may pin one plugin version but load a mismatched Unity
artifact version. Debugging protocol mismatches without a manifest will be
painful.

Recommended fix:

- Add an artifact manifest containing:
  - plugin version
  - Unity project commit SHA
  - Unity editor version
  - Android/iOS artifact IDs
  - build date
  - protocol schema version if kept
- Update `CHANGELOG.md` before the client starts consuming the plugin.
- Define a compatibility matrix or a single release process that publishes both
  plugin and artifacts together.

## Overdesign and Simplification Opportunities

### O1: Too Many Health and Status Channels

Current mechanisms include:

- native `queryRuntimeStatus`
- envelope `bridge.status` / `game.status`
- `bridge.ping` / `game.pong`
- repeated `game.ready`
- native `activePlayId`
- Unity `GamePlayState`

This is more surface area than the current product needs.

Recommended simplification:

- Pick one authoritative state source.
- Prefer Unity acknowledgement for gameplay readiness/busy state.
- Use native status only as a cached transport/runtime snapshot, or remove the
  envelope status route.
- Avoid `ping` causing another `game.ready`.

### O2: Two Terminal Message Types

`game.play.result` and `game.play.ended` should not both carry gameplay terminal
meaning.

Recommended simplification:

- Keep one typed outcome message.
- Treat route/surface detach as transport lifecycle, not gameplay outcome.

### O3: Double Handoff UI

Relevant files:

- `engines/unity/Assets/Scripts/Host/GameBridge.cs`
- `engines/unity/flutter_plugin/example/lib/src/screens/game_session_screen.dart`

Both Flutter and Unity have handoff/black overlay behavior.

Why this is risky:

Startup failures and result paths can show or hide overlays in different orders.
The user-visible symptom is a black screen.

Recommended simplification:

- In bridge-embedded mode, let Flutter own handoff/loading UI.
- Unity should focus on scene loading and gameplay.
- If Unity overlay remains, every error/result/cancel path must hide it
  explicitly.

### O4: Manual Cross-Language Model Sync

Launch payloads, result payloads, settings, enums, and protocol docs are all
manually synchronized.

Recommended simplification:

- Add a single source of truth:
  - JSON Schema
  - generated Dart/C# models
  - protobuf-like schema
  - or a minimal codegen script dedicated to this monorepo
- At minimum, add golden fixtures tested by Dart and C#.

### O5: Protocol Version Field Without Real Compatibility Strategy

`v` exists in envelopes, Unity rejects unsupported versions, Dart parses but
does not enforce the current version. Since compatibility is not required yet,
the field adds mental overhead without much benefit.

Recommended simplification:

- Either remove `v` for now and rely on monorepo lockstep changes.
- Or keep it but make all sides enforce it consistently and document it as a
  schema generation marker, not a compatibility mechanism.

## Other Issues That Can Grow Later

### M1: VFS Materializer Cache Does Not Refresh

Relevant file:

- `engines/unity/flutter_plugin/example/lib/src/services/level_vfs_materializer.dart`

The example materializer copies level assets into a temp directory only if the
directory does not already exist.

Risk:

The formal client will download, update, unpack, and cache levels. A stale VFS
directory can cause users to play an old chart or old audio after an update.

Recommended fix:

- Move VFS validation/materialization into a shared plugin/helper package.
- Include version, content hash, or etag in the cache key.
- Validate and canonicalize paths on the Dart side before launch.

### M2: Mock Runtime Does Not Match the Expected Mock Engine Role

Relevant files:

- Android and iOS mock bridge implementations
- Flutter plugin example app
- `Package.swift`
- Android Gradle artifact detection
- `docs/mock-engine.md`

Expected behavior:

The mock engine should serve two product and engineering workflows:

1. Automated tests should be able to simulate Unity responses deterministically.
   Tests need predictable ready events, health checks, settings acknowledgements,
   results, cancellations, failures, tier outcomes, calibration outcomes, logs,
   and engine-loss scenarios.
2. Non-Unity UI development should have a placeholder runtime, especially for
   environments such as Flutter Web or desktop UI work where using a mobile
   simulator/device and Unity artifacts is expensive or unavailable.

Current behavior:

The current mock is Android/iOS native glue, not a general mock engine. It is a
small protocol fake used when Unity artifacts are missing. It helps the example
compile and exercise basic channels, but it does not yet provide a scenario
model or a pure Dart implementation for Web/UI development.

Current gaps:

- no real scene loading
- no VFS validation
- no real scoring
- no real platform lifecycle
- different timing between Android and iOS
- iOS Simulator always uses mock because only a device slice exists
- Flutter Web cannot use the current native mock implementation
- Standard play returns a fixed failure instead of configurable completed,
  failed, cancelled, rejected, and engine-lost scenarios
- settings updates always report `applied: true`
- mock-only success can be confused with Unity integration success

Why this will scale badly:

If the mock remains a hidden native fallback, automated tests cannot reliably
cover the Flutter session state machine, and UI development on Web/desktop still
needs a separate fake. Developers may also misread iOS Simulator or no-artifact
success as proof that Unity startup, VFS, scene loading, callbacks, and lifecycle
are working.

Recommended fix:

- Treat the mock as a first-class runtime behind the same public Flutter-facing
  client abstraction as Unity.
- Add a pure Dart `MockGameCoreClient` for widget tests, Flutter Web, and desktop
  UI development.
- Make mock behavior scenario-driven instead of hard-coded in Kotlin/Swift.
  Required scenarios should include standard completed, standard failed,
  cancelled, rejected invalid payload, tier completed, tier retry, calibration
  completed, engine lost, and settings partially rejected.
- Keep Android/iOS native mocks only as platform fallback shims, and make them
  mirror the same canonical scenarios where practical.
- Expose engine mode and active mock scenario prominently in debug builds.
- Make CI distinguish mock-only tests from Unity integration tests.
- Document that iOS Simulator is mock-only unless a simulator Unity slice is
  produced.
- Use `docs/mock-engine.md` as the behavior contract for the expected mock.

### M3: Android Artifact Availability Is Decided at Gradle Configuration Time

Relevant file:

- `engines/unity/flutter_plugin/android/build.gradle.kts`

If the Unity AAR is missing at Gradle configuration time, the build config and
dependencies are set for mock mode. Installing artifacts afterward may require a
clean rebuild.

Recommended fix:

- Document `flutter clean` after artifact installation.
- Add a verification script.
- Consider failing fast when a Unity build is requested but artifacts are absent.

### M4: iOS Device-Only Unity Artifact

Relevant files:

- `engines/unity/flutter_plugin/ios/cytoid_game_core/Package.swift`
- `engines/unity/flutter_plugin/tool/build_unity_ios_framework.sh`

iOS Simulator builds silently use mock even if artifacts exist.

Recommended fix:

- Put this in onboarding and debug UI.
- Long term, add simulator slices or define simulator mock as an explicit product
  constraint.

### M5: Hard-Coded Android Class Names and Legacy Package Names

Relevant files:

- Android bridge Kotlin files
- Unity Android manifest/plugin files
- Unity Java/Kotlin callback paths

The project mixes app IDs, plugin namespace, and legacy Unity Activity names.

Recommended fix:

- Centralize these constants.
- Use Gradle manifest placeholders or generated config where possible.
- Add a release checklist item that validates the Unity Activity can be resolved
  from the formal Flutter app.

### M6: Tooling and Documentation Drift

Examples:

- `setup_unity_artifacts.sh` still attempts to download `lunar-console.aar`
  although Lunar Console has been removed.
- Host protocol docs mention paths that no longer match the Dart layout.
- Docs describe platform view-like architecture while Android uses an exclusive
  Unity Activity and iOS uses an exclusive Unity window.
- `CHANGELOG.md` is still a placeholder.

Recommended fix:

- Clean up artifact scripts before onboarding more client work.
- Add a documentation sync check for protocol docs if there are duplicated docs
  across repositories.
- Update README/onboarding to reflect true Android/iOS runtime presentation.

## Missing Flutter Plugin Surface

The following configuration or helper APIs should be exposed before production
Flutter screens are built on top of the plugin.

### Runtime and Session

- `waitForReady()`
- `PlaySession` or `runPlaySession()`
- pure Dart `MockGameCoreClient` for automated tests, Flutter Web, and desktop
  UI placeholder development
- explicit runtime mode:
  - `mock`
  - `unity`
  - `unavailable`
  - `failed`
- explicit lifecycle preference:
  - warm
  - teardown
  - platform default

### Launch and Gameplay

- typed `LevelMeta`
- typed `GameMod` list
- mod validation and conflict resolution
- typed `GameMode`
- selected difficulty validation
- `recordPlayEvents`
- ranked/session intent

### Settings

- language
- hit sound enum or validated string
- `HoldHitSoundTiming`
- `GraphicsQuality`
- full note type constants
- full note color/hitbox builder
- haptic settings split by gameplay/menu
- explicit profile/runtime/launch settings boundaries

### VFS and Assets

- path canonicalization
- VFS root validation
- required file validation
- cache key/version helpers
- materializer that supports updates and cache invalidation

### Results and Telemetry

- typed play outcome
- structured errors:
  - `code`
  - `message`
  - `details`
- optional telemetry event transfer
- public play event codec or explicit encode/decode helpers

## Recommended Work Packages

### Work Package 1: Terminal Outcome Protocol

Goal:

Every session has exactly one terminal gameplay outcome.

Tasks:

- Replace `completed`/`failed`/`tierRetry` field overlap with a typed outcome.
- Make Unity emit results for all bridge-embedded exits.
- Stop using `game.play.ended` for gameplay meaning.
- Update Dart `startPlay()` to return typed outcomes.
- Update mock runtimes to match Unity semantics.
- Update host protocol docs.

### Work Package 2: Runtime Readiness and Lifecycle

Goal:

Flutter can reliably know whether Unity is unavailable, starting, ready, busy,
failed, or intentionally kept warm.

Tasks:

- Remove artifact-presence-as-starting behavior.
- Make readiness come from Unity ack.
- Surface send failures to Dart.
- Add `waitForReady()`.
- Decide Android lifecycle policy and document it.
- Align iOS behavior as much as possible.

### Work Package 3: Plugin API Surface

Goal:

The formal Flutter client should not import `src/` or copy example constants.

Tasks:

- Expose typed settings enums.
- Change `mods` to typed `GameMod`.
- Add language and ranked/session intent.
- Add telemetry recording switch.
- Add VFS validation helpers.
- Add public result/telemetry codec helpers if needed.

### Work Package 4: Protocol Slimming

Goal:

Remove duplicated channels before the formal client depends on them.

Tasks:

- Decide one health/status route.
- Remove repeated ready behavior from ping.
- Convert `levelMetaJson` to typed object.
- Consider removing envelope `v`, or enforce it consistently.
- Split profile, launch, runtime, and debug settings.

### Work Package 5: Artifact and CI Release Gate

Goal:

Artifact builds are traceable and prove host integration.

Tasks:

- Add artifact manifest.
- Couple plugin version and artifact version.
- Fill `CHANGELOG.md`.
- Add Android Flutter host build/smoke.
- Add iOS SPM/link/build check when artifacts are produced.
- Add mock/unity mode visibility to example/debug UI.

### Work Package 6: First-Class Mock Engine

Goal:

The mock engine supports deterministic automated tests and non-Unity UI
development without being confused with real Unity integration.

Tasks:

- Implement a pure Dart `MockGameCoreClient` behind the same Flutter-facing
  interface as the native Unity client.
- Define serializable scenario fixtures for completed, failed, cancelled,
  rejected, tier retry, calibration, engine-lost, and partial-settings-rejection
  cases.
- Update Android and iOS native mocks to mirror the same canonical scenarios
  where practical.
- Make automation scenarios deterministic by default, with configurable delays,
  timestamps, logs, health failures, and result payloads.
- Add a placeholder profile for Web/desktop UI development with visible synthetic
  results and optional loading delays.
- Display `engineMode = mock` and the active scenario in debug UI.
- Keep mock-only CI separate from Unity artifact smoke tests.

## Suggested Launch Gate Checklist

Before the formal Flutter client starts building production flows on this
plugin, the following should be true:

- Every Unity bridge session returns a typed terminal outcome.
- Failed, cancelled, retried, completed, calibration, and tier outcomes are all
  represented without exceptions-as-control-flow.
- `ensureRuntimeStarted()` and/or `waitForReady()` has a clear contract.
- Android lifecycle strategy is explicit and tested over repeated sessions.
- iOS Simulator mock behavior is documented and visible.
- `GameLaunchPayload` no longer contains `levelMetaJson` as a string.
- Public plugin API exposes all settings the Flutter client owns.
- Mod and note type mappings are typed and validated.
- Play event recording is opt-in.
- CI builds a Flutter host with the produced Android artifact.
- Artifact/version manifest exists.
- `CHANGELOG.md` has a real initial version entry.
- Host protocol docs match actual file paths and runtime architecture.

## Quick Validation Plan

After the highest-priority fixes, run these checks:

1. Run Flutter plugin unit tests.
2. Run Dart protocol/model golden tests.
3. Build Android Unity artifacts.
4. Install artifacts into the plugin.
5. Run the Flutter example on Android real device.
6. Verify `getEngineMode()` reports `unity`.
7. Verify `waitForReady()` completes only after Unity `game.ready`.
8. Launch a tiny standard chart and receive a completed result.
9. Launch a failing chart or force-fail path and receive a failed result.
10. Cancel a session and receive a cancelled result.
11. Run tier retry and tier completion paths.
12. Verify repeated play sessions do not accumulate Activity/back-stack issues.
13. Verify settings update failures are reported.
14. Verify invalid mods and invalid note type keys fail fast.
15. Run iOS Simulator and confirm mock mode is visible.
16. Run iOS real device when artifacts are available.
17. Verify artifact manifest matches plugin version and commit SHA.

## Residual Risk

This review was static. Some lifecycle and memory issues can only be proven on
real devices with repeated sessions and real Unity artifacts.

The most important next step is not to patch around individual symptoms. The
project should first simplify the protocol and session semantics while the
Flutter client is still early enough to absorb breaking changes cheaply.
