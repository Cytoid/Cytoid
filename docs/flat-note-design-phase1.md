# Flat Note Design — Phase 1 Implementation Spec

Port the vector note design from the `cytoid-design` checkout (expected as a sibling of this repository; the parity generators honour `CYTOID_DESIGN_DIR`)
(`packages/notes`, branch `main`) into Unity Cytoid as an experimental note
style. **Read this doc fully before writing code.** It is the single source of
truth for API contracts, file layout, and verification. The TS sources are the
visual spec — port their math faithfully; where this doc constrains structure,
this doc wins.

## Scope decisions (agreed with product owner)

- NO baked frames, NO scene-graph sync engine. Each Flat renderer explicitly
  owns named part components and updates them per frame — same style as the
  Classic renderers (`ClassicNoteRenderer` et al).
- All note kinds reskinned: click, hold, long-hold, drag head/child, flick,
  drop click/drag (drop body = static capsule), click-drag head/child.
- Click-drag head renders with the **drag-head design + click-drag family
  palette**; its chain-follow behavior is whatever it is today (skin-only
  change). Note: the design repo's default family maps click-drag-head →
  click; we deliberately override that in phase 1.
- Hold head while holding: keep the EXISTING hold FX particle ripple
  (`Game.effectController.holdFx`). Do NOT port hold `press`/`loop` clips.
  The `hold-progress` ring clip is also NOT ported — progress shows in the
  new hold body bar instead.
- Hold vertical progress bar (new design): port from `bodies.ts holdBody`.
- Long-hold rail: port from `bodies.ts longHoldBody` including the final
  0.6 s collapse.
- Clear/miss effects: port from `effects.ts` + per-kind extras, all grades,
  including flick directional swipe arrows and drop capsule clears.
- Judgement TEXT: explicitly out of scope (a later phase adds it behind its
  own toggle).
- Drag chain connector lines: keep the existing `DragLineElement` unchanged.
- Chart-level and per-note color overrides are honored: the flat palette is
  always derived from the resolved seed color (see Palette below), with an
  override cache.

## Architecture

### Layers

1. **Pure C# math** (no `UnityEngine` references — must compile standalone):
   - `Flat/Palette/FlatColor.cs` — Rgba struct, sRGB↔OKLCH, gamut mapping, mix()
   - `Flat/Palette/FlatEase.cs` — port of `core/ease.ts`
   - `Flat/Palette/FlatTokens.cs` — port of `tokens.ts` constants (sizes,
     stroke widths, timing, size curves)
   - `Flat/Palette/FlatPalette.cs` — port of `palette.ts` (derive/create/tone/
     ringTone) + seed cache
   - `Flat/Mesh/FlatMeshBuilder.cs` — pure vertex/index/color accumulator
   - `Flat/Mesh/FlatGeom.cs` — pure geometry generators (disk, ring, arc,
     rounded rect, poly/line stroke with caps/joins, dashed line)
2. **Unity glue** (thin):
   - `Flat/Mesh/FlatNoteMesh.cs` — MonoBehaviour owning MeshFilter+MeshRenderer;
     converts builder output (design px, +y down) to world (y-flip, scale) and
     uploads; two meshes per object: normal blend + additive.
3. **Renderers** (mirror the Classic family):
   - `Flat/FlatNoteRendererBase.cs` : NoteRenderer
   - `Flat/Flat{Click,Hold,LongHold,DragHead,DragChild,Flick,DropClick,DropDrag}NoteRenderer.cs`
   - `Flat/FlatHoldBody.cs`, `Flat/FlatLongHoldRail.cs`
   - `Flat/FlatClearFx.cs` (+ pool in `Flat/FlatClearFxPool.cs`)

### Coordinate convention

All design math runs in design pixels: origin = note centre, **+y down**,
angles in radians, **0 = 12 o'clock, clockwise positive** (exactly
`core/scene.ts`). The ONLY place conversion happens is `FlatNoteMesh` upload:
world position = `new Vector3(x, -y, zBias) * worldScale`, rotation of whole
note GO is left to the existing Note machinery (drag heads already rotate
toward the next node — draw arrows at heading 0).

### Per-note mesh model

Each note gets ONE child GameObject ("FlatMesh") with `FlatNoteMesh`. All
parts of the head (+ hold body parts for hold notes) write into one
`FlatMeshBuilder` each frame; `FlatNoteMesh` uploads once → 1 draw call per
note, one shared vertex-color unlit transparent material. Within the mesh,
parts order back-to-front with small negative zBias steps (design draws
bodies below heads; keep per-part z ordering inside the builder). Additive
parts (only some clear-fx flashes) go to the second (additive) mesh.

- Whole-mesh rebuild per frame is fine during enter/clear (hundreds of verts).
  While holding and nothing changes, skip rebuild (dirty flag).
- Per-note opacity (see below) multiplies every vertex alpha → rewrite colors
  only when alpha changed.
- `renderer.sortingOrder` mirrors Classic conventions (below).

### Design px ↔ world

`worldScale = Game.Config.NoteTransformSizes[NoteType.Click] * BaseTransformScale / FlatTokens.Unit`
where `BaseTransformScale` is computed exactly like `ClassicNoteRenderer.OnNoteLoaded`
(chart size × GlobalNoteSizeMultiplier × per-note size override). Part
geometry is authored relative to `ctx.Size = FlatTokens.Unit * FlatTokens.Size[kind]`
(design px) — the generators never see world units.

Note overall scale during enter = design size curve (`FlatTokens.SizeCurve`),
NOT Classic's linear interpolation. `Model.initial_scale` blends on top:
`scale = initial_scale + (1 - initial_scale) * curve01(p)` where `curve01`
normalizes the design curve's spawn→1 range into 0→1. Apply as uniform mesh
scale. Default charts have initial_scale == 1 → pure design behavior.

### Palette (decided)

The palette is **always derived at runtime from the resolved seed color** —
direction (up/down) disappears as a concept because Cytoid's
`GetFillColor(note)` already encodes it (player normal/alt settings + chart
fill_colors). Per note:

```
seed = first non-clear of:
  1. Note.Model.Override.FillColor (per-note storyboard override)
  2. Game.Config.GetFillColorOverride(note)   (chart-level override)
  3. Game.Config.GetFillColor(note)           (player setting / chart mapping)
```

- Ring/ink derive from the seed via `derivePalette` (port of palette.ts).
- `grades` = `Game.Config.NoteGradeEffectColors` (already the same defaults
  as `tokens.grade`).
- Cache: `FlatPaletteCache` keyed by (family, seedRgbaPacked) living one game
  session; cleared in `GameConfig.OnGameLoaded`. Family from note type via the
  same mapping as `NoteColorChartOverrideMapping` (click/click-drag-head →
  click; drag-head/child → drag; click-drag-child → click-drag; hold → hold;
  long-hold → long-hold; flick → flick; drop-click → drop-click; drop-drag →
  drop-drag). Phase 1 note: family only affects DEFAULT hue — when seeded
  from a resolved color the ramp derives from that color, so family is
  effectively just a cache key + the default-seed path.
- Classic also has ring-color settings (`NoteRingColors`) — phase 1 flat
  ignores them and uses `palette.ring` (#FAFAFA) unless the design exposes a
  ring seed later.
- `UseFillColorForDragChildNodes == false` → Classic renders drag children in
  the RING color (white); mirror this: seed from `GetFillColor` result as-is
  (it already returns ring color in that case).

### FlatNoteRendererBase contract

Ported from ClassicNoteRenderer, with flat differences:

- ctor(note): base(note); create the FlatNoteMesh child; build part list.
- `OnNoteLoaded()`: compute BaseTransformScale / BaseTransformSize /
  BaseFillColor seed + resolve palette (cache); reset state.
- `Render()` (called from OnLateUpdate): mirror Classic's structure —
  component states, colors/palette updates, transform scale (design curve),
  opacity, collider (copy UpdateCollider verbatim logic), then build geometry.
- Opacity: port `UpdateComponentOpacity` math exactly (chart opacity, model
  opacity, 2× intro ramp, GlobalNoteOpacityMultiplier, Override.OpacityMultiplier).
  Multiply into all vertex alphas.
- Visibility window: `intro_time .. end_time + MissThreshold`, HideNotes mod,
  DragStackFollower → all parts hidden. NOTE: `Note.SetDragStackVisualsEnabled`
  only disables SpriteRenderer/SpriteMask — extend it in `Note.cs` to also
  disable `FlatNoteMesh` (one-line change, integration stage).
- Sorting: base = `(Note.Chart.note_list.Count - Note.Model.id) * 3` (same as
  Classic Ring). Head mesh sortingOrder = base; hold body mesh parts render in
  the same mesh with zBias (body behind head); FlatClearFx sortingOrder =
  base + 2 (above head, Classic FX uses similar layering).
- `OnClear(grade)`: hide head parts, spawn `FlatClearFx` (pooled) at the
  correct anchor (see below). Do NOT call `Game.effectController.PlayClearEffect`.
  Hit sounds etc. remain Note.Clear's business — untouched.
- `Dispose()`: destroy created GameObjects (mirror Classic hygiene).
- Clear FX anchor: normal kinds = note position at clear; hold = hold END
  position (mirror how `EffectController.PlayClearEffect` places hold FX —
  read it); long-hold = same rule as EffectController uses for LongHold.
  Miss = note position.
- Hold/LongHold renderer extras: bodyDirection via the existing
  `ClassicHoldNoteRenderer.ComputeLineFlipY(note, page)` (make that method
  public static reuse — it already is). `HoldNote.IsHolding`/`HoldProgress`,
  `Model.holdlength` drive the bar. Keep existing `holdFx` particles while
  holding (instantiate like ClassicHoldNoteRenderer does). Ignore
  `Model.style` (1/2) — flat renders the design's single style. No
  ProgressRing, no MeshTriangle, no SpriteMask in flat.

### FlatClearFx

One pooled component (`FlatClearFxPool`, sized generously e.g. 32, expandable)
playing one effect then returning to pool. Port `effects.ts` (grade-colored
burst: shock ring, flash, sector ring, shards, kind extras: flick
streaks/swipe arrows with direction, capsule clears for drops, beam/second
ring for hold/long-hold) as explicit part updates over t (seconds). Grades
from `Game.Config.NoteGradeEffectColors`. It owns its own FlatNoteMesh(es).
Per-kind flavor parameters mirror `effects.ts` (read the per-kind clear
definitions — e.g. flick passes swipe direction; the note renderer supplies
kind + heading/rotation as done in the TS `DrawContext`). Duration per grade
from `FlatTokens.Time.Clear`.

### Geometry generators (FlatGeom) — required set

All pure C#, writing into `FlatMeshBuilder` (positions in design px, +y down;
per-vertex Rgba; additive flag separate):

- disk(r, color)
- ring(r, width, color) — annulus
- arc(r, start, end, width, color, cap butt|round) — tessellated annulus
  sector; round caps = semicircle fans at ends
- roundedRect(x, y, w, h, rx, color)
- polyStroke(points, closed, width, color, join miter|round|bevel, cap) —
  used by chevrons, diamonds (closed, miter), flick edges, judgement-style
  strokes
- line(x1,y1,x2,y2, width, color, cap)
- dashedLine(x1,y1,x2,y2, width, color, dash[] px, dashOffset px, cap) —
  used by hold/long-hold conveyor (scrolling via dashOffset)
- polyFill(points, color)

Quality bar: smooth arcs (segment count adaptive: max(12, ceil(r/2px))),
correct miter bevel clamping, dash pattern exact at phase 0, zero allocation
per call in steady state (builders are pooled/reset).

### Settings & wiring (integration stage)

- `LocalPlayerSettings`: `[JsonProperty("use_experimental_note_design")] public bool UseExperimentalNoteDesign { get; set; } = false;`
- `SettingsFactory.InstantiateDeveloperSettings`: add a toggle row next to
  `UseExperimentalNoteAnimations` (copy that row's construction, ~line 512).
- `Game.cs` (~line 293): pass `Context.Player.Settings.UseExperimentalNoteAr && !Context.Player.Settings.UseExperimentalNoteDesign` into the Chart ctor
  (mutual exclusion — flat design forces Note AR off).
- `GameConfig`: `public bool UseFlatNoteDesign;` set in `OnGameLoaded` from the setting.
- Six `CreateRenderer` sites (ClickNote, HoldNote, LongHoldNote, DragHeadNote,
  DragChildNote, FlickNote): `if (Game.Config.UseFlatNoteDesign) return new FlatXxxNoteRenderer(this);` before the Classic ternary.
- Drop notes: inspect `DropClickNoteRenderer`/`DropDropDragNoteRenderer`
  creation path (they don't check UseClassicStyle today); give them the same
  flat dispatch (flat variants subclass/reuse their falling logic).
- `GamePlayRecord`: mirror the `ExperimentalNoteAr` field (line ~385) with an
  `ExperimentalNoteDesign` field — check `GamePlayRecordWire` for the schema
  pattern and version handling; follow it exactly.

### Verification (mandatory for pure-math layers)

`Tools/FlatParity/` — a net9.0 console project OUTSIDE Assets (Unity never
compiles it) that references the pure C# sources via `<Compile Include>`
links and runs numeric parity checks:

- `golden.ts` (run with `cd <cytoid-design>/packages/notes && npx tsx <this-repo>/Tools/FlatParity/golden.ts`)
  imports the design TS sources and dumps `golden.json`: OKLCH roundtrips,
  derived ramps for the 8 default family×direction seeds + 3 hex seeds
  (#35A7FF, #FF5964, #39E59E), tone() at fractional steps, ringTone samples,
  ease curve samples, sizeCurve samples.
- `Program.cs` loads golden.json and asserts C# outputs match within 1e-4
  (colors) / 1e-6 (scalars). Geometry kit: property tests (arc segment
  counts, dash duty/phase at offset 0, ring area ≈ 2πr·width, miter
  endpoint math) + committed C#-generated geometry snapshots.
- Run: `dotnet run --project Tools/FlatParity` — MUST pass before merge of
  each stage.

Code style: match the existing repo — global namespace (no `namespace`
declarations), 4-space indent, `var` where the repo uses it, no braces on
single statements where the repo omits them, file-scoped usings at top.
Comment only non-obvious constraints; ported functions may carry the TS
doc comment's intent in short form. Never reference UnityEngine from
Palette/ or Mesh/ pure files (FlatNoteMesh.cs excepted — it lives in Mesh/
but is the boundary).

## Appendix B — geometry API

Already fixed in the stub files `Flat/Mesh/FlatMeshBuilder.cs` and
`Flat/Mesh/FlatGeom.cs` (signatures there are the contract). Builder vertex
stride: x,y,z,r,g,b,a (design px, +y down, z = depth bias, 0 is fine — within
one mesh, part order = triangle emission order since the transparent queue
renders in order with ZWrite off).

## Appendix C — FlatClearFx pooling & placement

- `FlatClearFxPool`: lazily-created `SingletonMonoBehavior` under
  `Game.contentParent` (same parent `MeshTriangle` uses), prewarm 24, expand
  on demand, `OnCollect`-style reset between plays. Do NOT extend the classic
  `ObjectPool.Effect` enum.
- Placement mirrors `EffectController.PlayClearEffect`: at
  `Note.transform.position`, EXCEPT hold/long-hold with `Model.style == 1`
  → scanner Y (`Scanner.Instance.transform.position.y`).
- Effect lifetime = design durations (`FlatTokens.Time.Clear*` per grade);
  grades color from `Game.Config.NoteGradeEffectColors`. The classic
  speed-multiplier ladder is NOT applied (design durations already differ per
  grade).
- Scale: effect geometry is authored in the kind's design px via
  `ctx.Size`, uploaded with the note's `worldScale` — no classic
  GlobalNoteSizeMultiplier ladders.
- HideNotes mod: suppress spawning entirely (classic FX still spawn under
  HideNotes? — check; if classic suppresses, mirror).

## Appendix D — renderer-family conventions (Stage 2 agents)

- `FlatNoteMesh` (written, in `Flat/Mesh/FlatNoteMesh.cs`): `Create(parent,
  name, additive, sortingOrder)`, `SetWorldScale`, `Upload(FlatMeshBuilder)`,
  `SetVisible`, `SetSortingOrder`, `Dispose`. One per blended group per note.
- Every Flat renderer: check `Note.IsDragStackFollower` in Render and hide
  all meshes when set (Classic relies on UpdateComponentStates for this; the
  SetDragStackVisualsEnabled walk only disables SpriteRenderers, so the
  Render-level check is mandatory for flat).
- Global namespace, no `namespace` declarations anywhere in Flat/.
- Enter-progress `p` = `Mathf.Clamp((Game.Time - Model.intro_time) /
  (Model.start_time - Model.intro_time), 0, 1)` — same clamp Classic uses.
- Miss: on `OnClear(Miss)` play the miss effect and hide the head (no ghost
  head persists; the effect carries the visual).

## Appendix A — pure-math API contract (fixed; parity checker depends on it)

```csharp
// Flat/Palette/FlatColor.cs
public readonly struct Rgba { public readonly float R, G, B, A;
    public Rgba(float r, float g, float b, float a = 1f); }
public readonly struct Oklch { public readonly double L, C, H; }
public static class FlatColor {
    public static Rgba ParseHex(string hex);   // 3/4/6/8-digit, leading '#' optional
    public static string ToHex(Rgba c);        // "#rrggbb"
    public static Oklch RgbToOklch(Rgba c);
    public static Rgba OklchToRgb(Oklch c);    // chroma-reduction gamut mapping, L/h kept
    public static Rgba Mix(Rgba a, Rgba b, double t); // linear sRGB
}

// Flat/Palette/FlatEase.cs  (names mirror core/ease.ts, PascalCase)
public static class FlatEase {
    public static double Clamp(double v, double a = 0, double b = 1);
    public static double Clamp01(double v);
    public static double Lerp(double a, double b, double t);
    public static double Seg(double t, double a, double b);
    public static double Bump(double t, double a, double b);
    public static double Linear(double t);   public static double InQuad(double t);
    public static double OutQuad(double t);  public static double OutSine(double t);
    public static double InSine(double t);   public static double InOutQuad(double t);
    public static double InCubic(double t);  public static double OutCubic(double t);
    public static double InOutCubic(double t); public static double OutQuart(double t);
    public static double OutExpo(double t);  public static double InExpo(double t);
    public static double OutBack(double t, double s = 1.70158);
    public static double Ranged(double t, double a, double b, Func<double, double> ease);
    public static Func<double, double> CubicBezier(double x1, double y1, double x2, double y2);
    public readonly struct RiseSettle { public readonly double Spawn, Peak, PeakP, SettleP; }
    public static Func<double, double> RiseSettleCurve(RiseSettle curve, (double, double) s);
    public struct Rng { public Rng(uint seed); public double Next(); } // mulberry32
}

// Flat/Palette/FlatTokens.cs  (constants from tokens.ts)
public static class FlatTokens {
    public const double Unit = 128;
    public static double Size(NoteKind kind);        // tokens.size
    public static double StrokeRing = 0.085;  ...    // stroke block
    public static class Time { ... enter/holdPress/holdLoop/clear durations }
    public static FlatEase.RiseSettle SizeCurve(SizeKind kind); // click/hold/dragHead/dragChild
    public static (double, double) SizeCurveShape;   // tokens.sizeCurve.s
    // Grade colors are NOT token constants — they come from GameConfig at runtime.
}

// Flat/Palette/FlatPalette.cs
public enum FlatFamily { Click, Hold, LongHold, Flick, Drag, ClickDrag, DropClick, DropDrag }
public enum FlatDirection { Up, Down }
public sealed class FlatPalette {
    public Rgba Shade(int step);      // 50..950, exact steps verbatim
    public Rgba Tone(double step);    // piecewise-linear OKLCH, shortest-path hue
    public Rgba RingTone(double k);   // neutral 0.708 L spawn -> ring color
    public Rgba RingColor; public Rgba Ink; public double Hue;
}
public static class FlatPaletteDerive {
    public static FlatPalette FromHue(double hue);        // fitLightness + tokens.chroma
    public static FlatPalette FromSeed(Rgba seed);        // hex branch of derivePalette
    public static FlatPalette Default(FlatFamily f, FlatDirection d); // tokens.hue table
}
```

The TS→C# name mapping in tone/ringTone: `palette[a]` exact-step returns
must return the SAME hex as `oklch(...)` produced (round-trip through
OklchToRgb — do not store hex strings, store the derived Rgba per shade).

