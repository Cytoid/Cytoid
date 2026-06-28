# CytoidGameCore Mock Engine

Status: **behavior contract / design note**.

This document defines what the mock engine is expected to do, where the current
implementation differs, and how it should evolve alongside
`docs/host-protocol-v2.md`.

## Purpose

The mock engine exists for two different use cases:

1. **Automated tests**
   - Simulate Unity responses deterministically.
   - Exercise Flutter session orchestration, routing, settings application,
     error handling, result handling, and log handling without starting Unity.
   - Produce predictable outcomes that can be asserted in unit, widget, and
     integration tests.
2. **Non-Unity UI development**
   - Act as a placeholder runtime while developing Flutter UI in environments
     where Unity is unavailable or undesirable, such as Flutter Web or desktop
     UI work.
   - Reduce simulator/device pressure during ordinary UI development.
   - Let result screens, loading screens, tier flows, settings screens, and
     error states be developed without Unity artifacts.

The mock engine is **not** a gameplay simulator. It should not try to reproduce
Unity rendering, audio timing, chart parsing, hit detection, scoring, or
anti-cheat behavior.

## Current Implementation

Current native mock implementations:

- Android:
  `engines/unity/flutter_plugin/android/src/main/kotlin/org/cytoid/gamecore/MockGameCoreBridge.kt`
- iOS:
  `engines/unity/flutter_plugin/ios/cytoid_game_core/Sources/cytoid_game_core/MockGameCoreBridge.swift`

Current activation:

- Android uses the mock when the Unity AAR is not available at Gradle
  configuration time or Unity player classes cannot be found.
- iOS uses the mock when `UnityFramework.xcframework` is not available. iOS
  Simulator also uses the mock because the current Unity iOS artifact is
  device-only.

Current behavior:

- Emits `game.ready` after a short platform-specific delay.
- Responds to `bridge.status`.
- Echoes `bridge.ping` as `game.pong`.
- Acknowledges `bridge.settings.update` with `applied: true`.
- Responds to `bridge.play.end` with `game.play.ended`.
- Emits synthetic `game.logs.batch`.
- Responds to `bridge.play.start` with `game.play.result`.
- v1 "Standard" sessions (v2 `mode = "ranked"`) currently return a fixed failure:
  `error = "Unity artifact not mounted"`.
- Tier sessions return a synthetic success:
  `finalHealth ~= initialHealth * 0.85`,
  `endingCombo = initialCombo + 50`.

This behavior is useful as a protocol smoke fake, but it is not enough for the
desired automated test and non-Unity UI development workflows.

## Desired Architecture

The mock should be a first-class runtime implementation behind the same public
Flutter-facing interface as the Unity runtime.

Recommended layers:

```text
Flutter app / tests
  -> GameCoreClient interface
     -> NativeUnityGameCoreClient
     -> MockGameCoreClient
        -> Scenario-driven mock runtime
```

The important distinction is that the desired UI-development mock should not
depend on Android or iOS native plugin code. It should be available as a pure
Dart implementation so Flutter Web, widget tests, and desktop UI previews can
use it.

Native Android/iOS mocks can still exist, but they should share scenarios and
fixtures with the pure Dart mock so behavior does not drift.

## Runtime Profiles

The mock should support at least two profiles.

### Automation Profile

For tests.

Expected behavior:

- deterministic by default
- no artificial delay unless configured
- stable timestamps when configured
- stable session ids when injected by tests
- scenario outcomes selected explicitly
- no random score generation unless seeded
- throws or returns structured errors for invalid calls

Example use cases:

- `startPlay()` returns completed result.
- `startPlay()` returns failed result.
- `startPlay()` returns rejected result for invalid launch payload.
- `session.cancel()` returns cancelled result.
- health check fails after N checks to test lost-engine handling.
- settings update returns partial rejection.

### Placeholder Profile

For UI development.

Expected behavior:

- safe to use in Flutter Web or desktop development.
- can add small realistic delays for loading/result transitions.
- can produce plausible but clearly synthetic results.
- can cycle through configured scenarios from a debug menu.
- always exposes engine mode as `mock`.
- must never pretend to validate real chart, VFS, audio, storyboard, or Unity
  lifecycle behavior.

Example use cases:

- develop result screen layout.
- develop tier stage UI.
- develop loading and handoff UI.
- develop calibration result UI.
- develop settings screens and error states.

## Scenario Model

Mock behavior should be scenario-driven instead of hard-coded in platform files.

Suggested scenario shape:

```json
{
  "name": "ranked-completed",
  "engineReadyDelayMs": 0,
  "sessionResultDelayMs": 0,
  "health": {
    "failAfterChecks": null
  },
  "settings": {
    "rejectFields": []
  },
  "result": {
    "mode": "ranked",
    "mods": [],
    "outcome": {
      "kind": "completed"
    },
    "score": {
      "score": 950000,
      "accuracy": 0.97,
      "maxCombo": 500,
      "gradeCounts": {
        "perfect": 480,
        "great": 18,
        "good": 2,
        "bad": 0,
        "miss": 0
      },
      "early": 12,
      "late": 8
    },
    "flags": {
      "usedAutoMod": false
    },
    "telemetry": {
      "available": false,
      "eventsRecorded": 0,
      "bytes": 0
    }
  },
  "logs": []
}
```

Scenarios should be serializable fixtures so Dart tests, native mock tests, and
manual debug tools can use the same inputs.

## Required Scenarios

The mock should ship with a small set of canonical scenarios.

### `rankedCompleted`

Purpose:

- result screen development
- normal routing tests

Expected outcome:

- `mode = "ranked"`
- `outcome.kind = "completed"`
- level summary present
- score present
- no error
- `flags.usedAutoMod = false`
- `telemetry.available` reflects `options.recordPlayEvents`

### `rankedFailed`

Purpose:

- failed result UI
- retry flow tests

Expected outcome:

- `mode = "ranked"`
- `outcome.kind = "failed"`
- score may be present if the real engine supports failed score data
- no runtime error

### `practiceCompleted`

Purpose:

- practice mode result UI (lenient judgment)
- non-leaderboard upload path tests

Expected outcome:

- `mode = "practice"`
- `outcome.kind = "completed"`
- score present

### `cancelled`

Purpose:

- back navigation tests
- route cleanup tests

Expected outcome:

- `cancelled`
- cancellation reason present
- no exception-as-control-flow

### `rejectedInvalidPayload`

Purpose:

- validation UI
- launch failure handling

Expected outcome:

- `rejected`
- structured `invalid_payload` error

### `tierCompleted`

Purpose:

- tier stage result UI
- multi-stage host orchestration tests

Expected outcome:

- `completed`
- tier result present
- health/combo fields present

### `tierRetry`

Purpose:

- retry orchestration tests

Expected outcome:

- `outcome.kind = "tierRetry"` with `tierId` and `stageIndex`
- `tier` result payload present with the partial-stage ending values (health, combo, stageCount), matching the primary spec's rule that `tier` is required for all `mode = "tier"` results regardless of outcome kind

### `calibrationCompleted`

Purpose:

- offset settings UI
- calibration result handling

Expected outcome:

- `calibration`
- calibrated offset fields present

### `engineLost`

Purpose:

- liveness and recovery tests

Expected behavior:

- health checks stop responding or return `failed` after a configured point.
- if a session was active at the failure point, the mock MUST synthesize
  `session.failed` with `error.code` from the `runtime_*` family (typically
  `runtime_unreachable`), matching the primary spec's [Active-Session Runtime Failure](host-protocol-v2.md#active-session-runtime-failure)
  contract.

### `autoModSuppressesRecording`

Purpose:

- anti-cheat path tests
- auto-class mod result handling

Expected outcome:

- `mode = "ranked"` (or any mode) with `mods` containing an auto-class mod
- `outcome.kind = "completed"`
- no `session.telemetry` messages emitted
- `result.telemetry.available = false`
- `result.telemetry.eventsRecorded = 0`
- `result.flags.usedAutoMod = true`

### `settingsPartiallyRejected`

Purpose:

- settings form validation tests

Expected behavior:

- settings acknowledgement includes applied and rejected fields.

## Protocol Behavior

The mock must follow the same protocol as Unity. As of protocol v2, v1 is no
longer supported; mocks must implement v2 semantics only.

Required v2 behavior:

- `engine.ready`
- `health.check` / `health.ok`
- `settings.apply` / `settings.applied`
- `session.start` → `session.started` (accept ack) or `session.result` (reject with `outcome.kind = "rejected"`)
- `session.cancel` → `session.result` with `outcome.kind = "cancelled"`
- active-session runtime failure → `session.failed` (see `engineLost` scenario)
- optional `session.telemetry` (suppressed when auto-class mod is active)
- `logs.batch`

Cancellation must produce `session.result` with `outcome.kind = "cancelled"`
rather than a separate route-ended message. Mode and mods must be echoed in the
result. Auto-class mods must trigger the same recording suppression rule as
Unity (see `docs/host-protocol-v2.md` § Auto-class Recording Rule).

## Determinism Requirements

Automation tests must be able to control:

- ready delay
- result delay
- health-check behavior
- current time or timestamp provider
- score/result payload
- telemetry payload
- settings acknowledgement
- log batches
- error payloads

Default automation behavior should avoid timers where possible. Placeholder UI
behavior may use timers to make loading states visible.

## Visibility Requirements

Any development UI using the mock must make the runtime mode visible.

Recommended debug indicators:

- `Engine: mock`
- active scenario name
- protocol version
- whether the result is synthetic
- whether Unity artifacts are mounted but not used

This prevents developers from interpreting a mock green path as proof that Unity
startup, VFS, scene loading, or native lifecycle is correct.

## Non-Goals

The mock engine should not:

- parse charts
- play audio
- render notes
- simulate input timing
- validate storyboard behavior
- reproduce Unity memory/lifecycle behavior
- generate anti-cheat evidence
- replace real device smoke tests

## Implementation Plan

1. Introduce a Flutter-facing `GameCoreClient` or runtime abstraction that can
   be backed by native Unity or a pure Dart mock.
2. Implement `MockGameCoreClient` in Dart for widget tests, Flutter Web, and
   desktop UI development.
3. Define reusable scenario fixtures.
4. Update Android and iOS native mocks to consume the same scenario model where
   practical, or at least mirror the same canonical outcomes.
5. Add tests that assert mock and Unity-facing Dart models use the same result
   shapes.
6. Add a debug UI indicator for engine mode and scenario name.
7. When protocol v2 lands, update all mock implementations before or alongside
   Unity.

## Risks If Not Fixed

- Developers may build UI against the current fixed-failure v1 Standard mock (v2 `ranked` mode) and
  discover missing result states late.
- Flutter Web UI development will not benefit from the native mock because it is
  Android/iOS-only.
- iOS Simulator success can be mistaken for Unity success.
- Android/iOS mock behavior may drift and produce platform-specific test
  assumptions.
- Mock-only CI can pass while Unity integration is broken.
