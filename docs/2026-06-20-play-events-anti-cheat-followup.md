# Play Events & Anti-Cheat Follow-up

Status: **informational / roadmap**. No code changes scheduled. Written after the
`feat/play-event-recorder` work landed `GamePlayEventRecorder` + the `playEvents`
field on the game result payload.

## Threat model context

Cytoid Core Unity is an **open-source** client. Any secret embedded in the
binary (HMAC key, private key, salt) can be extracted from the distributed AAR.
Two consequences follow:

1. A client-side signature proves only "Unity signed this payload", **not**
   "this play was genuinely human-played". Memory edits or injected synthetic
   touch events produce a mathematically valid signature.
2. The only integrity that scales in an open-source client is **server-side
   replay re-verification**: server replays `playEvents` against the chart and
   recomputes the score. Client never holds a meaningful secret.

Current threats under consideration:

| Threat | Client signature | Server replay |
|--------|:----------------:|:-------------:|
| Flutter-side JSON tampering (hook the bridge, bump `score`) | partial — defeated by key extraction | — |
| Unity memory edit of `GameState.Score` before emit | no | yes |
| Auto-play not flagged by `usedAutoMod` (external bot) | no | yes (with human-likeness analysis) |
| Fabricated / replayed `playEvents` | no | yes (recompute + consistency) |

## Decision (this round)

**Do not add client-side cryptographic signing of results now.** Server
capabilities are still being planned; building a key-holding client before the
server contract exists is premature. The `feat/play-event-recorder` work is the
*foundation* for server-side replay, and that is the valuable part.

This document records what the client must guarantee for `playEvents` to be a
**reproducible replay input**, so future work does not regress it silently.

## Replay reproducibility gaps in current `playEvents`

The recorder (`engines/unity/Assets/Scripts/Game/GamePlayEvent.cs`) writes
`{t, f, p, x, y}` per sampled touch. Three areas need attention before a server
can deterministically replay a submitted score. **None are blockers for the
telemetry use case that just shipped** — they only matter once the server
actually re-judges.

### 1. Coordinate normalization is screen-relative, not canvas-relative

`x`/`y` are normalized against `Screen.width`/`Screen.height` (raw pixels):

```csharp
x = Normalize(finger.ScreenPosition.x, UnityEngine.Screen.width);
y = Normalize(finger.ScreenPosition.y, UnityEngine.Screen.height);
```

But the judgment field is laid out in a Canvas Scaler
`referenceResolution` space, and the active region is shifted by
`Screen.safeArea` (notch / punch-hole, see `Game/Screens/Overlay/OverlayScreen.cs`).
Two devices with the same aspect ratio but different notch geometry will record
different `x`/`y` for a tap on the *same* note.

**For replay:** the server needs the inverse map. Either
(a) record the normalization basis (screen w/h, safe area, reference resolution)
alongside the events, or (b) normalize in canvas space on the client instead of
screen space. Option (b) is cleaner but changes the wire format and breaks the
data already captured — prefer attaching the basis metadata and keeping the
current encoding.

### 2. Move sampling is lossy

`ShouldSampleMove` keeps a move event only when ≥1/60 s elapsed **and** ≥96 px
traveled since the last sample for that finger:

```csharp
private const float MoveSampleIntervalSeconds = 1f / 60f;
private const int MoveSampleDistance = 96;
```

For tap notes this is fine (down/up are forced). For drag / flick notes whose
judgment depends on the full trajectory, this can drop intermediate points the
server would need to re-judge accurately. **Investigate** whether the down +
sampled moves + up triple is sufficient for the server's drag judge, or whether
drag-class notes need denser sampling (separate `MoveSampleDistance` for
continuous note types, or unconditional sampling while a drag note is active).

### 3. Time base and drift tolerance undefined

`t` is `Mathf.RoundToInt(game.Time * 1000f)` — song time in ms. This is the right
base (deterministic, independent of wall clock). But:

- The real input moment and the recorded `t` differ by touch → frame → audio DSP
  latency. The client applies calibration offsets; the server replay must apply
  the *same* offsets or define an explicit tolerance window.
- `t` is rounded to integer ms. Sub-ms timing is lost. Confirm this matches the
  judgment-window granularity the server will re-apply.

**For replay:** the launch payload already carries calibration offsets; ensure
they travel with the result payload (or are re-asserted server-side) so replay
judgment uses identical timing.

## Cheap transport integrity (optional, low effort)

Independent of server replay, a low-cost mitigation for the "Flutter hook bumps
`score`" script-kiddie threat: have Unity emit a digest of selected result
fields computed via an obfuscated rule, so a Flutter-side patch that only edits
the JSON produces a mismatch. This is **obfuscation, not cryptography** — it
raises the bar from "trivial `frida` script" to "must reverse the digest rule",
which is enough to deter casual tampering. Do not treat it as a real integrity
guarantee; the server-replay path is the guarantee.

Defer until there is evidence of casual tampering, or until the server contract
lands — whichever is first.

## Acceptance criteria for a future anti-cheat PR

When server-side replay is built, the client side must, at minimum:

- [ ] Include normalization basis (screen w/h, safe area, reference resolution)
      in the result payload, OR switch to canvas-space normalization.
- [ ] Confirm drag/flick replay fidelity with the actual sampling rate; densify
      sampling for continuous note types if needed.
- [ ] Ensure calibration offsets accompany the result so replay timing matches.
- [ ] Decide and document the `t` rounding vs. judgment-window relationship.

Until then, `playEvents` is **telemetry and replay raw material only**, not an
anti-cheat input. Do not wire anything that treats its presence as proof of
legitimacy.
