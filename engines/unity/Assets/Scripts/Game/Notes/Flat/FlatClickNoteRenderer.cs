using System;

public class FlatClickNoteRenderer : FlatNoteRendererBase
{
    private static readonly Rgba Edge = FlatColor.ParseHex("#1A1B22");

    public FlatClickNoteRenderer(ClickNote note) : base(note)
    {
    }

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
        var w = FlatTokens.Unit * FlatTokens.StrokeRing * Math.Sqrt(size / FlatTokens.Unit);
        var hair = FlatTokens.Unit * FlatTokens.StrokeHair;
        var inner = r - w;
        var alpha = FlatEase.OutCubic(FlatEase.Seg(p, 0, 0.1));
        var build = FlatEase.OutQuart(FlatEase.Seg(p, 0, 0.4));
        var split = FlatEase.OutQuad(FlatEase.Seg(p, 0.4, 0.85));
        var coreR = inner * 0.8 * FlatEase.Lerp(0.36, 1, FlatEase.InQuad(FlatEase.Seg(p, 0.42, 0.94)));
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
        AssemblingRing(b, r - w / 2, FlatEase.Lerp(w * 0.45, w, build),
            WithOpacity(palette.RingTone(split), alpha), 3, build, FlatEase.Lerp(-Math.PI / 3, 0, build),
            WithOpacity(Edge, alpha), FlatTokens.StrokeEdgeFraction);
    }
}
