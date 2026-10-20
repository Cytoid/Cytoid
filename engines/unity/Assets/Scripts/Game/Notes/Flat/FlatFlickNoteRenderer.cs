using System;

// Flat flick (flick.ts flickEnter): diamond outline drawn edge by edge, click-style late
// core with double blink and centre slit, side chevrons that dash in on a short overshoot
// flight and relax onto the body, plus a fainter approach pair converging over the last
// 30 %. Like the classic flick arrows, the chevrons are note-local: the note GO rotation
// (chart rotation / next-note facing) carries them, so no heading math lives here.
public class FlatFlickNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    // flick.ts ARROW_SHAPE: the chevron flight shares the size curve's S shape.
    private static readonly Func<double, double> ArrowShape = FlatEase.CubicBezier(
        FlatTokens.SizeCurveShape.X1, 0, FlatTokens.SizeCurveShape.X2, 1);

    private readonly double[] chevron = new double[6];
    private readonly double[] diamond = new double[8];

    public FlatFlickNoteRenderer(FlickNote note) : base(note)
    {
    }

    protected FlickNote FlickNote => (FlickNote) Note;

    // FlatClearFxPool swaps the note's world rotation for these FX-frame degrees when
    // set (0 = right, 180 = left), so the streaks point along the actual swipe instead
    // of the note frame. The diamond/sectors are 4-fold symmetric, so only the streak
    // chevrons carry the flip. 0 (no directional swipe, auto) keeps the note rotation.
    protected override float? FlickClearRotationDegrees =>
        FlickNote.LastSwipeDirection == 0 ? null : FlickNote.LastSwipeDirection > 0 ? 0f : 180f;

    protected override void BuildGeometry(FlatMeshBuilder b) =>
        DrawEnter(b, EnterProgress, EnterScale, DesignSize, Palette);

    protected override Action<FlatMeshBuilder, float, float> CaptureExitHead()
    {
        var size = DesignSize;
        var palette = Palette;
        return (b, p, scale) => DrawEnter(b, p, scale, size, palette);
    }

    private void DrawEnter(FlatMeshBuilder b, double p, float enterScale, double size, FlatPalette palette)
    {
        var r = size / 2;
        // Full unit stroke — the flick does not thin its ring on small notes (flick.ts).
        var w = FlatTokens.Unit * FlatTokens.StrokeRing;
        // diamond vertex radius such that the outline fits the note box
        var rv = r - w * 0.7;
        var inner = rv - w * 1.1;
        var a = FlatEase.OutCubic(FlatEase.Seg(p, 0, 0.12));
        var build = FlatEase.OutQuart(FlatEase.Seg(p, 0, 0.45));

        var near = r * 0.98 + w * 0.6;
        var far = near + FlatTokens.Unit * 0.3;
        var x = FlatEase.Lerp(far, near, ArrowTravel(p));
        var arrowSize = r * 0.62;
        var arrowOpacity = FlatEase.OutCubic(FlatEase.Seg(p, 0.32, 0.45));

        var splitK = FlatEase.OutQuad(FlatEase.Seg(p, 0.4, 0.85));
        var core = FlatEase.Lerp(0.36, 1, FlatEase.InQuad(FlatEase.Seg(p, 0.42, 0.94)));
        var blink = Math.Max(FlatEase.Bump(p, 0.86, 0.92), FlatEase.Bump(p, 0.92, 1));
        // Click's frame thickening: from p 0.7 the outline thickens inward (outer
        // edge pinned — the centreline shifts inward by (thick-1)*w / 2cos45) while
        // the core rises onto its final inner edge: a core diamond of vertex radius
        // rv - w/cos45 has its edges on that boundary, so the two coincide at p = 1.
        var thick = FlatEase.Lerp(1, 1.5, FlatEase.InQuad(FlatEase.Seg(p, 0.7, 1)));
        var fw = FlatEase.Lerp(w * 0.45, w, build) * thick;
        var fr = rv - (thick - 1) * w / (2 * Math.Cos(Math.PI / 4));
        var coreMax = rv - w / Math.Cos(Math.PI / 4);

        // The chevron pairs live outside the scaled body group, so they cancel the
        // mesh-level enterScale exactly like the click approach ring does.
        var ap = FlatEase.Seg(p, 0.7, 1);
        if (ap > 0 && ap < 1 && enterScale != 0)
        {
            var first = b.VertCount;
            ChevronPair(b, FlatEase.Lerp(near + FlatTokens.Unit * 0.35, near, ap), w * 0.6,
                WithOpacity(palette.RingColor, a * 0.5 * FlatEase.Seg(ap, 0, 0.15)), arrowSize);
            ScaleGeometry(b, first, 1 / enterScale);
        }
        if (arrowOpacity > 0 && enterScale != 0)
        {
            var first = b.VertCount;
            ChevronPair(b, x, w * 0.85, WithOpacity(palette.RingTone(splitK), a * arrowOpacity), arrowSize);
            ScaleGeometry(b, first, 1 / enterScale);
        }

        // Body group: the base enterScale applies its clickSize(p) scale. Emission order
        // = flick.ts body children (fill, core, blink, slit, outline).
        FillDiamond(b, inner + w * 0.15, WithOpacity(palette.Tone(FlatEase.Lerp(700, 800, splitK)), a));
        if (splitK > 0)
            FillDiamond(b, coreMax * core, WithOpacity(palette.Tone(FlatEase.Lerp(700, 400, splitK)), a * splitK));
        if (blink > 0)
            FillDiamond(b, coreMax * core, WithOpacity(palette.RingColor, a * 0.3 * blink));
        // centre slit: flat nod to Cytoid's split-diamond flick fill
        FlatGeom.Line(b, 0, -inner * 0.5 * core, 0, inner * 0.5 * core, w * 0.5,
            WithOpacity(palette.Shade(600), a * FlatEase.OutCubic(FlatEase.Seg(p, 0.4, 0.75))), FlatCap.Butt);
        BuildingDiamond(b, fr, fw,
            WithOpacity(palette.RingTone(splitK), a), build, WithOpacity(Edge, a));
    }

    // flick.ts buildingDiamond: each edge grows from its midpoint (square-capped segments)
    // until the outline closes into one miter-stroked square; the stepped dark edge
    // (FlatEdge) rides the outer silhouette — a band whose centreline sits `offset`
    // from the silhouette along the edge normal has vertex radius
    // r + (width/2 + offset)/cos 45°, so every band's boundaries (miter tips included)
    // stay parallel to the white band's.
    private void BuildingDiamond(FlatMeshBuilder b, double r, double width, Rgba color, double k, Rgba edgeColor)
    {
        DiamondBand(b, r, width, color, k);
        var c45 = Math.Cos(Math.PI / 4);
        for (var i = 0; i < FlatEdge.Count; i++)
        {
            if (!FlatEdge.Band(i, width, out var offset, out var bandWidth, out var opacity)) continue;
            DiamondBand(b, r + (width / 2 + offset) / c45, bandWidth, WithOpacity(edgeColor, opacity), k);
        }
    }

    private void DiamondBand(FlatMeshBuilder b, double r, double width, Rgba color, double k)
    {
        DiamondPoints(diamond, r);
        if (k >= 0.999)
        {
            FlatGeom.PolyStroke(b, diamond, true, width, color, FlatJoin.Miter, FlatCap.Butt);
            return;
        }
        for (var i = 0; i < 4; i++)
        {
            var j = (i + 1) % 4;
            var mx = (diamond[i * 2] + diamond[j * 2]) / 2;
            var my = (diamond[i * 2 + 1] + diamond[j * 2 + 1]) / 2;
            FlatGeom.Line(b,
                FlatEase.Lerp(mx, diamond[i * 2], k), FlatEase.Lerp(my, diamond[i * 2 + 1], k),
                FlatEase.Lerp(mx, diamond[j * 2], k), FlatEase.Lerp(my, diamond[j * 2 + 1], k),
                width, color, FlatCap.Square);
        }
    }

    // flick.ts flickArrow/pair: ">" chevrons with a 90° opening (same as the diamond
    // corner), tip at ±px; the left one is the right rotated π about the note centre.
    private void ChevronPair(FlatMeshBuilder b, double px, double width, Rgba color, double arrowSize)
    {
        var cx = arrowSize * 0.3;
        var h = arrowSize * 0.6;
        chevron[0] = px + cx - h;
        chevron[1] = -h;
        chevron[2] = px + cx;
        chevron[3] = 0;
        chevron[4] = px + cx - h;
        chevron[5] = h;
        FlatGeom.PolyStroke(b, chevron, false, width, color, FlatJoin.Miter, FlatCap.Butt);
        chevron[0] = h - px - cx;
        chevron[1] = h;
        chevron[2] = -px - cx;
        chevron[3] = 0;
        chevron[4] = h - px - cx;
        chevron[5] = -h;
        FlatGeom.PolyStroke(b, chevron, false, width, color, FlatJoin.Miter, FlatCap.Butt);
    }

    private void FillDiamond(FlatMeshBuilder b, double r, Rgba color)
    {
        DiamondPoints(diamond, r);
        FlatGeom.PolyFill(b, diamond, color);
    }

    private static void DiamondPoints(double[] pts, double r)
    {
        // regularPolygon(4, r): first vertex at 12 o'clock, clockwise.
        pts[0] = 0;
        pts[1] = -r;
        pts[2] = r;
        pts[3] = 0;
        pts[4] = 0;
        pts[5] = r;
        pts[6] = -r;
        pts[7] = 0;
    }

    // flick.ts arrowTravel: delayed half-length dash at full speed — 0.30u flight over
    // 0.25–0.5 to 1.42× rest (~16 px overshoot), relaxing over 0.5–0.875, locked after.
    private static double ArrowTravel(double p)
    {
        if (p < 0.25) return 0;
        if (p < 0.5) return FlatEase.Lerp(0, 1.42, ArrowShape(FlatEase.Seg(p, 0.25, 0.5)));
        if (p < 0.875) return FlatEase.Lerp(1.42, 1, ArrowShape(FlatEase.Seg(p, 0.5, 0.875)));
        return 1;
    }
}
