// Port of cytoid-design packages/notes/src/tokens.ts — design tokens.
// PURE C#: no UnityEngine references (verified by Tools/FlatParity).

using System;

public enum NoteKind
{
    Click,
    Hold,
    LongHold,
    DragHead,
    DragChild,
    Flick,
    ClickDragHead,
    ClickDragChild,
    DropClick,
    DropDrag,
}

public enum SizeKind
{
    Click,
    Hold,
    DragHead,
    DragChild,
}

public readonly struct ExitCurve
{
    public readonly double Duration, Drop, Fall, Power;

    public ExitCurve(double duration, double drop, double fall, double power)
    {
        Duration = duration;
        Drop = drop;
        Fall = fall;
        Power = power;
    }
}

public static class FlatTokens
{
    public const double Unit = 128;

    private static readonly Rgba PerfectColor = FlatColor.ParseHex("#5BC0EB");
    private static readonly Rgba GreatColor = FlatColor.ParseHex("#FDE74C");
    private static readonly Rgba GoodColor = FlatColor.ParseHex("#9BC53D");
    private static readonly Rgba BadColor = FlatColor.ParseHex("#E55934");
    private static readonly Rgba MissColor = FlatColor.ParseHex("#6B6F7A");

    /// <summary>tokens.grade defaults; grade uses the lowercase TS names.</summary>
    public static Rgba GradeColor(string grade)
    {
        switch (grade)
        {
            case "perfect": return PerfectColor;
            case "great": return GreatColor;
            case "good": return GoodColor;
            case "bad": return BadColor;
            case "miss": return MissColor;
            default: throw new ArgumentOutOfRangeException(nameof(grade));
        }
    }

    /// <summary>Relative outer size per kind (tokens.size).</summary>
    public static double Size(NoteKind kind)
    {
        switch (kind)
        {
            case NoteKind.Click:
            case NoteKind.Hold:
            case NoteKind.LongHold:
            case NoteKind.ClickDragHead:
            case NoteKind.DropClick:
                return 1;
            case NoteKind.DragHead:
                return 0.675;
            case NoteKind.DragChild:
            case NoteKind.ClickDragChild:
                return 0.375;
            case NoteKind.Flick:
                return 1.125;
            case NoteKind.DropDrag:
                return 0.8;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    // tokens.stroke — relative to unit
    public const double StrokeRing = 0.085;
    public const double StrokeHair = 0.022;
    public const double StrokeHoldBody = 0.284;

    public static class Time
    {
        public static class Exit
        {
            public const double Rewind = 1;
            public static readonly ExitCurve Tap = new ExitCurve(0.3, 0.8, 1, 3);
            public static readonly ExitCurve HoldHead = new ExitCurve(0.3, 1, 1, 3);
            public static readonly ExitCurve HoldBody = new ExitCurve(0.15, 0.8, 0.8, 3);
            public static readonly ExitCurve DragChild = new ExitCurve(0.15, 1, 1, 1);
        }
        public const double HoldPress = 0.2;
        public const double HoldLoop = 0.6;
        public const double ClearPerfect = 0.42;
        public const double ClearGreat = 0.46;
        public const double ClearGood = 0.52;
        public const double ClearBad = 0.56;
        public const double ClearMiss = 0.5;

        public static double ClearDuration(string grade)
        {
            switch (grade)
            {
                case "perfect": return ClearPerfect;
                case "great": return ClearGreat;
                case "good": return ClearGood;
                case "bad": return ClearBad;
                case "miss": return ClearMiss;
                default: throw new ArgumentOutOfRangeException(nameof(grade));
            }
        }
    }

    public static (double X1, double X2) SizeCurveShape => (0.7, 0.75);

    // Holds have one monotone ease, rather than a RiseSettle overshoot.
    public static (double Spawn, (double X1, double X2) Ease) HoldSizeCurve => (0.34, (0.75, 0.3));
    public const double HoldWakeFrom = 0.3;
    public const double HoldWakeTo = 0.85;

    /// <summary>Rise/settle enter-size flow for clicks and drags (holds use HoldSizeCurve).</summary>
    public static FlatEase.RiseSettle SizeCurve(SizeKind kind)
    {
        switch (kind)
        {
            case SizeKind.Click:
                return new FlatEase.RiseSettle(0.319, 1.06, 0.878, 0.976);
            case SizeKind.DragHead:
                return new FlatEase.RiseSettle(0.293, 1.05, 0.766, 0.872);
            case SizeKind.DragChild:
                return new FlatEase.RiseSettle(0.259, 1.03, 0.766, 0.851);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}

// Shared pure math from hold.ts / bodies.ts, exercised by Tools/FlatParity.
// tokens.stroke.edge: the dark edge on a band's outer silhouette — the flat answer
// to overlapping notes (two white rings would merge; the edge keeps a boundary).
// Instead of one solid keyline it is a stepped falloff: abutting, non-overlapping
// bands of tokens.edge, widths as fractions of the ring band, opacity falling off
// from the silhouette away — a shadow read without blur, filters or gradients.
// Placed outside the silhouette by default: a cast-shadow halo, near-invisible on
// the stage, a boundary on whatever note lies below. 'Inside' would eat into the
// ring band instead, keeping the calibrated silhouette.
public static class FlatEdge
{
    public const int Count = 3;
    public const bool Outside = true;
    private static readonly double[] Fractions = { 0.10, 0.10, 0.10 };
    private static readonly double[] Opacities = { 0.9, 0.5, 0.2 };

    // Band i resolved against a band of `width`: centreline offset from the
    // silhouette (negative = inward), band width, opacity. Bands abut exactly, so
    // semi-transparent steps never double up; inside bands are clamped to the
    // ring band. Allocation-free: callers loop i over Count.
    public static bool Band(int i, double width, out double offset, out double bandWidth, out double opacity)
    {
        offset = bandWidth = opacity = 0;
        if (!(width > 0) || i < 0 || i >= Count) return false;
        var cursor = 0.0;
        for (var j = 0; j <= i; j++)
        {
            var w = width * Math.Max(0, Fractions[j]);
            if (!Outside) w = Math.Min(w, width - cursor);
            if (j == i)
            {
                if (!(w > 0) || !(Opacities[j] > 0)) return false;
                var c = cursor + w / 2;
                offset = Outside ? c : -c;
                bandWidth = w;
                opacity = Math.Min(1, Opacities[j]);
                return true;
            }
            cursor += Math.Max(0, w);
        }
        return false;
    }
}

// drop.ts: the classic drop sprites' proportions, measured at the 50 % alpha
// contour at PPU = canvas width (Note-DropClick-Ring 536 × 76.8, its fill
// 360.8 × 68.8 and core 144.8 × 32.8; Note-DropDrag-Ring 404 × 76.8, its fill
// 244.8 × 60.8). Capsules are relative to the kind size; fill and core are
// fractions of the body capsule.
public static class FlatDropDesign
{
    public static readonly (double W, double H) ClickCapsule = (1, 76.8 / 536);
    public static readonly (double W, double H) ClickFill = (360.8 / 536, 68.8 / 76.8);
    public static readonly (double W, double H) ClickCore = (144.8 / 536, 32.8 / 76.8);
    public static readonly (double W, double H) DragCapsule = (1, 76.8 / 404);
    public static readonly (double W, double H) DragFill = (244.8 / 404, 60.8 / 76.8);
}

public static class FlatHoldDesign
{
    public const double CoreRadius = 0.6;
    public const double CoreShade = 900;
    public const double BracketRadius = 1.2;
    public const double BracketHalf = Math.PI * 2 * 0.16;
    public const double BracketWidth = 2.6;
    public const double BracketFrom = 0.1;
    public const double BracketFadeTo = 0.25;
    public const double BracketTurns = 1.05;
    public const double BracketDamping = 5;
    public const double BracketSettle = 4.0 / 60;

    public const double BodyRevealStart = 0.08;
    public const double BodyRevealEnd = 0.35;
    public const double BodyRevealFadeEnd = 0.22;
    public const double BodyRevealFrom = 0.8;

    private static readonly Func<double, double> SizeEase =
        FlatEase.CubicBezier(FlatTokens.HoldSizeCurve.Ease.X1, 0, FlatTokens.HoldSizeCurve.Ease.X2, 1);

    // Real intro -> hit seconds, with the pre-hit rest capped to the last 20%.
    public static double BracketLandP(double approach) =>
        1 - Math.Min(0.2, BracketSettle / Math.Max(1e-3, approach));

    public static double BracketSpin(double p, double approach)
    {
        var u = FlatEase.Seg(p, BracketFrom, BracketLandP(approach));
        var damped = (1 - Math.Exp(-BracketDamping * u)) / (1 - Math.Exp(-BracketDamping));
        return -Math.PI * 2 * BracketTurns * (1 - damped);
    }

    public static double Size(double p, double approach) =>
        FlatEase.Lerp(FlatTokens.HoldSizeCurve.Spawn, 1, SizeEase(FlatEase.Seg(p, 0, BracketLandP(approach))));

    public static double Wake(double p) =>
        FlatEase.InOutSine(FlatEase.Seg(p, FlatTokens.HoldWakeFrom, FlatTokens.HoldWakeTo));

    public static (double Stretch, double Alpha) BodyReveal(double appear) =>
        (FlatEase.Lerp(BodyRevealFrom, 1, FlatEase.OutCubic(FlatEase.Seg(appear, BodyRevealStart, BodyRevealEnd))),
            FlatEase.Seg(appear, BodyRevealStart, BodyRevealFadeEnd));
}
