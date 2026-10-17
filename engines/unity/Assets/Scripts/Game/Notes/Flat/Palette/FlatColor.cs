// Port of cytoid-design packages/notes/src/core/color.ts — OKLCH color math.
// PURE C#: no UnityEngine references (verified by Tools/FlatParity).

using System;
using System.Globalization;

public readonly struct Rgba
{
    public readonly float R, G, B, A;

    public Rgba(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}

public readonly struct Oklch
{
    public readonly double L, C, H;

    public Oklch(double l, double c, double h)
    {
        L = l;
        C = c;
        H = h;
    }
}

public static class FlatColor
{
    public static Rgba ParseHex(string hex)
    {
        var hash = hex.IndexOf('#');
        var h = (hash < 0 ? hex : hex.Remove(hash, 1)).Trim();
        if (h.Length == 3 || h.Length == 4)
        {
            var expanded = "";
            foreach (var c in h)
            {
                expanded += new string(c, 2);
            }
            h = expanded;
        }
        var n = ParseHexInt(h.Substring(0, Math.Min(6, h.Length)));
        // The TS RGB parser ignores the fourth/eighth (alpha) digit.
        return new Rgba(((n >> 16) & 255) / 255f, ((n >> 8) & 255) / 255f, (n & 255) / 255f);
    }

    public static string ToHex(Rgba c) => "#" + HexChannel(c.R) + HexChannel(c.G) + HexChannel(c.B);

    public static Oklch RgbToOklch(Rgba c)
    {
        var lr = ToLinear(c.R);
        var lg = ToLinear(c.G);
        var lb = ToLinear(c.B);
        var l = CubeRoot(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb);
        var m = CubeRoot(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
        var s = CubeRoot(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
        var lightness = 0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s;
        var a = 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s;
        var b = 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s;
        var chroma = Math.Sqrt(a * a + b * b);
        var hue = (Math.Atan2(b, a) * 180) / Math.PI;
        if (hue < 0)
        {
            hue += 360;
        }
        return new Oklch(lightness, chroma, hue);
    }

    public static Rgba OklchToRgb(Oklch c)
    {
        var lin = OklchToLinear(c);
        if (!InGamut(lin))
        {
            var lo = 0.0;
            var hi = c.C;
            for (var i = 0; i < 20; i++)
            {
                var mid = (lo + hi) / 2;
                if (InGamut(OklchToLinear(new Oklch(c.L, mid, c.H))))
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }
            lin = OklchToLinear(new Oklch(c.L, lo, c.H));
        }
        return new Rgba(GammaChannel(lin.Item1), GammaChannel(lin.Item2), GammaChannel(lin.Item3));
    }

    public static Rgba Mix(Rgba a, Rgba b, double t) => new Rgba(
        (float)(a.R + ((double)b.R - a.R) * t),
        (float)(a.G + ((double)b.G - a.G) * t),
        (float)(a.B + ((double)b.B - a.B) * t),
        (float)(a.A + ((double)b.A - a.A) * t));

    private static double ToLinear(double c) =>
        c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double ToGamma(double c) =>
        c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

    private static double CubeRoot(double c) => c < 0 ? -Math.Pow(-c, 1.0 / 3) : Math.Pow(c, 1.0 / 3);

    private static (double, double, double) OklchToLinear(Oklch c)
    {
        var hr = (c.H * Math.PI) / 180;
        var a = c.C * Math.Cos(hr);
        var b = c.C * Math.Sin(hr);
        var l = Math.Pow(c.L + 0.3963377774 * a + 0.2158037573 * b, 3);
        var m = Math.Pow(c.L - 0.1055613458 * a - 0.0638541728 * b, 3);
        var s = Math.Pow(c.L - 0.0894841775 * a - 1.291485548 * b, 3);
        return (
            4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s);
    }

    private static bool InGamut((double, double, double) v) =>
        v.Item1 >= -1e-4 && v.Item1 <= 1 + 1e-4 &&
        v.Item2 >= -1e-4 && v.Item2 <= 1 + 1e-4 &&
        v.Item3 >= -1e-4 && v.Item3 <= 1 + 1e-4;

    private static float GammaChannel(double v) =>
        (float)Math.Min(1, Math.Max(0, ToGamma(Math.Min(1, Math.Max(0, v)))));

    private static string HexChannel(double v)
    {
        var n = (long)Math.Floor(v * 255 + 0.5);
        var hex = n < 0 ? "-" + (-n).ToString("x", CultureInfo.InvariantCulture) : n.ToString("x", CultureInfo.InvariantCulture);
        return hex.PadLeft(2, '0');
    }

    private static int ParseHexInt(string h)
    {
        var i = 0;
        var sign = 1;
        if (i < h.Length && (h[i] == '+' || h[i] == '-'))
        {
            sign = h[i++] == '-' ? -1 : 1;
        }
        if (i + 1 < h.Length && h[i] == '0' && (h[i + 1] == 'x' || h[i + 1] == 'X'))
        {
            i += 2;
        }
        var n = 0;
        for (; i < h.Length; i++)
        {
            var c = h[i];
            var digit = c >= '0' && c <= '9' ? c - '0' :
                c >= 'a' && c <= 'f' ? c - 'a' + 10 :
                c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
            if (digit < 0)
            {
                break;
            }
            n = n * 16 + digit;
        }
        return sign * n;
    }
}
