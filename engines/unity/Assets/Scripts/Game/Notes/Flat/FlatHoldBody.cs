using System;

// bodies.ts holdBody. Reused scratch storage keeps the capsule clip out of Unity's scene graph.
public sealed class FlatHoldBody
{
    private readonly FlatMeshBuilder parts = new FlatMeshBuilder();
    private readonly double[] dash = { DashPeriod / 2, DashPeriod / 2 };
    private readonly Point[] capsule = new Point[64];
    private readonly Point[] workA = new Point[68];
    private readonly Point[] workB = new Point[68];
    private readonly double[] border = new double[128];

    public static double Width => FlatTokens.Unit * FlatTokens.StrokeHoldBody;
    public static double DashPeriod => Math.Max(2, Math.Floor(Width * 1.1 + 0.5));
    public static double EndLength => Math.Max(2, Math.Floor(Width * 0.9 + 0.5));

    public void Draw(FlatMeshBuilder b, FlatPalette palette, double length, double progress,
        double time, double appear, bool flipY)
    {
        var hair = FlatTokens.Unit * FlatTokens.StrokeHair;
        var l = Math.Max(0, length);
        var a = FlatEase.Clamp01(appear);
        var reveal = FlatHoldDesign.BodyReveal(a);
        var shown = l * reveal.Stretch;
        var opacity = reveal.Alpha;
        if (shown <= 0 || opacity <= 0) return;

        var p = FlatEase.Clamp01(progress);
        var done = l * p;
        var r = Width / 2;
        var dashEnd = shown - EndLength;
        var rx = Math.Min(r, (shown + r) / 2);
        var count = BuildCapsule(r, shown, rx);

        // The near rounded end is hidden by the head. Trim at y=0 to match the TS track rect.
        parts.Reset();
        FlatGeom.RoundedRect(parts, -r, -r, Width, shown + r, rx,
            Alpha(palette.Shade(800), opacity * 0.92));
        AppendClipped(b, count, flipY, true);

        parts.Reset();
        if (dashEnd > done)
        {
            // FlatGeom offsets advance along the path. Keep the rest phase Canvas-compatible,
            // but advance the live flow from the far end toward the head (the TS intent).
            var offset = time * Width * 4 - (shown - dashEnd);
            FlatGeom.DashedLine(parts, 0, dashEnd, 0, done, hair * 2.4,
                Alpha(palette.Shade(400), opacity * 0.75), dash, offset, FlatCap.Butt);
        }
        if (done > 0)
        {
            FlatGeom.RoundedRect(parts, -r, 0, Width, done, 0, Alpha(palette.Shade(400), opacity));
            if (dashEnd > 0)
                FlatGeom.Line(parts, 0, 0, 0, Math.Min(done, dashEnd), hair * 2,
                    Alpha(palette.RingColor, opacity), FlatCap.Butt);
        }
        var lit = FlatEase.OutCubic(FlatEase.Seg(p, 0.9, 1));
        FlatGeom.Disk(parts, 0, shown - r,
            FlatEase.Lerp(hair * 2.2, hair * 3.2, FlatEase.OutBack(lit, 2)),
            Alpha(lit > 0 ? palette.RingColor : palette.RingTone(0), opacity * FlatEase.Lerp(0.75, 1, lit)));
        AppendClipped(b, count, flipY, false);

        // A hairline border traces the capsule unclipped (#12): it keeps the empty
        // track's length readable and rims the filled bar once the fill covers the
        // inner half. Drawn on top, inside the body's reveal opacity.
        for (var i = 0; i < count; i++)
        {
            border[i * 2] = capsule[i].X;
            border[i * 2 + 1] = flipY ? capsule[i].Y : -capsule[i].Y;
        }
        FlatGeom.PolyStroke(b, border, true, hair * 1.6,
            Alpha(palette.Shade(400), opacity * 0.9), FlatJoin.Round, FlatCap.Butt);
    }

    private int BuildCapsule(double r, double shown, double rx)
    {
        var segments = Math.Max(7, (int) Math.Ceiling(Math.Max(12, Math.Ceiling(rx / 2)) / 4));
        var count = 0;
        for (var corner = 0; corner < 4; corner++)
        {
            var x = corner < 2 ? r - rx : -r + rx;
            var y = corner == 0 || corner == 3 ? -r + rx : shown - rx;
            for (var i = 0; i <= segments; i++)
            {
                var angle = (corner - 1) * Math.PI / 2 + i * Math.PI / (2 * segments);
                capsule[count++] = new Point(x + Math.Cos(angle) * rx, y + Math.Sin(angle) * rx);
            }
        }
        return count;
    }

    // Sutherland-Hodgman on each triangle preserves part order and clips even a tiny unrolling dot.
    private void AppendClipped(FlatMeshBuilder b, int edges, bool flipY, bool trimNear)
    {
        for (var t = 0; t < parts.Tris.Count; t += 3)
        {
            var input = workA;
            var output = workB;
            var count = 3;
            for (var i = 0; i < 3; i++)
            {
                var at = parts.Tris[t + i] * 7;
                input[i] = new Point(parts.Verts[at], parts.Verts[at + 1]);
            }
            for (var edge = 0; edge < edges + (trimNear ? 1 : 0) && count > 0; edge++)
            {
                var a = edge < edges ? capsule[edge] : new Point(0, 0);
                var c = edge < edges ? capsule[(edge + 1) % edges] : new Point(1, 0);
                var nextCount = 0;
                var previous = input[count - 1];
                var previousDistance = Cross(a, c, previous);
                for (var i = 0; i < count; i++)
                {
                    var current = input[i];
                    var distance = Cross(a, c, current);
                    if ((distance >= 0) != (previousDistance >= 0))
                    {
                        var k = previousDistance / (previousDistance - distance);
                        output[nextCount++] = new Point(FlatEase.Lerp(previous.X, current.X, k),
                            FlatEase.Lerp(previous.Y, current.Y, k));
                    }
                    if (distance >= 0) output[nextCount++] = current;
                    previous = current;
                    previousDistance = distance;
                }
                var swap = input;
                input = output;
                output = swap;
                count = nextCount;
            }
            if (count < 3) continue;
            var vertex = parts.Tris[t] * 7;
            var color = new Rgba(parts.Verts[vertex + 3], parts.Verts[vertex + 4],
                parts.Verts[vertex + 5], parts.Verts[vertex + 6]);
            var first = b.VertCount;
            for (var i = 0; i < count; i++)
                b.Vert(input[i].X, flipY ? input[i].Y : -input[i].Y, 0, color);
            for (var i = 1; i < count - 1; i++)
            {
                if (flipY) b.Tri(first, first + i, first + i + 1);
                else b.Tri(first, first + i + 1, first + i);
            }
        }
    }

    private static double Cross(Point a, Point b, Point p) =>
        (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    private static Rgba Alpha(Rgba color, double opacity) =>
        new Rgba(color.R, color.G, color.B, (float) (color.A * opacity));

    private readonly struct Point
    {
        public readonly double X, Y;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
