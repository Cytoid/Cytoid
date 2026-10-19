// Port of cytoid-design packages/notes/src/core/ease.ts — easing & timeline helpers.
// PURE C#: no UnityEngine references (verified by Tools/FlatParity).

using System;

public static class FlatEase
{
    public static double Clamp(double v, double a = 0, double b = 1) => Math.Min(b, Math.Max(a, v));

    public static double Clamp01(double v) => Clamp(v, 0, 1);

    // effects.ts: absolute seconds since chart hit, independent of approach speed.
    public static double ExitAlpha(double t, ExitCurve curve)
    {
        if (t < 0) return 1;
        var p = t / curve.Duration;
        return p >= 1 ? 0 : Math.Max(0, curve.Drop - curve.Fall * Math.Pow(p, curve.Power));
    }

    public static double ExitRewind(double t, double approach) =>
        Math.Max(0, 1 - Math.Max(0, t) * FlatTokens.Time.Exit.Rewind / Math.Max(1e-6, approach));

    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static double Seg(double t, double a, double b)
    {
        if (b <= a)
        {
            return t >= b ? 1 : 0;
        }
        return Clamp01((t - a) / (b - a));
    }

    public static double Bump(double t, double a, double b)
    {
        var x = Seg(t, a, b);
        return x <= 0 || x >= 1 ? 0 : Math.Sin(x * Math.PI);
    }

    public static double Linear(double t) => t;

    public static double InQuad(double t) => t * t;

    public static double OutQuad(double t) => 1 - (1 - t) * (1 - t);

    public static double OutSine(double t) => Math.Sin((t * Math.PI) / 2);

    public static double InSine(double t) => 1 - Math.Cos((t * Math.PI) / 2);

    public static double InOutSine(double t) => (1 - Math.Cos(t * Math.PI)) / 2;

    public static double InOutQuad(double t) => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;

    public static double InCubic(double t) => t * t * t;

    public static double OutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    public static double InOutCubic(double t) => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    public static double OutQuart(double t) => 1 - Math.Pow(1 - t, 4);

    public static double OutQuint(double t) => 1 - Math.Pow(1 - t, 5);

    public static double OutExpo(double t) => t >= 1 ? 1 : 1 - Math.Pow(2, -10 * t);

    public static double InExpo(double t) => t <= 0 ? 0 : Math.Pow(2, 10 * t - 10);

    public static double OutBack(double t, double s = 1.70158)
    {
        var c3 = s + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + s * Math.Pow(t - 1, 2);
    }

    public static double Ranged(double t, double a, double b, Func<double, double> ease) => ease(Seg(t, a, b));

    public static Func<double, double> CubicBezier(double x1, double y1, double x2, double y2)
    {
        var cx = 3 * x1;
        var bx = 3 * (x2 - x1) - cx;
        var ax = 1 - cx - bx;
        var cy = 3 * y1;
        var by = 3 * (y2 - y1) - cy;
        var ay = 1 - cy - by;
        return t =>
        {
            if (t <= 0)
            {
                return y1;
            }
            if (t >= 1)
            {
                return y2;
            }
            var lo = 0.0;
            var hi = 1.0;
            var u = t;
            for (var i = 0; i < 24; i++)
            {
                u = (lo + hi) / 2;
                if (((ax * u + bx) * u + cx) * u < t)
                {
                    lo = u;
                }
                else
                {
                    hi = u;
                }
            }
            return ((ay * u + by) * u + cy) * u;
        };
    }

    public readonly struct RiseSettle
    {
        public readonly double Spawn, Peak, PeakP, SettleP;

        public RiseSettle(double spawn, double peak, double peakP, double settleP)
        {
            Spawn = spawn;
            Peak = peak;
            PeakP = peakP;
            SettleP = settleP;
        }
    }

    public static Func<double, double> RiseSettleCurve(RiseSettle curve, (double, double) s)
    {
        var shape = CubicBezier(s.Item1, 0, s.Item2, 1);
        return p =>
        {
            if (p < curve.PeakP)
            {
                return Lerp(curve.Spawn, curve.Peak, shape(Seg(p, 0, curve.PeakP)));
            }
            if (p < curve.SettleP)
            {
                return Lerp(curve.Peak, 1, shape(Seg(p, curve.PeakP, curve.SettleP)));
            }
            return 1;
        };
    }

    public struct Rng
    {
        private uint state;

        public Rng(uint seed)
        {
            state = seed;
        }

        public double Next()
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                var t = state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
