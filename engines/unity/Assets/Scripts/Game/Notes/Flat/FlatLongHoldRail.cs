using System;

// bodies.ts longHoldBody, in design pixels; top and bottom are distances from the head.
public sealed class FlatLongHoldRail
{
    public const double CollapseSeconds = 0.6;
    private readonly double[] dash = { FlatHoldBody.DashPeriod / 2, FlatHoldBody.DashPeriod / 2 };

    public void Draw(FlatMeshBuilder b, FlatPalette palette, double top, double bottom, double progress,
        double time, double holdDuration, double appear)
    {
        var hair = FlatTokens.Unit * FlatTokens.StrokeHair;
        var a = FlatEase.Clamp01(appear);
        var reveal = FlatHoldDesign.BodyReveal(a);
        var opacity = reveal.Alpha;
        top = Math.Max(0, top) * reveal.Stretch;
        bottom = Math.Max(0, bottom) * reveal.Stretch;
        if (top + bottom <= 0 || opacity <= 0) return;

        var p = FlatEase.Clamp01(progress);
        var shrink = FlatEase.InSine(holdDuration > 0
            ? FlatEase.Clamp01((time - Math.Max(0, holdDuration - CollapseSeconds)) /
                               Math.Min(CollapseSeconds, holdDuration))
            : FlatEase.Seg(p, 0.86, 1));
        var width = FlatHoldBody.Width * (1 - 0.85 * shrink);
        var seal = 1 - shrink;
        var flow = time * FlatHoldBody.Width * 4;
        FlatGeom.RoundedRect(b, -width / 2, -top, width, top + bottom, 0,
            Alpha(palette.Shade(800), opacity * 0.55));
        FlatGeom.Line(b, -width / 2, -top, -width / 2, bottom, hair * 1.6,
            Alpha(palette.Shade(400), opacity * 0.9), FlatCap.Butt);
        FlatGeom.Line(b, width / 2, -top, width / 2, bottom, hair * 1.6,
            Alpha(palette.Shade(400), opacity * 0.9), FlatCap.Butt);

        // Negate Canvas's phase-flow: FlatGeom's positive offset advances toward the head.
        if (top > 0)
            Conveyor(b, -top, -top * p, flow, hair, Alpha(palette.Shade(400), opacity * 0.75 * seal));
        if (bottom > 0)
            Conveyor(b, bottom, bottom * p, flow - dash[0], hair,
                Alpha(palette.Shade(400), opacity * 0.75 * seal));
        if (p > 0)
        {
            var fillWidth = FlatEase.Lerp(width * 0.6, width, Math.Min(1, p * 4));
            FlatGeom.RoundedRect(b, -fillWidth / 2, -top * p, fillWidth, (top + bottom) * p, 0,
                Alpha(palette.Shade(400), opacity));
            if (top > 0)
                Conveyor(b, -top * p, 0, flow, hair, Alpha(palette.Shade(800), opacity * 0.55 * seal));
            if (bottom > 0)
                Conveyor(b, bottom * p, 0, flow - dash[0], hair,
                    Alpha(palette.Shade(800), opacity * 0.55 * seal));
        }
    }

    private void Conveyor(FlatMeshBuilder b, double from, double to, double phase, double hair, Rgba color)
    {
        if (color.A <= 0) return;
        FlatGeom.DashedLine(b, 0, from, 0, to, hair * 2.4, color, dash, phase, FlatCap.Butt);
    }

    private static Rgba Alpha(Rgba color, double opacity) =>
        new Rgba(color.R, color.G, color.B, (float) (color.A * opacity));
}
