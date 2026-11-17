using System;
using UnityEngine;

// Flat drag head: the head visuals (regular drags AND click-drag heads) draw from
// the compiled skin's slots in FlatNoteRendererBase (DrawSkinHead). This subclass
// keeps the chain lifecycle: a connected regular drag head holds its last frame
// instead of rewinding (the children keep travelling), click-drag heads pin their
// drawn head and NoteId to the anchored head pose (Classic's UpdateCDragHeadPosition),
// and the SEPARATE chain indicator — the drag arrow shown while the chain is
// pending — is still drawn imperatively here (it has no skin slot).
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
    private Quaternion? approachRotation;
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
        approachRotation = null;
        HeadMesh.transform.localPosition = Vector3.zero;
        HeadMesh.transform.localRotation = Quaternion.identity;
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
        approachRotation = null;
        HeadMesh.transform.localPosition = Vector3.zero;
        HeadMesh.transform.localRotation = Quaternion.identity;
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
            // A miss freezes the head at its approach direction, not wherever the
            // travelling transform was pointing when the miss threshold fired.
            pinnedRotation = grade == NoteGrade.Miss
                ? approachRotation ?? Note.transform.localRotation
                : Note.transform.localRotation;
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
        // anchored (mesh space). The NoteId GameObject needs the same pin in
        // transform space: convert the anchor offset into child space,
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

    // A regular head past its hit time (chain alive) draws the declarative `active`
    // slot — seconds since the trigger — instead of the frozen enter pose. Falls
    // back to the enter slot at progress 1 when the skin doesn't bind it (the
    // previous behaviour). CDrag heads keep the base lifecycle: they pin at the
    // anchor and the separate indicator owns the chain window.
    private bool UsesActiveSlot(CompiledSkin skin)
    {
        if (IsCDrag || Exiting || !HoldsSteady) return false;
        var active = skin.Slot(Kind, "active");
        return active is { UsesUnsupported: false, UsesImages: false };
    }

    // While the head follows its chain, the active slot's clock (seconds since
    // the trigger) drives rebuilds — the base enter progress saturates at 1 and
    // would freeze the mesh even for animated active slots.
    protected override double SkinHeadDriver(CompiledSkin skin)
    {
        if (UsesActiveSlot(skin)) return Game.Time - Note.Model.start_time;
        return base.SkinHeadDriver(skin);
    }

    protected override void DrawSkinHead(FlatMeshBuilder b)
    {
        var skin = FlatSkinRuntime.Current;
        if (UsesActiveSlot(skin))
        {
            using (FlatSkinRuntime.PaintMarker.Auto())
                SkinPainterInstance.DrawNoteSlot(skin.Slot(Kind, "active"), SkinHeadDriver(skin), SkinHeadContext(), new SkinSinks(b));
            return;
        }
        base.DrawSkinHead(b);
    }

    // A CDrag note's transform travels its chain (DragHeadNote.OnGameLateUpdate),
    // but the drawn head must hold the tap spot — Classic pins its sprites every
    // frame (UpdateCDragHeadPosition); the skin path does the same in mesh-
    // transform space, which needs no geometry rebuilds. The direction freezes at
    // the chain's departure (approach) and again at judgment (pinnedRotation);
    // cleared heads hide entirely in OnClear, so this only ever shows an
    // unjudged or missed head holding its anchor.
    private void UpdateAnchorPin()
    {
        var transform = Note.transform;
        var scale = transform.localScale;
        if (scale.x == 0 || scale.y == 0 || scale.z == 0 ||
            float.IsNaN(scale.sqrMagnitude) || float.IsInfinity(scale.sqrMagnitude)) return;
        if (Game.Time >= Note.Model.start_time && approachRotation == null)
            approachRotation = transform.localRotation;
        var inverse = Quaternion.Inverse(transform.localRotation);
        var offset = inverse * (Note.Model.CalculatePosition(Game.Chart) - transform.localPosition);
        var position = new Vector3(offset.x / scale.x, offset.y / scale.y, offset.z / scale.z);
        var frozen = pinnedRotation ?? approachRotation;
        var rotation = frozen.HasValue
            ? Quaternion.Euler(0, 0, frozen.Value.eulerAngles.z - transform.localEulerAngles.z)
            : Quaternion.identity;
        HeadMesh.transform.localPosition = position;
        HeadMesh.transform.localRotation = rotation;
        if (additiveHeadMesh != null)
        {
            additiveHeadMesh.transform.localPosition = position;
            additiveHeadMesh.transform.localRotation = rotation;
        }
    }

    protected override void Render()
    {
        base.Render();
        if (IsCDrag) UpdateAnchorPin();
        var visible = IndicatorVisible();
        IndicatorMesh.SetVisible(visible);
        if (!visible) return;
        // The chain departs on chart time, independently of the anchored head's
        // judgement, rewind, scale and alpha. Retain storyboard size/opacity.
        var cancel = BaseTransformSize != 0 ? 1 / BaseTransformSize : 1;
        IndicatorMesh.transform.localScale = new Vector3(cancel, cancel, 1);
        indicatorBuilder.Reset();
        BuildDragHeadGeometry(indicatorBuilder, 1, FlatTokens.Unit * FlatTokens.Size(NoteKind.DragHead),
            DesignSize, Palette, false);
        for (var i = 6; i < indicatorBuilder.Verts.Count; i += 7) indicatorBuilder.Verts[i] *= EasedOpacity;
        IndicatorMesh.Upload(indicatorBuilder);
    }

    public override void Dispose()
    {
        IndicatorMesh.Dispose();
        base.Dispose();
    }

    // Chain-indicator head: drag.ts headDraw at p = 1 (settled pose) — fill disk +
    // chain arrow + assembling ring. Drawn on the separate indicator mesh, which
    // rides the travelling note transform; the SKIN head stays anchored elsewhere.
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
