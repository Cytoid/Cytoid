# Real-Device Smoke Checklist (F3)

This is the human-gated real-device exercise that follows a successful
`flutter-smoke` CI run. CI proves only that the example app links the AAR and
produces an APK; the contracts below can only be observed on actual hardware.

The host protocol is documented in `docs/host-protocol-v2.md`. Each section
below names the implementation task that introduced the contract so the tester
can cross-reference the expected envelope shape.

## Prerequisites

- Recent `flutter-smoke` CI run green on the commit under test.
- Real Android device (API 24+, per `engines/unity/flutter_plugin/android/build.gradle.kts:47`)
  and/or real iOS device (simulator builds are device-only per the plugin README).
- Example app installed from the same artifact revision that CI built.
- `adb logcat -s Unity ActivityManager` (Android) or Console.app (iOS) attached
  for envelope / lifecycle observation.

## T4 — Runtime failure synthesis primitive

Verify the active-session routing rule: a synthesized runtime failure MUST
arrive as `session.failed`, NEVER as `engine.error`, when a session is active.

- [ ] Force the engine into a `generationChange` recovery mid-session on
      Android (e.g., trigger Unity Activity recreation while a play session is
      active). Confirm the host receives exactly one `session.failed` envelope
      with `error.code = "runtime_recreated"`.
- [ ] Verify no `engine.error` envelope is emitted for the same session —
      the active-session routing rule forbids both.
- [ ] iOS analog: trigger `unityDidUnload` while a session is active. Confirm
      `error.code = "runtime_surface_lost"` arrives via `session.failed` only.

Reference: `.omo/evidence/task-4-v2-host-impl-failure.json` is the contract
fixture for the synthesized envelope shape.

## T5 — Android native send-failure routing

Verify that native-side send failures (Android `sendToUnity` /
`returnToFlutterActivity`) are routed per the v2 § Active-Session Runtime
Failure contract, with the sanitized `error.message` form.

- [ ] With NO active session, force a `sendToUnity` failure (e.g., uninstall
      the Unity bridge method). Confirm the host receives `engine.error` with
      `error.code = "runtime_exception"`, message of the form
      `"<ExceptionClassSimpleName>: <first message line>"`, and NO
      `details.stackTrace` field.
- [ ] With an active session (`activeSessionId != null`), force the same
      failure. Confirm the host receives `session.failed` ONLY
      (`error.code = "runtime_unreachable"`),
      and receives NO `engine.error` envelope.
- [ ] Repeat both cases for `returnToFlutterActivity` failure. Same routing.

Reference: `.omo/evidence/task-5-v2-host-impl-failure.json` is the contract
fixture for the active-session send-failure envelope.

## T6 — iOS framework-load failure

Verify that iOS framework-load failure at startup emits `engine.error`
(pre-session routing — NEVER `session.failed`) with the typed `error.code`
and `details.frameworkPath`.

- [ ] Force `bundleOpenFailed(path:)` by shipping a build with a corrupt or
      missing `UnityFramework.xcframework`. Confirm the host observes
      `engine.error` with `error.code = "runtime_unavailable"`,
      `error.details.frameworkPath` populated, and `state = failed` in the
      next `queryRuntime` snapshot.
- [ ] Confirm NO `session.failed` envelope is synthesized — pre-session
      failures use `engine.error` exclusively.
- [ ] Verify `showGameSurface` short-circuits with the same `error.code`
      (`runtime_unavailable`) when called after the failure, and does NOT
      attempt to present the Unity window.

Reference: `.omo/evidence/task-6-v2-host-impl-failure.json` is the contract
fixture for the framework-load-failure envelope.

## T9 — Activity lifecycle + 10-session memory regression

Verify the warm-resident Unity Activity policy holds across arbitrary session
cycles and that the `unityActivityInstanceCount` counter never exceeds 1.

- [ ] Run 10 sequential session cycles: select level → play → result → select
      next. Confirm via logcat that `unityActivityInstanceCount` stays at 0 or
      1 throughout. A value > 1 indicates Activity accumulation (memory leak)
      and is a release blocker.
- [ ] Background the app mid-session (Home key). Confirm `runtimeState.onSuspend()`
      fires (READY|BUSY → SUSPENDED) and the single-slot prior state is
      preserved.
- [ ] Resume the app. Confirm `runtimeState.onResume()` restores the prior
      state and the session continues (or receives `session.failed` if the
      engine reclaimed the surface — see T4).
- [ ] Force-stop the Unity Activity while a session is active
      (`adb shell am force-stop me.tigerhix.cytoid` on the Unity process).
      Confirm `synthesizeRuntimeFailure(SURFACE_LOST, sessionId)` fires
      (`error.code = "runtime_surface_lost"`) and the runtime transitions to
      FAILED.
- [ ] Force-stop the Unity Activity with NO active session. Confirm the
      runtime transitions to UNAVAILABLE (caller must `startRuntime()` again),
      NOT to FAILED — no phantom `session.failed` should be synthesized.

Reference: `.omo/evidence/task-9-v2-host-impl-failure.json` is the contract
fixture for the SURFACE_LOST envelope.

## Refresh-rate restoration (T9 scope guard)

This was explicitly moved out of automated scope (F3 owns it). Verify:

- [ ] During active gameplay, the runtime applies an exclusive display
      refresh rate (device-dependent).
- [ ] After `hideGameSurface()` returns the user to the Flutter Activity, the
      system default refresh rate is restored within ~1500ms. If the device
      stays at the elevated rate indefinitely, file a regression bug.

## Backgrounded-session resume (T9 + T7)

The interaction between T9's SUSPENDED state and T7's `PlaySession.run()` /
`waitForReady` primitives:

- [ ] Background the app while `PlaySession.run()` is awaiting
      `session.result`. Confirm the continuation is preserved across the
      suspend/resume (no premature `CytoidGameCoreTimeoutException`).
- [ ] If the engine reclaims the surface during background, confirm the host
      observes `session.failed` with `error.code = "runtime_surface_lost"`
      (via T9's SURFACE_LOST trigger) rather than hanging until the
      `waitForReady` timeout.

## C# Wave v2 Contracts

The following sections verify the C# engine-side v2 contracts introduced in
the migration plan. Each test is performed on both Android and iOS real devices.

### T-engine.ready — startup payload shape and generation

Verify the engine emits a properly shaped `engine.ready` envelope after startup
and that the generation counter increments across engine recreations.

- [ ] After cold app launch, verify the host receives `engine.ready` with:
      - `schema: "cytoid.game-core.v2"`
      - `payload.engine: "unity"`
      - `payload.engineVersion: "6000.0.75f1"` (or current Unity version)
      - `payload.generation: 1` (int, starts at 1)
      - Optional `payload.display` object with `targetFrameRate` and
        `screenRefreshRate` (may be omitted on some devices)
- [ ] Force-quit the Unity Activity/process mid-session and relaunch. Verify
      the next `engine.ready` carries `generation: 2` (incremented).
- [ ] Repeat the recreation cycle once more. Verify `generation: 3`.
- [ ] Confirm no `engine.ready` emits twice without a failure/recreation in
      between (generation is idempotent within a single runtime lifecycle).

Reference: `docs/host-protocol-v2.md:231-260`.

### T-session.started — session acknowledgment ordering

Verify `session.started` is emitted after successful `session.start` validation,
before any `session.result`, and carries the correct echo payload.

- [ ] Send a valid `session.start` with `mode: "ranked"` and a complete level.
      Confirm the host receives:
      - `type: "session.started"` with the same `id` as the request
      - `payload.sessionId` matching the envelope `id`
      - `payload.mode` echoing `"ranked"`
      - `payload.generation` matching the current `engine.ready` generation
- [ ] Verify `session.started` arrives BEFORE any `session.result` or
      `session.telemetry` for that session.
- [ ] Send a malformed `session.start` (missing `level` field). Confirm the
      host receives `session.result` with `outcome.kind = "rejected"` and NO
      `session.started` is emitted (rejection bypasses the ack).
- [ ] Send a `session.start` while another session is active. Confirm the
      response is `session.result` with `outcome.kind = "rejected"`,
      `error.code = "overlapping_session"` and NO `session.started`.

Reference: `docs/host-protocol-v2.md:908-937`.

### T-session.telemetry — opt-in recording and auto-mod suppression

Verify telemetry is emitted only when requested and that auto-class mods
suppress it completely.

- [ ] Launch a session with `options.recordPlayEvents: true` and NO auto-class
      mods. Confirm:
      - `type: "session.telemetry"` arrives BEFORE `session.result`
      - `payload.playEvents.format: "json.v1"`
      - `payload.playEvents.events` is a non-empty array of touch events
      - Each event has short-name fields: `t` (int, ms), `f` (int, finger),
        `p` (string: "down"/"move"/"up"), `x` (int, 0-65535), `y` (int, 0-65535)
- [ ] Verify `session.result` telemetry summary carries:
      - `telemetry.available: true`
      - `telemetry.eventsRecorded` > 0
      - `telemetry.bytes` > 0 (approximate uncompressed size)
- [ ] Launch a session with `options.recordPlayEvents: true` AND an auto-class
      mod (e.g., `mods: ["auto"]`). Confirm:
      - NO `session.telemetry` envelope arrives
      - `session.result.telemetry.available: false`
      - `session.result.telemetry.eventsRecorded: 0`
      - `session.result.telemetry.bytes: 0`
      - `session.result.flags.usedAutoMod: true`
- [ ] Launch a session with `options.recordPlayEvents: false`. Confirm:
      - NO `session.telemetry` envelope arrives
      - `session.result.telemetry.available: false`

Reference: `docs/host-protocol-v2.md:974-1014, 866-881`.

### T-session.result-outcomes — all outcome kinds

Verify each outcome kind produces the correct `session.result` shape.

#### completed — normal finish

- [ ] Play a level normally and let it finish. Verify:
      - `outcome.kind: "completed"`
      - `level` object present: `id`, `title`, `difficulty`, `difficultyLevel`
      - `score` object present with full breakdown
      - `flags.usedAutoMod: false` (no auto-mod active)
      - `mode` and `mods` echo the launch request
      - `timestamp` is a Unix epoch millisecond integer

#### failed/hpDepleted — HP depletion

- [ ] Drain HP to zero during play (easy chart, miss repeatedly). Verify:
      - `outcome.kind: "failed"`
      - `outcome.reason: "hpDepleted"`
      - `score` object present (partial score data)
      - No `calibration` or `tier` payloads (not a calibration/tier session)

#### cancelled/userBack — host-initiated cancellation

- [ ] Start a session and send `session.cancel` with `reason: "userBack"` mid-play.
      Verify:
      - `outcome.kind: "cancelled"`
      - `outcome.reason: "userBack"` (echoes the cancel request)
      - No `score` object (cancelled mid-game, no meaningful completion data)
      - `sessionId` matches the cancelled session

#### tierRetry — engine-side retry (if tier mode is reachable)

- [ ] If tier mode is reachable from the example app, start a tier session and
      trigger an engine-side retry (e.g., in-engine retry affordance). Verify:
      - `outcome.kind: "tierRetry"`
      - `outcome.tierId` present (from the tier launch payload)
      - `outcome.stageIndex` present (stage that triggered retry)
      - `tier` object still present with partial stage state:
        `tierId`, `stageIndex`, `stageCount`, `health`, `maxHealth`, `combo`

#### calibration — calibration mode completion

- [ ] Run a calibration session. Verify:
      - `outcome.kind: "calibration"`
      - `calibration` object present with optional:
        `baseNoteOffset` (global calibrated offset)
        `levelNoteOffset` (level-specific calibrated offset)
      - No `score` object (calibration does not produce scoring data)

#### rejected — malformed session.start

- [ ] Send a malformed `session.start` (e.g., missing `level` field). Verify:
      - `outcome.kind: "rejected"`
      - `error` object present:
        `error.code` (e.g., `"invalid_payload"`)
        `error.message` (human-readable diagnostic)
        Optional `error.details` with structured debug info
      - NO `session.started` was emitted (rejection bypasses ack)
      - `sessionId` matches the rejected request id

Reference: `docs/host-protocol-v2.md:1054-1233`.

### T-settings.applied — realtime vs deferred field classification

Verify that `settings.apply` correctly classifies fields as applied, deferred, or
rejected based on realtime-safety rules and session state.

- [ ] During active play, send `settings.apply` with a realtime-safe field:
      `runtime.musicVolume: 0.5`. Verify:
      - `settings.applied` envelope arrives
      - `payload.applied: true`
      - `payload.appliedFields` contains `"runtime.musicVolume"`
      - `payload.deferredFields` is empty (or does not contain the realtime field)
      - `payload.rejectedFields` is empty
- [ ] During active play, send `settings.apply` with a non-realtime field:
      `visual.noteSize: 1`. Verify:
      - `payload.appliedFields` does NOT contain `"visual.noteSize"`
      - `payload.deferredFields` contains `"visual.noteSize"` (deferred to next
        `session.start`)
- [ ] During active play, send `settings.apply` with both a realtime field
      (`runtime.musicVolume`) and a non-realtime field (`visual.noteSize`).
      Verify:
      - `payload.appliedFields` contains `"runtime.musicVolume"`
      - `payload.deferredFields` contains `"visual.noteSize"`
- [ ] While idle (no active session), send `settings.apply` with
      `visual.noteSize: 1`. Verify:
      - `payload.appliedFields` contains `"visual.noteSize"` (applied immediately)
      - `payload.deferredFields` is empty

Reference: `docs/host-protocol-v2.md:386-417, 740-750`.

## Sign-off

Record the following for each release candidate:

- Commit SHA tested: ____________________
- `flutter-smoke` CI run URL: ____________________
- Device model(s) + OS version: ____________________
- ENGINE_MODE observed at install time (must match `unity` for a release
  candidate): ____________________
- Any deviations from the checklist above: ____________________

A release candidate MUST NOT ship with any unchecked box above, except where
an explicit "out of scope" note is recorded with reviewer sign-off.
