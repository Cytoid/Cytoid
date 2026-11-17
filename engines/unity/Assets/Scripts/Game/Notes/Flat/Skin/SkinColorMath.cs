// Port of cytoid-design packages/notes/src/core/color.ts — the exact `mix`
// semantics (OKLab interpolation with premultiplied alpha, 8-bit quantized
// endpoints/results) that color tracks and shade sampling run through.
// The legacy FlatColor.Mix is a straight RGBA lerp and must NOT be used by the
// skin runtime. PURE C#: no UnityEngine references.

using System;
using System.Globalization;

/// <summary>A resolved color: quantized RGBA plus, when it came verbatim from an
/// authored string, that exact spelling (dumps reproduce the TS strings).</summary>
public readonly struct SkinColor
{
    public readonly Rgba Value;
    public readonly string Authored;

    public SkinColor(Rgba value, string authored = null)
    {
        Value = value;
        Authored = authored;
    }

    public static readonly SkinColor Transparent = new SkinColor(new Rgba(0, 0, 0, 0), "#00000000");

    /// <summary>TS compares endpoint strings; authored spellings compare as strings.</summary>
    public bool Equals(in SkinColor other) =>
        Authored != null && Authored == other.Authored || ValueEquals(other.Value);

    public bool ValueEquals(in Rgba other) =>
        Value.R == other.R && Value.G == other.G && Value.B == other.B && Value.A == other.A;
}

public static class SkinColorMath
{
    /// <summary>parseHex: RGB from the first 6 hex digits; alpha digits ignored here.</summary>
    public static Rgba ParseHexRgb(string hex) => FlatColor.ParseHex(hex);

    /// <summary>parseHexAlpha — alpha component of any supported Hex spelling.</summary>
    public static double ParseHexAlpha(string hex)
    {
        var h = hex.Substring(1);
        if (h.Length == 4)
            return ParseHexByte(h[3], h[3]) / 255.0;
        return h.Length == 8 ? ParseHexByte(h[6], h[7]) / 255.0 : 1;
    }

    /// <summary>Full parse (RGB + alpha), matching TS parseHex for RGB and parseHexAlpha for A.</summary>
    public static Rgba ParseHexRgba(string hex) =>
        new Rgba(FlatColor.ParseHex(hex).R, FlatColor.ParseHex(hex).G, FlatColor.ParseHex(hex).B,
            (float) ParseHexAlpha(hex));

    private static int ParseHexByte(char a, char b) =>
        16 * HexDigit(a) + HexDigit(b);

    private static int HexDigit(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        throw new FormatException($"invalid hex digit '{c}'");
    }

    /// <summary>toHex channel value: JS Math.round = Floor(x + 0.5) — C# Math.Round is banker's rounding.</summary>
    public static int Quantize255(double v) => (int) Math.Floor(v * 255 + 0.5);

    /// <summary>8-bit quantized channel as float (k/255), matching a toHex→parseHex round trip.</summary>
    public static float QuantizedChannel(double v) => Quantize255(Math.Min(1, Math.Max(0, v))) / 255f;

    public static Rgba Quantize(in Rgba c) => new Rgba(
        QuantizedChannel(c.R), QuantizedChannel(c.G), QuantizedChannel(c.B), QuantizedChannel(c.A));

    /// <summary>oklch() — OKLCH → sRGB with chroma-reduction gamut mapping, quantized to 8 bits.</summary>
    public static Rgba Oklch(double l, double c, double h) => Quantize(FlatColor.OklchToRgb(new Oklch(l, c, h)));

    /// <summary>mix() — OKLab interpolation with premultiplied alpha. Endpoints return verbatim
    /// (preserving authored refs); alpha 0 collapses to #00000000; the result is 8-bit quantized.</summary>
    public static SkinColor Mix(in SkinColor a, in SkinColor b, double t)
    {
        if (a.Equals(b) || t <= 0) return a;
        if (t >= 1) return b;
        var aa = (double) a.Value.A;
        var ba = (double) b.Value.A;
        var alpha = aa + (ba - aa) * t;
        if (alpha == 0) return SkinColor.Transparent;
        // TS parses endpoint RGB with the alpha digits stripped and interpolates raw
        // OKLab (l, a, b) — compute the lab components directly, no Oklch round trip.
        var x = RgbToOklab(a.Value);
        var y = RgbToOklab(b.Value);
        var k = ba * t / alpha;
        var l = x.L + (y.L - x.L) * k;
        var oa = x.A + (y.A - x.A) * k;
        var ob = x.B + (y.B - x.B) * k;
        var rgb = Oklch(l, Math.Sqrt(oa * oa + ob * ob), Math.Atan2(ob, oa) * 180 / Math.PI);
        return new SkinColor(new Rgba(rgb.R, rgb.G, rgb.B, QuantizedChannel(alpha)));
    }

    public readonly struct Oklab
    {
        public readonly double L, A, B;

        public Oklab(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }
    }

    /// <summary>rgbToOklab — ported coefficient-for-coefficient from core/color.ts.</summary>
    public static Oklab RgbToOklab(in Rgba rgb)
    {
        var lr = ToLinear(rgb.R);
        var lg = ToLinear(rgb.G);
        var lb = ToLinear(rgb.B);
        var l = CubeRoot(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb);
        var m = CubeRoot(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
        var s = CubeRoot(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
        return new Oklab(
            0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s);
    }

    private static double ToLinear(double c) =>
        c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double CubeRoot(double c) => c < 0 ? -Math.Pow(-c, 1.0 / 3) : Math.Pow(c, 1.0 / 3);

    /// <summary>Canonical lowercase hex string for a quantized color (dump rendering).</summary>
    public static string ToHexString(in Rgba c)
    {
        var hex = HexByte(c.R) + HexByte(c.G) + HexByte(c.B);
        return c.A < 1f ? "#" + hex + HexByte(c.A) : "#" + hex;
    }

    private static string HexByte(float v)
    {
        var n = Math.Clamp((int) Math.Floor(v * 255 + 0.5), 0, 255);
        return n.ToString("x", CultureInfo.InvariantCulture).PadLeft(2, '0');
    }

    /// <summary>Valid per the schema: #rgb, #rgba, #rrggbb or #rrggbbaa (case-insensitive).</summary>
    public static bool IsHexColor(string text) =>
        text.Length == 4 || text.Length == 5 || text.Length == 7 || text.Length == 9;
}
