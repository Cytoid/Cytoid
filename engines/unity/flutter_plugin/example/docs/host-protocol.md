# CytoidGameCore Protocol

Bridge ↔ game communication between the **CytoidGameCore** Flutter plugin and the embedded Unity gameplay core (Godot in the future).

## Goals

- **Engine independence** — message types and payloads must not reference Unity scenes, Godot nodes, or engine-specific APIs.
- **Reuse existing contracts** — `GameLaunchPayload` and `GameResultPayload` from `cytoid-core-unity` are the canonical game data models.
- **Thin adapters** — each engine implements a small bridge that maps envelopes to its native APIs.
- **No Unity-specific Flutter plugins** — embedding uses hand-rolled Platform Views and `MethodChannel` / `EventChannel`, not `flutter_unity_widget`.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  L1 — Application protocol (Dart)                           │
│  CytoidGameCoreClient, CytoidGameCoreEnvelope, payload models           │
└───────────────────────────┬─────────────────────────────────┘
                            │ JSON strings
┌───────────────────────────▼─────────────────────────────────┐
│  L2 — Platform embedder (Kotlin / Swift)                    │
│  Exclusive fullscreen session                               │
│  MethodChannel: cytoid/game_core                            │
│  EventChannel:  cytoid/game_core/events                     │
└───────────────────────────┬─────────────────────────────────┘
                            │ engine-specific IPC
┌───────────────────────────▼─────────────────────────────────┐
│  L3 — Engine adapter (C# / GDScript)                        │
│  Unity: GameBridge → GameLaunchBridge / GameResultBridge │
│  Godot: host_bridge.gd (future)                             │
└─────────────────────────────────────────────────────────────┘
```

Flutter business code only depends on **L1**. Replacing Unity with Godot requires changes to **L2 mount logic** and **L3 adapter** only.

## Transport

### Visual surface

| Property | Value |
|----------|-------|
| Presentation | Exclusive fullscreen session |
| Flutter API | `CytoidGameCoreClient.showGameSurface()` / `hideGameSurface()` |
| Native responsibility | Show, pause, dismiss, and restore the game core surface |

### Message channels

| Channel | Direction | Purpose |
|---------|-----------|---------|
| `MethodChannel('cytoid/game_core')` | Flutter -> native | Send envelopes (`invokeMethod('send', jsonString)`) |
| `EventChannel('cytoid/game_core/events')` | native -> Flutter | Stream of inbound envelopes |

All messages are **UTF-8 JSON strings**. One string equals one envelope.

Native code forwards inbound messages to the engine adapter (e.g. `UnitySendMessage("GameBridge", "OnBridgeMessage", json)` on Android). Engine adapters must not leak `SendMessage` game object names or method names to Dart.

## Envelope format

Every message uses the same top-level shape:

```json
{
  "v": 1,
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "bridge.play.start",
  "payload": {}
}
```

| Field | Type | Description |
|-------|------|-------------|
| `v` | int | Protocol version. Bump on breaking changes. Current: **1**. |
| `id` | string | Correlation id. A `bridge.play.start` and its `game.play.result` share the same `id`. |
| `type` | string | Message kind (see table below). |
| `payload` | object | Type-specific body. May be `{}` for messages with no data. |

## Message types

| `type` | Direction | Description |
|--------|-----------|-------------|
| `game.ready` | Engine → Flutter | Engine finished initialization; safe to send `bridge.play.start`. |
| `bridge.status` | Flutter → Engine | Query runtime state. |
| `game.status` | Engine → Flutter | Runtime state response. |
| `bridge.ping` | Flutter → Engine | Connectivity / latency probe. |
| `game.pong` | Engine → Flutter | Echo of `bridge.ping` payload. |
| `bridge.settings.update` | Flutter → Engine | Apply player settings to the idle runtime or active session. |
| `game.settings.updated` | Engine → Flutter | Acknowledge `bridge.settings.update`. |
| `bridge.play.start` | Flutter → Engine | Begin a play session. Payload is `GameLaunchPayload`. |
| `game.play.result` | Engine → Flutter | Session finished. Payload is `GameResultPayload` (includes failures). |
| `bridge.play.end` | Flutter → Engine | Leave the play route; pause or detach the engine surface. |
| `game.play.ended` | Engine → Flutter | Acknowledge an intentional `bridge.play.end`; Flutter returns to the previous route. |
| `game.logs.batch` | Engine → Flutter | Buffered Unity logs emitted on time, size, or severity triggers. |

There is no separate `game.error` type. Failures are reported through `game.play.result` with `failed: true` and an `error` field, matching `GameResultBridge.EmitError` in Unity.

### `game.ready`

Sent once after the engine runtime is initialized (Unity: after `Context` application init).

```json
{
  "v": 1,
  "id": "…",
  "type": "game.ready",
  "payload": {
    "initialized": true,
    "engine": "unity",
    "engineVersion": "6000.0.75f1"
  }
}
```

`engine` and `engineVersion` are optional debug metadata, not required for Flutter logic.

### `bridge.ping` / `game.pong`

```json
{ "v": 1, "id": "…", "type": "bridge.ping", "payload": { "text": "hello" } }
{ "v": 1, "id": "…", "type": "game.pong", "payload": { "text": "hello" } }
```

The `id` on `game.pong` should match the originating `bridge.ping`.

While waiting for `game.play.result`, Flutter does **not** use a fixed chart-length timeout. `CytoidGameCoreClient` polls every few seconds:

1. `bridge.status` / `game.status` (runtime still busy with the session id).
2. `bridge.ping` with `payload.text = "heartbeat"`; expect matching `game.pong`.

If both fail repeatedly, Flutter raises `CytoidGameCoreLostException`. Long charts (20+ minutes) are supported as long as the host keeps responding.

During an active play session, Unity must answer heartbeat pings with `game.pong` only — not another `game.ready`.

### `game.logs.batch`

Engine → Flutter only. Unity sends logs in batches. Every Unity log level enters
the same buffer, preserving the order in which Unity observed them. The buffer is
flushed when:

1. The flush interval elapses.
2. The buffer reaches the size threshold.
3. A warning, error, exception, or assert is logged.

```json
{
  "v": 1,
  "id": "…",
  "type": "game.logs.batch",
  "payload": {
    "reason": "trigger",
    "triggerLevel": "error",
    "timestamp": "2026-05-27T12:00:00.000Z",
    "truncated": false,
    "logs": [
      {
        "level": "log",
        "message": "Entering game from level detail",
        "stackTrace": null,
        "timestamp": "2026-05-27T11:59:58.000Z",
        "playId": "optional-active-play-id"
      }
    ]
  }
}
```

Unity keeps a bounded send buffer and sends at most the latest diagnostic slice in
one batch. If older entries were dropped before the batch was sent, `truncated` is
true.

Dart consumer: `CytoidGameCoreClient.logBatchEvents` → `CytoidGameCoreLogBatch`.

### `bridge.play.start`

Payload fields mirror `GameLaunchPayload` in `Cytoid/engines/unity/Assets/Scripts/Game/GameLaunchPayload.cs`.

See [GameLaunchPayload](#gamelaunchpayload) below.

Unity adapter:

```
envelope.payload → JSON string → GameLaunchBridge.StartGame(json)
```

### `game.play.result`

Payload fields mirror `GameResultPayload` in `Cytoid/engines/unity/Assets/Scripts/Game/GameResultPayload.cs`.

Unity adapter:

```
GameResultBridge.OnResultJson → wrap in envelope { type: "game.play.result", id: playId }
```

### `bridge.play.end`

```json
{
  "v": 1,
  "id": "…",
  "type": "bridge.play.end",
  "payload": {}
}
```

Flutter sends this when navigating away from the play route. The native embedder pauses the engine and may detach the surface.

## Payload reference

Dart models live in `lib/host/models/`. C# sources in `cytoid-core-unity` are authoritative for field names and semantics.

### GameLaunchPayload

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `levelMetaJson` | string | yes | Serialized `LevelMeta` JSON. |
| `selectedDifficulty` | string | yes | Difficulty id (e.g. `"hard"`). |
| `assets` | object | yes | VFS root and selected VFS-relative asset paths. |
| `settings` | object | no | `GameLaunchSettings`. |
| `mods` | string[] | no | Active mod ids. See [Mod values](#mod-values) below. |
| `gameMode` | string | no | Game mode. `"Standard"` (default), `"Practice"`, `"Calibration"`, `"GlobalCalibration"`, or `"Tier"`. Omit for standard play. `GlobalCalibration` is launched from settings with the `teages.offset_guide` level. |
| `tierPlay` | object | yes when `gameMode` is `"Tier"` | Initial state for one tier stage session. See [TierPlayLaunch](#tierplaylaunch). |

#### TierPlayLaunch

Required when `gameMode` is `"Tier"`. Each `bridge.play.start` plays **one** chart; Flutter orchestrates multi-stage runs.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `tierId` | string | no | Echoed in result; core does not interpret. |
| `stageIndex` | int | yes | 0-based stage index (echoed). |
| `stageCount` | int | no | Echoed for UI. |
| `maxHealth` | number | yes | HP cap for this session. |
| `initialHealth` | number | no | Starting HP; default `maxHealth`. |
| `initialCombo` | int | no | Starting cumulative combo; default `0`. |
| `introLabel` | string | no | Non-empty → tier intro splash text. |

Allowed mods in Tier: `Fast`, `Slow`, `HideScanline`, `HideNotes`. Pause ends the session as a failure.

#### GameLaunchSettings

| Field | Type |
|-------|------|
| `baseNoteOffset` | float? |
| `levelNoteOffset` | float? |
| `headsetNoteOffset` | float? |
| `judgmentOffset` | float? |
| `noteSize` | float? |
| `horizontalMargin` | int? |
| `verticalMargin` | int? |
| `restrictPlayAreaAspectRatio` | bool? |
| `coverOpacity` | float? |
| `musicVolume` | float? |
| `soundEffectsVolume` | float? |
| `hitSound` | string? |
| `displayStoryboardEffects` | bool? |
| `displayBoundaries` | bool? |
| `skipMusicOnCompletion` | bool? |
| `displayEarlyLateIndicators` | bool? |
| `displayNoteIds` | bool? |
| `useExperimentalNoteAr` | bool? |
| `useExperimentalNoteAnimations` | bool? |
| `clearEffectsSize` | float? |
| `displayProfiler` | bool? | Toggle Graphy FPS/RAM/audio overlay (`DisplayProfiler`). |
| `adaptOverlayToSafeArea` | bool? | When true, move gameplay overlay UI away from safe-area cutouts. Defaults to true. |
| `hitboxSizes` | object? | Map of note type id (`"0"`–`"7"`) → hitbox tier (`0` small, `1` medium, `2` large). Partial updates merge into existing sizes. |
| `noteRingColors` | object? | Map of note type id → `#RRGGBB` ring color. |
| `noteFillColors` | object? | Map of note type id → `#RRGGBB` primary fill color. |
| `noteFillColorsAlt` | object? | Map of note type id → `#RRGGBB` alternate fill color. |
| `useFillColorForDragChildNodes` | bool? | When true, drag child nodes use fill color instead of ring-only styling. |
| `holdHitSoundTiming` | string? | `Begin`, `End`, or `Both` (PascalCase). |
| `graphicsQuality` | string? | `VeryLow`, `Low`, `Medium`, `High`, or `Ultra`. |
| `hitTapticFeedback` | bool? | In-game hit haptic feedback (iOS taptic / Android vibration). |
| `useNativeAudio` | bool? | Route hit sounds through native audio when supported. |
| `androidDspBufferSize` | int? | Android DSP buffer size; `-1` restores the engine default. |

#### Mod values

Valid strings for the `mods` array (case-sensitive, PascalCase):

| Value | Description |
|-------|-------------|
| `Auto` | Auto-play all notes |
| `AutoDrag` | Auto-play drag notes |
| `AutoHold` | Auto-play hold notes |
| `AutoFlick` | Auto-play flick notes |
| `AP` | All-perfect challenge |
| `FC` | Full-combo challenge |
| `Hard` | Hard mode (HP drain) |
| `ExHard` | Extreme hard mode (overrides Hard) |
| `HideScanline` | Hide the scanline |
| `HideNotes` | Hide all notes |
| `Fast` | Increase chart speed (mutually exclusive with Slow) |
| `Slow` | Decrease chart speed (mutually exclusive with Fast) |
| `FlipX` | Mirror chart horizontally |
| `FlipY` | Mirror chart vertically |
| `FlipAll` | Mirror both axes (supersedes FlipX and FlipY) |

**Conflict rules** (engine enforces; Flutter UI should also prevent):
- `Fast` and `Slow` are mutually exclusive.
- `FlipAll` supersedes `FlipX` and `FlipY`; selecting any of `FlipX`/`FlipY` removes `FlipAll`.
- `ExHard` overrides `Hard`; both may not be active simultaneously.
- Any `Auto*` mod removes `AP` and `FC`; selecting `AP` or `FC` removes all `Auto*` mods.

#### GameLaunchAssets

Large files must not cross the MethodChannel. Flutter writes assets to a shared app directory and sends one VFS root plus selected VFS-relative paths.

| Field | Type | Description |
|-------|------|-------------|
| `vfsUri` | string | Required local `file://` URI to the level root directory. Unity stores this as `Level.Path`. |
| `chartPath` | string | Required VFS-relative path to chart JSON. |
| `musicPath` | string | Required VFS-relative path to the music file. |
| `storyboardPath` | string | Optional VFS-relative path to storyboard JSON. If omitted, Unity uses `chart.storyboard.path`, then `storyboard.json`. |

Asset path rules:

- Asset paths are relative to `vfsUri`; do not send absolute paths or URI strings.
- Leading `/` is treated as `./`, so `/storyboard.json` resolves to `storyboard.json`.
- `\` is normalized to `/`, `.` and repeated separators are collapsed.
- `..`, NUL characters, URI schemes, Windows drive paths, and UNC paths are rejected.
- After canonicalization, every resolved asset must remain inside the VFS root.

Unity `ExternalGameContentProvider` reads chart, music, storyboard, background, storyboard sprites, and storyboard videos from the VFS. Missing chart/music files fail the launch; missing storyboard continues without storyboard.

### GameResultPayload

Extends score fields from `LastPlayResult`.

| Field | Type | Description |
|-------|------|-------------|
| `completed` | bool | Chart was finished normally. |
| `failed` | bool | Session failed (error, abort, etc.). |
| `usedAutoMod` | bool | Auto mod was active. |
| `error` | string? | Error detail when `failed` is true. |
| `gameMode` | string? | Completed session mode. |
| `calibratedBaseNoteOffset` | double? | New global base note offset after `GlobalCalibration`. |
| `calibratedLevelNoteOffset` | double? | New per-level relative note offset after `Calibration`. |
| `timestamp` | string | ISO 8601 UTC. |
| `levelId` | string? | Level identifier. |
| `title` | string? | Level title. |
| `difficulty` | string? | Difficulty id. |
| `difficultyLevel` | int? | Numeric difficulty level. |
| `score` | int? | Final score. |
| `accuracy` | double? | Accuracy percentage. |
| `maxCombo` | int? | Maximum combo. |
| `gradeCounts` | map<string, int>? | Judgment grade counts. |
| `early` | int? | Early tap count. |
| `late` | int? | Late tap count. |
| `averageTimingError` | double? | Mean timing error (ms). |
| `standardTimingError` | double? | Std dev of timing error (ms). |
| `tierPlay` | object? | Present when `gameMode` is `"Tier"` and the stage finishes in-engine. See [TierPlayResult](#tierplayresult). |
| `tierRetry` | string? | Host retry hint for Tier. When set, Flutter should dismiss the play route and re-issue `bridge.play.start` for the same stage (Unity does not retry Tier in-engine). |

**Tier host routing**

| User action in Unity | Host message | Flutter behavior |
|----------------------|--------------|------------------|
| Failed / Paused **Go back** | `game.play.ended` | Pop play route; no result screen. |
| Tier Failed **Retry** | `game.play.result` with `tierRetry` | Pop play route; re-launch same stage from Flutter. |
| Tier stage clear | `game.play.result` with `tierPlay` | Show result / advance run. |

#### TierPlayResult

| Field | Type | Description |
|-------|------|-------------|
| `tierId` | string? | Echo from launch. |
| `stageIndex` | int | Echo from launch. |
| `finalHealth` | number | HP at session end. |
| `maxHealth` | number | HP cap for this session. |
| `endingCombo` | int | Cumulative combo after this stage. |

Tier stage **clear** always returns `game.play.result` with `tierPlay`. Failed-screen **Go back** ends the session without `game.play.result`; Tier **Retry** returns `game.play.result` with `tierRetry` only. Multi-stage tier runs are orchestrated entirely in Flutter.

## Session lifecycle

```
Flutter                    Native                  Engine
   │                         │                        │
   │── ensureRuntimeStarted ──►│                        │
   │── showGameSurface ───────►│── show surface ──────►│
   │                         │                        │ init Context
   │◄── game.ready ──────────│◄───────────────────────│
   │── bridge.settings.update ───────►│── OnBridgeMessage ──────►│
   │◄── game.settings.updated ─────│◄───────────────────────│
   │                         │                        │
   │── bridge.play.start (id=S1) ───►│── OnBridgeMessage ──────►│
   │                         │                        │ play chart
   │◄── game.play.result (id=S1) ─│◄───────────────────────│
   │                         │                        │ idle (host mode)
   │── hideGameSurface ───────►│── pause / detach ─────►│
```

When the user intentionally leaves `/game`, Flutter sends `bridge.play.end`, waits for
`game.play.ended` when possible, hides the surface, and pops back to the previous
route without creating a result page.

### Host mode (Unity)

When embedded in Flutter, Unity starts from `CoreHostBootstrap` and must **not** load or return to the Unity `Navigation` scene. Instead:

1. Emit `game.play.result` via `GameResultBridge`.
2. Remain idle until the next `bridge.play.start`.

Standalone debug builds (`DebugNavigationController`) keep the existing Navigation flow and do not use this protocol.

## Error handling

| Scenario | Behavior |
|----------|----------|
| Malformed envelope JSON | Native or adapter logs; optionally emit `game.play.result` with `failed: true`. |
| `bridge.play.start` before `game.ready` | Flutter should await `game.ready`; native may queue or reject. |
| Overlapping `bridge.play.start` | v1: reject with `game.play.result` (`failed: true`, error describes conflict). |
| Invalid or escaping VFS asset path | Reject with `game.play.result` (`failed: true`, error describes the path issue). |
| Missing required chart/music file | Reject with `game.play.result` (`failed: true`, error describes the missing file). |

## Versioning

- Increment `v` only for breaking envelope or payload changes.
- Additive payload fields (e.g. `assets`) do not require a version bump.
- Dart and engine adapters should reject unknown `v` values.

## Related code

| Location | Role |
|----------|------|
| `../lib/` | Dart protocol models and `CytoidGameCoreClient` |
| `unity-artifacts.md` | Unity artifact setup |
| `Cytoid/engines/unity/Assets/Scripts/Game/GameLaunchBridge.cs` | Unity entry for external launches |
| `Cytoid/engines/unity/Assets/Scripts/Game/GameResultBridge.cs` | Unity result emission |
| `Cytoid/engines/unity/Assets/Scripts/Host/` | Unity adapter |

## Implementation phases

1. **Phase 0** — Dart models and protocol document.
2. **Phase 1** — Plugin channels, mock engine, `bridge.ping` / `game.ready` smoke test.
3. **Phase 2** — Prebuilt Unity artifacts, `bridge.play.start` / `game.play.result` end-to-end. See [unity-artifacts.md](./unity-artifacts.md).
4. **Phase 3** — Build scripts, submodule layout, CI.
5. **Phase 4** — Flutter shell routes, level download, URI-based assets.
