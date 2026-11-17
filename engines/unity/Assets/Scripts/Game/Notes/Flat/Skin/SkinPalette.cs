// Port of cytoid-design packages/notes/src/palette.ts — shade scales, the
// neutral ring band, tone sampling and grade ramps, all running through the
// exact SkinColorMath.Mix. The legacy FlatPalette interpolates OKLCH directly
// and belongs to the imperative fallback path only.
// PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;

public sealed class SkinNotePalette
{
    internal static readonly int[] Shades = { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };

    /// <summary>Quantized scale colors; exact steps return these verbatim.</summary>
    public readonly Rgba[] Scale;
    /// <summary>Authored hex spelling per shade, when the value came verbatim from a string
    /// (shade 400 of a grade ramp, or any hex-authored base); null → canonical spelling.</summary>
    public readonly string[] Authored;
    /// <summary>Resting ring colour (palette.ring) — the authored string drives ringTone's branch.</summary>
    public readonly string RingHex;
    public readonly Rgba Ring;
    public readonly Rgba Ink;
    public readonly double Hue;

    internal SkinNotePalette(Rgba[] scale, string[] authored, Rgba ring, string ringHex, Rgba ink, double hue)
    {
        Scale = scale;
        Authored = authored;
        Ring = ring;
        RingHex = ringHex;
        Ink = ink;
        Hue = hue;
    }

    internal SkinNotePalette WithBase400(string hex)
    {
        var scale = (Rgba[]) Scale.Clone();
        var authored = (string[]) Authored.Clone();
        scale[SkinPalette.BaseShadeIndex] = SkinColorMath.ParseHexRgb(hex);
        authored[SkinPalette.BaseShadeIndex] = hex;
        return new SkinNotePalette(scale, authored, Ring, RingHex, Ink, Hue);
    }

    public int ShadeIndex(int step) => Array.IndexOf(Shades, step);

    /// <summary>Exact shade — verbatim from the derived scale (authored spelling preserved).</summary>
    public SkinColor Shade(int index) => new SkinColor(Scale[index], Authored[index]);
}

public static class SkinPalette
{
    public const int BaseShadeIndex = 4; // 400
    private const double LightnessTop = 0.985;
    private const double NeutralBaseL = 0.708; // oklch 0.708 0 0 — Tailwind neutral 400

    private static readonly (double L, double C)[] ScaleShape =
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

    /// <summary>The neutral ring band — the family scale's shape with zero chroma.</summary>
    public static readonly Rgba[] Neutral = BuildNeutral();
    public static readonly string Neutral50Hex = SkinColorMath.ToHexString(Neutral[0]);

    private static Rgba[] BuildNeutral()
    {
        var scale = new Rgba[SkinNotePalette.Shades.Length];
        for (var i = 0; i < scale.Length; i++)
            scale[i] = SkinColorMath.Oklch(ShadeLightness(NeutralBaseL, i), 0, 0);
        return scale;
    }

    public static double FitLightness(double h)
    {
        var d = Math.Abs(((h - 100 + 540) % 360) - 180);
        return 0.705 + 0.13 * Math.Exp(-Math.Pow(d / 48, 2));
    }

    private static double ShadeLightness(double lightness, int index)
    {
        var step = SkinNotePalette.Shades[index];
        var k = ScaleShape[index].L;
        if (index == BaseShadeIndex)
            return lightness;
        return step < 400 ? lightness + (Math.Max(lightness, LightnessTop) - lightness) * k : lightness * k;
    }

    public sealed class Options
    {
        public double Chroma = 0.165;
        public double Saturation = 1;
        public string Ring;
    }

    /// <summary>derivePalette(number) — a hue keeps the design's lightness/chroma balance.</summary>
    public static SkinNotePalette FromHue(double hue, Options options = null)
    {
        options ??= new Options();
        var h = ((hue % 360) + 360) % 360;
        return Derive(FitLightness(h), options.Chroma * options.Saturation, h, options);
    }

    /// <summary>derivePalette(hex) — the seed's own lightness and chroma are respected.</summary>
    public static SkinNotePalette FromHex(string hex, Options options = null)
    {
        options ??= new Options();
        var c = FlatColor.RgbToOklch(SkinColorMath.ParseHexRgb(hex));
        return Derive(c.L, c.C * options.Saturation, c.H, options);
    }

    private static SkinNotePalette Derive(double l, double c, double h, Options options)
    {
        var scale = new Rgba[SkinNotePalette.Shades.Length];
        var authored = new string[SkinNotePalette.Shades.Length];
        for (var i = 0; i < scale.Length; i++)
            scale[i] = SkinColorMath.Oklch(ShadeLightness(l, i), c * ScaleShape[i].C, h);
        var ringHex = options.Ring ?? Neutral50Hex;
        var ink = SkinColorMath.Oklch(0.24, Math.Min(0.04, c * 0.3), h);
        return new SkinNotePalette(scale, authored, SkinColorMath.ParseHexRgb(ringHex), ringHex, ink, h);
    }

    /// <summary>sampleScale — piecewise-linear between neighbouring shades via the exact mix;
    /// exact steps (k ≤ 0 / k ≥ 1) return the scale colour verbatim.</summary>
    public static SkinColor SampleScale(SkinNotePalette palette, double step)
    {
        var s = Math.Min(950, Math.Max(50, step));
        var i = 0;
        while (i < SkinNotePalette.Shades.Length - 2 && s > SkinNotePalette.Shades[i + 1]) i++;
        var a = SkinNotePalette.Shades[i];
        var b = SkinNotePalette.Shades[i + 1];
        var k = (s - a) / (double) (b - a);
        if (k <= 0) return palette.Shade(i);
        if (k >= 1) return palette.Shade(i + 1);
        return SkinColorMath.Mix(palette.Shade(i), palette.Shade(i + 1), k);
    }

    /// <summary>Sample the neutral band at any (fractional) step.</summary>
    public static SkinColor NeutralTone(double step) => SampleNeutralScale(step);

    private static SkinColor SampleNeutralScale(double step)
    {
        var s = Math.Min(950, Math.Max(50, step));
        var i = 0;
        while (i < SkinNotePalette.Shades.Length - 2 && s > SkinNotePalette.Shades[i + 1]) i++;
        var a = SkinNotePalette.Shades[i];
        var b = SkinNotePalette.Shades[i + 1];
        var k = (s - a) / (double) (b - a);
        if (k <= 0) return new SkinColor(Neutral[i]);
        if (k >= 1) return new SkinColor(Neutral[i + 1]);
        return SkinColorMath.Mix(new SkinColor(Neutral[i]), new SkinColor(Neutral[i + 1]), k);
    }

    /// <summary>tone(palette, step) — the note family scale at any fractional step.</summary>
    public static SkinColor Tone(SkinNotePalette palette, double step) => SampleScale(palette, step);

    /// <summary>ringTone(palette, step) — the neutral ring band; a custom resting colour
    /// re-anchors only the 100 → 50 leg. The neutral-vs-custom branch is the authored
    /// ring string compared against the canonical neutral 50, exactly like the TS.</summary>
    public static SkinColor RingTone(SkinNotePalette palette, double step)
    {
        if (step >= 100 || palette.RingHex == Neutral50Hex)
            return SampleNeutralScale(step);
        return SkinColorMath.Mix(new SkinColor(Neutral[1]), new SkinColor(palette.Ring, palette.RingHex),
            Math.Min(1, Math.Max(0, (100 - step) / 50.0)));
    }
}

/// <summary>Grade ramps — derivePalette(grade, chroma 0) with the exact supplied colour at 400.</summary>
public sealed class SkinGradePalettes
{
    private readonly Dictionary<SkinGrade, string> grades;
    private readonly Dictionary<SkinGrade, SkinNotePalette> cache = new Dictionary<SkinGrade, SkinNotePalette>();

    public SkinGradePalettes(IReadOnlyDictionary<SkinGrade, string> gradeColors)
    {
        grades = new Dictionary<SkinGrade, string>();
        foreach (var grade in SkinTimingTokens.Grades)
            grades[grade] = gradeColors != null && gradeColors.TryGetValue(grade, out var hex) ? hex : null;
    }

    public string ColorOf(SkinGrade grade) => grades[grade];

    public SkinNotePalette Palette(SkinGrade grade)
    {
        if (cache.TryGetValue(grade, out var cached)) return cached;
        var hex = grades[grade];
        if (hex == null)
            throw new SkinRenderException($"grade color for {SkinTimingTokens.GradeKey(grade)} is not configured");
        var scale = SkinPalette.FromHex(hex).WithBase400(hex);
        cache[grade] = scale;
        return scale;
    }
}

/// <summary>The resolved colour sources of one draw: the note's own family palette, the
/// bound grade palette (feedback/judgement slots), named family lookups and grade ramps.</summary>
public sealed class SkinPaletteContext
{
    public SkinNotePalette Note; // ctx.palette
    public SkinGradePalettes Grades; // ctx.grades → gradePalette ramps
    public Func<FlatFamily, FlatDirection, SkinNotePalette> Family; // ctx.colors.family
}
