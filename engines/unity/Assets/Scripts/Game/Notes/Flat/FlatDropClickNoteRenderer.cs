using System;
using UnityEngine;

// Flat drop click (drop.ts dropClick): a static capsule — fill + white ring band + dark
// keyline + white core bar. Drop notes have no enter curve (a still image that falls into
// the scanline), mirroring DropClickNoteRenderer; the fall offset moves only the
// FlatNoteMesh child so the collider stays anchored at the landing scanline.
public class FlatDropClickNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    // Base localPosition of the mesh child, snapshotted ONCE from the pristine child
    // (pool-reuse safety: see DropClickNoteRenderer).
    private Vector3 meshBaseLocal;
    private Vector3 noteIdBaseLocal;

    public FlatDropClickNoteRenderer(ClickNote note) : base(note)
    {
        meshBaseLocal = HeadMesh.transform.localPosition;
        if (DisplayNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    protected override void Render()
    {
        base.Render();
        var localOffset = DropNoteOffset.ComputeLocalOffset(Note);
        HeadMesh.transform.localPosition = meshBaseLocal + localOffset;
        // The ID rides the falling capsule like DropClickNoteRenderer.
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
        // dark keyline on the ring's outer edge (flat overlap separator): the keyline
        // path is grown by (sw - e) so the capsule silhouette is unchanged
        var e = sw * FlatTokens.StrokeEdgeFraction;

        CapsuleFill(b, w - sw, h - sw, Palette.Shade(400));
        CapsuleStroke(b, w - sw / 2, h - sw / 2, sw, Palette.RingColor);
        CapsuleStroke(b, w - sw / 2 + (sw - e), h - sw / 2 + (sw - e), e, Edge);
        CapsuleFill(b, w * 0.5, h * 0.26, Palette.RingColor);
    }

    private static void CapsuleFill(FlatMeshBuilder b, double w, double h, Rgba color)
    {
        FlatGeom.RoundedRect(b, -w / 2, -h / 2, w, h, h / 2, color);
    }

    // Strokes the rounded-rect path (w × h, rx = h/2): two straight edges + two
    // semicircle ends — the same decomposition FlatClearFx uses for capsule outlines.
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
