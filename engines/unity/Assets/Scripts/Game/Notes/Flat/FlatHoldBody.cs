using System;

// bodies.ts holdBody.
public sealed class FlatHoldBody
{
    private readonly double[] dash = { DashPeriod / 2, DashPeriod / 2 };

    public static double Width => FlatTokens.Unit * FlatTokens.StrokeHoldBody;
    public static double DashPeriod => Math.Max(2, Math.Floor(Width * 1.1 + 0.5));

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

        // The body is cut flat exactly at the hold end — no rounded cap, no release
        // dot — so the flat edge meets the scanline the moment the hold completes.
        // The near end hides under the head by extending to -r.
        FlatGeom.RoundedRect(b, -r, flipY ? 0 : -shown, Width, shown, 0,
            Alpha(palette.Shade(800), opacity * 0.92));

        if (shown > done)
        {
            // Conveyor dashes scroll toward the head; the phase is anchored at the
            // far end, so offset advances with time alone (FlatGeom offsets move
            // dash starts forward along the path — the path starts at the far end).
            FlatGeom.DashedLine(b, 0, Y(shown, flipY), 0, Y(done, flipY), hair * 2.4,
                Alpha(palette.Shade(400), opacity * 0.75), dash, time * Width * 4, FlatCap.Butt);
        }
        if (done > 0)
        {
            FlatGeom.RoundedRect(b, -r, flipY ? 0 : -done, Width, done, 0,
                Alpha(palette.Shade(400), opacity));
            FlatGeom.Line(b, 0, Y(0, flipY), 0, Y(done, flipY), hair * 2,
                Alpha(palette.RingColor, opacity), FlatCap.Butt);
        }

        // Two thin side rails replace the capsule border: unclipped so their full
        // hair width reads, the far end open (no cap line across it), the near end
        // hidden under the head. The fill later covers their inner half.
        FlatGeom.Line(b, -r, Y(-r, flipY), -r, Y(shown, flipY), hair * 1.6,
            Alpha(palette.Shade(400), opacity * 0.9), FlatCap.Butt);
        FlatGeom.Line(b, r, Y(-r, flipY), r, Y(shown, flipY), hair * 1.6,
            Alpha(palette.Shade(400), opacity * 0.9), FlatCap.Butt);
    }

    private static double Y(double y, bool flipY) => flipY ? y : -y;

    private static Rgba Alpha(Rgba color, double opacity) =>
        new Rgba(color.R, color.G, color.B, (float) (color.A * opacity));
}
