using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class FlatNoteRendererBase : NoteRenderer
{
    public static readonly Dictionary<(FlatFamily Family, int SeedPacked, int RingPacked), FlatPalette> FlatPaletteCache =
        new Dictionary<(FlatFamily, int, int), FlatPalette>();

    private static Game paletteGame;
    private static SkinPainter skinPainter;
    private readonly FlatMeshBuilder geometry = new FlatMeshBuilder();
    private float uploadedOpacity = -1;
    private FlatPalette builtPalette;
    private bool geometryDirty = true;
    private int headVertexCount;

    protected readonly FlatNoteMesh HeadMesh;
    protected float BaseTransformScale;
    protected float BaseTransformSize;
    protected Color BaseFillColor;
    protected FlatPalette Palette;
    protected float EasedOpacity;
    protected double ApproachSeconds => Note.Model.start_time - Note.Model.intro_time;
    protected float EnterProgress;
    protected float WorldScale;
    protected int SortingOrder;

    /// <summary>Design px → world for linked visuals (drag chain links ride the source
    /// head's scale, so chain and head meet edge to edge).</summary>
    public float LinkWorldScale => WorldScale;

    /// <summary>Current eased opacity of this note's visuals; chain links multiply it
    /// so the connection fades with its source head.</summary>
    public float LinkOpacity => EasedOpacity;
    protected bool HeadVisible;
    protected readonly bool DisplayNoteId;
    protected readonly NoteId NoteId;
    protected bool Missed { get; private set; }
    protected bool ClearedSuccessfully => Note.IsCleared && !Missed;
    protected virtual bool CanExit => true;
    // Drops exit too now — the exit fade IS their miss (the fade rides the note
    // past the line; see DropNoteOffset.ComputeRideLocalOffset).
    protected virtual bool Exiting => Note.IsCleared ? Missed : CanExit && Game.Time > Note.Model.start_time;
    protected virtual double ExitAnchor => Note.Model.start_time;
    /// <summary>Exit-clock compression: drops riding a fast fall compress their exit
    /// clock so the fade completes exactly at the travel cap (DropNoteOffset.ExitClockScale).
    /// 1 for every other kind.</summary>
    protected virtual double ExitClockScale => 1;
    protected double ExitTime => ((double) Game.Time - ExitAnchor) * ExitClockScale;
    protected ExitCurve HeadExitCurve => Kind == NoteKind.DropClick || Kind == NoteKind.DropDrag
        ? FlatTokens.Time.Exit.Drop
        : Kind == NoteKind.Hold || Kind == NoteKind.LongHold
            ? FlatTokens.Time.Exit.HoldHead
            : Kind == NoteKind.DragChild || Kind == NoteKind.ClickDragChild
                ? FlatTokens.Time.Exit.DragChild : FlatTokens.Time.Exit.Tap;

    protected virtual NoteKind Kind => KindFor(Note.Type);
    protected virtual FlatFamily Family => FamilyFor(Note.Type);
    protected double DesignSize => FlatTokens.Unit * FlatTokens.Size(Kind);
    // Clear-FX world rotation override (Unity Z degrees) for directional flick
    // swipes; null keeps the note's rotation.
    protected virtual float? FlickClearRotationDegrees => null;
    protected virtual Quaternion ExitLocalRotation => Note.transform.localRotation;

    protected FlatNoteRendererBase(Note note) : base(note)
    {
        // Note.Initialize creates the renderer before SetData supplies Chart/Model.
        HeadMesh = FlatNoteMesh.Create(Note.transform, "FlatMesh", false, 0);
        HeadMesh.SetVisible(false);
        foreach (var sprite in Note.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = false;
        foreach (var mask in Note.GetComponentsInChildren<SpriteMask>(true)) mask.enabled = false;
        // Generate note ID — same gate as ClassicNoteRenderer. Created after the sprite
        // sweep so the prefab's own visuals keep their enabled state.
        if (Game.Config.DisplayNoteIds)
        {
            DisplayNoteId = true;
            NoteId = Object.Instantiate(GameObjectProvider.Instance.noteIdPrefab, Note.transform);
            NoteId.Visible = true;
            NoteId.gameObject.SetActive(false);
        }
    }

    // One warn when a skin slot a renderer needs is missing or capability-
    // unsupported: the flat path renders nothing for that part — no classic
    // fallback. Shared with DragLineElement's chainLink slot.
    private static bool warnedSlotMissing;
    internal static void WarnSlotMissingOnce(NoteKind kind, string slot)
    {
        if (warnedSlotMissing) return;
        warnedSlotMissing = true;
        Debug.LogWarning($"[FlatSkinRuntime] slot '{slot}' unavailable for {kind}; it will not draw");
    }

    public override void OnNoteLoaded()
    {
        BaseTransformScale = (float) Note.Chart.size * Game.Config.GlobalNoteSizeMultiplier;
        if (Note.Model.size != double.MinValue) BaseTransformScale *= (float) Note.Model.size;
        BaseTransformSize = Game.Config.NoteTransformSizes[Note.Type] * BaseTransformScale;
        WorldScale = Game.Config.NoteTransformSizes[NoteType.Click] * BaseTransformScale / (float) FlatTokens.Unit;
        SortingOrder = (Note.Chart.note_list.Count - Note.Model.id) * 3;
        HeadMesh.SetSortingOrder(SortingOrder);
        HeadMesh.SetWorldScale(WorldScale);
        HeadMesh.SetVisible(false);
        // Reset the pooled pose; UpdateTransformScale cancels its base size on the mesh only.
        Note.transform.localScale = Vector3.one;
        HeadMesh.transform.localScale = Vector3.one;
        BaseFillColor = ResolvePaletteSeed(Note);
        Palette = ResolvePalette(Note, Family);
        EnterProgress = EasedOpacity = 0;
        HeadVisible = false;
        Missed = false;
        ResetGeometry();
        if (DisplayNoteId) NoteId.SetModel(Note.Model);
    }

    protected override void Render()
    {
        UpdateComponentStates();
        UpdateColors();
        UpdateTransformScale();
        UpdateComponentOpacity();
        UpdateCollider();
        // A slot draw that threw latches skinBroken: the head stays hidden for this
        // note instance (collected notes reset it) — there is no imperative
        // geometry to fall back to.
        if (skinBroken)
        {
            HeadMesh.SetVisible(false);
            if (additiveHeadMesh != null) additiveHeadMesh.SetVisible(false);
            return;
        }
        if (!HeadVisible) return;

        var introSpan = Note.Model.start_time - Note.Model.intro_time;
        skinEnterElapsed = Game.Time - Note.Model.intro_time;
        skinEnterProgress = introSpan > double.Epsilon
            ? Mathf.Clamp((float) ((Game.Time - Note.Model.intro_time) / introSpan), 0f, 1f)
            : 1f;
        var skinDriver = SkinHeadDriver(FlatSkinRuntime.Current);

        // Steady poses (held heads, clamped progress) skip both rebuild and upload
        // until the driver, palette or an invalidation changes. The skin owns the
        // head's size curve, fade-in and exit playback.
        var rebuild = geometryDirty || builtSkinDriver != skinDriver || builtPalette != Palette;
        if (rebuild)
        {
            geometry.Reset();
            // The additive sink composes within ONE DrawSkinHead call (hold
            // press/loopUnder); across rebuilds it must start empty — holds with a
            // loopUnder rebuild every frame, so a missing reset accumulates layers
            // (ghosting, overdraw, unbounded vertex growth) until recycle.
            additiveGeometry.Reset();
            // Subclass slot composition draws inside this try: any failure hides
            // this note's head exactly once, at the catch site — never per frame.
            try { DrawSkinHead(geometry); }
            catch (Exception e)
            {
                skinBroken = true;
                HeadMesh.SetVisible(false);
                if (additiveHeadMesh != null) additiveHeadMesh.SetVisible(false);
                Debug.LogWarning($"[FlatSkinRuntime] {Kind} head slot draw failed, head hidden: {e.Message}");
                return;
            }
            headVertexCount = geometry.VertCount;
            // NO REALWORLD TEST: additive head sink (reset rides the rebuild cadence).
            if (additiveGeometry.VertCount > 0 && additiveHeadMesh == null)
            {
                additiveHeadMesh = FlatNoteMesh.Create(Note.transform, "FlatMeshAdd", true, SortingOrder + 1);
                additiveHeadMesh.SetWorldScale(WorldScale);
                additiveHeadMesh.SetVisible(false);
            }
            builtSkinDriver = skinDriver;
            builtPalette = Palette;
            geometryDirty = false;
        }
        if (!rebuild && uploadedOpacity == EasedOpacity) return;

        // Fused opacity + design→world transfer. The geometry builder keeps source
        // alphas: a steady head only re-uploads when its opacity changes — no
        // intermediate copy.
        using (FlatSkinRuntime.UploadCopyMarker.Auto())
        {
            HeadMesh.Upload(geometry, EasedOpacity, EasedOpacity, headVertexCount);
            // NO REALWORLD TEST: additive head upload (same opacity envelope).
            if (additiveHeadMesh != null)
            {
                if (additiveGeometry.VertCount > 0)
                    additiveHeadMesh.Upload(additiveGeometry, EasedOpacity, EasedOpacity, additiveGeometry.VertCount);
                additiveHeadMesh.SetVisible(HeadVisible && additiveGeometry.VertCount > 0);
            }
        }
        uploadedOpacity = EasedOpacity;
    }

    // Skin-mode head: enter or exit slot, in design px at scale 1. Render() owns
    // the try/catch — a throwing slot draw hides the head for this note exactly
    // once, at the DrawSkinHead call site.
    private bool skinBroken;
    private float skinEnterProgress;

    /// <summary>Unclamped seconds since the note's intro began — the driver for Loop enters
    /// (drops), whose phase must keep advancing after the clamped progress saturates.</summary>
    private double skinEnterElapsed;
    private double builtSkinDriver = double.NaN;
    private SkinDrawContext skinContext;

    // NO REALWORLD TEST: additive sink for head slots using blend:add (flat has
    // none). Lazily created on first use; renders above the vector head at
    // sorting order +1 — emission interleaving between the two meshes is lost,
    // the same documented deviation as the FX additive mesh.
    protected FlatNoteMesh additiveHeadMesh;
    protected readonly FlatMeshBuilder additiveGeometry = new FlatMeshBuilder();

    protected static SkinPainter SkinPainterInstance => skinPainter ??= new SkinPainter();

    // Slot driver: seconds into the exit, the 0..1 enter progress — or, for a
    // loop enter (drops), the loop's phase clock. This value doubles as the
    // rebuild-cache key (builtSkinDriver), so the draw and the cache must ride
    // one clock — 3db59e34 moved only the draw clock onto elapsed seconds and
    // the mesh froze once the clamped progress saturated at 1. A positive
    // period cycles, so the clock is the unclamped seconds since the intro
    // began; a frozen period (<= 0) samples progress 0 forever
    // (SkinSlots.SlotProgress), so a constant driver lets the cache go steady
    // instead of re-uploading identical geometry every frame. Holds override
    // this while holding to drive their press/loopUnder composition.
    protected virtual double SkinHeadDriver(CompiledSkin skin)
    {
        if (Exiting) return ExitTime;
        var enter = EnterSlot(skin);
        if (enter is { UsesUnsupported: false, UsesImages: false } && enter.Contract.Mode == SkinClipMode.Loop)
            return enter.Period > 0 ? skinEnterElapsed : 0;
        return (double) skinEnterProgress;
    }

    private CompiledSkin enterSlotSkin;
    private CompiledSkinSlot enterSlot;

    /// <summary>The skin's enter slot, cached per skin instance. The driver doubles
    /// as the rebuild-cache key and is checked every frame, and Slot()'s lookup
    /// builds a key string per call — reference-checking the skin keeps the
    /// steady path allocation-free. Skin swaps compile a new instance
    /// (SwapCompiledSkin), so the guard re-resolves exactly when needed — and
    /// invalidates the built geometry: two skins can agree on driver AND palette
    /// (e.g. both loop enters frozen at driver 0) yet paint entirely different
    /// frames, so the reference change itself must force one rebuild.</summary>
    protected CompiledSkinSlot EnterSlot(CompiledSkin skin)
    {
        if (!ReferenceEquals(enterSlotSkin, skin))
        {
            enterSlotSkin = skin;
            enterSlot = skin.Slot(Kind, "enter");
            InvalidateGeometry();
        }
        return enterSlot;
    }

    // Skin-mode head: enter or exit slot, in design px at scale 1. No try/catch
    // here — Render() wraps the call site so subclass overrides never repeat it.
    protected virtual void DrawSkinHead(FlatMeshBuilder b)
    {
        var skin = FlatSkinRuntime.Current;
        // The exit fetch only runs inside rebuilds; the enter slot rides the
        // per-skin cache (shared with the per-frame driver check).
        var slot = Exiting ? skin.Slot(Kind, "exit") : EnterSlot(skin);
        // No image sink here: image slots gate to classic via CanPaintHead above;
        // this repeat check keeps a swapped-in skin from dropping quads silently.
        if (slot == null || slot.UsesUnsupported || slot.UsesImages)
        {
            WarnSlotMissingOnce(Kind, Exiting ? "exit" : "enter");
            return;
        }
        using (FlatSkinRuntime.PaintMarker.Auto())
        {
            SkinPainterInstance.DrawNoteSlot(slot, SkinHeadDriver(skin), SkinHeadContext(), new SkinSinks(b));
        }
    }

    /// <summary>Per-renderer reusable draw context (filled in place — the per-frame
    /// hot path allocates nothing). Detached exits snapshot via HeadContextOf.</summary>
    protected SkinDrawContext SkinHeadContext()
    {
        using var _ = FlatSkinRuntime.BindingMarker.Auto();
        var alternative = Note.Model.UseAlternativeColor();
        var palettes = FlatSkinRuntime.PaletteContextOf(Game, Family,
            alternative ? FlatDirection.Down : FlatDirection.Up,
            BaseFillColor, Game.Config.NoteRingColors[Note.Type][0]);
        skinContext ??= new SkinDrawContext();
        FlatSkinRuntime.FillHeadContext(skinContext, Kind, palettes, alternative, ApproachSeconds);
        return skinContext;
    }

    // Signed press time minus chart time in seconds (positive = late) for the
    // FAST/SLOW label. Holds override this with the head's press time.
    protected virtual double TimingError() => Game.Time - (Note.Model.start_time + Note.JudgmentOffset);

    // Skin-mode exit capture: the exit slot is a pure function of (slot, driver,
    // context) — the detached visual's p (rewind progress) converts back to seconds.
    // This is THE head capture for collection-surviving exits; there is no
    // imperative capture anymore.
    private Action<FlatMeshBuilder, float, float> CaptureSkinExitHead()
    {
        var slot = FlatSkinRuntime.Current?.Slot(Kind, "exit");
        if (slot == null || slot.UsesUnsupported || slot.UsesImages) return null;
        var approach = Math.Max(1e-6, ApproachSeconds);
        // Independent snapshot: the renderer's cached context is pooled and reused.
        var context = FlatSkinRuntime.HeadContextOf(Note, Family, SkinHeadContext().Palettes, ApproachSeconds);
        var painter = skinPainter;
        if (painter == null) return null;
        return (b, p, scale) => painter.DrawNoteSlot(slot,
            (1 - (double) p) * approach / SkinTimingTokens.Note.ExitRewind, context, new SkinSinks(b));
    }
    protected virtual Action<FlatMeshBuilder, float> CaptureExitBody() => null;
    // Sorting layer of the live and detached exit body; 0 is Default. Holds put
    // theirs on classic's hold-line layers, beneath the chain layer.
    protected virtual int BodySortingLayerId => 0;

    protected void InvalidateGeometry() => geometryDirty = true;

    protected virtual void UpdateComponentStates()
    {
        // Once exiting, the visual window belongs to the curve, not gameplay's miss deadline.
        var inWindow = Exiting ? FlatEase.ExitAlpha(ExitTime, HeadExitCurve) > 0 :
            Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold;
        HeadVisible = !Note.IsDragStackFollower && (!Note.IsCleared || Missed) && inWindow &&
                      !Game.State.Mods.Contains(Mod.HideNotes);
        HeadMesh.SetVisible(HeadVisible);
        if (additiveHeadMesh != null && !HeadVisible) additiveHeadMesh.SetVisible(false);
        UpdateNoteId();
    }

    // Classic's NoteId window: the note's own click lifecycle, independent of the
    // Flat exit curves. Drag-head renderers override this with the chain window.
    protected virtual bool NoteIdVisible => !Note.IsCleared &&
        Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold;

    // Mirrors ClassicNoteRenderer's NoteId rules: stack-follower and HideNotes
    // suppression, and per-frame counter-rotation so the label stays upright
    // while the note transform turns toward its next node.
    protected virtual void UpdateNoteId()
    {
        if (!DisplayNoteId) return;
        NoteId.Visible = true;
        if (Note.IsDragStackFollower || !NoteIdVisible || Game.State.Mods.Contains(Mod.HideNotes))
        {
            NoteId.gameObject.SetActive(false);
            return;
        }
        NoteId.gameObject.SetActive(true);
        NoteId.transform.localEulerAngles = new Vector3(0, 0, -Note.transform.localEulerAngles.z);
    }

    protected virtual void UpdateColors()
    {
        BaseFillColor = ResolvePaletteSeed(Note);
        Palette = ResolvePalette(Note, Family);
    }

    protected virtual void UpdateTransformScale()
    {
        EnterProgress = Exiting ? (float) FlatEase.ExitRewind(ExitTime, ApproachSeconds) :
            Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time), 0f, 1f);
        // The skin's enter/exit slots own the size curve (their transform.scale
        // tracks); storyboard SizeMultiplier still applies through the transform.
        var transformSize = BaseTransformSize * Note.Model.Override.SizeMultiplier;
        // Keep Classic's parent scale for the collider and hold particles. Cancel the base
        // size on the head because FlatNoteMesh already uploads vertices in world units.
        Note.transform.localScale = new Vector3(transformSize, transformSize, Note.transform.localScale.z);
        var meshScale = BaseTransformSize != 0 ? 1 / BaseTransformSize : 1;
        HeadMesh.transform.localScale = new Vector3(meshScale, meshScale, 1);
    }

    protected virtual void UpdateComponentOpacity()
    {
        var maxOpacity = (float) Note.Chart.opacity;
        if (Note.Model.opacity != double.MinValue)
        {
            maxOpacity = (float) Note.Model.opacity;
        }

        // The skin's enter animation carries its own fade-in track; the engine's
        // 2× intro ramp would double it.
        EasedOpacity = maxOpacity;
        EasedOpacity *= Game.Config.GlobalNoteOpacityMultiplier;
        EasedOpacity *= Note.Model.Override.OpacityMultiplier;
    }

    protected virtual void UpdateCollider()
    {
        if (Note.IsDragStackFollower)
        {
            Collider.enabled = false;
            return;
        }

        Collider.enabled = Game.Time >= Note.Model.intro_time && Game.Time <= Note.Model.end_time + Note.MissThreshold;

        var radius = Note.Game.Config.NoteHitboxSizes[Note.Type];
        if (Note.Model.hitbox != double.MinValue) radius *= (float) Note.Model.hitbox;
        radius *= Note.Model.Override.SizeMultiplier;
        radius *= Note.Model.Override.HitboxMultiplier;
        Collider.radius = radius;
    }

    public override void OnClear(NoteGrade grade)
    {
        base.OnClear(grade);
        Missed = grade == NoteGrade.Miss;
        if (Missed)
        {
            Render();
            // The exit slot owns the miss visuals for every design now — drops ride
            // their fade past the line exactly like the other kinds ride their rewind
            // (by MISS time the ride has always ended: it fits inside the 0.2 s drop
            // miss window). The MISS label rides the judgement clock through the
            // feedback-only FX.
            if (FlatSkinRuntime.Ready)
                FlatClearFxPool.GetInstance(Game).PlayFeedback(Note, WorldScale);
        }
        else
        {
            HeadVisible = false;
            HeadMesh.SetVisible(false);
        }
        if (!Missed && !Game.State.Mods.Contains(Mod.HideNotes))
            FlatClearFxPool.GetInstance(Game).Play(Note, grade, WorldScale,
                TimingError(), FlickClearRotationDegrees);
    }

    public override void OnCollect()
    {
        ContinueExitAfterCollection();
        base.OnCollect();
        HeadMesh.SetVisible(false);
        if (additiveHeadMesh != null) additiveHeadMesh.SetVisible(false);
        additiveGeometry.Reset();
        HeadVisible = false;
        Missed = false;
        skinBroken = false;
        BaseTransformScale = BaseTransformSize = WorldScale = EasedOpacity = EnterProgress = 0;
        BaseFillColor = default;
        Palette = null;
        if (DisplayNoteId) NoteId.gameObject.SetActive(false);
        ResetGeometry();
    }

    private void ContinueExitAfterCollection()
    {
        // Regular drag heads ARE the travelling indicator and render their exit
        // on the live mesh until gameplay collects the chain. Never freeze a copy.
        if (Note.Type == NoteType.DragHead) return;
        if (!Missed || !Note.IsCleared || Note.IsDragStackFollower ||
            FlatEase.ExitAlpha(ExitTime, HeadExitCurve) <= 0 ||
            Game.State.Mods.Contains(Mod.HideNotes)) return;
        var drawHead = CaptureSkinExitHead();
        if (drawHead == null) return;
        var maxOpacity = Note.Model.opacity == double.MinValue ? (float) Note.Chart.opacity : (float) Note.Model.opacity;
        FlatNoteExitVisual.Play(Note, WorldScale, SortingOrder, BodySortingLayerId, HeadExitCurve, maxOpacity,
            drawHead, CaptureExitBody(), ExitAnchor, ExitLocalRotation, ExitClockScale);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (HeadMesh != null) HeadMesh.Dispose();
        if (additiveHeadMesh != null) additiveHeadMesh.Dispose();
        if (NoteId != null) Object.Destroy(NoteId.gameObject);
    }

    private void ResetGeometry()
    {
        geometry.Reset();
        uploadedOpacity = -1;
        builtSkinDriver = double.NaN;
        builtPalette = null;
        geometryDirty = true;
        headVertexCount = 0;
    }

    public static Color ResolvePaletteSeed(Note note)
    {
        return ResolvePaletteSeed(note.Model, note.Game);
    }

    /// <summary>Model-based variant for linked visuals whose source note may not be
    /// spawned yet (drag chain links).</summary>
    public static Color ResolvePaletteSeed(ChartModel.Note model, Game game)
    {
        var seed = model.Override.FillColor;
        if (seed.HasValue && seed.Value != Color.clear) return seed.Value;
        seed = game.Config.GetFillColorOverride(model);
        if (seed.Value != Color.clear) return seed.Value;
        return game.Config.GetFillColor(model);
    }

    public static FlatPalette ResolvePalette(Note note, FlatFamily family)
    {
        if (paletteGame != note.Game)
        {
            ClearPaletteCache();
            paletteGame = note.Game;
        }
        var seed = ResolvePaletteSeed(note);
        var rgba = (Color32) seed;
        var packed = rgba.r | rgba.g << 8 | rgba.b << 16 | rgba.a << 24;
        // The ring is a palette-level material tone (derivePalette's ring option):
        // the chart's ring_color, else the player's ring color.
        var ring = note.Game.Config.NoteRingColors[note.Type][0];
        var ringRgba = (Color32) ring;
        var ringPacked = ringRgba.r | ringRgba.g << 8 | ringRgba.b << 16 | ringRgba.a << 24;
        var key = (family, packed, ringPacked);
        if (!FlatPaletteCache.TryGetValue(key, out var palette))
        {
            palette = FlatPaletteDerive.FromSeed(new Rgba(seed.r, seed.g, seed.b, seed.a),
                new Rgba(ring.r, ring.g, ring.b, ring.a));
            FlatPaletteCache.Add(key, palette);
        }
        return palette;
    }

    // Integration calls this from GameConfig.OnGameLoaded; changing Game also resets the cache above.
    public static void ClearPaletteCache() => FlatPaletteCache.Clear();

    public static FlatFamily FamilyFor(NoteType type)
    {
        switch (type)
        {
            case NoteType.Click: return FlatFamily.Click;
            case NoteType.Hold: return FlatFamily.Hold;
            case NoteType.LongHold: return FlatFamily.LongHold;
            case NoteType.DragHead:
            case NoteType.DragChild: return FlatFamily.Drag;
            // Phase 1 deliberately uses the click-drag family for the head too.
            case NoteType.CDragHead:
            case NoteType.CDragChild: return FlatFamily.ClickDrag;
            case NoteType.Flick: return FlatFamily.Flick;
            case NoteType.DropClick: return FlatFamily.DropClick;
            case NoteType.DropDrag: return FlatFamily.DropDrag;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public static NoteKind KindFor(NoteType type)
    {
        switch (type)
        {
            case NoteType.Click: return NoteKind.Click;
            case NoteType.Hold: return NoteKind.Hold;
            case NoteType.LongHold: return NoteKind.LongHold;
            case NoteType.DragHead: return NoteKind.DragHead;
            case NoteType.DragChild: return NoteKind.DragChild;
            case NoteType.CDragHead: return NoteKind.ClickDragHead;
            case NoteType.CDragChild: return NoteKind.ClickDragChild;
            case NoteType.Flick: return NoteKind.Flick;
            case NoteType.DropClick: return NoteKind.DropClick;
            case NoteType.DropDrag: return NoteKind.DropDrag;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    protected static Rgba WithOpacity(Rgba color, double opacity) =>
        new Rgba(color.R, color.G, color.B, (float) (color.A * opacity));

    protected static double WakeAmount(double p) => FlatEase.InOutQuad(FlatEase.Seg(p, 0.45, 0.7));
    protected static double WakeShade(double lit, double wake) => lit + 200 * (1 - wake);

    // parts.ts assemblingRing: the band, then the stepped edge on its outer
    // silhouette (FlatEdge) — each edge band centred `offset` from `r + width/2`,
    // sharing the band's segment/spin build so it assembles with the ring.
    // (Still used by the drag-head chain indicator, the last imperative draw.)
    protected static void AssemblingRing(FlatMeshBuilder b, double r, double width, Rgba color,
        int segments, double k, double spin, Rgba edge)
    {
        RingBand(b, r, width, color, segments, k, spin);
        var outer = r + width / 2;
        for (var i = 0; i < FlatEdge.Count; i++)
        {
            if (!FlatEdge.Band(i, width, out var offset, out var bandWidth, out var opacity)) continue;
            RingBand(b, outer + offset, bandWidth, WithOpacity(edge, opacity), segments, k, spin);
        }
    }

    private static void RingBand(FlatMeshBuilder b, double r, double width, Rgba color, int segments, double k, double spin)
    {
        if (k >= 0.999)
        {
            FlatGeom.Ring(b, 0, 0, r, width, color);
            return;
        }
        var span = Math.PI * 2 / segments;
        var half = span * k / 2;
        for (var i = 0; i < segments; i++)
        {
            var center = spin + i * span;
            FlatGeom.Arc(b, 0, 0, r, center - half, center + half, width, color);
        }
    }
}
