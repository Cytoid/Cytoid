using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Skin-only clear/miss FX: the compiled skin's clear + feedback slots own
/// all geometry (drawn into the normal/additive/image sinks). When the skin cannot
/// serve the requested slots the effect stays inactive — there is deliberately no
/// imperative fallback (the skin runtime is the only flat implementation).</summary>
public class FlatClearFx : MonoBehaviour
{
    // Judgement FX (burst + FAST/SLOW label) renders exactly one layer above the
    // notes: the dedicated GameFx layer sits directly over Default (note heads)
    // and the hold/drag line layers, while GameMain (scanner, note IDs),
    // storyboard layers 1-2 and the game overlays stay above the effect. Within
    // the layer, image runs climb from their role's base and the vector/label
    // meshes sit above every run (classic's text-over-burst relationship); runs
    // can never cross a note's layer band.
    private static readonly int OverlayLayer = SortingLayer.NameToID("GameFx");

    // GameFx-internal orders, split by ROLE first (clear burst below, feedback
    // label above), then by medium: a legal "vector clear + image judgement/
    // FAST-SLOW" skin keeps the label readable over the burst, and the burst
    // never covers the label either way. Image runs climb from their role's base
    // (bounded far below the next band — a role would need 500 runs to overflow);
    // vector meshes take fixed orders. Same-role ties (normal vs additive) are
    // the same bounded ties classic accepted.
    private const int ClearImageRunOrderBase = -2000;
    private const int ClearVectorOrder = -1500;
    private const int FeedbackImageRunOrderBase = -1000;
    private const int FeedbackVectorOrder = -500;

    private readonly FlatMeshBuilder normal = new FlatMeshBuilder();
    private readonly FlatMeshBuilder additive = new FlatMeshBuilder();
    // Skin image quads (textured) ride this dedicated builder; a skin's images can
    // span several atlas pages, so consecutive same-page runs each get their own
    // pooled mesh (grown to the busiest frame, see UploadImageRuns).
    private readonly FlatMeshBuilder image = new FlatMeshBuilder();
    private readonly List<FlatNoteMesh> imageRunMeshes = new List<FlatNoteMesh>();
    private int imageMeshCounter;
    // The clear slot gets its own builder/mesh trio so its burst stays in a lower
    // order band than the feedback label regardless of medium (image vs vector).
    private readonly FlatMeshBuilder clearNormal = new FlatMeshBuilder();
    private readonly FlatMeshBuilder clearAdditive = new FlatMeshBuilder();
    private readonly FlatMeshBuilder clearImage = new FlatMeshBuilder();
    private readonly List<FlatNoteMesh> clearImageRunMeshes = new List<FlatNoteMesh>();
    private FlatNoteMesh normalMesh;
    private FlatNoteMesh additiveMesh;
    private FlatNoteMesh clearNormalMesh;
    private FlatNoteMesh clearAdditiveMesh;
    private FlatClearFxPool pool;
    private Game game;
    private NoteGrade grade;
    private NoteKind kind;
    private float textWorldScale;
    private float startTime;
    private double duration;
    private bool playing;
    private double timingError;
    private bool showJudgementText;
    private bool showTimingLabel;
    // Slots are re-resolved per Play and cleared on collect so a pooled instance
    // never renders through a swapped-out skin registry.
    private CompiledSkinSlot skinClearSlot;
    private CompiledSkinSlot skinFeedbackSlot;
    private SkinDrawContext skinContext;
    private static SkinPainter painter;
    private static SkinPainter Painter => painter ??= new SkinPainter();

    internal void Initialize()
    {
        normalMesh = FlatNoteMesh.Create(transform, "FlatFxMesh", false, FeedbackVectorOrder);
        additiveMesh = FlatNoteMesh.Create(transform, "FlatFxFlash", true, FeedbackVectorOrder);
        clearNormalMesh = FlatNoteMesh.Create(transform, "FlatFxClearMesh", false, ClearVectorOrder);
        clearAdditiveMesh = FlatNoteMesh.Create(transform, "FlatFxClearFlash", true, ClearVectorOrder);
        normalMesh.SetSortingLayer(OverlayLayer);
        additiveMesh.SetSortingLayer(OverlayLayer);
        clearNormalMesh.SetSortingLayer(OverlayLayer);
        clearAdditiveMesh.SetSortingLayer(OverlayLayer);
    }

    /// <summary>Clear burst + judgement/FAST-SLOW feedback for a judged note. Returns
    /// false (effect stays inactive, caller returns the instance to the pool) when
    /// the skin cannot serve the note kind's clear slot.</summary>
    internal bool Play(FlatClearFxPool owner, Game sourceGame, NoteGrade noteGrade, NoteKind noteKind,
        Vector3 position, Quaternion rotation, float worldScale,
        double noteTimingError, Color seed = default, double approachSeconds = 1.2, Note sourceNote = null)
    {
        pool = owner;
        game = sourceGame;
        grade = noteGrade;
        kind = noteKind;
        // Text settings and the label slot are snapshotted at spawn: a mid-effect
        // settings change must never jump the label between slots.
        timingError = noteTimingError;
        var settings = Context.Player.Settings;
        showJudgementText = settings.DisplayJudgementText;
        showTimingLabel = settings.DisplayEarlyLateIndicators;
        duration = Duration(grade);
        startTime = game.Time;
        skinClearSlot = null;
        skinFeedbackSlot = null;
        if (sourceNote != null && FlatSkinRuntime.Ready)
        {
            var skin = FlatSkinRuntime.Current;
            var skinGrade = ToSkinGrade(grade);
            var clearSlot = grade == NoteGrade.Miss
                ? skin.Slot(kind, "miss")
                : skin.Slot(kind, "clear:" + SkinTimingTokens.GradeKey(skinGrade));
            if (clearSlot != null && !clearSlot.UsesUnsupported)
            {
                skinClearSlot = clearSlot;
                var feedback = SelectFeedback(showJudgementText, showTimingLabel, hasTiming: true, skinGrade);
                if (feedback.HasValue)
                {
                    var (view, feedbackGrade, _) = feedback.Value;
                    // Timing binds only for the views that carry the label; the signed
                    // press error picks the fast/slow binding (negative = early = FAST).
                    var timing = view == SkinFeedbackView.Judgement
                        ? (SkinTiming?) null
                        : timingError < 0 ? SkinTiming.Fast : SkinTiming.Slow;
                    var feedbackSlot = skin.Feedback(kind, view, feedbackGrade, timing);
                    if (feedbackSlot != null && !feedbackSlot.UsesUnsupported)
                        skinFeedbackSlot = feedbackSlot;
                }
                // One context per pooled instance, refilled per play (no allocation).
                skinContext ??= new SkinDrawContext();
                var alternative = sourceNote.Model.UseAlternativeColor();
                FlatSkinRuntime.FillHeadContext(skinContext, kind,
                    FlatSkinRuntime.PaletteContextOf(game, FlatNoteRendererBase.FamilyFor(sourceNote.Type),
                        alternative ? FlatDirection.Down : FlatDirection.Up,
                        seed, game.Config.NoteRingColors[sourceNote.Type][0]),
                    alternative, approachSeconds);
            }
        }
        if (skinClearSlot == null) return false;
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = Vector3.one;
        ApplyWorldScale(worldScale);
        playing = true;
        gameObject.SetActive(true);
        Render(0);
        return true;
    }

    /// <summary>
    /// Skin-only miss label for misses that ride the head's exit slot: tap kinds
    /// keep their exit in skin mode, so no clear FX spawns for them and only the
    /// judgement feedback is needed. Draws just the feedback slot; when the skin
    /// cannot serve it the effect stays inactive — the head's exit owns the miss
    /// visuals.
    /// </summary>
    internal bool PlayFeedbackOnly(FlatClearFxPool owner, Game sourceGame, Note note,
        float worldScale)
    {
        if (!FlatSkinRuntime.Ready) return false;
        // Settings snapshot like Play: a mid-effect change must never add/drop the label.
        var judgementText = Context.Player.Settings.DisplayJudgementText;
        if (!judgementText) return false;
        // Feedback is bound per note kind, so resolve it before selecting the slot.
        var noteKind = FlatNoteRendererBase.KindFor(note.Type);
        var slot = FlatSkinRuntime.Current.Feedback(noteKind, SkinFeedbackView.Judgement, SkinGrade.Miss, null);
        if (slot == null || slot.UsesUnsupported) return false;

        pool = owner;
        game = sourceGame;
        grade = NoteGrade.Miss;
        kind = noteKind;
        skinClearSlot = null;
        skinFeedbackSlot = slot;
        timingError = 0;
        showJudgementText = judgementText;
        showTimingLabel = false;
        duration = slot.Contract.Duration;
        startTime = game.Time;
        skinContext ??= new SkinDrawContext();
        var alternative = note.Model.UseAlternativeColor();
        FlatSkinRuntime.FillHeadContext(skinContext, kind,
            FlatSkinRuntime.PaletteContextOf(game, FlatNoteRendererBase.FamilyFor(note.Type),
                alternative ? FlatDirection.Down : FlatDirection.Up,
                FlatNoteRendererBase.ResolvePaletteSeed(note), game.Config.NoteRingColors[note.Type][0]),
            alternative, note.Model.start_time - note.Model.intro_time);
        transform.SetPositionAndRotation(MissLabelPosition(note), note.transform.rotation);
        transform.localScale = Vector3.one;
        ApplyWorldScale(worldScale);
        playing = true;
        gameObject.SetActive(true);
        Render(0);
        return true;
    }

    // Spawn origin for the miss label. A CDrag's transform travels its chain
    // (DragHeadNote.OnGameLateUpdate) while its drawn head stays pinned at the
    // anchor — the old imperative path pinned the MISS text origin the same way
    // (DrawMissJudgement → PinToAnchor), and Classic's anchored head never rides
    // the chain either. Ride the anchor for CDrag; every other kind's transform
    // IS its head.
    private static Vector3 MissLabelPosition(Note note)
    {
        if (note.Type != NoteType.CDragHead) return note.transform.position;
        var anchor = note.Model.CalculatePosition(note.Game.Chart);
        var parent = note.transform.parent;
        return parent != null ? parent.TransformPoint(anchor) : anchor;
    }

    /// <summary>
    /// Feedback-mode selection — the C# port of renderFeedback (Design PR #33):
    /// the timing label only coexists with grades that carry timing
    /// (SkinTimingTokens.TimedGrades) and only when a timing was actually judged.
    /// PERFECT/MISS (or absent timing) degrade to judgement-only; with the
    /// judgement text off, a timing-eligible grade renders the label alone; with
    /// both settings off there is no feedback at all. Pure function over plain
    /// values so the contract is unit-testable without Unity objects.
    /// </summary>
    public static (SkinFeedbackView View, SkinGrade Grade, SkinTiming Timing)? SelectFeedback(
        bool judgementText, bool timingLabel, bool hasTiming, SkinGrade grade)
    {
        if (timingLabel && hasTiming && SkinTimingTokens.TimedGrades.Contains(grade))
            return (judgementText ? SkinFeedbackView.Combined : SkinFeedbackView.Timing,
                grade, SkinTiming.Fast);
        if (judgementText) return (SkinFeedbackView.Judgement, grade, SkinTiming.Fast);
        return null;
    }

    private static SkinGrade ToSkinGrade(NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Perfect: return SkinGrade.Perfect;
            case NoteGrade.Great: return SkinGrade.Great;
            case NoteGrade.Good: return SkinGrade.Good;
            case NoteGrade.Bad: return SkinGrade.Bad;
            case NoteGrade.Miss: return SkinGrade.Miss;
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }

    private void Update()
    {
        if (!playing) return;
        if (game == null || game.Time < startTime || game.Time - startTime >= duration)
        {
            pool.Collect(this);
            return;
        }
        Render((game.Time - startTime) / duration);
    }

    internal void OnCollect()
    {
        playing = false;
        normalMesh.SetVisible(false);
        additiveMesh.SetVisible(false);
        clearNormalMesh.SetVisible(false);
        clearAdditiveMesh.SetVisible(false);
        foreach (var mesh in imageRunMeshes) mesh.SetVisible(false);
        foreach (var mesh in clearImageRunMeshes) mesh.SetVisible(false);
        normal.Reset();
        additive.Reset();
        image.Reset();
        clearNormal.Reset();
        clearAdditive.Reset();
        clearImage.Reset();
        pool = null;
        game = null;
        grade = NoteGrade.None;
        kind = default;
        duration = 0;
        textWorldScale = 0;
        startTime = 0;
        timingError = 0;
        showJudgementText = showTimingLabel = false;
        skinClearSlot = null;
        skinFeedbackSlot = null;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (normalMesh != null) normalMesh.Dispose();
        if (additiveMesh != null) additiveMesh.Dispose();
        if (clearNormalMesh != null) clearNormalMesh.Dispose();
        if (clearAdditiveMesh != null) clearAdditiveMesh.Dispose();
        foreach (var mesh in imageRunMeshes) mesh.Dispose();
        imageRunMeshes.Clear();
        foreach (var mesh in clearImageRunMeshes) mesh.Dispose();
        clearImageRunMeshes.Clear();
    }

    private void SetImageMeshScales(float scale)
    {
        foreach (var mesh in imageRunMeshes) mesh.SetWorldScale(scale);
        foreach (var mesh in clearImageRunMeshes) mesh.SetWorldScale(scale);
    }

    /// <summary>Applies the play-time design→world scale to every FX mesh — the
    /// feedback trio, the clear trio and both image run pools (pooled instances
    /// replay at different zoom levels, so no mesh may keep a stale scale; the
    /// clear pair was missed once and rendered design pixels at scale 1).</summary>
    internal void ApplyWorldScale(float worldScale)
    {
        textWorldScale = worldScale;
        normalMesh.SetWorldScale(worldScale);
        additiveMesh.SetWorldScale(worldScale);
        clearNormalMesh.SetWorldScale(worldScale);
        clearAdditiveMesh.SetWorldScale(worldScale);
        SetImageMeshScales(worldScale);
    }

    private void Render(double u)
    {
        normal.Reset();
        additive.Reset();
        clearNormal.Reset();
        clearAdditive.Reset();
        // The clear + feedback slots own all geometry, driven by the judgement
        // clock (seconds since the hit; slot mode Once divides by the contract
        // duration, which equals the engine's Duration(grade) clock).
        // Rotation contract (product spec): the burst graphics ride the FX
        // transform's full world rotation (note heading + storyboard); the
        // judgement / FAST-SLOW text is screen-locked — the feedback range is
        // counter-rotated against that same world rotation every frame
        // (KeepTextUpright). Render runs per frame, so storyboard rotation stays
        // tracked. PlayFeedbackOnly (miss labels) flows through the same path.
        var t = u * duration;
        // Clear and feedback draw into SEPARATE builder trios so each role owns
        // an order band (burst below, label above — see the order constants).
        // Reset the image builders per frame here (not with the other builders
        // above): the painter re-bases its per-draw UV/quad list whenever a draw
        // starts against a freshly emptied image sink, so each builder stays
        // index-aligned with exactly its own role's quads.
        image.Reset();
        clearImage.Reset();
        var visible = !game.State.Mods.Contains(Mod.HideNotes);
        if (skinClearSlot != null)
        {
            Painter.DrawNoteSlot(skinClearSlot, t, skinContext, new SkinSinks(clearNormal, clearAdditive, clearImage));
            clearNormalMesh.Upload(clearNormal);
            clearAdditiveMesh.Upload(clearAdditive);
            clearNormalMesh.SetVisible(visible && clearNormal.VertCount > 0);
            clearAdditiveMesh.SetVisible(visible && clearAdditive.VertCount > 0);
            // Upload the clear runs now: the feedback draw below re-bases the
            // painter's image lists against the (empty) feedback image sink.
            UploadImageRuns(clearImage, clearImageRunMeshes, ClearImageRunOrderBase, visible);
        }
        else
        {
            clearNormalMesh.SetVisible(false);
            clearAdditiveMesh.SetVisible(false);
        }
        if (skinFeedbackSlot != null)
        {
            var firstNormal = normal.VertCount;
            var firstAdditive = additive.VertCount;
            var firstImage = image.VertCount;
            Painter.DrawFeedbackSlot(skinFeedbackSlot, t, skinContext, new SkinSinks(normal, additive, image));
            KeepTextUpright(normal, firstNormal, transform.rotation, textWorldScale);
            KeepTextUpright(additive, firstAdditive, transform.rotation, textWorldScale);
            KeepTextUpright(image, firstImage, transform.rotation, textWorldScale);
        }
        normalMesh.Upload(normal);
        additiveMesh.Upload(additive);
        normalMesh.SetVisible(visible && normal.VertCount > 0);
        additiveMesh.SetVisible(visible && additive.VertCount > 0);
        UploadImageRuns(image, imageRunMeshes, FeedbackImageRunOrderBase, visible);
    }

    // Textured image quads (one quad per image frame, UVs from the painter) upload
    // to Sprites/Default meshes, one role band per call. The painter emits quads in
    // draw order; consecutive same-page quads batch into one run and each run
    // uploads to its own pooled mesh — one mesh per PAGE would batch harder, but
    // merging same-page runs (A₁ → B → A₂) loses the cross-page draw order, and
    // overlapping translucent images would blend in the wrong order.
    private void UploadImageRuns(FlatMeshBuilder source, List<FlatNoteMesh> pool, int orderBase, bool visible)
    {
        foreach (var mesh in pool) mesh.SetVisible(false);
        if (source.VertCount == 0) return;
        var quads = Painter.LastImageQuads;
        var quadCount = source.VertCount / 4;
        if (quads == null || quads.Count < quadCount) return;
        var runStart = 0; // first quad of the current same-page run
        var run = 0;
        for (var q = 1; q <= quadCount; q++)
        {
            if (q < quadCount && quads[q].Texture == quads[runStart].Texture) continue;
            UploadImageRun(source, pool, orderBase, run++, quads[runStart].Texture, runStart * 4, q * 4, visible);
            runStart = q;
        }
    }

    // Scratch staging for one run at a time; the per-run mesh pool grows to the
    // busiest frame and is reused across frames — image FX stay zero-allocation.
    private readonly FlatMeshBuilder imageRun = new FlatMeshBuilder();
    private Vector2[] imageUvBuffer = Array.Empty<Vector2>();

    private void UploadImageRun(FlatMeshBuilder source, List<FlatNoteMesh> pool, int orderBase, int run,
        Texture2D page, int firstVert, int endVert, bool visible)
    {
        if (run >= pool.Count)
        {
            var created = FlatNoteMesh.Create(transform, $"FlatFxImage{imageMeshCounter++}", false, 0);
            pool.Add(created);
        }
        var mesh = pool[run];
        mesh.SetWorldScale(textWorldScale);
        // Run order climbs from the role's base inside GameFx: runs stack in
        // painter order across pages, beneath the role's vector meshes, and can
        // never reach a note's sorting layer.
        mesh.SetSortingOrder(orderBase + run);
        mesh.SetSortingLayer(OverlayLayer);
        var count = endVert - firstVert;
        var uvList = Painter.LastImageUv;
        // Reused per pooled instance (grows only): image FX stay zero-allocation.
        if (imageUvBuffer.Length < count) Array.Resize(ref imageUvBuffer, Math.Max(count, imageUvBuffer.Length * 2));
        var uv = imageUvBuffer;
        for (var i = 0; i < count; i++)
            uv[i] = new Vector2(uvList[(firstVert + i) * 2], uvList[(firstVert + i) * 2 + 1]);
        imageRun.Reset();
        for (var i = firstVert; i < endVert; i++)
        {
            var o = i * 7;
            for (var f = 0; f < 7; f++) imageRun.Verts.Add(source.Verts[o + f]);
        }
        foreach (var index in source.Tris)
            if (index >= firstVert && index < endVert)
                imageRun.Tris.Add(index - firstVert);
        mesh.UploadTextured(imageRun, uv, page, 1f);
        mesh.SetVisible(visible);
    }

    // FlatNoteMesh uploads (x, -y) * worldScale, then the Transform applies
    // rotation. Invert in that same frame, only for the appended text range;
    // the pivot is the text anchor at the local origin, never the effect bounds.
    internal static void KeepTextUpright(FlatMeshBuilder b, int firstVertex, Quaternion rotation, float worldScale)
    {
        if (worldScale == 0) return;
        var inverse = Quaternion.Inverse(rotation);
        for (var i = firstVertex * 7; i < b.Verts.Count; i += 7)
        {
            var point = inverse * new Vector3(b.Verts[i] * worldScale, -b.Verts[i + 1] * worldScale, b.Verts[i + 2]);
            b.Verts[i] = point.x / worldScale;
            b.Verts[i + 1] = -point.y / worldScale;
            b.Verts[i + 2] = point.z;
        }
    }

    private static double Duration(NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Perfect: return FlatTokens.Time.ClearPerfect;
            case NoteGrade.Great: return FlatTokens.Time.ClearGreat;
            case NoteGrade.Good: return FlatTokens.Time.ClearGood;
            case NoteGrade.Bad: return FlatTokens.Time.ClearBad;
            case NoteGrade.Miss: return FlatTokens.Time.ClearMiss;
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }
}
