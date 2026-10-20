using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class FlatNoteRendererBase : NoteRenderer
{
    public static readonly Dictionary<(FlatFamily Family, int SeedPacked, int RingPacked), FlatPalette> FlatPaletteCache =
        new Dictionary<(FlatFamily, int, int), FlatPalette>();

    private static Game paletteGame;
    private readonly FlatMeshBuilder geometry = new FlatMeshBuilder();
    private readonly FlatMeshBuilder upload = new FlatMeshBuilder();
    private Func<double, double> sizeCurve;
    private double spawnScale;
    private float builtProgress = -1;
    private float builtEnterScale = -1;
    private float uploadedOpacity = -1;
    private FlatPalette builtPalette;
    private bool geometryDirty = true;
    private float missedAt;
    private bool missTextEnabled;
    private bool builtMissTextActive;
    private int headVertexCount;

    protected readonly FlatNoteMesh HeadMesh;
    protected float BaseTransformScale;
    protected float BaseTransformSize;
    protected Color BaseFillColor;
    protected FlatPalette Palette;
    protected float EasedOpacity;
    protected double ApproachSeconds => Note.Model.start_time - Note.Model.intro_time;
    protected float EnterProgress;
    protected float EnterScale;
    protected float WorldScale;
    protected int SortingOrder;
    protected bool HeadVisible;
    protected readonly bool DisplayNoteId;
    protected readonly NoteId NoteId;
    protected bool Missed { get; private set; }
    protected bool ClearedSuccessfully => Note.IsCleared && !Missed;
    protected virtual bool CanExit => true;
    protected bool UsesExit => Kind != NoteKind.DropClick && Kind != NoteKind.DropDrag;
    protected virtual bool Exiting => UsesExit && (Note.IsCleared ? Missed : CanExit && Game.Time > Note.Model.start_time);
    protected virtual double ExitAnchor => Note.Model.start_time;
    protected double ExitTime => (double) Game.Time - ExitAnchor;
    protected ExitCurve HeadExitCurve => Kind == NoteKind.Hold || Kind == NoteKind.LongHold
        ? FlatTokens.Time.Exit.HoldHead
        : Kind == NoteKind.DragChild || Kind == NoteKind.ClickDragChild
            ? FlatTokens.Time.Exit.DragChild : FlatTokens.Time.Exit.Tap;
    private float HeadOpacity => EasedOpacity * (Exiting ? (float) FlatEase.ExitAlpha(ExitTime, HeadExitCurve) : 1);

    protected virtual NoteKind Kind => KindFor(Note.Type);
    protected virtual FlatFamily Family => FamilyFor(Note.Type);
    protected double DesignSize => FlatTokens.Unit * FlatTokens.Size(Kind);
    // Animated bodies can override this, or call InvalidateGeometry when their inputs change.
    protected virtual bool GeometryChangesEveryFrame => false;
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
        if (Kind == NoteKind.Hold || Kind == NoteKind.LongHold)
        {
            spawnScale = FlatTokens.HoldSizeCurve.Spawn;
            var approach = ApproachSeconds;
            sizeCurve = p => FlatHoldDesign.Size(p, approach);
        }
        else
        {
            var curve = FlatTokens.SizeCurve(SizeKindFor(Kind));
            spawnScale = curve.Spawn;
            sizeCurve = FlatEase.RiseSettleCurve(curve, FlatTokens.SizeCurveShape);
        }
        EnterProgress = EasedOpacity = 0;
        EnterScale = 1;
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
        if (!HeadVisible) return;

        // The miss text animates on the judgement clock even when the exit pose holds still.
        var missTextActive = MissTextActive();
        if (missTextActive || missTextActive != builtMissTextActive) geometryDirty = true;
        var rebuild = geometryDirty || GeometryChangesEveryFrame || builtProgress != EnterProgress ||
                      builtEnterScale != EnterScale || builtPalette != Palette;
        if (rebuild)
        {
            geometry.Reset();
            BuildGeometry(geometry);
            headVertexCount = geometry.VertCount;
            DrawMissJudgement(geometry);
            builtMissTextActive = missTextActive;
            builtProgress = EnterProgress;
            builtEnterScale = EnterScale;
            builtPalette = Palette;
            geometryDirty = false;
        }
        var opacity = HeadOpacity;
        if (!rebuild && uploadedOpacity == opacity) return;

        // Keep source alphas intact: a steady head only rewrites colors when opacity changes.
        upload.Reset();
        upload.Verts.AddRange(geometry.Verts);
        upload.Tris.AddRange(geometry.Tris);
        // FlatJudgement owns the label's fade and full judgement window, even
        // after the chart-time head exit has become transparent.
        for (var i = 6; i < upload.Verts.Count; i += 7)
            upload.Verts[i] *= i < headVertexCount * 7 ? opacity : EasedOpacity;
        HeadMesh.Upload(upload);
        uploadedOpacity = opacity;
    }

    /// <summary>Design pixels, +y down. The base applies EnterScale and storyboard size to the head geometry.</summary>
    protected abstract void BuildGeometry(FlatMeshBuilder b);

    // The miss label runs on the judgement clock (missedAt), separate from the exit's
    // chart anchor and head exit fade.
    private bool MissTextActive() => Missed && UsesExit && missTextEnabled &&
        Game.Time - missedAt >= 0 && Game.Time - missedAt < FlatTokens.Time.ClearDuration("miss");

    protected virtual void DrawMissJudgement(FlatMeshBuilder b)
    {
        if (!MissTextActive() || EnterScale == 0) return;
        var first = b.VertCount;
        // TS composes judgement beside the exit, outside the head's size curve.
        // Cancel it after tessellation so the stroke, sink and AA fringe all keep size.
        FlatJudgement.Draw(b, "miss", Game.Time - missedAt);
        ScaleGeometry(b, first, 1 / EnterScale);
        FlatClearFx.KeepTextUpright(b, first, Note.transform.rotation,
            WorldScale * EnterScale * Note.Model.Override.SizeMultiplier);
    }

    // Signed press time minus chart time in seconds (positive = late) for the
    // FAST/SLOW label. Holds override this with the head's press time.
    protected virtual double TimingError() => Game.Time - (Note.Model.start_time + Note.JudgmentOffset);

    // Capture drawing inputs, never a pooled Note/Model. Drawing helpers may share
    // scratch arrays: live heads and detached exits both render on Unity's main thread.
    protected virtual Action<FlatMeshBuilder, float, float> CaptureExitHead() => null;
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
        HeadVisible = !Note.IsDragStackFollower && (!Note.IsCleared || Missed && UsesExit) &&
                      (inWindow || MissTextActive()) &&
                      !Game.State.Mods.Contains(Mod.HideNotes);
        HeadMesh.SetVisible(HeadVisible);
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
        EnterScale = ScaleAt(EnterProgress);
        var scale = EnterScale * Note.Model.Override.SizeMultiplier;
        // Keep Classic's parent scale for the collider and hold particles. Cancel the base
        // size on the head because FlatNoteMesh already uploads vertices in world units.
        var transformSize = BaseTransformSize * scale;
        Note.transform.localScale = new Vector3(transformSize, transformSize, Note.transform.localScale.z);
        var meshScale = BaseTransformSize != 0 ? 1 / BaseTransformSize : 1;
        HeadMesh.transform.localScale = new Vector3(meshScale, meshScale, 1);
    }

    private float ScaleAt(float progress) => ScaleAt(progress, sizeCurve, spawnScale, Note.Model.initial_scale);

    private static float ScaleAt(float progress, Func<double, double> sizeCurve, double spawnScale, float initial)
    {
        var curve = sizeCurve(progress);
        var curve01 = (curve - spawnScale) / (1 - spawnScale);
        // The spec reserves initial_scale == 1 for the unmodified design curve.
        return (float) (initial == 1f ? curve : initial + (1 - initial) * curve01);
    }

    protected virtual void UpdateComponentOpacity()
    {
        var maxOpacity = (float) Note.Chart.opacity;
        if (Note.Model.opacity != double.MinValue)
        {
            maxOpacity = (float) Note.Model.opacity;
        }

        if (Note.TimeUntilStart > 0)
            EasedOpacity =
                Mathf.Clamp((Game.Time - Note.Model.intro_time) / (Note.Model.start_time - Note.Model.intro_time) * 2f,
                    0f, maxOpacity);
        else EasedOpacity = maxOpacity;

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
        if (Missed && UsesExit)
        {
            // Track the miss moment for the exit's MISS label; settings are captured
            // at spawn so the label never appears or vanishes mid-exit.
            missedAt = Game.Time;
            missTextEnabled = Context.Player.Settings.DisplayJudgementText;
            Render();
        }
        else
        {
            HeadVisible = false;
            HeadMesh.SetVisible(false);
        }
        // Only drops retain a miss effect; all other misses ride their exit curves.
        if ((!Missed || !UsesExit) && !Game.State.Mods.Contains(Mod.HideNotes))
            FlatClearFxPool.GetInstance(Game).Play(Note, grade, WorldScale, SortingOrder + 2, Family,
                TimingError(), FlickClearRotationDegrees);
    }

    public override void OnCollect()
    {
        ContinueExitAfterCollection();
        base.OnCollect();
        HeadMesh.SetVisible(false);
        HeadVisible = false;
        Missed = false;
        missedAt = 0;
        missTextEnabled = false;
        BaseTransformScale = BaseTransformSize = WorldScale = EasedOpacity = EnterProgress = 0;
        EnterScale = 1;
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
        if (!Missed || !Note.IsCleared || !UsesExit || Note.IsDragStackFollower ||
            (FlatEase.ExitAlpha(ExitTime, HeadExitCurve) <= 0 && !MissTextActive()) ||
            Game.State.Mods.Contains(Mod.HideNotes)) return;
        var drawHead = CaptureExitHead();
        if (drawHead == null) return;
        var curve = sizeCurve;
        var spawn = spawnScale;
        var initial = Note.Model.initial_scale;
        var maxOpacity = Note.Model.opacity == double.MinValue ? (float) Note.Chart.opacity : (float) Note.Model.opacity;
        FlatNoteExitVisual.Play(Note, WorldScale, SortingOrder, BodySortingLayerId, HeadExitCurve, maxOpacity,
            p => ScaleAt(p, curve, spawn, initial), drawHead, CaptureExitBody(), ExitAnchor, missedAt, missTextEnabled,
            ExitLocalRotation);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (HeadMesh != null) HeadMesh.Dispose();
        if (NoteId != null) Object.Destroy(NoteId.gameObject);
    }

    private void ResetGeometry()
    {
        geometry.Reset();
        upload.Reset();
        builtProgress = builtEnterScale = uploadedOpacity = -1;
        builtPalette = null;
        geometryDirty = true;
        builtMissTextActive = false;
        headVertexCount = 0;
    }

    public static Color ResolvePaletteSeed(Note note)
    {
        var seed = note.Model.Override.FillColor;
        if (seed.HasValue && seed.Value != Color.clear) return seed.Value;
        seed = note.Game.Config.GetFillColorOverride(note.Model);
        if (seed.Value != Color.clear) return seed.Value;
        return note.Game.Config.GetFillColor(note.Model);
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

    private static SizeKind SizeKindFor(NoteKind kind)
    {
        switch (kind)
        {
            case NoteKind.Hold:
            case NoteKind.LongHold: return SizeKind.Hold;
            case NoteKind.DragHead: return SizeKind.DragHead;
            case NoteKind.DragChild:
            case NoteKind.ClickDragChild: return SizeKind.DragChild;
            default: return SizeKind.Click;
        }
    }

    protected static Rgba WithOpacity(Rgba color, double opacity) =>
        new Rgba(color.R, color.G, color.B, (float) (color.A * opacity));

    // Gauge/body geometry outside the design's scaled head group can compensate for EnterScale.
    protected internal static void ScaleGeometry(FlatMeshBuilder b, int firstVertex, double scale)
    {
        for (var i = firstVertex * 7; i < b.Verts.Count; i += 7)
        {
            b.Verts[i] *= (float) scale;
            b.Verts[i + 1] *= (float) scale;
        }
    }

    protected static double WakeAmount(double p) => FlatEase.InOutQuad(FlatEase.Seg(p, 0.45, 0.7));
    protected static double WakeShade(double lit, double wake) => lit + 200 * (1 - wake);

    // parts.ts assemblingRing: the band, then the stepped edge on its outer
    // silhouette (FlatEdge) — each edge band centred `offset` from `r + width/2`,
    // sharing the band's segment/spin build so it assembles with the ring.
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
