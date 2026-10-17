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

    /// <summary>tokens.grade defaults; grade uses the lowercase TS names.</summary>
    public static Rgba GradeColor(string grade)
    {
        switch (grade)
        {
            case "perfect": return FlatColor.ParseHex("#5BC0EB");
            case "great": return FlatColor.ParseHex("#FDE74C");
            case "good": return FlatColor.ParseHex("#9BC53D");
            case "bad": return FlatColor.ParseHex("#E55934");
            case "miss": return FlatColor.ParseHex("#6B6F7A");
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
    public const double StrokeEdgeFraction = 0.34;
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
