using System;
using UnityEngine;

// Flat drag head. Regular drags port drag.ts headDraw: fill disk + chain arrow +
// two-arc assembling ring, waking 200 shade steps over the click's WAKE window.
// Click-drag heads port click.ts clickDragHead: drawn exactly like a CLICK, no
// arrow — only the chain (child beads + path) says it is a drag start — with the
// click-drag family palette. The note GameObject already faces the chain
// (Note.OnGameLateUpdate), so the regular head's arrow is drawn at heading 0 and
// the GO rotation carries it.
public class FlatDragHeadNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    // drag.ts STEADY: ring and arrow settle by this progress.
    private const double Steady = 0.2;

    private readonly double[] arrow = new double[8];
    private readonly FlatMeshBuilder indicatorBuilder = new FlatMeshBuilder();
    private readonly FlatNoteMesh IndicatorMesh;
    private readonly Vector3 noteIdBaseLocal;
    private Quaternion? pinnedRotation;
    private bool IsCDrag => Note.Type == NoteType.CDragHead;
    protected override Quaternion ExitLocalRotation => pinnedRotation ?? base.ExitLocalRotation;

    // A connected regular drag head is the chain's anchor: past its hit time it
    // holds its last frame instead of riding the exit — the children keep
    // travelling, so a rewind or fade would strand the drag without a head. The
    // hold starts in the unjudged window, so the miss never pops the size back
    // mid-rewind, and a late save is seamless. Chainless heads and click-drag
    // heads (pinned at their anchor; the separate indicator owns the chain)
    // keep the design's exit.
    private bool HoldsSteady =>
        !IsCDrag && ((DragHeadNote) Note).EndNoteModel != null && Game.Time > Note.Model.start_time;

    protected override bool Exiting => base.Exiting && !HoldsSteady;

    // Following changes the counter-offset even when progress and palette settle.
    protected override bool GeometryChangesEveryFrame => IsCDrag;

    public FlatDragHeadNoteRenderer(DragHeadNote note) : base(note)
    {
        IndicatorMesh = FlatNoteMesh.Create(note.transform, "FlatDragIndicator", false, 0);
        IndicatorMesh.SetVisible(false);
        if (DisplayNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    protected override FlatFamily Family => IsCDrag ? FlatFamily.ClickDrag : FlatFamily.Drag;

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        pinnedRotation = null;
        IndicatorMesh.SetSortingOrder(SortingOrder - 1);
        IndicatorMesh.SetWorldScale(WorldScale);
        IndicatorMesh.SetVisible(false);
        // Note renders in Game's update BEFORE DragHeadNote moves/rotates in its
        // game-late-update. Refresh CDrag after both, using the displayed pose.
        HeadMesh.UpdateGeometryAfterMovement = IsCDrag ? (Action) Render : null;
    }

    public override void OnCollect()
    {
        HeadMesh.UpdateGeometryAfterMovement = null;
        IndicatorMesh.SetVisible(false);
        base.OnCollect();
        pinnedRotation = null;
        if (DisplayNoteId)
        {
            NoteId.transform.localPosition = noteIdBaseLocal;
            NoteId.transform.localRotation = Quaternion.identity;
        }
    }

    public override void OnClear(NoteGrade grade)
    {
        if (IsCDrag)
        {
            pinnedRotation = Note.transform.localRotation;
            if (DisplayNoteId) NoteId.gameObject.SetActive(false);
        }
        base.OnClear(grade);
    }

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (IsCDrag || HeadVisible || !(ClearedSuccessfully || HoldsSteady) ||
            Note.IsDragStackFollower || Game.State.Mods.Contains(Mod.HideNotes))
            return;
        // Regular drag heads are their own travelling indicator — on a clear and,
        // via HoldsSteady, on a miss past the hit time. CDrag heads disappear on
        // success; their separate indicator owns the chain window.
        var end = ((DragHeadNote) Note).EndNoteModel;
        if (end != null && ((DragHeadNote) Note).IsChainActive(Game))
        {
            HeadVisible = true;
            HeadMesh.SetVisible(true);
            // Entering indicator mode changes the geometry even if progress is 1.
            InvalidateGeometry();
        }
    }

    private bool IndicatorVisible()
    {
        var end = ((DragHeadNote) Note).EndNoteModel;
        // An early-tapped head shows the indicator immediately: the transform is
        // still parked at the head position until start_time, so it departs from
        // the tap spot exactly when the chain does.
        return IsCDrag && !Note.IsDragStackFollower && !Game.State.Mods.Contains(Mod.HideNotes) &&
               (ClearedSuccessfully || Game.Time >= Note.Model.start_time) && end != null &&
               ((DragHeadNote) Note).IsChainEndPending(Game);
    }

    // Regular drag heads ARE the travelling head, so their ID lives on the chain
    // window (ClassicDragHeadNoteRenderer). CDrag heads keep the base click lifecycle.
    protected override bool NoteIdVisible
    {
        get
        {
            if (IsCDrag) return ((DragHeadNote) Note).IsHeadNoteIdActive(Game);
            var end = ((DragHeadNote) Note).EndNoteModel;
            return end != null && ((DragHeadNote) Note).IsChainActive(Game);
        }
    }

    protected override void UpdateNoteId()
    {
        base.UpdateNoteId();
        if (!DisplayNoteId || !IsCDrag || !NoteId.gameObject.activeSelf) return;
        // The CDrag transform travels with the chain while the drawn head stays
        // anchored (PinToAnchor, mesh space). The NoteId GameObject needs the same
        // pin in transform space: convert the anchor offset into child space,
        // cancelling the travelling parent's rotation AND scale.
        var transform = Note.transform;
        var scale = transform.localScale;
        if (scale.x == 0 || scale.y == 0 || scale.z == 0 ||
            float.IsNaN(scale.sqrMagnitude) || float.IsInfinity(scale.sqrMagnitude)) return;
        var rotation = Quaternion.Inverse(transform.localRotation);
        var offset = rotation * (Note.Model.CalculatePosition(Game.Chart) - transform.localPosition);
        offset = new Vector3(offset.x / scale.x, offset.y / scale.y, offset.z / scale.z);
        NoteId.transform.localPosition = noteIdBaseLocal + offset;
    }

    protected override void Render()
    {
        base.Render();
        var visible = IndicatorVisible();
        IndicatorMesh.SetVisible(visible);
        if (!visible) return;
        // The chain departs on chart time, independently of the anchored head's
        // judgement, rewind, scale and alpha. Retain storyboard size/opacity.
        var scale = BaseTransformSize * EnterScale;
        var cancel = scale != 0 ? 1 / scale : 1;
        IndicatorMesh.transform.localScale = new Vector3(cancel, cancel, 1);
        indicatorBuilder.Reset();
        BuildDragHeadGeometry(indicatorBuilder, 1, FlatTokens.Unit * FlatTokens.Size(NoteKind.DragHead),
            DesignSize, Palette, false);
        for (var i = 6; i < indicatorBuilder.Verts.Count; i += 7) indicatorBuilder.Verts[i] *= EasedOpacity;
        IndicatorMesh.Upload(indicatorBuilder);
    }

    protected override void BuildGeometry(FlatMeshBuilder b)
    {
        if (!IsCDrag)
        {
            DrawHead(b, EnterProgress, EnterScale, DesignSize, Palette, false, ClearedSuccessfully, Exiting);
            return;
        }

        if (ClearedSuccessfully) return;
        if (Exiting && !pinnedRotation.HasValue) pinnedRotation = Note.transform.localRotation;
        var first = b.VertCount;
        BuildClickDragGeometry(b, EnterProgress, EnterScale, DesignSize, Palette);
        PinToAnchor(b, first, Exiting ? pinnedRotation : null);
    }

    public override void Dispose()
    {
        IndicatorMesh.Dispose();
        base.Dispose();
    }

    protected override void DrawMissJudgement(FlatMeshBuilder b)
    {
        var first = b.VertCount;
        base.DrawMissJudgement(b);
        // The base already counter-rotates text into the world frame. Only its
        // origin needs pinning; rotating it again would undo that compensation.
        if (IsCDrag) PinToAnchor(b, first);
    }

    private void PinToAnchor(FlatMeshBuilder b, int firstVertex, Quaternion? orientation = null)
    {
        // CalculatePosition and localPosition are in the note layer's frame.
        // Undo the note's LOCAL rotation before upload re-applies it; using world
        // rotation here would incorrectly undo the layer's rotation as well.
        var offset = Quaternion.Inverse(Note.transform.localRotation) *
                     (Note.Model.CalculatePosition(Game.Chart) - Note.transform.localPosition);
        // The mesh cancels BaseTransformSize, leaving enter/storyboard scale.
        // Upload scales x/y by WorldScale and flips y, but leaves z in world units.
        var scale = Vector3.Scale(Note.transform.localScale, HeadMesh.transform.localScale);
        if (WorldScale == 0 || scale.x == 0 || scale.y == 0 || scale.z == 0) return;
        // Freeze the head at its captured pose before adding the anchor offset.
        // Work in upload's (x, -y) frame so inverse live Z rotation has the right
        // sign, including on a rotated note layer.
        var rotation = orientation.HasValue
            ? Quaternion.Inverse(Note.transform.localRotation) * orientation.Value : Quaternion.identity;
        for (var i = firstVertex * 7; i < b.Verts.Count; i += 7)
        {
            var point = rotation * new Vector3(b.Verts[i] * WorldScale * scale.x,
                -b.Verts[i + 1] * WorldScale * scale.y, b.Verts[i + 2] * scale.z);
            b.Verts[i] = (point.x + offset.x) / (WorldScale * scale.x);
            b.Verts[i + 1] = -(point.y + offset.y) / (WorldScale * scale.y);
            b.Verts[i + 2] = (point.z + offset.z) / scale.z;
        }
    }

    protected override Action<FlatMeshBuilder, float, float> CaptureExitHead()
    {
        var size = DesignSize;
        var palette = Palette;
        var clickDrag = Note.Type == NoteType.CDragHead;
        return (b, p, scale) => DrawHead(b, p, scale, size, palette, clickDrag, false, true);
    }

    private void DrawHead(FlatMeshBuilder b, double p, float enterScale, double designSize,
        FlatPalette palette, bool clickDrag, bool indicator, bool exiting)
    {
        // The body rewinds its own approach; the regular head's directional arrow
        // stays built throughout the exit. Only a successful regular-drag clear
        // settles the head. CDrag indicators use their separate mesh; this wrapper
        // never draws one.
        if (clickDrag)
            BuildClickDragGeometry(b, p, enterScale, designSize, palette);
        else
            BuildDragHeadGeometry(b, indicator ? 1 : p, designSize, designSize, palette, exiting);
    }

    private void BuildDragHeadGeometry(FlatMeshBuilder b, double p, double size, double designSize, FlatPalette palette, bool exiting)
    {
        var r = size / 2;
        var w = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(designSize / FlatTokens.Unit);
        var alpha = FlatEase.Seg(p, 0, 0.06);
        var k = FlatEase.OutCubic(FlatEase.Seg(p, 0, Steady));
        var wake = WakeAmount(p);

        FlatGeom.Disk(b, 0, 0, r - w + 0.5, WithOpacity(palette.Tone(WakeShade(400, wake)), alpha));
        BuildArrow(b, (r - w) * 0.95, exiting ? 1 : FlatEase.OutCubic(FlatEase.Seg(p, Steady * 0.3, Steady)),
            wake, exiting ? 1 : alpha, palette);
        AssemblingRing(b, r - w / 2, w, WithOpacity(palette.RingTone(wake), alpha), 2, k,
            FlatEase.Lerp(-Math.PI / 2, 0, k), WithOpacity(Edge, alpha));
    }

    // click.ts clickDragHead: the click enter, verbatim — no arrow glyph.
    private void BuildClickDragGeometry(FlatMeshBuilder b, double p, float enterScale, double designSize, FlatPalette palette)
    {
        var r = designSize / 2;
        var w = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(designSize / FlatTokens.Unit);
        var hair = FlatTokens.Unit * FlatTokens.StrokeHair;
        var inner = r - w;
        var alpha = FlatEase.OutCubic(FlatEase.Seg(p, 0, 0.1));
        var build = FlatEase.OutQuart(FlatEase.Seg(p, 0, 0.4));
        var split = FlatEase.OutQuad(FlatEase.Seg(p, 0.4, 0.85));
        // The click-drag head is the click enter (click.ts clickDragHead), so it
        // carries the click's frame thickening: the frame grows inward by half a
        // stroke over p 0.7-1 while the core rises onto its final inner edge — no
        // base rim left at the hit.
        var frameW = FlatEase.Lerp(w * 0.45, w, build) *
                     FlatEase.Lerp(1, 1.5, FlatEase.InQuad(FlatEase.Seg(p, 0.7, 1)));
        var coreR = (inner - w / 2) * FlatEase.Lerp(0.36, 1, FlatEase.InQuad(FlatEase.Seg(p, 0.42, 0.94)));
        var blink = Math.Max(FlatEase.Bump(p, 0.86, 0.92), FlatEase.Bump(p, 0.92, 1));

        // click.ts places the approach outside the scaled body group.
        var approach = FlatEase.Seg(p, 0.7, 1);
        if (approach > 0 && approach < 1 && enterScale != 0)
        {
            var first = b.VertCount;
            FlatGeom.Ring(b, 0, 0, FlatEase.Lerp(r * 1.83, r - w / 2, approach),
                hair * FlatEase.Lerp(0.375, 0.525, approach),
                WithOpacity(palette.RingColor, alpha * 0.5 * FlatEase.Seg(approach, 0, 0.15)));
            ScaleGeometry(b, first, 1 / enterScale);
        }

        FlatGeom.Disk(b, 0, 0, inner + 0.5, WithOpacity(palette.Tone(FlatEase.Lerp(700, 800, split)), alpha));
        if (split > 0)
            FlatGeom.Disk(b, 0, 0, coreR, WithOpacity(palette.Tone(FlatEase.Lerp(700, 400, split)), alpha * split));
        if (blink > 0)
            FlatGeom.Disk(b, 0, 0, coreR, WithOpacity(palette.RingColor, alpha * 0.3 * blink));
        // frame: outer edge pinned at R, thickens inward by half a stroke over 0.7-1
        AssemblingRing(b, r - frameW / 2, frameW,
            WithOpacity(palette.RingTone(split), alpha), 3, build, FlatEase.Lerp(-Math.PI / 3, 0, build),
            WithOpacity(Edge, alpha));
    }

    // drag.ts dragArrow: arrowHead(inner * 1.05), opacity show, scale lerp(0.6, 1, show).
    private void BuildArrow(FlatMeshBuilder b, double inner, double show, double wake, double alpha, FlatPalette palette)
    {
        if (show <= 0) return;
        var s = inner * 1.05 / 2 * FlatEase.Lerp(0.6, 1, show);
        arrow[0] = 0;
        arrow[1] = -s;
        arrow[2] = s * 0.82;
        arrow[3] = s * 0.78;
        arrow[4] = 0;
        arrow[5] = s * 0.3;
        arrow[6] = -s * 0.82;
        arrow[7] = s * 0.78;
        FlatGeom.PolyFill(b, arrow, WithOpacity(palette.RingTone(wake), alpha * show));
    }
}
