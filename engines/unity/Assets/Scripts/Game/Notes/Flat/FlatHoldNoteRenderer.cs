using System;
using UnityEngine;

public class FlatHoldNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    /// <summary>
    /// Design stage layering puts bodies under EVERY note head (scanline < bodies
    /// < connections < notes < effects), so the bar lives on its own mesh below the
    /// per-note scale: note meshes start at (count - id) * 3 >= 3 and drag chain
    /// lines sit at raw ids >= 1.
    /// </summary>
    private const int BodySortingOrder = 0;

    private readonly FlatHoldBody body = new FlatHoldBody();
    private readonly FlatMeshBuilder bodyBuilder = new FlatMeshBuilder();
    protected readonly ParticleSystem HoldFx;
    private readonly FlatNoteMesh BodyMesh;
    protected HoldNote HoldNote => (HoldNote) Note;
    protected bool BodyVisible;
    protected override bool CanExit => !HoldNote.IsHolding;
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
        BodyMesh = FlatNoteMesh.Create(Note.transform, "FlatBodyMesh", false, BodySortingOrder);
        BodyMesh.SetVisible(false);
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
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        if (headPartVisible != headVisible || BodyVisible != bodyVisible || wasHolding != HoldNote.IsHolding ||
            BodyTop != top || BodyBottom != bottom || BodyFlipY != flipY || BodyProgress != progress ||
            BodyTime != time || BodySizeMultiplier != sizeMultiplier)
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
            return;
        }
        BodyMesh.SetVisible(true);
        BodyMesh.SetWorldScale(WorldScale);
        // Cancel the parent's BaseTransformSize/EnterScale the same way the head mesh
        // does; the bar must not pulse with the enter curve (it has its own grow-in).
        var bodyMeshScale = BaseTransformSize != 0 ? 1 / BaseTransformSize : 1;
        BodyMesh.transform.localScale = new Vector3(bodyMeshScale, bodyMeshScale, 1);
        bodyBuilder.Reset();
        var first = bodyBuilder.VertCount;
        BuildBodyGeometry(bodyBuilder);
        // The stretched body is outside holdEnter's scaled group. Only the head pulses in size.
        ScaleGeometry(bodyBuilder, first, 1 / EnterScale);
        for (var i = 6; i < bodyBuilder.Verts.Count; i += 7) bodyBuilder.Verts[i] *= BodyOpacity;
        BodyMesh.Upload(bodyBuilder);
    }

    protected override void BuildGeometry(FlatMeshBuilder b)
    {
        if (headPartVisible) DrawHead(b, EnterProgress, DesignSize, Palette, ApproachSeconds, BodyFlipY);
    }

    protected override Action<FlatMeshBuilder, float, float> CaptureExitHead()
    {
        var size = DesignSize;
        var palette = Palette;
        var approach = ApproachSeconds;
        var flipY = BodyFlipY;
        return (b, p, scale) => DrawHead(b, p, size, palette, approach, flipY);
    }

    protected override Action<FlatMeshBuilder, float> CaptureExitBody()
    {
        if (BodySizeMultiplier == 0) return null;
        var palette = Palette;
        var length = BodyTop / BodySizeMultiplier;
        var flipY = BodyFlipY;
        return (b, p) => body.Draw(b, palette, length, 0, 0, p, flipY);
    }

    protected virtual void BuildBodyGeometry(FlatMeshBuilder b)
    {
        // Compensate lengths, not vertex Y: endpoints stay fixed while caps/dots remain round.
        body.Draw(b, Palette, BodyTop / BodySizeMultiplier, BodyProgress, BodyTime, EnterProgress, BodyFlipY);
    }

    private void DrawHead(FlatMeshBuilder b, double p, double size, FlatPalette palette, double approach, bool flipY)
    {
        // Always by approach progress: an early press keeps animating by time
        // (the settled pose is simply enter at p = 1).
        var r = size / 2;
        var w = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(size / FlatTokens.Unit);
        var inner = r - w;
        var k = FlatEase.OutCubic(FlatEase.Seg(p, 0, 0.35));
        var glyph = FlatEase.Seg(p, 0.2, 0.42);
        var split = FlatEase.InOutQuad(FlatEase.Seg(p, 0.08, 0.35));
        var wake = FlatHoldDesign.Wake(p);
        var alpha = FlatEase.Seg(p, 0, 0.08);
        // Fixed-radius lock arcs sit perpendicular to the body at rest. A body
        // flip rotates the pair by pi; it never changes their radius or damping.
        var bracketAlpha = alpha * FlatEase.Seg(p, FlatHoldDesign.BracketFrom, FlatHoldDesign.BracketFadeTo);
        if (bracketAlpha > 0)
        {
            var spin = FlatHoldDesign.BracketSpin(p, approach) + (flipY ? Math.PI : 0);
            var color = WithOpacity(palette.RingTone(wake), bracketAlpha);
            var width = FlatTokens.Unit * FlatTokens.StrokeHair * FlatHoldDesign.BracketWidth;
            for (var i = 0; i < 2; i++)
            {
                var mid = Math.PI / 2 + i * Math.PI + spin;
                FlatGeom.Arc(b, 0, 0, r * FlatHoldDesign.BracketRadius,
                    mid - FlatHoldDesign.BracketHalf, mid + FlatHoldDesign.BracketHalf, width, color, false);
            }
        }
        FlatGeom.Disk(b, 0, 0, inner + 0.5,
            WithOpacity(palette.Tone(FlatEase.Lerp(600, WakeShade(400, wake), split)), alpha));
        FlatGeom.Disk(b, 0, 0, FlatEase.Lerp(inner + 0.5, r * FlatHoldDesign.CoreRadius, split),
            WithOpacity(palette.Tone(FlatEase.Lerp(600, WakeShade(FlatHoldDesign.CoreShade, wake), split)), alpha));
        if (glyph > 0)
        {
            var scale = FlatEase.Lerp(0.4, 1, FlatEase.OutBack(glyph, 2));
            var glyphRadius = r * 0.2 * scale;
            var glyphWidth = FlatTokens.Unit * FlatTokens.StrokeHair * 1.6 * scale;
            var color = WithOpacity(palette.RingTone(wake), alpha * FlatEase.Seg(glyph, 0, 0.5));
            BuildGlyphGeometry(b, glyphRadius, glyphWidth, color);
        }
        AssemblingRing(b, r - w / 2, FlatEase.Lerp(w * 0.5, w, k),
            WithOpacity(palette.RingTone(wake), alpha), 2, FlatEase.OutQuart(k),
            FlatEase.Lerp(-Math.PI / 2, 0, k), WithOpacity(Edge, alpha), FlatTokens.StrokeEdgeFraction);
    }

    protected virtual void BuildGlyphGeometry(FlatMeshBuilder b, double radius, double width, Rgba color)
    {
        FlatGeom.Ring(b, 0, 0, radius, width, color);
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
        }
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        InvalidateGeometry();
    }

    public override void OnCollect()
    {
        base.OnCollect();
        ResetHoldState();
        BodyMesh.SetVisible(false);
        HoldFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (HoldFx != null) UnityEngine.Object.Destroy(HoldFx.gameObject);
        BodyMesh.Dispose();
    }

    private void ResetHoldState()
    {
        releaseExitAnchor = null;
        headPartVisible = BodyVisible = wasHolding = BodyFlipY = false;
        BodyTop = BodyBottom = BodyProgress = BodyTime = 0;
        BodySizeMultiplier = 1;
    }
}
