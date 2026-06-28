# CytoidGameCore Host Protocol v2

Status: **draft target protocol**. This document describes the next breaking
protocol for communication between the Flutter host and the embedded gameplay
core. It is intentionally not backward compatible with protocol v1.

The v2 design is based on the pre-launch review in `PROBLEMS.md`. Its main goal
is to remove ambiguous session endings, duplicated health/status channels, and
stringly typed launch/settings fields before the formal Flutter client depends
on them.

## Design Principles

- **Lockstep evolution:** Flutter, native glue, Unity, mock runtimes, and docs
  are changed together. There is no requirement to support older protocol
  versions at runtime.
- **One session, one terminal outcome:** every `session.start` must end with
  exactly one of `session.result` or `session.failed`, including failure,
  cancellation, rejection, tier retry, calibration, and runtime death.
- **Readiness is acknowledged by the engine:** artifact presence does not imply
  readiness. The host must wait for `engine.ready` or an equivalent native API
  that is backed by engine acknowledgement.
- **One health path:** long sessions use one `health.check` / `health.ok` path,
  not separate status polling plus ping/pong.
- **Typed payloads over opaque strings:** `levelMeta`, settings, mods, note
  types, outcome kinds, and errors are structured data.
- **Flutter owns user-facing profile state:** language, ranked intent, haptics,
  telemetry recording, and settings UI must be expressible through the plugin.
- **Telemetry is opt-in:** gameplay touch events are not included in every
  result by default.
- **Native lifecycle is transport, not gameplay:** showing or hiding the engine
  surface must not create a fake gameplay outcome.
- **Report-only outcomes:** the protocol layer reports session facts (mode
  echo, mods used, score, telemetry availability). Upload eligibility,
  leaderboard policy, and ranked adjudication are server-side concerns, not
  protocol-layer enforcement.

## Layers

```text
Flutter application
  -> Dart plugin session API
  -> MethodChannel / EventChannel native bridge
  -> Android Activity or iOS Unity window lifecycle
  -> Engine wire protocol
  -> Unity GameBridge / future engine adapter
```

The Dart application should normally use a higher-level plugin API, for example
`waitForReady()` or `PlaySession`, instead of constructing envelopes directly.
The wire protocol remains documented because native glue, mock runtimes, tests,
and Unity must share the same contract.

## Native Runtime Contract

Runtime lifecycle is controlled by native/plugin APIs, not by gameplay protocol
messages.

Recommended public Dart-level operations:

- `queryRuntime()`
- `startRuntime()`
- `waitForReady()`
- `showSurface()`
- `hideSurface()`
- `runSession()` or `PlaySession`

Recommended native runtime state:

| State | Meaning |
|-------|---------|
| `unavailable` | No usable engine artifact/runtime is available. |
| `starting` | Runtime startup was explicitly requested and is in progress. |
| `ready` | Engine acknowledged initialization and can accept `session.start`. |
| `busy` | Engine is running a session. |
| `suspended` | Runtime is intentionally paused/hidden but can be resumed. |
| `failed` | Startup, surface presentation, or engine initialization failed. |

The native bridge may cache runtime state, but `ready` and `busy` should be
derived from engine acknowledgement, not from artifact existence alone.

Runtime snapshot shape:

```json
{
  "engine": "unity",
  "mode": "unity",
  "state": "ready",
  "generation": 3
}
```

Optional fields are omitted when their value would be null or empty. The
example above is a `ready` snapshot with no active session and no error;
`activeSessionId` and `error` are therefore absent. A `busy` snapshot would
include `activeSessionId`; a `failed` snapshot would include `error`.

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `engine` | string | yes | Engine adapter id, for example `unity` or `mock`. |
| `mode` | string | yes | Runtime mode: `unity`, `mock`, or `unavailable`. |
| `state` | string | yes | One of the runtime states above. |
| `generation` | int | yes | Incremented when the engine runtime is recreated. |
| `activeSessionId` | string | conditional | Required when `state = "busy"`. |
| `error` | object | conditional | Required when `state = "failed"`. See [ErrorPayload](#errorpayload). |

The engine runtime is **resident within an app lifecycle**. The host controls
startup explicitly via `startRuntime()` and may pre-load the engine before any
session. The runtime is not torn down between sessions; it is released only
when the host process or engine surface is destroyed. App backgrounding
surfaces as `state = "suspended"`, not as a teardown.

## Wire Envelope

All engine wire messages are UTF-8 JSON strings sent through the native bridge.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "session.start",
  "payload": {}
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schema` | string | yes | Exact schema marker. Must be `cytoid.game-core.v2`. This is a fail-fast marker, not a compatibility negotiation mechanism. |
| `id` | string | yes | Correlation id. For sessions, this is also the session id. |
| `type` | string | yes | Message type. |
| `payload` | object | yes | Type-specific payload. Use `{}` if empty. |

Receivers must reject an unknown `schema`, unknown `type`, malformed payload, or
payload value outside the documented enum sets. Rejections caused by
`session.start` must be returned as `session.result` with
`outcome.kind = "rejected"` and the same `id`.

## Message Types

| Type | Direction | Description |
|------|-----------|-------------|
| `engine.ready` | Engine -> Flutter | Engine initialized and can accept a session. |
| `engine.error` | Engine -> Flutter | Non-session runtime error. Active-session runtime death uses `session.failed`; other session errors use `session.result`. |
| `health.check` | Flutter -> Engine | Single liveness/state check during startup or active play. |
| `health.ok` | Engine -> Flutter | Response to `health.check`. |
| `settings.apply` | Flutter -> Engine | Apply runtime/profile settings outside a launch payload. |
| `settings.applied` | Engine -> Flutter | Structured settings acknowledgement. |
| `session.start` | Flutter -> Engine | Start one gameplay session. |
| `session.started` | Engine -> Flutter | Acknowledgement that `session.start` was accepted and gameplay is beginning. Not emitted on rejection. |
| `session.cancel` | Flutter -> Engine | Request cancellation of the active session. |
| `session.telemetry` | Engine -> Flutter | Optional telemetry stream when requested by launch options. |
| `session.result` | Engine -> Flutter | Terminal gameplay outcome message (engine-active outcomes). Runtime death uses `session.failed`. |
| `session.failed` | Engine -> Flutter | Terminal runtime-failure message for an active session (native-bridge synthesis). |
| `logs.batch` | Engine -> Flutter | Buffered engine logs. |

Removed v1 message types:

- `bridge.status`
- `game.status`
- `bridge.ping`
- `game.pong`
- `bridge.play.start`
- `bridge.play.end`
- `game.play.ended`
- `game.play.result`
- `bridge.settings.update`
- `game.settings.updated`
- `game.logs.batch`

## Message Ordering

### Runtime Startup

```text
Flutter/native startRuntime()
  -> native starts or resumes engine
  <- engine.ready
Flutter waitForReady() resolves
```

`engine.ready` is sent once per engine generation after the engine has completed
initialization. If the engine is recreated, the next ready event must use an
incremented `generation`.

### Normal Session

```text
Flutter -> session.start (id = S1)
Engine  -> session.started (id = S1)              | session.result (id = S1, outcome.kind = "rejected")
Engine  -> optional session.telemetry * N (id = S1, only if recording enabled and no auto-class mod)
Engine  -> session.result (id = S1)
Flutter/native retains the resident runtime; surface visibility is a host UX decision
```

### Cancellation

```text
Flutter -> session.cancel (id = S1)
Engine  -> session.result (id = S1, outcome.kind = "cancelled")
Flutter/native retains the resident runtime; surface visibility is a host UX decision
```

Cancellation is a gameplay outcome, not a route acknowledgement. There is no
`session.cancelled` or `game.play.ended` equivalent.

### Rejected Start

```text
Flutter -> session.start (id = S2)
Engine  -> session.result (id = S2, outcome.kind = "rejected", error = ...)
```

The engine should reject overlapping sessions, invalid payloads, missing assets,
unsupported modes, and invalid settings with a structured result.

### Health Check

```text
Flutter -> health.check (id = H1)
Engine  -> health.ok (id = H1)
```

The Flutter client should use this single check for long-running liveness. A
healthy active session is represented by `state = "busy"` and the matching
`activeSessionId`.

## `engine.ready`

Engine -> Flutter.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "ready-3",
  "type": "engine.ready",
  "payload": {
    "engine": "unity",
    "engineVersion": "6000.0.75f1",
    "generation": 3,
    "display": {
      "targetFrameRate": 120,
      "screenRefreshRate": 120
    }
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `engine` | string | yes | Engine adapter id. |
| `engineVersion` | string? | no | Engine/runtime version for diagnostics. |
| `generation` | int | yes | Runtime generation. |
| `display` | object? | no | Diagnostic display information. |

## `engine.error`

Engine -> Flutter.

Used for non-session runtime errors after the engine is initialized. The
engine MUST route errors into the matching response type when one exists:

| Error source | Response type |
|---|---|
| `session.start` payload validation | `session.result` with `outcome.kind = "rejected"` |
| `settings.apply` field validation | `settings.applied` with `rejectedFields` / `errors` |
| Active session runtime failure | `session.failed` (see [Active-Session Runtime Failure](#active-session-runtime-failure)) |
| Mismatched / stale / duplicate `session.cancel` | `engine.error` with the relevant `code` (see [Cancel Edge Cases](#cancel-edge-cases)) |
| Other background runtime errors | `engine.error` |

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "runtime-error-1",
  "type": "engine.error",
  "payload": {
    "error": {
      "code": "engine_exception",
      "message": "Unhandled exception in engine adapter.",
      "details": {
        "stackTrace": "..."
      }
    }
  }
}
```

## `health.check`

Flutter -> Engine.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "health-1",
  "type": "health.check",
  "payload": {
    "activeSessionId": "session-123"
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `activeSessionId` | string? | no | Session id Flutter expects to still be active. |

## `health.ok`

Engine -> Flutter.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "health-1",
  "type": "health.ok",
  "payload": {
    "engine": "unity",
    "generation": 3,
    "state": "busy",
    "activeSessionId": "session-123",
    "time": {
      "engineUptimeMs": 240000,
      "sessionTimeMs": 90500
    }
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `engine` | string | yes | Engine adapter id. |
| `generation` | int | yes | Runtime generation. |
| `state` | string | yes | One of `starting`, `ready`, `busy`, `suspended`, or `failed`. Aligns with the runtime snapshot state enum except `unavailable` (no engine exists to answer `health.check`). |
| `activeSessionId` | string? | no | Current active session id. |
| `time` | object? | no | Diagnostic timing information. |
| `error` | object? | no | Present when `state = "failed"`. |

## `settings.apply`

Flutter -> Engine.

This message updates runtime/profile settings without starting a new session.
Launch-specific settings should be included in `session.start`.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "settings-1",
  "type": "settings.apply",
  "payload": {
    "profile": {
      "language": "zh-CN",
      "hitTapticFeedback": true,
      "menuTapticFeedback": true
    },
    "runtime": {
      "musicVolume": 0.8,
      "soundEffectsVolume": 1.0
    },
    "visual": {
      "displayProfiler": false
    }
  }
}
```

Rules:

- `runtime` settings may be applied during active play only if the engine marks
  them as realtime-safe.
- `profile` settings should be applied while idle. If a profile setting cannot
  be applied during active play, it must be returned in `deferredFields` or
  `rejectedFields`.
- The engine must not reply with unconditional success. Every ignored or invalid
  field should be visible in the acknowledgement.

## `settings.applied`

Engine -> Flutter.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "settings-1",
  "type": "settings.applied",
  "payload": {
    "applied": true,
    "appliedFields": [
      "profile.language",
      "runtime.musicVolume",
      "runtime.soundEffectsVolume"
    ],
    "deferredFields": [],
    "rejectedFields": [],
    "errors": []
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `applied` | bool | yes | True if there were no rejected fields. |
| `appliedFields` | string[] | yes | Fully qualified fields that were applied. |
| `deferredFields` | string[] | yes | Valid fields accepted but not yet applied. |
| `rejectedFields` | string[] | yes | Fields rejected by validation or state. |
| `errors` | ErrorPayload[] | yes | Structured errors. |

## `session.start`

Flutter -> Engine.

Starts exactly one gameplay session. The envelope `id` is the session id.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "session-123",
  "type": "session.start",
  "payload": {
    "mode": "ranked",
    "level": {
      "meta": {
        "schema_version": 1,
        "version": 1,
        "id": "example.level",
        "title": "Example Level",
        "artist": "Example Artist",
        "music": { "path": "music.ogg" },
        "charts": [
          {
            "type": "hard",
            "difficulty": 14,
            "path": "charts/hard.json"
          }
        ]
      },
      "selectedDifficulty": "hard",
      "assets": {
        "vfsUri": "file:///data/user/0/org.cytoid/cache/levels/example.level/",
        "chartPath": "charts/hard.json",
        "musicPath": "music.ogg",
        "storyboardPath": "storyboard.json"
      }
    },
    "mods": ["fast"],
    "settings": {
      "profile": {
        "language": "zh-CN",
        "baseNoteOffset": 0,
        "levelNoteOffset": 0,
        "headsetNoteOffset": -0.05,
        "judgmentOffset": 0,
        "hitTapticFeedback": true,
        "menuTapticFeedback": true
      },
      "runtime": {
        "musicVolume": 0.85,
        "soundEffectsVolume": 1.0
      },
      "visual": {
        "noteSize": 0,
        "horizontalMargin": 1,
        "verticalMargin": 1,
        "restrictPlayAreaAspectRatio": true,
        "coverOpacity": 0.15,
        "displayStoryboardEffects": true,
        "displayBoundaries": false,
        "skipMusicOnCompletion": false,
        "displayEarlyLateIndicators": true,
        "displayNoteIds": false,
        "useExperimentalNoteAr": false,
        "useExperimentalNoteAnimations": false,
        "clearEffectsSize": 1.0,
        "displayProfiler": false,
        "adaptOverlayToSafeArea": true,
        "graphicsQuality": "high"
      },
      "audio": {
        "hitSound": "click1",
        "holdHitSoundTiming": "both",
        "useNativeAudio": false,
        "androidDspBufferSize": -1
      },
      "noteStyle": {
        "hitboxSizes": {
          "click": "large",
          "hold": "large",
          "longHold": "large",
          "dragHead": "large",
          "dragChild": "large",
          "flick": "medium",
          "cDragHead": "large",
          "cDragChild": "large"
        },
        "ringColors": {
          "click": "#FFFFFF",
          "hold": "#FFFFFF",
          "longHold": "#FFFFFF",
          "dragHead": "#FFFFFF",
          "dragChild": "#FFFFFF",
          "flick": "#FFFFFF",
          "cDragHead": "#FFFFFF",
          "cDragChild": "#FFFFFF"
        },
        "fillColors": {
          "click": "#35A7FF",
          "hold": "#35A7FF",
          "longHold": "#F2C85A",
          "dragHead": "#39E59E",
          "dragChild": "#39E59E",
          "flick": "#35A7FF",
          "cDragHead": "#39E59E",
          "cDragChild": "#39E59E"
        },
        "fillColorsAlt": {
          "click": "#FF5964",
          "hold": "#FF5964",
          "longHold": "#F2C85A",
          "dragHead": "#39E59E",
          "dragChild": "#39E59E",
          "flick": "#FF5964",
          "cDragHead": "#39E59E",
          "cDragChild": "#39E59E"
        },
        "useFillColorForDragChildNodes": true
      }
    },
    "options": {
      "recordPlayEvents": true
    }
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `mode` | string | yes | Session mode. See [Session modes](#session-modes). |
| `level` | object | yes | Level metadata, selected difficulty, and VFS assets. Required for all modes including `globalCalibration`, which must reference a dedicated calibration level supplied by the host. |
| `mods` | string[] | yes | Typed mod ids. Empty array if no mods. |
| `settings` | object | yes | Complete settings snapshot for this session. |
| `options` | object | yes | Session intent and telemetry options. |
| `tier` | object? | yes when `mode = "tier"` | Tier stage input. |

### Session Modes

Allowed values:

- `ranked`
- `practice`
- `calibration`
- `globalCalibration`
- `tier`

Mode semantics:

- `ranked`: default competitive mode. Uses strict judgment windows. Scores are
  eligible for leaderboard submission.
- `practice`: beginner-friendly mode. Uses lenient judgment windows (Cytus
  II-style). Scores still upload but are excluded from leaderboards.
- `calibration`: per-level note offset calibration.
- `globalCalibration`: global note offset calibration. Must be paired with a
  dedicated calibration level supplied via `level`.
- `tier`: multi-stage competitive mode. Functionally identical to `ranked`
  per stage (same judgment, same ranked eligibility). Flutter orchestrates
  HP, combo, and progress across multiple `tier` sessions; the engine treats
  each session as an isolated single stage.

Modes are lower camel case on the wire. Engine adapters map them to local enums.
Unknown modes must reject the session.

### LevelPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `meta` | object | yes | Structured `LevelMeta`. See [LevelMetaPayload](#levelmetapayload). No double-encoded JSON string. |
| `selectedDifficulty` | string | yes | Difficulty/chart id in `meta.charts[].type`. Must match one of the chart sections. |
| `assets` | object | yes | VFS root and selected relative paths. See [AssetPayload](#assetpayload). |

### LevelMetaPayload

Mirrors the Unity `LevelMeta` model. The host MUST supply a `LevelMeta` that
passes the validation rules below.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schema_version` | int | yes | Schema version. Currently `1`. |
| `version` | int | yes | Level revision number. |
| `id` | string | yes | Unique level id. Non-empty. |
| `title` | string | yes | Display title. |
| `title_localized` | string? | no | Optional localized title override. |
| `artist` | string | yes | Artist name. |
| `artist_localized` | string? | no | Optional localized artist override. |
| `artist_source` | string? | no | Artist credit URL or source. |
| `illustrator` | string? | no | Illustrator name. |
| `illustrator_source` | string? | no | Illustrator credit URL or source. |
| `charter` | string? | no | Charter name. |
| `storyboarder` | string? | no | Storyboard author name. |
| `music` | object | yes | Main music. See [MusicSection](#musicsection). |
| `music_preview` | object? | no | Preview clip. Same shape as `music`. |
| `background` | object? | no | Background image. Same shape as `music`. |
| `charts` | object[] | yes | One or more chart sections. See [ChartSection](#chartsection). |

#### MusicSection

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | VFS-relative file path. |

#### ChartSection

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | yes | Difficulty id: `easy`, `hard`, or `extreme`. |
| `name` | string? | no | Optional display name override for this chart. |
| `difficulty` | int | yes | Difficulty level integer. Unity normalizes legacy values internally; send the raw value the level ships with. |
| `path` | string | yes | VFS-relative chart path. |
| `music_override` | object? | no | Per-chart music override. Same shape as `music`. |
| `storyboard` | object? | no | Per-chart storyboard. See [StoryboardSection](#storyboardsection). |

#### StoryboardSection

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `path` | string | yes | VFS-relative storyboard path. Default `storyboard.json` if omitted by the source level. |
| `localizations` | object | yes | Map of BCP-47 locale → VFS-relative localization path. Empty object if no localizations. |

Validation rules:

- `id` MUST be non-empty.
- `charts` MUST contain at least one entry whose `type` is `easy`, `hard`, or `extreme`.
- `selectedDifficulty` (on the parent `LevelPayload`) MUST match one of `charts[].type`.

### AssetPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `vfsUri` | string | yes | File URI ending with a directory separator. |
| `chartPath` | string | yes | VFS-relative chart path. |
| `musicPath` | string | yes | VFS-relative music path. |
| `storyboardPath` | string? | no | VFS-relative storyboard path. |
| `checksum` | object? | no | Optional content identity for diagnostics/cache validation. |

Path rules:

- Paths must be relative to `vfsUri`.
- Absolute paths are invalid.
- `..` traversal is invalid.
- Empty required paths are invalid.
- Flutter should validate before sending.
- The engine should validate again before loading.

### SessionOptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `recordPlayEvents` | bool | yes | Whether input telemetry should be recorded and sent. |

If `recordPlayEvents = true`, the engine sends `session.telemetry` strictly
before `session.result`. The engine MUST suppress telemetry when an auto-class
mod is active regardless of this flag (see [Auto-class Recording Rule](#auto-class-recording-rule)).

### TierLaunchPayload

Required when `mode = "tier"`.

```json
{
  "tierId": "tier.example",
  "stageIndex": 0,
  "stageCount": 3,
  "maxHealth": 100,
  "initialHealth": 100,
  "initialCombo": 0,
  "introLabel": "Stage 1"
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `tierId` | string | yes | Host-defined tier/run id. |
| `stageIndex` | int | yes | 0-based stage index. |
| `stageCount` | int | yes | Total stage count for UI/echo. |
| `maxHealth` | number | yes | HP cap. |
| `initialHealth` | number | yes | Starting HP. |
| `initialCombo` | int | yes | Starting cumulative combo. |
| `introLabel` | string? | no | Optional intro text. |

## SettingsPayload

Settings are grouped by ownership and realtime behavior.

```json
{
  "profile": {},
  "runtime": {},
  "visual": {},
  "audio": {},
  "noteStyle": {}
}
```

### Settings Completeness

`session.start.settings` and `settings.apply` have different completeness
requirements:

| Source | Completeness | Behavior |
|---|---|---|
| `session.start.settings` | **Full snapshot required** | All 5 groups MUST be present. Every field within each group MUST be populated with the host's intended value for the session. Missing fields are a validation error. |
| `settings.apply` | **Partial update allowed** | Any subset of the 5 groups MAY be present. Within a present group, any subset of fields MAY be supplied; unspecified fields retain their current value. |

When the engine receives a `settings.apply`:

- For each specified field: validate, apply if valid, or append to `rejectedFields`.
- For each specified field that is valid but cannot be applied without a session
  restart: append to `deferredFields` and apply on the next `session.start`.
- All other current values are preserved.

### Settings Realtime Safety

Only the following fields are **realtime-safe** — applying them via
`settings.apply` while a session is active (`state = "busy"`) takes effect
immediately without requiring a session restart:

| Group | Realtime-safe fields |
|---|---|
| `profile` | (none) |
| `runtime` | `musicVolume`, `soundEffectsVolume` |
| `visual` | (none — visual changes require a new session) |
| `audio` | (none — audio backend changes require a new session) |
| `noteStyle` | (none — note style requires a new session) |

All fields not listed above MUST be reported in `deferredFields` if sent via
`settings.apply` during active play, and will only take effect on the next
`session.start`.

### ProfileSettings

| Field | Type | Description |
|-------|------|-------------|
| `language` | string | BCP-47 language tag, for example `en`, `zh-CN`, `ja`. |
| `baseNoteOffset` | number | Global note offset. |
| `levelNoteOffset` | number? | Level-specific note offset. Applies only with an active/selected level. |
| `headsetNoteOffset` | number | Headset offset. |
| `judgmentOffset` | number | Judgment offset. |
| `hitTapticFeedback` | bool | Gameplay haptics. |
| `menuTapticFeedback` | bool | Menu/transition haptics inside engine UI if used. |

### RuntimeSettings

| Field | Type | Description |
|-------|------|-------------|
| `musicVolume` | number | Music volume. Realtime-safe (see [Settings Realtime Safety](#settings-realtime-safety)). |
| `soundEffectsVolume` | number | SFX volume. Realtime-safe (see [Settings Realtime Safety](#settings-realtime-safety)). |

### VisualSettings

| Field | Type | Description |
|-------|------|-------------|
| `noteSize` | number | Note size scalar. |
| `horizontalMargin` | int | Horizontal play area margin. |
| `verticalMargin` | int | Vertical play area margin. |
| `restrictPlayAreaAspectRatio` | bool | Preserve play area aspect ratio. |
| `coverOpacity` | number | Cover opacity. |
| `displayStoryboardEffects` | bool | Storyboard effects enabled. |
| `displayBoundaries` | bool | Play boundary display. |
| `skipMusicOnCompletion` | bool | Fade/skip outro after completion. |
| `displayEarlyLateIndicators` | bool | Early/late UI. |
| `displayNoteIds` | bool | Debug note ids. |
| `useExperimentalNoteAr` | bool | Experimental note AR. |
| `useExperimentalNoteAnimations` | bool | Experimental note animations. |
| `clearEffectsSize` | number | Clear effect size. |
| `displayProfiler` | bool | In-engine profiler visibility. |
| `adaptOverlayToSafeArea` | bool | Adapt overlays to safe area. |
| `graphicsQuality` | string | See [Enum values](#enum-values). |

### AudioSettings

| Field | Type | Description |
|-------|------|-------------|
| `hitSound` | string | Known hit sound id. Unknown values must be rejected. |
| `holdHitSoundTiming` | string | See [Enum values](#enum-values). |
| `useNativeAudio` | bool | Use native audio backend when available. |
| `androidDspBufferSize` | int | Android DSP buffer size. `-1` means engine default. |

### NoteStyleSettings

| Field | Type | Description |
|-------|------|-------------|
| `hitboxSizes` | object | Note type -> `small`, `medium`, or `large`. |
| `ringColors` | object | Note type -> CSS hex color. |
| `fillColors` | object | Note type -> CSS hex color. |
| `fillColorsAlt` | object | Note type -> CSS hex color. |
| `useFillColorForDragChildNodes` | bool | Use fill color for drag children. |

Note type keys:

- `click`
- `hold`
- `longHold`
- `dragHead`
- `dragChild`
- `flick`
- `cDragHead`
- `cDragChild`

All 8 note type keys are **REQUIRED** in every note style map (`hitboxSizes`,
`ringColors`, `fillColors`, `fillColorsAlt`). The engine MUST reject sparse maps
with `invalid_payload`. The application is responsible for storing and sending
fully-populated note style objects; the plugin exposes a `NoteStyle` type for
this purpose and does not provide default-filling builders.

## Enum Values

### Mods

Allowed mod ids:

- `flipX`
- `flipY`
- `flipAll`
- `slow`
- `fast`
- `fc`
- `ap`
- `hard`
- `exHard`
- `hideScanline`
- `hideNotes`
- `autoDrag`
- `autoHold`
- `autoFlick`
- `auto`

The plugin should validate conflicts before sending. The engine MUST also reject
invalid combinations.

Conflict rules (deterministic; engine MUST enforce):

| Mod combination | Engine behavior |
|---|---|
| `fast` + `slow` | Reject with `invalid_mods`. |
| `flipAll` + `flipX` | Reject with `invalid_mods`. Plugin MUST normalize to one before send. |
| `flipAll` + `flipY` | Reject with `invalid_mods`. Plugin MUST normalize to one before send. |
| `flipAll` + `flipX` + `flipY` | Reject with `invalid_mods`. |
| `auto` + `autoDrag` / `autoHold` / `autoFlick` | Accept; `auto` takes precedence and the individual auto mod is ignored. `flags.usedAutoMod = true`. |
| Multiple individual auto mods (e.g. `autoDrag` + `autoHold`, no `auto`) | Accept; all listed auto mods apply. `flags.usedAutoMod = true`. |

Any other combination not listed above is accepted by default.

### Auto-class Recording Rule

Auto-class mods are: `auto`, `autoDrag`, `autoHold`, `autoFlick`.

When a session is launched with any auto-class mod active, the engine MUST:

- NOT send any `session.telemetry` message for that session, regardless of
  `options.recordPlayEvents`.
- Set `session.result.telemetry.available = false`.
- Set `session.result.flags.usedAutoMod = true`.

The engine does NOT reject `mode = "ranked"` combined with auto-class mods.
Whether the resulting score is eligible for leaderboard upload is a server-side
policy decision based on the reported `flags.usedAutoMod` and telemetry
availability.

### Graphics Quality

Allowed values:

- `veryLow`
- `low`
- `medium`
- `high`
- `ultra`

### Hold Hit Sound Timing

Allowed values:

- `begin`
- `end`
- `both`

### Hitbox Size

Allowed values:

- `small`
- `medium`
- `large`

## `session.started`

Engine -> Flutter.

Emitted exactly once after a `session.start` has been accepted, before any
`session.telemetry` or `session.result` for that session id. Signals that
payload validation passed, assets resolved, and gameplay is beginning. Not
emitted when the session is rejected — rejection goes straight to
`session.result` with `outcome.kind = "rejected"`.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "session-123",
  "type": "session.started",
  "payload": {
    "sessionId": "session-123",
    "mode": "ranked",
    "generation": 3
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sessionId` | string | yes | Same as envelope `id`. |
| `mode` | string | yes | Echo of `session.start.mode`. |
| `generation` | int | yes | Runtime generation that accepted the session. |

## `session.cancel`

Flutter -> Engine.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "session-123",
  "type": "session.cancel",
  "payload": {
    "reason": "userBack"
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `reason` | string | yes | `userBack`, `hostNavigation`, `appBackgrounded`, `surfaceLost`, or `unknown`. |

The engine responds with `session.result` using the same `id`,
`outcome.kind = "cancelled"`, and `outcome.reason` echoing the cancel request's
`reason`.

### Cancel Edge Cases

| Trigger | Engine response |
|---|---|
| `id` matches `activeSessionId` and session is active | `session.result` with `outcome.kind = "cancelled"` and matching `reason`. |
| `id` does not match any active session | `engine.error` with `code = "unknown_session"`. No `session.result`. |
| Cancel after `session.result` already sent for that id | `engine.error` with `code = "not_active"`. Idempotent rejection; the prior terminal result stands. |
| Cancel before the engine has accepted `session.start` (no `session.started` yet) | Engine SHOULD treat the cancel as a hint: if validation was going to succeed, emit `session.result` with `outcome.kind = "cancelled"`; if validation was going to fail, emit the original `rejected` result. Either way the session ends in exactly one terminal result. |
| Duplicate cancel with the same id while active | `engine.error` with `code = "already_cancelling"`. Engine SHOULD deduplicate and avoid emitting two cancelled results. |

## `session.telemetry`

Engine -> Flutter.

Only sent when `options.recordPlayEvents = true` or another future telemetry
option is enabled.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "session-123",
  "type": "session.telemetry",
  "payload": {
    "sessionId": "session-123",
    "playEvents": {
      "format": "json.v1",
      "events": [
        {
          "t": 1234,
          "f": 0,
          "p": "down",
          "x": 32000,
          "y": 44000
        }
      ]
    }
  }
}
```

Rules:

- Telemetry is optional and never required to show a result screen.
- All `session.telemetry` messages for a given session MUST arrive strictly
  before the matching `session.result`. `session.result` is terminal; no
  telemetry may follow it.
- The result payload carries only a telemetry summary (see
  [ResultTelemetryPayload](#resulttelemetrypayload)); large event arrays are
  transported exclusively via `session.telemetry`.
- If payload size becomes a problem, this message can be chunked in a later
  schema revision. Do not add chunking before it is needed.

### SessionTelemetryPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sessionId` | string | yes | Same as envelope `id`. |
| `playEvents` | object | yes | See [PlayEventsPayload](#playeventspayload). |

### PlayEventsPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `format` | string | yes | Event payload format id. Currently `json.v1`. Engine MUST reject unknown formats. |
| `events` | object[] | yes | Recorded play events. See [PlayEventPayload](#playeventpayload). May be empty. |

### PlayEventPayload

Single recorded touch event. Field names are short to keep large telemetry
batches small.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `t` | int | yes | Milliseconds since the session gameplay clock started. Non-negative. |
| `f` | int | yes | Finger index, 0-based. Stable within one finger lifecycle (down → move* → up). |
| `p` | string | yes | Phase: `down`, `move`, or `up`. |
| `x` | int | yes | Normalized screen X, `0`–`65535` inclusive (left to right across the rendered play surface). |
| `y` | int | yes | Normalized screen Y, `0`–`65535` inclusive (bottom to top across the rendered play surface). |

Sampling rules:

- `down` and `up` events are always recorded when input reaches the engine.
- `move` events are sampled at most every ~16 ms (60 Hz) AND when the finger
  moves ≥ 96 normalized units (~0.15 % of the screen) since the last sample.
  Both conditions are engine-side; the host does not control sampling.
- Recording is suspended while the game is paused; no events are emitted
  during pause.
- Coordinates are normalized against the engine's rendered surface, not the
  physical display. `0,0` is bottom-left; `65535,65535` is top-right.

## `session.result`

Engine -> Flutter.

This is the only terminal gameplay message for a session.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "session-123",
  "type": "session.result",
  "payload": {
    "sessionId": "session-123",
    "mode": "ranked",
    "mods": ["fast"],
    "outcome": {
      "kind": "completed"
    },
    "level": {
      "id": "example.level",
      "title": "Example Level",
      "difficulty": "hard",
      "difficultyLevel": 14
    },
    "score": {
      "score": 998765,
      "accuracy": 0.9987,
      "maxCombo": 1234,
      "gradeCounts": {
        "perfect": 1200,
        "great": 20,
        "good": 0,
        "bad": 0,
        "miss": 0
      },
      "early": 12,
      "late": 8,
      "averageTimingError": -1.2,
      "standardTimingError": 14.5
    },
    "flags": {
      "usedAutoMod": false
    },
    "telemetry": {
      "available": true,
      "eventsRecorded": 1234,
      "bytes": 45678
    },
    "timestamp": 1782148800000
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sessionId` | string | yes | Same as envelope `id`. |
| `mode` | string | yes | Echo of session.start `mode`. |
| `mods` | string[] | yes | Echo of session.start `mods`. Empty array if none. |
| `outcome` | object | yes | See [OutcomePayload](#outcomepayload). |
| `level` | object | conditional | Required when `mode` is not `calibration` and a level was supplied to `session.start`. |
| `score` | object | conditional | Required when score data exists. Includes failed runs if the engine computes failed scores. Omit only when no score is meaningful (e.g. pure rejection before play, calibration without scoring). |
| `calibration` | object | conditional | Required when `outcome.kind = "calibration"`. |
| `tier` | object | conditional | Required when `mode = "tier"`, regardless of outcome kind. Carries the per-stage ending state. |
| `flags` | object | yes | See [FlagsPayload](#flagspayload). |
| `telemetry` | object | yes | See [ResultTelemetryPayload](#resulttelemetrypayload). |
| `error` | object | conditional | Required when `outcome.kind = "rejected"`. See [ErrorPayload](#errorpayload). |
| `timestamp` | int | yes | Unix epoch milliseconds. |

### FlagsPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `usedAutoMod` | bool | yes | True if any auto-class mod was active. See [Auto-class Recording Rule](#auto-class-recording-rule). |

### ResultTelemetryPayload

Telemetry summary carried on every result. Indicates whether full telemetry is
available via separate `session.telemetry` messages.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `available` | bool | yes | Whether `session.telemetry` messages were sent for this session. False when auto-class mod suppressed recording or when `options.recordPlayEvents = false`. |
| `eventsRecorded` | int | yes | Number of recorded play events. `0` when `available = false`. |
| `bytes` | int | yes | Approximate uncompressed size of recorded events. `0` when `available = false`. |

### OutcomePayload

Every result has exactly one outcome kind.

#### Completed

```json
{ "kind": "completed" }
```

#### Failed

Gameplay failure, not a protocol/runtime error.

```json
{
  "kind": "failed",
  "reason": "hpDepleted"
}
```

Allowed reasons:

- `hpDepleted`
- `manualFail`
- `tierHpDepleted`
- `unknown`

#### Cancelled

Host/user cancellation.

```json
{
  "kind": "cancelled",
  "reason": "userBack"
}
```

Allowed reasons:

- `userBack`
- `hostNavigation`
- `appBackgrounded`
- `surfaceLost`
- `unknown`

#### Rejected

The engine rejected a `session.start` before gameplay began.

```json
{
  "kind": "rejected"
}
```

`error` must be present on the result payload.

#### Tier Retry

Emitted only when a tier stage is interrupted mid-game by an **engine-side**
retry request — for example the user clicked a "retry this stage" affordance
inside the engine's UI while the stage was still in progress. Host-initiated
retry (user clicked retry on Flutter's result screen after `session.result`)
does NOT use this outcome; the host simply sends a new `session.start` for the
same stage.

```json
{
  "kind": "tierRetry",
  "tierId": "tier.example",
  "stageIndex": 1
}
```

The `tier` field on the result payload MUST still be present (the stage did
start and produce partial state), carrying the partial-stage ending values.

The host treats this outcome as terminal for the session and decides whether
to start the next attempt at the same `stageIndex`.

#### Calibration

Calibration session completed with calibrated offsets.

```json
{
  "kind": "calibration"
}
```

`calibration` must be present on the result payload.

### Active-Session Runtime Failure

When the engine runtime fails or is recreated while a session is active, the
session MUST still terminate with exactly one terminal envelope. Because the
runtime itself may be dead or unreachable, the NATIVE BRIDGE synthesizes a
`session.failed` envelope for the active session — never a `session.result`.
The engine does not emit `session.failed`; the C# engine in particular is
v1-only on the wire and emits no v2 outcomes at all. The bridge's
`synthesizeRuntimeFailure` primitive is the sole producer of this envelope.

| Scenario | Required behavior |
|---|---|
| Engine exception during active session | Native bridge synthesizes `session.failed` with `error.code = "runtime_exception"`. |
| Native bridge cannot deliver messages (Unity process gone) | Native bridge synthesizes `session.failed` with `error.code = "runtime_unreachable"`. |
| Engine generation increments while `activeSessionId` is non-null | Native bridge synthesizes `session.failed` for the active session with `error.code = "runtime_recreated"`, BEFORE emitting the next `engine.ready`. |
| Surface loss during active play | Native bridge synthesizes `session.failed` with `error.code = "runtime_surface_lost"`. |

After any runtime-failed termination:

- The runtime snapshot MUST transition to `state = "failed"` (or `ready` again
  if the engine recovered and emitted a new `engine.ready` with incremented
  `generation`).
- `activeSessionId` MUST be cleared.
- The next `engine.ready` (if any) MUST carry an incremented `generation`.

`engine.error` is not used for active-session failures; `session.failed`
carries the terminal outcome so the host's session promise is preserved.

### `session.failed`

Engine -> Flutter. Synthesized by the NATIVE BRIDGE (never the engine) when a
session is killed by a runtime-side event the engine itself cannot report. The
engine's own terminal outcomes use `session.result`; `session.failed` carries
ONLY the failure — the runtime is dead and cannot produce score, flags, or
telemetry, so those fields are absent by design.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "<sessionId>",
  "type": "session.failed",
  "payload": {
    "sessionId": "<sessionId>",
    "error": {
      "code": "runtime_unreachable",
      "message": "Native bridge cannot deliver messages to the engine.",
      "details": {}
    },
    "timestamp": 1782148800000
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sessionId` | string | yes | Same as envelope `id`. The session that failed. |
| `error` | object | yes | See [ErrorPayload](#errorpayload). `code` is from the `runtime_*` family (`runtime_recreated`, `runtime_unreachable`, `runtime_surface_lost`, `runtime_exception`); specific failure types MAY use a more precise code (e.g. `asset_load_failed` mid-play). |
| `timestamp` | int | yes | Unix epoch milliseconds. |

This envelope carries NO `outcome`, `flags`, `telemetry`, `mode`, `level`,
`score`, `calibration`, or `tier` field. The envelope's existence IS the
runtime-failure signal; the previous `session.result` runtime-death outcome
shape is removed.

### LevelResultPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | yes | Level id. |
| `title` | string | yes | Level title. |
| `difficulty` | string | yes | Difficulty id. |
| `difficultyLevel` | int | yes | Difficulty level. |

### ScorePayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `score` | int | yes | Numeric score. |
| `accuracy` | number | yes | Accuracy from 0 to 1. |
| `maxCombo` | int | yes | Max combo. |
| `gradeCounts` | object | yes | Stable grade keys. |
| `early` | int | yes | Early count. |
| `late` | int | yes | Late count. |
| `averageTimingError` | number? | no | Average timing error. |
| `standardTimingError` | number? | no | Standard timing error. |

Grade keys should be stable lower camel case names. The engine adapter maps from
local grade enums.

### CalibrationResultPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `baseNoteOffset` | number? | no | Global calibrated offset. |
| `levelNoteOffset` | number? | no | Level calibrated offset. |

### TierResultPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `tierId` | string | yes | Host-defined tier id. |
| `stageIndex` | int | yes | Stage index. |
| `stageCount` | int? | conditional | Total stage count. May be omitted on tier-retry outcomes where partial-stage state is unavailable. |
| `health` | number? | conditional | Ending health. May be omitted on tier-retry outcomes where partial-stage state is unavailable. |
| `maxHealth` | number | yes | HP cap. |
| `combo` | int | yes | Ending cumulative combo. |

## ErrorPayload

Structured error shape used by `engine.error`, `settings.applied`, and
`session.result`.

```json
{
  "code": "invalid_payload",
  "message": "level.selectedDifficulty does not exist in level.meta.",
  "details": {
    "field": "level.selectedDifficulty",
    "value": "hard"
  }
}
```

Fields:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `code` | string | yes | Stable machine-readable error code. |
| `message` | string | yes | Human-readable diagnostic message in English. |
| `details` | object? | no | Structured debug details. |

Recommended error codes:

| Code | Used by | Response type |
|---|---|---|
| `runtime_unavailable` | Engine not initialized or no artifact available. | `engine.error`, or `session.result` with `outcome.kind = "rejected"` if a session was requested. |
| `runtime_not_ready` | Engine exists but has not emitted `engine.ready` for the current generation. | `engine.error`, or `session.result` with `outcome.kind = "rejected"`. |
| `runtime_recreated` | Engine generation incremented while a session was active. | `session.failed`. |
| `runtime_unreachable` | Native bridge cannot reach the engine process. | `session.failed`. |
| `runtime_surface_lost` | Engine surface was lost during active play. | `session.failed`. |
| `runtime_exception` | Unhandled engine exception during a session. | `session.failed`. |
| `overlapping_session` | A `session.start` arrived while another session was active. | `session.result` with `outcome.kind = "rejected"`. |
| `unknown_session` | `session.cancel` referenced an unknown session id. | `engine.error`. |
| `not_active` | `session.cancel` arrived after the session had already terminated. | `engine.error`. |
| `already_cancelling` | Duplicate `session.cancel` for an already-cancelling session. | `engine.error`. |
| `invalid_payload` | `session.start` payload failed structural validation. | `session.result` with `outcome.kind = "rejected"`. |
| `invalid_settings` | Settings payload failed validation. | `session.result` with `outcome.kind = "rejected"` (for `session.start`); `settings.applied` with `rejectedFields` (for `settings.apply`). |
| `invalid_mods` | Mod list failed validation (unknown id or conflict). | `session.result` with `outcome.kind = "rejected"`. |
| `invalid_level_meta` | `level.meta` failed schema validation. | `session.result` with `outcome.kind = "rejected"`. |
| `missing_asset` | Referenced VFS path does not exist. | `session.result` with `outcome.kind = "rejected"`. |
| `asset_load_failed` | VFS path exists but content could not be parsed/loaded. | `session.result` with `outcome.kind = "rejected"` (pre-play) or `session.failed` (mid-play). |
| `unsupported_mode` | `mode` is not a known session mode. | `session.result` with `outcome.kind = "rejected"`. |
| `engine_exception` | Unhandled engine exception outside any session. | `engine.error`. |

Cancellation is NOT an error: a successful `session.cancel` flow returns
`session.result` with `outcome.kind = "cancelled"` and no `error` payload.

## `logs.batch`

Engine -> Flutter.

```json
{
  "schema": "cytoid.game-core.v2",
  "id": "logs-10",
  "type": "logs.batch",
  "payload": {
    "reason": "trigger",
    "triggerLevel": "error",
    "timestamp": 1782148800000,
    "truncated": false,
    "logs": [
      {
        "level": "error",
        "message": "Failed to load chart.",
        "stackTrace": "...",
        "timestamp": 1782148799000,
        "sessionId": "session-123"
      }
    ]
  }
}
```

Changes from v1:

- `timestamp` uses Unix epoch milliseconds.
- `playId` is renamed to `sessionId`.
- Message type is `logs.batch`, without engine-specific `game.` prefix.

### LogsBatchPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `reason` | string | yes | Why this batch was emitted: `periodic`, `trigger`, `flush`, or `crash`. |
| `triggerLevel` | string | conditional | Required when `reason = "trigger"`. The log level that caused the trigger, e.g. `error`. |
| `timestamp` | int | yes | Unix epoch milliseconds. Time the batch was produced. |
| `truncated` | bool | yes | Whether the batch hit a size or count limit and dropped earlier entries. |
| `logs` | object[] | yes | Log entries. See [LogEntryPayload](#logentrypayload). |

### LogEntryPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `level` | string | yes | Severity: `debug`, `info`, `warning`, `error`, or `fatal`. |
| `message` | string | yes | Log message text. |
| `stackTrace` | string? | no | Stack trace when the entry captures a thrown exception. |
| `timestamp` | int | yes | Unix epoch milliseconds. |
| `sessionId` | string? | conditional | Required when the entry is bound to an active session (e.g. gameplay logs). Omit for engine-global logs. |

## Mock Runtime Requirements

Mock runtimes must implement the same protocol semantics as Unity:

- emit `engine.ready`
- answer `health.check`
- return `session.result` for every `session.start`
- return `session.result` with `outcome.kind = "cancelled"` for cancellation
- validate obvious malformed payloads
- expose `mode = "mock"` in runtime snapshots
- never silently pretend to be Unity

Mock results do not need real scoring, VFS, or rendering, but the shape and
ordering of messages should match Unity.

## Dart API Expectations

The public plugin API should hide most envelope mechanics from application code.

Suggested API shape:

```dart
final runtime = await client.queryRuntime();
await client.startRuntime();
await client.waitForReady();

final session = client.createSession(launch);
await session.showSurface();
final result = await session.run();
await session.hideSurface();
```

Minimum public concepts:

- `GameRuntimeSnapshot`
- `GameRuntimeState`
- `GameEngineMode`
- `GameSession`
- `GameSessionLaunch`
- `GameSessionResult`
- `GameSessionOutcome`
- `GameSessionOptions`
- `GameSettings`
- `GameMod`
- `NoteType`
- `NoteStyle`
- `GraphicsQuality`
- `HoldHitSoundTiming`
- `GameCoreError`

Application code should not import `src/` models or duplicate example-only wire
constants.

## Unity Implementation Notes

Suggested mapping from v1 files:

- `GameBridgeRouter`
  - replace v1 message switch with v2 message types
  - remove `pendingSettings` merge into launch payloads
  - reject invalid payloads with `session.result`
- `GameBridge`
  - emit `engine.ready`
  - send exactly one `session.result`
  - stop using `game.play.ended`
  - keep handoff overlay out of protocol semantics
- `GameResultBridge`
  - build `GameSessionResult`
  - support completed, failed, cancelled, rejected, tier retry, and calibration
  - make telemetry opt-in
- `GameLaunchPayload`
  - replace `levelMetaJson` with structured `level.meta`
  - use typed lower camel wire names for mode/mod/settings enums
- `ExternalGameContentProvider`
  - validate VFS and mods strictly
  - reject invalid values instead of ignoring them

## Migration From v1

This is a breaking migration. No compatibility shim is required.

| v1 | v2 |
|----|----|
| `v: 1` | `schema: "cytoid.game-core.v2"` (permanent fail-fast marker, not a compatibility field) |
| `bridge.play.start` | `session.start` |
| `game.play.result` | `session.result` |
| `bridge.play.end` | `session.cancel` |
| `game.play.ended` | removed; use `session.result` with `cancelled` |
| `bridge.status` + `game.status` | `health.check` + `health.ok` |
| `bridge.ping` + `game.pong` | `health.check` + `health.ok` |
| `bridge.settings.update` | `settings.apply` |
| `game.settings.updated` | `settings.applied` |
| `game.logs.batch` | `logs.batch` |
| `levelMetaJson` | `level.meta` |
| `mods: string[]` PascalCase | typed lower camel mod ids |
| `completed` / `failed` booleans | `outcome.kind` |
| ISO timestamps | Unix epoch milliseconds |
| default play events in result | opt-in via `options.recordPlayEvents` + separate `session.telemetry` message; result carries summary only |
| `mode: "standard"` | `mode: "ranked"` |
| `options.ranked: bool` | encoded in `mode` (`ranked` vs `practice`) |
| `options.lifecycleAfterResult` | removed; runtime is warm-resident by contract |
| `flags.ranked` | removed; echo `mode` instead |
| `flags.recordedPlayEvents` | removed; observable via `result.telemetry.available` |
| sparse note style maps | rejected; all 8 note type keys required |
| `lifecyclePolicy: warm\|teardown\|platformDefault` | removed; warm-only |
| auto-class mod with `recordPlayEvents = true` | engine suppresses telemetry; `result.telemetry.available = false`; `flags.usedAutoMod = true` |
| pre-launch pending-settings merge into next `session.start` | removed; settings do not bleed across sessions |

## Implementation Checklist

1. Add v2 Dart models and tests.
2. Add v2 C# models and strict validation.
3. Update Android and iOS mock runtimes to v2 first.
4. Update Unity bridge router and result emission.
5. Replace example session flow with `waitForReady()` or `PlaySession`.
6. Add golden JSON fixtures shared by Dart and C# tests.
7. Add Android real-device or emulator host smoke with Unity artifacts.
8. Update old v1 docs or clearly mark them as legacy.
9. Update `PROBLEMS.md` when the P0 protocol issues are resolved.
