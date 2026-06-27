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
arrive as `session.result` with `outcome.kind = "runtimeFailed"`, NEVER as
`engine.error`, when a session is active.

- [ ] Force the engine into a `generationChange` recovery mid-session on
      Android (e.g., trigger Unity Activity recreation while a play session is
      active). Confirm the host receives exactly one `session.result` envelope
      with `outcome.kind = "runtimeFailed"` and `error.code = "runtime_recreated"`.
- [ ] Verify no `engine.error` envelope is emitted for the same session —
      the active-session routing rule forbids both.
- [ ] iOS analog: trigger `unityDidUnload` while a session is active. Confirm
      `error.code = "runtime_surface_lost"` arrives via `session.result` only.

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
      failure. Confirm the host receives `session.result` ONLY
      (`error.code = "runtime_unreachable"`, `outcome.kind = "runtimeFailed"`),
      and receives NO `engine.error` envelope.
- [ ] Repeat both cases for `returnToFlutterActivity` failure. Same routing.

Reference: `.omo/evidence/task-5-v2-host-impl-failure.json` is the contract
fixture for the active-session send-failure envelope.

## T6 — iOS framework-load failure

Verify that iOS framework-load failure at startup emits `engine.error`
(pre-session routing — NEVER `session.result`) with the typed `error.code`
and `details.frameworkPath`.

- [ ] Force `bundleOpenFailed(path:)` by shipping a build with a corrupt or
      missing `UnityFramework.xcframework`. Confirm the host observes
      `engine.error` with `error.code = "runtime_unavailable"`,
      `error.details.frameworkPath` populated, and `state = failed` in the
      next `queryRuntime` snapshot.
- [ ] Confirm NO `session.result` envelope is synthesized — pre-session
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
      state and the session continues (or receives `session.result` if the
      engine reclaimed the surface — see T4).
- [ ] Force-stop the Unity Activity while a session is active
      (`adb shell am force-stop me.tigerhix.cytoid` on the Unity process).
      Confirm `synthesizeRuntimeFailure(SURFACE_LOST, sessionId)` fires
      (`error.code = "runtime_surface_lost"`) and the runtime transitions to
      FAILED.
- [ ] Force-stop the Unity Activity with NO active session. Confirm the
      runtime transitions to UNAVAILABLE (caller must `startRuntime()` again),
      NOT to FAILED — no phantom `session.result` should be synthesized.

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
      observes `session.result` with `outcome.kind = "runtimeFailed"`
      (via T9's SURFACE_LOST trigger) rather than hanging until the
      `waitForReady` timeout.

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
