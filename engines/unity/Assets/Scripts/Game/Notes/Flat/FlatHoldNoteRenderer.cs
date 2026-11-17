using System;
using UnityEngine;

public class FlatHoldNoteRenderer : FlatNoteRendererBase
{
    /// <summary>
    /// Classic's layer scheme: the body renders on the dedicated GameHoldLine sorting
    /// layer (long holds: GameLongHoldLine), which sits below GameDragLine and
    /// Default. Sorting layers outrank sorting orders, so this — not a low order —
    /// is what keeps the bar under chains and every note head; the note's own order
    /// then keeps overlapping bars deterministic. The progress ring stays on Default
    /// like classic's ProgressRing: above chains, below every head (negative order),
    /// later ids on top.
    /// </summary>
    private static readonly int HoldLineLayer = SortingLayer.NameToID("GameHoldLine");
    protected override int BodySortingLayerId => HoldLineLayer;

    private readonly FlatMeshBuilder bodyBuilder = new FlatMeshBuilder();
    private readonly FlatMeshBuilder ringBuilder = new FlatMeshBuilder();
    protected readonly ParticleSystem HoldFx;
    private readonly FlatNoteMesh BodyMesh;
    private readonly FlatNoteMesh RingMesh;
    protected HoldNote HoldNote => (HoldNote) Note;
    protected bool BodyVisible;
    protected override bool CanExit => !HoldNote.IsHolding;

    private MeshTriangle Triangle;
    private double pressT;

    // sb style 2: the body shrinks toward the head while pressing (classic's Line
    // shrink) instead of filling up. Drawn as a shorter body at progress 0, with
    // the shrink and the conveyor phase frozen at release — resetting them would
    // regrow the body to full length mid-exit.
    protected bool StyleTwo => Note.Model.style == 2;
    protected double styleTwoShrink = 1;
    protected double styleTwoTime;
    private float BodyOpacity => EasedOpacity *
        (Exiting ? (float) FlatEase.ExitAlpha(ExitTime, FlatTokens.Time.Exit.HoldBody) : 1);
    protected double BodyTop;
    protected double BodyBottom;
    protected double BodyProgress;
    protected double BodyTime;
    protected bool BodyFlipY;
    protected float BodySizeMultiplier;
    private bool headPartVisible;
    private bool wasHolding;
    private double? releaseExitAnchor;
    protected override double ExitAnchor => releaseExitAnchor ?? base.ExitAnchor;

    public FlatHoldNoteRenderer(HoldNote note) : base(note)
    {
        BodyMesh = FlatNoteMesh.Create(Note.transform, "FlatBodyMesh", false, 0);
        BodyMesh.SetVisible(false);
        RingMesh = FlatNoteMesh.Create(Note.transform, "FlatProgressRingMesh", false, 0);
        RingMesh.SetVisible(false);
        // Classic's scanner triangle, reused verbatim: it fills the body-to-line
        // gap. The flat body bar draws from the skin's body slot, and the progress
        // ring from the skin's progress slot (both on their own flat meshes).
        Triangle = UnityEngine.Object.Instantiate(ClassicHoldNoteRendererProvider.Instance.trianglePrefab,
            Game.contentParent.transform).GetComponent<MeshTriangle>();
        Triangle.gameObject.SetActive(false);
        HoldFx = UnityEngine.Object.Instantiate(Game.effectController.holdFx, Note.transform, false);
        HoldFx.transform.DeltaZ(-0.001f);
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        ResetHoldState();
        var main = HoldFx.main;
        main.startColor = BaseFillColor;
        // Classic's fill is ring order - 1, and its hold particles are another order below.
        HoldFx.GetComponent<ParticleSystemRenderer>().sortingOrder = SortingOrder - 2;
        BodyMesh.SetSortingLayer(BodySortingLayerId);
        BodyMesh.SetSortingOrder(SortingOrder);
        RingMesh.SetSortingOrder(-SortingOrder);
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Triangle.gameObject.SetActive(true);
        Triangle.Note = Note;
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        var headVisible = HeadVisible;
        var inWindow = Exiting ? FlatEase.ExitAlpha(ExitTime, FlatTokens.Time.Exit.HoldBody) > 0 :
            Game.Time >= Note.Model.intro_time + Note.JudgmentOffset &&
            Game.Time <= Note.Model.end_time + Note.MissThreshold + Note.JudgmentOffset;
        var bodyVisible = !Note.IsDragStackFollower && (!Note.IsCleared || Missed) && inWindow &&
                          !Game.State.Mods.Contains(Mod.HideNotes);
        GetBodyDimensions(out var top, out var bottom, out var flipY);
        var progress = HoldNote.IsHolding ? FlatEase.Clamp01(HoldNote.HoldProgress) : 0;
        // Anchor to chart time, not elapsed render frames or press time: late hits still finish on time.
        var time = HoldNote.IsHolding ? Math.Max(0, Game.Time - (Note.Model.start_time + Note.JudgmentOffset)) : 0;
        var sizeMultiplier = Note.Model.Override.SizeMultiplier;
        // Keyed to the hold's chart start, not the press moment (holdPress's t is
        // "seconds since hold start"): an early press must not sink the head
        // before the hold actually begins. Frozen at release (not reset): the
        // head was pressed when it died, so the exit must not pop the sink back
        // to full size.
        var livePressT = pressT;
        if (HoldNote.IsHolding)
            livePressT = Math.Max(0, Game.Time - (Note.Model.start_time + Note.JudgmentOffset));
        var liveShrink = styleTwoShrink;
        var liveStyleTwoTime = styleTwoTime;
        if (StyleTwo && HoldNote.IsHolding)
        {
            liveShrink = 1 - progress;
            liveStyleTwoTime = time;
        }
        if (headPartVisible != headVisible || BodyVisible != bodyVisible || wasHolding != HoldNote.IsHolding ||
            BodyTop != top || BodyBottom != bottom || BodyFlipY != flipY || BodyProgress != progress ||
            BodyTime != time || BodySizeMultiplier != sizeMultiplier || pressT != livePressT ||
            styleTwoShrink != liveShrink || styleTwoTime != liveStyleTwoTime)
            InvalidateGeometry();
        headPartVisible = headVisible;
        BodyVisible = bodyVisible;
        wasHolding = HoldNote.IsHolding;
        BodyTop = top;
        BodyBottom = bottom;
        BodyFlipY = flipY;
        BodyProgress = progress;
        BodyTime = time;
        BodySizeMultiplier = sizeMultiplier;
        pressT = livePressT;
        styleTwoShrink = liveShrink;
        styleTwoTime = liveStyleTwoTime;

        // Head and body now live on separate meshes with independent visibility.
        HeadVisible = headPartVisible;
        var playFx = BodyVisible && HoldNote.IsHolding && Game.State.IsPlaying &&
                     Game.Time > Note.Model.start_time + Note.JudgmentOffset;
        if (playFx && !HoldFx.isPlaying)
        {
            var emission = HoldFx.emission;
            emission.enabled = true;
            HoldFx.Play();
        }
        else if (!playFx && HoldFx.isPlaying)
            HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (Note.IsDragStackFollower || Game.State.Mods.Contains(Mod.HideNotes))
            HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // The scanner triangle shares classic's window: armed with the note's own
        // judgement window, driven only while the hold is live. The progress ring
        // draws in the flat mesh below (RenderBody).
        var inHoldWindow = !Note.IsCleared &&
                           Game.Time >= Note.Model.intro_time + Note.JudgmentOffset &&
                           Game.Time <= Note.Model.end_time + Note.MissThreshold + Note.JudgmentOffset;
        if (inHoldWindow)
        {
            switch (Note.Model.style)
            {
                case 1: Triangle.enabled = true; break;
                case 2: Triangle.enabled = false; break;
            }
            if (Game.State.Mods.Contains(Mod.HideNotes)) Triangle.enabled = false;
            if (HoldNote.IsHolding && Game.Time >= Note.Model.start_time)
                Triangle.OnUpdate();
        }
        else
        {
            Triangle.enabled = false;
            Triangle.Reset();
        }
    }

    protected virtual void GetBodyDimensions(out double top, out double bottom, out bool flipY)
    {
        top = WorldScale != 0 ? Math.Max(0, Note.Model.holdlength / WorldScale) : 0;
        bottom = 0;
        var page = Game.Chart.Model.page_list[Note.Model.page_index];
        flipY = ClassicHoldNoteRenderer.ComputeLineFlipY(Note.Model, page);
    }

    protected override void UpdateTransformScale()
    {
        // An early press must NOT snap to the settled pose: the head keeps playing
        // its time-driven approach (scale + enter frames) until p reaches 1,
        // matching Classic and the design's press-anchored state.
        base.UpdateTransformScale();
        var fxScale = (float) Note.Chart.size * Note.Model.Override.SizeMultiplier;
        if (Note.Model.size != double.MinValue) fxScale *= (float) Note.Model.size;
        HoldFx.transform.SetLocalScale(5 * (1 + Context.Player.Settings.ClearEffectsSize) * fxScale);
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        var main = HoldFx.main;
        main.startColor = new Color(BaseFillColor.r, BaseFillColor.g, BaseFillColor.b, BaseFillColor.a * EasedOpacity);
    }

    protected override void UpdateCollider()
    {
        base.UpdateCollider();
        if (HoldNote.IsHolding) Collider.radius *= 1.3333f;
    }

    protected override void Render()
    {
        base.Render();
        RenderBody();
    }

    /// <summary>
    /// The conveyor scrolls with chart time, so the bar rebuilds every frame it is
    /// visible; per-vertex alpha carries the note opacity like the head mesh.
    /// </summary>
    private void RenderBody()
    {
        if (!BodyVisible || WorldScale == 0f || BodySizeMultiplier == 0f)
        {
            BodyMesh.SetVisible(false);
            RingMesh.SetVisible(false);
            return;
        }
        // Cancel the parent's BaseTransformSize the same way the head mesh does; the
        // bar must not pulse with the enter curve (it has its own grow-in).
        var meshScale = BaseTransformSize != 0 ? 1 / BaseTransformSize : 1;
        bodyBuilder.Reset();
        BuildBodyGeometry(bodyBuilder);
        UploadBodyPart(BodyMesh, bodyBuilder, meshScale);

        // hold.ts draws the progress clip outside the head's size curve, so the
        // ring shares the body's parent-scale cancellation. It appears when the
        // hold is live past the press, same gate classic's cutoffs used.
        var ringVisible = HoldNote.IsHolding && Game.Time > Note.Model.start_time + Note.JudgmentOffset;
        if (!ringVisible)
        {
            RingMesh.SetVisible(false);
            return;
        }
        ringBuilder.Reset();
        // The skin's hold-progress slot is the only ring implementation.
        holdSlots.Resolve(FlatSkinRuntime.Current, Kind);
        var progressSlot = holdSlots.Progress;
        if (progressSlot == null || progressSlot.UsesUnsupported || progressSlot.UsesImages)
        {
            WarnSlotMissingOnce(Kind, "progress");
            RingMesh.SetVisible(false);
            return;
        }
        SkinPainterInstance.DrawNoteSlot(progressSlot, BodyProgress, SkinHeadContext(), new SkinSinks(ringBuilder));
        UploadBodyPart(RingMesh, ringBuilder, meshScale);
    }

    private void UploadBodyPart(FlatNoteMesh mesh, FlatMeshBuilder b, float meshScale)
    {
        mesh.SetVisible(true);
        mesh.SetWorldScale(WorldScale);
        mesh.transform.localScale = new Vector3(meshScale, meshScale, 1);
        for (var i = 6; i < b.Verts.Count; i += 7) b.Verts[i] *= BodyOpacity;
        mesh.Upload(b);
    }

    // =================================================================
    // Skin-mode holding composition (loopUnder + press).
    //
    // Deviation from TS (packages/notes): TypeScript composes the holding state as
    // loopUnder → body → press → loopOver → progress. Unity keeps the body bar on
    // its own sorting layer (the skin's body slot), so loopUnder draws under the
    // press head on the head mesh; loopOver is skipped (no equivalent sink);
    // progress draws on the ring mesh, which sorts below every head (see RenderBody).
    // =================================================================

    // Raw pressT while a loopUnder is bound keeps the looping under-layer rebuilding
    // every frame; the clamped value lets a steady press pose hit the rebuild skip.
    protected override double SkinHeadDriver(CompiledSkin skin)
    {
        holdSlots.Resolve(skin, Kind);
        // pressT is chart-anchored (clamped at 0 before the hold's start): an
        // early press keeps the settled enter pose instead of triggering the
        // hold layers, exactly like the press-clock gate.
        if (!Exiting && HoldNote.IsHolding && pressT > 0)
        {
            var press = holdSlots.Press;
            if (press is { UsesUnsupported: false, UsesImages: false })
                return holdSlots.LoopUnder is { UsesUnsupported: false, UsesImages: false }
                    ? pressT : Math.Min(pressT, press.Contract.Duration);
        }
        return base.SkinHeadDriver(skin);
    }

    protected override void DrawSkinHead(FlatMeshBuilder b)
    {
        var skin = FlatSkinRuntime.Current;
        if (Exiting || !HoldNote.IsHolding || pressT <= 0) { base.DrawSkinHead(b); return; }
        holdSlots.Resolve(skin, Kind);
        var press = holdSlots.Press;
        var under = holdSlots.LoopUnder;
        // No usable press slot: the head draws nothing while holding — the flat
        // path has no cross-slot fallback (image slots have no head sink either).
        if (press == null || press.UsesUnsupported || press.UsesImages)
        {
            WarnSlotMissingOnce(Kind, "press");
            return;
        }
        var painter = SkinPainterInstance;
        using (FlatSkinRuntime.PaintMarker.Auto())
        {
            if (under is { UsesUnsupported: false, UsesImages: false })
                painter.DrawNoteSlot(under, pressT, SkinHeadContext(), new SkinSinks(b, additiveGeometry));
            painter.DrawNoteSlot(press, Math.Min(pressT, press.Contract.Duration), SkinHeadContext(), new SkinSinks(b, additiveGeometry));
        }
    }

    protected override Action<FlatMeshBuilder, float> CaptureExitBody()
    {
        if (BodySizeMultiplier == 0) return null;
        // Detached exits ride the skin body slot too: independent snapshots
        // (slot, options, context) — pure function, no pooled-state references.
        // Without the slot the detached body simply doesn't draw (the live bar
        // has the same contract).
        holdSlots.Resolve(FlatSkinRuntime.Current, Kind);
        var slot = holdSlots.Body;
        if (slot == null || slot.UsesUnsupported || slot.UsesImages) return null;
        var painter = SkinPainterInstance;
        var context = FlatSkinRuntime.HeadContextOf(Note, Family, SkinHeadContext().Palettes, ApproachSeconds);
        context.BodyDirection = BodyFlipY ? FlatDirection.Down : FlatDirection.Up;
        var options = new SkinInputs.BodyOptions
        {
            Length = BodyTop / BodySizeMultiplier * (StyleTwo ? styleTwoShrink : 1),
            Progress = 0,
            T = StyleTwo ? styleTwoTime : 0,
            // Appear rides the exit rewind progress per frame (offline
            // semantics: HoldDuration stays null, conveyor frozen).
        };
        return (b, p) =>
        {
            options.Appear = p;
            painter.DrawBodySlot(slot, options, context, new SkinSinks(b));
        };
    }

    // Reused body-options payload (fields fully overwritten per draw).
    protected SkinInputs.BodyOptions bodyOptions;

    // Per-skin slot cache (press/loopUnder/progress/body): CompiledSkin.Slot()
    // builds a key string per call, so the hold path's per-frame queries ride a
    // reference check instead. Long-hold reuses it for its body slots.
    protected readonly SkinSlotCache holdSlots = new SkinSlotCache();

    /// <summary>The parametric body slot is the only body implementation (clip
    /// CPU-clipped at emission). Style-2 keeps the engine-side shrink (the skin
    /// cannot read collapse on holds); reveal/appear ride EnterProgress. Without
    /// the slot the bar simply doesn't draw — warned once.</summary>
    protected virtual void BuildBodyGeometry(FlatMeshBuilder b)
    {
        if (TryDrawSkinBody(b)) return;
        WarnSlotMissingOnce(Kind, "body");
    }

    /// <summary>Skin-mode body: the parametric body slot (clip CPU-clipped at emission).</summary>
    protected bool TryDrawSkinBody(FlatMeshBuilder b)
    {
        holdSlots.Resolve(FlatSkinRuntime.Current, Kind);
        var slot = holdSlots.Body;
        if (slot == null || slot.UsesUnsupported || slot.UsesImages) return false;
        bodyOptions ??= new SkinInputs.BodyOptions();
        var options = bodyOptions;
        var length = BodyTop / BodySizeMultiplier;
        options.Length = StyleTwo ? length * styleTwoShrink : length;
        options.Progress = StyleTwo ? 0 : BodyProgress;
        options.T = StyleTwo ? styleTwoTime : BodyTime;
        options.Top = double.NaN;
        options.Bottom = double.NaN;
        options.HoldDuration = null;
        options.Appear = EnterProgress;
        var context = SkinHeadContext();
        context.BodyDirection = BodyFlipY ? FlatDirection.Down : FlatDirection.Up;
        SkinPainterInstance.DrawBodySlot(slot, options, context, new SkinSinks(b));
        return true;
    }

    // The renderer's only OnClear fires at the hold end (HoldNote.OnGameUpdate /
    // UpdateFinger), so the label shows the HEAD PRESS timing captured from
    // HoldingStartTime. Never-pressed holds (auto) fall back to the clear moment.
    protected override double TimingError()
    {
        var press = HoldNote.HoldingStartTime;
        return press == float.MaxValue
            ? base.TimingError()
            : press - (Note.Model.start_time + Note.JudgmentOffset);
    }

    public override void OnClear(NoteGrade grade)
    {
        // UpdateFinger clears after removing the last finger, while the press
        // timestamp is still available. Never-pressed deadline misses keep the
        // chart anchor; a pressed head was alive until release/completion.
        if (grade == NoteGrade.Miss && HoldNote.HoldingStartTime != float.MaxValue && !HoldNote.IsHolding)
            releaseExitAnchor = Game.Time;
        base.OnClear(grade);
        if (grade != NoteGrade.Miss)
        {
            headPartVisible = BodyVisible = false;
            BodyMesh.SetVisible(false);
            RingMesh.SetVisible(false);
        }
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Triangle.Reset();
        Triangle.enabled = false;
        InvalidateGeometry();
    }

    public override void OnCollect()
    {
        base.OnCollect();
        ResetHoldState();
        BodyMesh.SetVisible(false);
        RingMesh.SetVisible(false);
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Triangle.Reset();
        Triangle.gameObject.SetActive(false);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (HoldFx != null) UnityEngine.Object.Destroy(HoldFx.gameObject);
        if (Triangle != null) UnityEngine.Object.Destroy(Triangle.gameObject);
        BodyMesh.Dispose();
        RingMesh.Dispose();
    }

    private void ResetHoldState()
    {
        releaseExitAnchor = null;
        headPartVisible = BodyVisible = wasHolding = BodyFlipY = false;
        BodyTop = BodyBottom = BodyProgress = BodyTime = pressT = 0;
        styleTwoShrink = 1;
        styleTwoTime = 0;
        BodySizeMultiplier = 1;
    }
}
