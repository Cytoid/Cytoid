// Port of cytoid-design judgement.ts / timing.ts (experiments #5 / #6).
// PURE C#: design pixels, +y down, seconds since judgement. Geometry is appended.

#nullable enable

using System;
using System.Collections.Generic;

/// <summary>Per-glyph pop curve for DrawText: At(i) = OutBack(Seg(u, from + i*step, to + i*step), back).
/// A value type standing in for the captured lambda the TS port used — judgement/timing text
/// redraws every animated frame, so the closure showed up as steady GC garbage.</summary>
public readonly struct GlyphPop
{
    private readonly double u, from, step, to, back;

    public GlyphPop(double u, double from, double step, double to, double back)
    {
        this.u = u;
        this.from = from;
        this.step = step;
        this.to = to;
        this.back = back;
    }

    public double At(int index) => FlatEase.OutBack(FlatEase.Seg(u, from + index * step, to + index * step), back);
}

/// <summary>Chamfered monoline glyphs. Text is case-sensitive; unknown glyphs are skipped.</summary>
public static class FlatText
{
    private sealed class Glyph
    {
        public readonly double Width;
        public readonly double[][] Strokes;
        public readonly bool[]? Closed;

        public Glyph(double width, double[][] strokes, bool[]? closed = null)
        {
            Width = width;
            Strokes = strokes;
            Closed = closed;
        }
    }

    private const double W = 0.62, K = 0.18, M = 0.82;
    // Normalized 1-unit cap box, y down. R has a bowl lead-in, 45-degree
    // leg and vertical foot; M/W use chamfered corners and a centre stem.
    private static readonly Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>
    {
        { 'P', new Glyph(W, new[] { new double[] { 0, 1, 0, 0, W - K, 0, W, K, W, 0.5 - K, W - K, 0.5, 0, 0.5 } }) },
        { 'E', new Glyph(W, new[] { new double[] { W, 0, 0, 0, 0, 1, W, 1 }, new double[] { 0, 0.5, W * 0.82, 0.5 } }) },
        { 'R', new Glyph(W, new[] { new double[] { 0, 1, 0, 0, W - K, 0, W, K, W, 0.5 - K, W - K, 0.5, 0, 0.5 }, new double[] { W * 0.2, 0.5, W * 0.42, 0.5, W, 0.5 + W * 0.58, W, 1 } }) },
        { 'F', new Glyph(W, new[] { new double[] { W, 0, 0, 0, 0, 1 }, new double[] { 0, 0.5, W * 0.82, 0.5 } }) },
        { 'C', new Glyph(W, new[] { new double[] { W, 0, K, 0, 0, K, 0, 1 - K, K, 1, W, 1 } }) },
        { 'T', new Glyph(W, new[] { new double[] { 0, 0, W, 0 }, new double[] { W / 2, 0, W / 2, 1 } }) },
        { 'G', new Glyph(W, new[] { new double[] { W, 0, K, 0, 0, K, 0, 1 - K, K, 1, W, 1, W, 0.55, W * 0.48, 0.55 } }) },
        { 'A', new Glyph(W, new[] { new double[] { 0, 1, 0, K, K, 0, W - K, 0, W, K, W, 1 }, new double[] { 0, 0.58, W, 0.58 } }) },
        { 'O', new Glyph(W, new[] { new double[] { K, 0, W - K, 0, W, K, W, 1 - K, W - K, 1, K, 1, 0, 1 - K, 0, K } }, new[] { true }) },
        { 'D', new Glyph(W, new[] { new double[] { 0, 0, W - K, 0, W, K, W, 1 - K, W - K, 1, 0, 1 } }, new[] { true }) },
        { 'B', new Glyph(W, new[] { new double[] { 0, 0, W - K, 0, W, K, W, 0.5 - K, W - K, 0.5, 0, 0.5 }, new double[] { W - K, 0.5, W, 0.5 + K, W, 1 - K, W - K, 1, 0, 1, 0, 0 } }) },
        { 'M', new Glyph(M, new[] { new double[] { 0, 1, 0, K, K, 0, M - K, 0, M, K, M, 1 }, new double[] { M / 2, 0, M / 2, 0.6 } }) },
        { 'I', new Glyph(0, new[] { new double[] { 0, 0, 0, 1 } }) },
        { 'S', new Glyph(W, new[] { new double[] { W, 0, K, 0, 0, K, 0, 0.5 - K, K, 0.5, W - K, 0.5, W, 0.5 + K, W, 1 - K, W - K, 1, 0, 1 } }) },
        { 'X', new Glyph(W * 0.9, new[] { new double[] { 0, 0, W * 0.9, 1 }, new double[] { W * 0.9, 0, 0, 1 } }) },
        { 'L', new Glyph(W * 0.9, new[] { new double[] { 0, 0, 0, 1, W * 0.9, 1 } }) },
        { 'W', new Glyph(M, new[] { new double[] { 0, 0, 0, 1 - K, K, 1, M - K, 1, M, 1 - K, M, 0 }, new double[] { M / 2, 0.4, M / 2, 1 } }) },
        { '0', new Glyph(W, new[] { new double[] { K, 0, W - K, 0, W, K, W, 1 - K, W - K, 1, K, 1, 0, 1 - K, 0, K } }, new[] { true }) },
        { '1', new Glyph(0.34, new[] { new double[] { 0, 0.16, 0.17, 0, 0.17, 1 } }) },
        { '2', new Glyph(W, new[] { new double[] { 0, K, K, 0, W - K, 0, W, K, W, 0.42, 0.08, 1, W, 1 } }) },
        { '3', new Glyph(W, new[] { new double[] { 0, 0, W - K, 0, W, K, W, 1 - K, W - K, 1, K, 1, 0, 1 - K }, new double[] { 0, 0.5, W * 0.75, 0.5 } }) },
        { '4', new Glyph(W, new[] { new double[] { W * 0.68, 0, 0, 0.62, W, 0.62 }, new double[] { W * 0.68, 0, W * 0.68, 1 } }) },
        { '5', new Glyph(W, new[] { new double[] { W, 0, K, 0, 0, K, 0, 0.5 - K, K, 0.5, W - K, 0.5, W, 0.5 + K, W, 1 - K, W - K, 1, 0, 1 } }) },
        { '6', new Glyph(W, new[] { new double[] { W, K, W - K, 0, K, 0, 0, K, 0, 1 - K, K, 1, W - K, 1, W, 1 - K, W, 0.5 + K, W - K, 0.5, 0.06, 0.5 } }) },
        { '7', new Glyph(W, new[] { new double[] { 0, 0, W, 0, W * 0.38, 1 } }) },
        { '8', new Glyph(W, new[] { new double[] { K, 0, W - K, 0, W, K, W, 1 - K, W - K, 1, K, 1, 0, 1 - K, 0, K }, new double[] { 0, 0.5, W, 0.5 } }, new[] { true, false }) },
        { '9', new Glyph(W, new[] { new double[] { 0, 1 - K, K, 1, W - K, 1, W, 1 - K, W, K, W - K, 0, K, 0, 0, K, 0, 0.5 - K, K, 0.5, W - 0.06, 0.5 } }) },
        { '%', new Glyph(0.78, new[] { new double[] { 0.72, 0, 0.06, 1 }, new double[] { 0.02, 0.06, 0.3, 0.06, 0.3, 0.34, 0.02, 0.34 }, new double[] { 0.48, 0.66, 0.76, 0.66, 0.76, 0.94, 0.48, 0.94 } }, new[] { false, true, true }) },
        { '.', new Glyph(0.26, new[] { new double[] { 0.05, 0.9, 0.21, 0.9, 0.21, 1, 0.05, 1 } }, new[] { true }) },
    };

    public static double MeasureText(string text, double H, double? gap = null)
    {
        var width = 0.0;
        var count = 0;
        foreach (var ch in text)
        {
            if (!Glyphs.TryGetValue(ch, out var g)) continue;
            width += g.Width * H;
            count++;
        }
        return width + (gap ?? H * 0.32) * Math.Max(0, count - 1);
    }

    /// <summary>Centred static layout. pop returns the TS outBack value (may exceed 1).
    /// x/y translate the text centre; stroke width scales with each glyph's pop.</summary>
    public static void DrawText(FlatMeshBuilder b, string text, double H, Rgba color,
        double? gap = null, double? stroke = null, GlyphPop? pop = null,
        double alpha = 1, double x = 0, double y = 0, double z = 0)
    {
        var g = gap ?? H * 0.32;
        var sw = stroke ?? H * 0.18;
        var cursor = -MeasureText(text, H, g) / 2;
        var index = 0;
        foreach (var ch in text)
        {
            if (!Glyphs.TryGetValue(ch, out var glyph)) continue;
            var k = pop != null ? pop.Value.At(index) : 1;
            var cx = cursor + glyph.Width * H / 2;
            if (k > 0)
            {
                var scale = FlatEase.Lerp(0.4, 1, k);
                var tint = Opacity(color, alpha * Math.Min(1, k * 1.5));
                for (var j = 0; j < glyph.Strokes.Length; j++)
                {
                    var source = glyph.Strokes[j];
                    // Shared scratch (main-thread-only renderers): one buffer reused
                    // across glyph strokes keeps animated text out of the GC.
                    if (scratch.Length < source.Length) scratch = new double[source.Length];
                    var points = scratch;
                    for (var p = 0; p < source.Length; p += 2)
                    {
                        points[p] = x + cx + (source[p] - glyph.Width / 2) * H * scale;
                        points[p + 1] = y + (source[p + 1] - 0.5) * H * scale;
                    }
                    FlatGeom.PolyStroke(b, points, source.Length,
                        glyph.Closed != null && j < glyph.Closed.Length && glyph.Closed[j],
                        sw * scale, tint, FlatJoin.Miter, FlatCap.Square, z);
                }
            }
            cursor += glyph.Width * H + g;
            index++;
        }
    }

    private static double[] scratch = Array.Empty<double>();

    internal static Rgba Opacity(Rgba color, double alpha) =>
        new Rgba(color.R, color.G, color.B, (float)(color.A * alpha));
}

/// <summary>grade is one of perfect/great/good/bad/miss (lowercase TS names).
/// The caller supplies GameConfig's grade colour; null uses the design default.
/// Draw is centred on the effect, as judgementOffset is (0,0).</summary>
public static class FlatJudgement
{
    public static double CapHeight(double unit = FlatTokens.Unit) => unit * 0.12;

    public static (double X, double Y) Offset() => (0, 0);

    public static string Text(string grade)
    {
        switch (grade)
        {
            case "perfect": return "PERFECT";
            case "great": return "GREAT";
            case "good": return "GOOD";
            case "bad": return "BAD";
            case "miss": return "MISS";
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }

    public static void Draw(FlatMeshBuilder b, string grade, double t,
        Rgba? colorOverride = null, double alpha = 1, double unit = FlatTokens.Unit, double z = 0)
    {
        var u = t / FlatTokens.Time.ClearDuration(grade);
        var H = CapHeight(unit);
        var color = colorOverride ?? FlatTokens.GradeColor(grade);
        var miss = grade == "miss";
        if (miss)
        {
            // TS mix returns a hex string; the design's 8-bit RGB rounding is
            // reproduced numerically (HexChannel's Floor(v*255+0.5) → /255) so the
            // animated label no longer allocates strings every frame.
            var mixed = FlatColor.Mix(color, new Rgba(1, 1, 1), 0.35);
            color = new Rgba(HexQuantized(mixed.R), HexQuantized(mixed.G), HexQuantized(mixed.B), color.A);
        }
        var text = Text(grade);
        var gap = FlatEase.Lerp(H, H * 0.32, FlatEase.OutCubic(FlatEase.Seg(u, 0, 0.4)));
        var pop = new GlyphPop(u, 0.02, 0.035, 0.2, 2);
        var dy = (miss ? 1 : -1) * H * 0.12 * FlatEase.OutCubic(u);
        var opacity = alpha * (1 - FlatEase.Seg(u, 0.7, 1));
        FlatText.DrawText(b, text, H, color, gap: gap, pop: pop, alpha: opacity, y: dy, z: z);
        var lk = FlatEase.OutCubic(FlatEase.Seg(u, 0.05, 0.4));
        if (!miss && lk > 0)
        {
            var finalW = FlatText.MeasureText(text, H);
            var left = (-finalW / 2 - H * 0.3) * lk;
            var right = left + (finalW + H * 0.6) * lk;
            var top = H * 0.8 + dy;
            underline[0] = left;
            underline[1] = top;
            underline[2] = right;
            underline[3] = top;
            underline[4] = right;
            underline[5] = top + H * 0.08;
            underline[6] = left;
            underline[7] = top + H * 0.08;
            FlatGeom.PolyFill(b, underline, FlatText.Opacity(color, opacity * 0.7), z);
        }
    }

    private static float HexQuantized(double v) => (long) Math.Floor(v * 255 + 0.5) / 255f;

    private static readonly double[] underline = new double[8];
}

/// <summary>timing is fast/slow, grade uses lowercase TS names. Draw is around
/// the label's own centre (TS drawTiming); add Offset(stacked) when placing it.
/// Perfect/miss emit nothing. Colours are fixed design bad/good, independent of caller colours.</summary>
public static class FlatTiming
{
    public static bool ShowsTiming(string grade) => grade == "great" || grade == "good" || grade == "bad";

    public static string TimingOf(double error) => error < 0 ? "fast" : "slow";

    public static double CapHeight(bool stacked, double unit = FlatTokens.Unit) =>
        FlatJudgement.CapHeight(unit) * (stacked ? 0.6 : 1);

    public static (double X, double Y) Offset(bool stacked, double unit = FlatTokens.Unit) =>
        stacked ? (0, FlatJudgement.CapHeight(unit) * 1.18 + CapHeight(true, unit) / 2) : (0, 0);

    public static void DrawLabel(FlatMeshBuilder b, string timing, double H, Rgba color,
        GlyphPop? pop = null, double alpha = 1, double x = 0, double y = 0, double z = 0)
    {
        if (timing != "fast" && timing != "slow") throw new ArgumentOutOfRangeException(nameof(timing));
        var fast = timing == "fast";
        var text = fast ? "FAST" : "SLOW";
        var sw = H * 0.22;
        var textW = FlatText.MeasureText(text, H);
        var chevW = H * 0.34;
        var total = textW + H * 0.72 + chevW;
        var cx = fast ? -total / 2 + chevW / 2 : total / 2 - chevW / 2;
        var tx = fast ? total / 2 - textW / 2 : -total / 2 + textW / 2;
        var tip = fast ? -chevW / 2 : chevW / 2;
        chevron[0] = x + cx - tip;
        chevron[1] = y - H * 0.4;
        chevron[2] = x + cx + tip;
        chevron[3] = y;
        chevron[4] = x + cx - tip;
        chevron[5] = y + H * 0.4;
        FlatGeom.PolyStroke(b, chevron, false, sw, FlatText.Opacity(color, alpha), FlatJoin.Miter, FlatCap.Square, z);
        FlatText.DrawText(b, text, H, color, stroke: sw, pop: pop, alpha: alpha, x: x + tx, y: y, z: z);
    }

    private static readonly double[] chevron = new double[6];

    public static void Draw(FlatMeshBuilder b, string timing, string grade, double t,
        bool stacked, double alpha = 1, double unit = FlatTokens.Unit, double z = 0)
    {
        var duration = FlatTokens.Time.ClearDuration(grade);
        if (!ShowsTiming(grade)) return;
        var u = t / duration;
        var H = CapHeight(stacked, unit);
        var dir = timing == "fast" ? -1 : 1;
        var k = FlatEase.OutCubic(FlatEase.Seg(u, 0.06, 0.4));
        var pop = new GlyphPop(u, 0.06, 0.03, 0.24, 1.6);
        var dx = dir * H * 0.5 * (k - 1);
        var dy = -FlatJudgement.CapHeight(unit) * 0.12 * FlatEase.OutCubic(u);
        var opacity = FlatEase.Lerp(0, 1, FlatEase.Seg(u, 0.06, 0.2)) * (1 - FlatEase.Seg(u, 0.7, 1));
        DrawLabel(b, timing, H, FlatTokens.GradeColor(timing == "fast" ? "bad" : "good"), pop, alpha * opacity, dx, dy, z);
    }
}
