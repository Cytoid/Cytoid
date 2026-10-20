using System;
using UnityEngine;

// Flat drop click (drop.ts dropClick): a static capsule ported from Cytoid's Classic
// sprite design — ring-colour body capsule, fill capsule on top, dark keyline on the
// outer edge, pure white core bar, at the sprites' measured proportions. Drop notes
// have no enter curve (a still image that falls into the scanline), mirroring
// DropClickNoteRenderer; the fall offset moves only the FlatNoteMesh child so the
// collider stays anchored at the landing scanline.
public class FlatDropClickNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");
    // The classic core bar is pure white, not palette-driven (Cytoid: Color.white).
    private static readonly Rgba Core = new Rgba(1, 1, 1, 1);

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
        var w = DesignSize * FlatDropDesign.ClickCapsule.W;
        var h = DesignSize * FlatDropDesign.ClickCapsule.H;
        var ringWidth = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(DesignSize / FlatTokens.Unit);

        CapsuleFill(b, w, h, Palette.RingColor);
        CapsuleFill(b, w * FlatDropDesign.ClickFill.W, h * FlatDropDesign.ClickFill.H, Palette.Shade(400));
        // Stepped edge (FlatEdge): a band whose centreline sits `offset` from the
        // silhouette is the capsule grown by 2·offset (rx = h/2 keeps it parallel).
        for (var i = 0; i < FlatEdge.Count; i++)
        {
            if (!FlatEdge.Band(i, ringWidth, out var offset, out var bandWidth, out var opacity)) continue;
            CapsuleStroke(b, w + 2 * offset, h + 2 * offset, bandWidth, WithOpacity(Edge, opacity));
        }
        CapsuleFill(b, w * FlatDropDesign.ClickCore.W, h * FlatDropDesign.ClickCore.H, Core);
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
