using System;
using UnityEngine;

// Flat drop drag (drop.ts dropDrag): a shorter static capsule with two notches instead of
// a core (drop drags are never connected). Same falling/size behavior as
// FlatDropClickNoteRenderer — SYNC-WARNING: the offset plumbing deliberately mirrors the
// Classic drop renderer pair (DropClickNoteRenderer / DropDragNoteRenderer), which also
// keep two parallel implementations instead of a shared base.
public class FlatDropDragNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    // Base localPosition of the mesh child, snapshotted ONCE from the pristine child
    // (pool-reuse safety: see DropDragNoteRenderer).
    private Vector3 meshBaseLocal;
    private Vector3 noteIdBaseLocal;

    public FlatDropDragNoteRenderer(Note note) : base(note)
    {
        meshBaseLocal = HeadMesh.transform.localPosition;
        if (DisplayNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    protected override void Render()
    {
        base.Render();
        var localOffset = DropNoteOffset.ComputeLocalOffset(Note);
        HeadMesh.transform.localPosition = meshBaseLocal + localOffset;
        // The ID rides the falling capsule like DropDragNoteRenderer.
        if (DisplayNoteId) DropNoteOffset.ApplyToChild(NoteId.transform, noteIdBaseLocal, localOffset);
    }

    public override void OnCollect()
    {
        base.OnCollect();
        HeadMesh.transform.localPosition = meshBaseLocal;
        if (DisplayNoteId) NoteId.transform.localPosition = noteIdBaseLocal;
    }

    // Drop notes ignore the enter-size curve: always full BaseTransformSize.
    protected override void UpdateTransformScale()
    {
        var transformSize = BaseTransformSize * Note.Model.Override.SizeMultiplier;
        Note.transform.localScale = new Vector3(transformSize, transformSize, Note.transform.localScale.z);
        var meshScale = BaseTransformSize != 0 ? 1 / BaseTransformSize : 1;
        HeadMesh.transform.localScale = new Vector3(meshScale, meshScale, 1);
        EnterScale = 1;
    }

    protected override void BuildGeometry(FlatMeshBuilder b)
    {
        // drop.ts DROP_ASPECT { w: 1.15, h: 0.3 }
        var w = DesignSize * 1.15;
        var h = DesignSize * 0.3;
        var sw = h * 0.2;
        var e = sw * FlatTokens.StrokeEdgeFraction;

        CapsuleFill(b, w - sw, h - sw, Palette.Shade(400));
        CapsuleStroke(b, w - sw / 2, h - sw / 2, sw, Palette.RingColor);
        CapsuleStroke(b, w - sw / 2 + (sw - e), h - sw / 2 + (sw - e), e, Edge);

        // two vertical notches mark the note as a drag
        var nh = h * 0.42;
        for (var s = -1; s <= 1; s += 2)
        {
            var x = s * w * 0.16;
            FlatGeom.Line(b, x, -nh / 2, x, nh / 2, sw * 0.9, Palette.Shade(600), FlatCap.Round);
        }
    }

    private static void CapsuleFill(FlatMeshBuilder b, double w, double h, Rgba color)
    {
        FlatGeom.RoundedRect(b, -w / 2, -h / 2, w, h, h / 2, color);
    }

    private static void CapsuleStroke(FlatMeshBuilder b, double w, double h, double width, Rgba color)
    {
        var radius = h / 2;
        var end = (w - h) / 2;
        FlatGeom.Line(b, -end, -radius, end, -radius, width, color, FlatCap.Butt);
        FlatGeom.Line(b, -end, radius, end, radius, width, color, FlatCap.Butt);
        FlatGeom.Arc(b, end, 0, radius, 0, Math.PI, width, color);
        FlatGeom.Arc(b, -end, 0, radius, Math.PI, Math.PI * 2, width, color);
    }
}
