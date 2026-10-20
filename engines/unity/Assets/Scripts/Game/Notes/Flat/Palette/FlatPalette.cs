// Port of cytoid-design packages/notes/src/palette.ts — seed-derived shade scale.
// PURE C#: no UnityEngine references (verified by Tools/FlatParity).

using System;

public enum FlatFamily
{
    Click,
    Hold,
    LongHold,
    Flick,
    Drag,
    ClickDrag,
    DropClick,
    DropDrag,
}

public enum FlatDirection
{
    Up,
    Down,
}

public sealed class FlatPalette
{
    internal static readonly int[] Shades = { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };

    private readonly Rgba[] shades;
    private readonly Oklch[] parsedShades;
    private readonly Oklch ringTarget;

    /// <param name="ring">Ring material tone; tokens.ring (#FAFAFA) when omitted.</param>
    internal FlatPalette(Rgba[] colors, Rgba ink, double hue, Rgba? ring = null)
    {
        shades = new Rgba[Shades.Length];
        parsedShades = new Oklch[Shades.Length];
        for (var i = 0; i < Shades.Length; i++)
        {
            // TS stores hex strings: interpolation starts from these rounded RGBs.
            shades[i] = QuantizedRgb(colors[i]);
            parsedShades[i] = FlatColor.RgbToOklch(shades[i]);
        }
        RingColor = ring ?? FlatColor.ParseHex("#FAFAFA");
        ringTarget = FlatColor.RgbToOklch(RingColor);
        Ink = QuantizedRgb(ink);
        Hue = hue;
    }

    /// <summary>Exact shade 50..950 — verbatim from the derived scale.</summary>
    public Rgba Shade(int step)
    {
        var i = Array.IndexOf(Shades, step);
        if (i < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }
        return shades[i];
    }

    /// <summary>Piecewise-linear OKLCH sample at a (fractional) step, clamped to [50, 950].</summary>
    public Rgba Tone(double step)
    {
        var s = Math.Min(950, Math.Max(50, step));
        var i = 0;
        while (i < Shades.Length - 2 && s > Shades[i + 1])
        {
            i++;
        }
        var a = Shades[i];
        var b = Shades[i + 1];
        var k = (s - a) / (b - a);
        if (k <= 0)
        {
            return shades[i];
        }
        if (k >= 1)
        {
            return shades[i + 1];
        }
        var pa = parsedShades[i];
        var pb = parsedShades[i + 1];
        var dh = ((pb.H - pa.H + 540) % 360) - 180;
        return QuantizedRgb(new Oklch(
            pa.L + (pb.L - pa.L) * k,
            pa.C + (pb.C - pa.C) * k,
            pa.H + dh * k));
    }

    /// <summary>Ring wake: neutral L 0.708 spawn rising to the ring color at k = 1.</summary>
    public Rgba RingTone(double k)
    {
        var t = Math.Min(1, Math.Max(0, k));
        return QuantizedRgb(new Oklch(0.708 + (ringTarget.L - 0.708) * t, ringTarget.C * t, ringTarget.H));
    }

    public Rgba RingColor { get; }

    public Rgba Ink { get; }

    public double Hue { get; }

    private static Rgba QuantizedRgb(Oklch c) => QuantizedRgb(FlatColor.OklchToRgb(c));

    private static Rgba QuantizedRgb(Rgba rgb)
    {
        // Match ToHex/ParseHex's 8-bit rounding and opaque alpha without strings.
        // HexChannel promotes each float channel to double before multiplying.
        return new Rgba(
            (float) Math.Floor((double) rgb.R * 255 + 0.5) / 255f,
            (float) Math.Floor((double) rgb.G * 255 + 0.5) / 255f,
            (float) Math.Floor((double) rgb.B * 255 + 0.5) / 255f);
    }
}

public static class FlatPaletteDerive
{
    private const double LightnessTop = 0.985;
    private const double Chroma = 0.165;
    private const int BaseShade = 400;

    private static readonly (double L, double C)[] Scale =
    {
        (0.96, 0.12),
        (0.86, 0.25),
        (0.69, 0.45),
        (0.43, 0.6),
        (0, 1),
        (0.88, 0.97),
        (0.76, 0.9),
        (0.66, 0.75),
        (0.55, 0.55),
        (0.47, 0.45),
        (0.36, 0.35),
    };

    /// <summary>derivePalette(number) — fitLightness(h) + tokens.chroma.</summary>
    public static FlatPalette FromHue(double hue, Rgba? ring = null)
    {
        var h = ((hue % 360) + 360) % 360;
        return Derive(FitLightness(h), Chroma, h, ring);
    }

    /// <summary>derivePalette(hex) — the seed's own L, c, h are respected at shade 400.</summary>
    public static FlatPalette FromSeed(Rgba seed, Rgba? ring = null)
    {
        var c = FlatColor.RgbToOklch(seed);
        return Derive(c.L, c.C, c.H, ring);
    }

    /// <summary>tokens.hue default table for one family × direction.</summary>
    public static FlatPalette Default(FlatFamily family, FlatDirection direction, Rgba? ring = null)
    {
        switch (family)
        {
            case FlatFamily.Click:
            case FlatFamily.Hold:
            case FlatFamily.Flick:
            case FlatFamily.ClickDrag:
            case FlatFamily.DropClick:
                return FromHue(direction == FlatDirection.Up ? 247 : 20, ring);
            case FlatFamily.LongHold:
                return FromHue(88, ring);
            case FlatFamily.Drag:
            case FlatFamily.DropDrag:
                return FromHue(160, ring);
            default:
                throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    private static double FitLightness(double h)
    {
        var d = Math.Abs(((h - 100 + 540) % 360) - 180);
        return 0.705 + 0.13 * Math.Exp(-Math.Pow(d / 48, 2));
    }

    private static double ShadeLightness(double lightness, int index)
    {
        var step = FlatPalette.Shades[index];
        var k = Scale[index].L;
        if (step == BaseShade)
        {
            return lightness;
        }
        return step < BaseShade ? lightness + (Math.Max(lightness, LightnessTop) - lightness) * k : lightness * k;
    }

    private static FlatPalette Derive(double l, double c, double h, Rgba? ring = null)
    {
        var colors = new Rgba[FlatPalette.Shades.Length];
        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = FlatColor.OklchToRgb(new Oklch(ShadeLightness(l, i), c * Scale[i].C, h));
        }
        var ink = FlatColor.OklchToRgb(new Oklch(0.24, Math.Min(0.04, c * 0.3), h));
        return new FlatPalette(colors, ink, h, ring);
    }
}
