using System;
using UnityEngine;

// Flat drop drag (drop.ts dropDrag): the classic capsule build, shorter and squatter,
// no core — the classic sprites carry no drag marker, the 0.8 kind size is the only
// cue (drop drags are never connected). Same falling/size behavior as
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
        var w = DesignSize * FlatDropDesign.DragCapsule.W;
        var h = DesignSize * FlatDropDesign.DragCapsule.H;
        var ringWidth = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(DesignSize / FlatTokens.Unit);

        CapsuleFill(b, w, h, Palette.RingColor);
        CapsuleFill(b, w * FlatDropDesign.DragFill.W, h * FlatDropDesign.DragFill.H, Palette.Shade(400));
        // Stepped edge (FlatEdge): a band whose centreline sits `offset` from the
        // silhouette is the capsule grown by 2·offset (rx = h/2 keeps it parallel).
        for (var i = 0; i < FlatEdge.Count; i++)
        {
            if (!FlatEdge.Band(i, ringWidth, out var offset, out var bandWidth, out var opacity)) continue;
            CapsuleStroke(b, w + 2 * offset, h + 2 * offset, bandWidth, WithOpacity(Edge, opacity));
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
