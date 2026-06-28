# learnings — unity-core-v2-migration

Cumulative memory for stateless subagents working on this plan.

## 2026-06-28 T0 — orchestration start
Atlas session opencode:ses_0f32b302affeTDCXz6xb574DV0 began executing plan.

## 2026-06-28 T3 — existing telemetry event shape
`GamePlayEvent` already uses v2 short wire fields (`t`, `f`, `p`, `x`, `y`), so `GamePlayEventRecorder.SnapshotAsWireObjects()` can wrap `Snapshot()` without remapping.

## 2026-06-28 T5 — pending settings and full session.start snapshots
The v2 `session.start.settings` full-snapshot requirement means router pending settings should not synthesize a missing `settings` object anymore; `settings.apply` can still store a flat patch for local application, but `session.start` validation remains owned by `ExternalGameContentProvider.FlattenLaunchSettings(..., requireFullSnapshot: true)`.

## 2026-06-28 Wave 1 complete → Wave 2 propagation
- C# envelope uses `schema: "cytoid.game-core.v2"` (string fail-fast). Native bridges must match (Metis H3 + Codex #14: add schema validation at native bridge entry points).
- All C# emission of `session.result` / `session.telemetry` flows through `GameBridge.OnResultJson` / `GameBridge.OnTelemetryJson` events (single-owner rule). Native side continues to consume these as inbound Unity messages via onUnityMessage — no bridge-side change needed for THAT path.
- Wave 1 emits v2 only. Native v1 fallback helpers in Kotlin/Swift (`isGameResultMessage`, `isHostReadyMessage`, `isGameStartMessage`, `isSessionEndMessageV1`) are now dead code that must be REMOVED in Wave 2 (Metis H3: REPLACE v1 detection with v2 in emit()/onUnityMessage BEFORE deleting).
- C# `engine.ready` now carries `generation` (int starting at 1, bumped on every SendReadyToBridge). Native `RuntimeStateMachine.onEngineReady` already bumps generation — ensure native still recognizes the new engine.ready payload shape (just `engine`/`engineVersion`/`generation`/`display?` — no `initialized`/`targetFrameRate`/`screenRefreshRate` fields anymore).
- v1 `game.ready` is no longer emitted by C#. Native `engine.ready || game.ready` branches in iOS CytoidGameCoreBridge.swift line 222 must drop the `game.ready` half.

## 2026-06-28 T7 — Kotlin bridge schema gate
- Android `CytoidGameCoreBridge.emit()` is the single BUSY→READY transition point for forwarded `session.result`; `onUnityMessage` must not also call `runtimeState.onSessionEnded()` for `session.result` after `emit()` or the terminal path is duplicated.
- Android malformed-schema regression tests should use missing or wrong `schema` fixtures rather than preserving `"v":2` literals; this keeps the schema rejection coverage while satisfying the Wave 2 fixture migration gate.

## 2026-06-28 T8 — Swift bridge v2-only entry/emit gates
- `CytoidGameCoreBridge.emitEvent` is the single Swift BUSY→READY owner for inbound `session.result`; `onUnityMessage` no longer performs a second result transition.
- Swift bridge entry points now fail closed on missing or wrong `schema`; this means Wave 3 must update the Swift mock before mock-mode startup can drive `engine.ready` through the outer bridge again.

## 2026-06-28 T9 — Kotlin mock v2 smoke fake
- Android mock mode now emits schema-gated v2 envelopes only: `engine.ready`, `session.started`, `session.result`, `health.ok`, `settings.applied`, and `logs.batch`.
- The Kotlin mock is intentionally a protocol smoke fake: it emits one default result per mode and never emits `session.telemetry`; result telemetry is always unavailable, with `flags.usedAutoMod=true` when any auto-class mod is present.
- Structurally malformed `session.start` payloads are rejected via delayed `session.result(outcome.kind="rejected")` so the outer Android bridge can still expose the READY→BUSY transition immediately after accepting outbound `session.start`.

## 2026-06-28 T10 — Swift mock v2 smoke behavior
- Swift `MockGameCoreBridge` now accepts only v2 `schema` envelopes and emits `engine.ready`, `session.started`, `session.result`, `health.ok`, `settings.applied`, and `logs.batch` with v2 payload shapes. Mock-mode startup satisfies T8's schema gate because `emitHostReady` sends `schema: "cytoid.game-core.v2"` and `type: "engine.ready"`.
- The Swift mock intentionally remains a protocol smoke fake: it never emits `session.telemetry`; all default outcomes, including sessions with auto-class mods, carry `telemetry: {available:false, eventsRecorded:0, bytes:0}`.

## 2026-06-28 Wave 4 — Dart/example v2 migration gotchas
- The example app has local UI enums named `GraphicsQuality` and `HoldHitSoundTiming`; v2 settings exports protocol enums with the same names. `ExampleSettings` now prefixes protocol types with `core.` and maps UI enum values explicitly.
- T12's grep gate rejects the literal `gameMode` in `example_level.dart`; the v1 `GameMode` → v2 `SessionMode` mapping lives in `ExampleMods.toSessionMode()` so the launch builder remains v2-only.
- Running `flutter test example/` from the plugin root exposed a stale example test package import. The test now uses a relative import so both plugin-root and example-root test invocations can resolve `example_level.dart`.

## 2026-06-28 T16 — Smoke checklist extension gotchas
- Smoke doc T4/T5/T6/T9 sections from PR #177 already cover native failure-synthesis contracts; T16 adds C# wave contracts only (engine.ready, session.started, session.telemetry, session.result outcomes, settings.applied).
- Evidence fixtures for T16 are templates (pass=false, verdict fields blank) — user fills them during real-device smoke. Pattern mirrors PR #177's `task-*-v2-host-impl-failure.json` but adds scenario-specific verdict fields.
- iOS Simulator constraint documented in smoke doc's Prerequisites section — simulator runs mock-only because Unity artifact is device-only. Real-device verification required for Unity contracts.
- T-session.telemetry verifies both the emission path AND the auto-mod suppression rule (NO telemetry envelope + summary zeros when auto-mod active, even with recordPlayEvents=true).
- T-session.result-outcomes splits into multiple sub-tests per outcome kind (completed, failed/hpDepleted, cancelled/userBack, tierRetry, calibration, rejected) to ensure envelope shape verification for each distinct terminal case.
