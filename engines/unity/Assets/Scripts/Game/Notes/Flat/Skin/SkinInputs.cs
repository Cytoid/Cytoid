// Port of cytoid-design packages/notes/src/skin/inputs.ts — the named geometry
// and time inputs value expressions reference ({ input: 'innerHalf' }).
// Everything derives from the DrawContext alone, per draw call.
// PURE C#: no UnityEngine references.

using System;

public enum SkinInput
{
    Unit = 0,
    Size,
    Heading,
    R,
    W,
    Inner,
    RingC,
    InnerHalf,
    Hair,
    RingW,
    BracketR,
    BracketW,
    HoldLanding,
    GlyphW,
    CoreR,
    ProgressR,
    ProgressW,
    FlickW,
    Rv,
    FlickInner,
    CoreMax,
    FrameOuter,
    Near,
    NearFar,
    ApproachNear,
    ArrowSize,
    ArrowTip,
    ArrowHalf,
    ExitRewindEnd,
    Length,
    Top,
    Bottom,
    Appear,
    Progress,
    Elapsed,
    Remaining,
    Duration,
    BodyWidth,
    DashPeriod,
    Collapse,
    Count,
}

public static class SkinInputNames
{
    private static readonly string[] Names =
    {
        "unit", "size", "heading", "R", "W", "inner", "ringC", "innerHalf", "hair", "ringW",
        "bracketR", "bracketW", "holdLanding", "glyphW", "coreR", "progressR", "progressW",
        "flickW", "rv", "flickInner", "coreMax", "frameOuter", "near", "nearFar", "approachNear",
        "arrowSize", "arrowTip", "arrowHalf", "exitRewindEnd", "length", "top", "bottom",
        "appear", "progress", "elapsed", "remaining", "duration", "bodyWidth", "dashPeriod", "collapse",
    };

    public static bool TryParse(string name, out SkinInput input)
    {
        for (var i = 0; i < Names.Length; i++)
        {
            if (Names[i] != name) continue;
            input = (SkinInput) i;
            return true;
        }
        input = default;
        return false;
    }

    public static string Name(SkinInput input) => Names[(int) input];
}

/// <summary>The resolved input table of one draw call: values + presence flags
/// (TS keeps absent keys undefined; the interpreter must throw reading them).</summary>
public sealed class SkinInputTable
{
    public readonly double[] Values = new double[(int) SkinInput.Count];
    public readonly bool[] Present = new bool[(int) SkinInput.Count];

    public void Clear()
    {
        Array.Clear(Present, 0, Present.Length);
    }

    public double Get(SkinInput input)
    {
        var i = (int) input;
        if (!Present[i])
            throw new SkinRenderException($"unknown input \"{SkinInputNames.Name(input)}\"");
        return Values[i];
    }

    public void Set(SkinInput input, double value)
    {
        var i = (int) input;
        Values[i] = value;
        Present[i] = true;
    }

    /// <summary>holdLanding / exitRewindEnd time inputs must be finite when read.</summary>
    public double GetTime(SkinInput input)
    {
        var value = Get(input);
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new SkinRenderException($"missing time input \"{SkinInputNames.Name(input)}\"");
        return value;
    }
}

/// <summary>DrawContext — everything a clip needs to draw one frame (notes/types.ts).</summary>
public sealed class SkinDrawContext
{
    public NoteKind Kind;
    /// <summary>Base ring and hair widths in context pixels, before kind-specific adjustment.</summary>
    public double RingStroke, HairStroke;
    public SkinPaletteContext Palettes;
    /// <summary>Bound by judgement slots; absent for note animation previews.</summary>
    public SkinGrade? Grade;
    public FlatDirection Direction, BodyDirection;
    /// <summary>Chain heading for drag-head arrows, radians (0 = up, clockwise).</summary>
    public double Heading;
    /// <summary>Real approach window in seconds (intro → hit).</summary>
    public double Approach;
    /// <summary>Note outer diameter in px (unit × kind size × scale).</summary>
    public double Size;
    /// <summary>Click note diameter in px (the global unit, scaled).</summary>
    public double Unit;

    public SkinDrawContext Clone()
    {
        var copy = new SkinDrawContext();
        copy.CopyFrom(this);
        return copy;
    }

    public void CopyFrom(SkinDrawContext source)
    {
        Kind = source.Kind;
        RingStroke = source.RingStroke;
        HairStroke = source.HairStroke;
        Palettes = source.Palettes;
        Grade = source.Grade;
        Direction = source.Direction;
        BodyDirection = source.BodyDirection;
        Heading = source.Heading;
        Approach = source.Approach;
        Size = source.Size;
        Unit = source.Unit;
    }
}

public static class SkinInputs
{
    /// <summary>The hold brackets land `settle` seconds (≈ 4 frames @ 60 fps) before the hit.</summary>
    private const double BracketSettle = 4.0 / 60.0;

    /// <summary>Ring stroke for a note of this size (thinner on small notes).</summary>
    public static double RingWidth(SkinDrawContext ctx) =>
        ctx.RingStroke * Math.Pow(ctx.Size / ctx.Unit, 0.5);

    public static void Resolve(SkinDrawContext ctx, SkinInputTable inputs)
    {
        inputs.Clear();
        var unit = ctx.Unit;
        var size = ctx.Size;
        var r = size / 2;
        var w = RingWidth(ctx);
        var hair = ctx.HairStroke;
        inputs.Set(SkinInput.Unit, unit);
        inputs.Set(SkinInput.Size, size);
        inputs.Set(SkinInput.Heading, ctx.Heading);
        inputs.Set(SkinInput.R, r);
        inputs.Set(SkinInput.W, w);
        inputs.Set(SkinInput.Inner, r - w);
        inputs.Set(SkinInput.RingC, r - w / 2);
        inputs.Set(SkinInput.InnerHalf, r - w * 1.5);
        inputs.Set(SkinInput.Hair, hair);
        // unscaled ring stroke (unit · ring) — judgement effects stroke with this
        inputs.Set(SkinInput.RingW, ctx.RingStroke);
        if (ctx.Kind == NoteKind.Hold || ctx.Kind == NoteKind.LongHold)
        {
            inputs.Set(SkinInput.BracketR, r * 1.2);
            inputs.Set(SkinInput.BracketW, hair * 2.6);
            inputs.Set(SkinInput.HoldLanding, 1 - Math.Min(0.2, BracketSettle / Math.Max(1e-3, ctx.Approach)));
            inputs.Set(SkinInput.GlyphW, hair * 1.6);
            inputs.Set(SkinInput.CoreR, r * 0.6);
            inputs.Set(SkinInput.ProgressR, r * 1.34);
            inputs.Set(SkinInput.ProgressW, unit * 0.083);
        }
        else if (ctx.Kind == NoteKind.Flick)
        {
            // the flick strokes with the *unscaled* ring width
            var fw = ctx.RingStroke;
            var rv = r - fw * 0.7;
            inputs.Set(SkinInput.FlickW, fw);
            inputs.Set(SkinInput.Rv, rv);
            inputs.Set(SkinInput.FlickInner, rv - fw * 1.1);
            inputs.Set(SkinInput.CoreMax, rv - fw / Math.Cos(Math.PI / 4));
            inputs.Set(SkinInput.FrameOuter, rv + fw / 2);
            inputs.Set(SkinInput.Near, r * 0.98 + fw * 0.6);
            inputs.Set(SkinInput.NearFar, r * 0.98 + fw * 0.6 + unit * 0.3);
            inputs.Set(SkinInput.ApproachNear, r * 0.98 + fw * 0.6 + unit * 0.35);
            inputs.Set(SkinInput.ArrowSize, r * 0.62);
            inputs.Set(SkinInput.ArrowTip, r * 0.62 * 0.3);
            inputs.Set(SkinInput.ArrowHalf, r * 0.62 * 0.6);
        }
    }

    public sealed class BodyOptions
    {
        public double Length = double.NaN; // hold
        public double Top = double.NaN, Bottom = double.NaN; // long-hold
        public double Progress;
        public double T; // seconds — scrolls the centre dashes while holding
        public double? HoldDuration; // total chart hold duration; null offline
        public double Appear = 1;
    }

    /// <summary>Fixed geometry and clock contract for parametric body slots.</summary>
    public static void ResolveBody(SkinDrawContext ctx, BodyOptions options, SkinInputTable inputs)
    {
        Resolve(ctx, inputs);
        var isHold = ctx.Kind == NoteKind.Hold;
        var hasGeometry = isHold ? !double.IsNaN(options.Length) : !double.IsNaN(options.Top) && !double.IsNaN(options.Bottom);
        if (!hasGeometry)
            throw new SkinRenderException($"{SkinTimingTokens.KindKey(ctx.Kind)}: incorrect body geometry");
        var geometryFinite = isHold
            ? IsFinite(options.Length)
            : IsFinite(options.Top) && IsFinite(options.Bottom);
        if (!IsFinite(options.Progress) || !geometryFinite)
            throw new SkinRenderException("body geometry and progress must be finite");
        var elapsed = double.IsNaN(options.T) ? 0 : options.T;
        var duration = options.HoldDuration.HasValue ? Math.Max(0, options.HoldDuration.Value) : 0;
        var progress = Math.Max(0, Math.Min(1, options.Progress));
        var collapseWindow = Math.Min(0.6, duration);
        if (isHold) inputs.Set(SkinInput.Length, Math.Max(0, options.Length));
        else
        {
            inputs.Set(SkinInput.Top, Math.Max(0, options.Top));
            inputs.Set(SkinInput.Bottom, Math.Max(0, options.Bottom));
        }
        inputs.Set(SkinInput.Appear, Math.Max(0, Math.Min(1, options.Appear)));
        inputs.Set(SkinInput.Progress, progress);
        inputs.Set(SkinInput.Elapsed, elapsed);
        inputs.Set(SkinInput.Duration, duration);
        inputs.Set(SkinInput.Remaining, Math.Max(0, duration - elapsed));
        inputs.Set(SkinInput.BodyWidth, ctx.Unit * 0.284);
        inputs.Set(SkinInput.DashPeriod, Math.Max(2, Math.Round(ctx.Unit * 0.284 * 1.1)));
        // Without a live duration (offline strips), retain the progress-sampled pose.
        inputs.Set(SkinInput.Collapse, duration > 0
            ? Math.Max(0, Math.Min(1, (elapsed - Math.Max(0, duration - collapseWindow)) / collapseWindow))
            : Math.Max(0, Math.Min(1, (progress - 0.86) / (1 - 0.86))));
    }

    public sealed class ChainOptions
    {
        /// <summary>Current source→destination distance in px (required, finite).</summary>
        public double Length = double.NaN;
        /// <summary>Seconds since the link started growing — the sustain clock; absent → 0.</summary>
        public double T = double.NaN;
    }

    /// <summary>Fixed geometry and clock contract for the chain-link slot. Growth and
    /// retraction stay engine-private: the skin sees the full 0..length template
    /// plus the sustain clock only.</summary>
    public static void ResolveChain(SkinDrawContext ctx, ChainOptions options, SkinInputTable inputs)
    {
        Resolve(ctx, inputs);
        // Present-but-non-finite values name the input (inputs.ts resolveChainInputs);
        // the geometry message is for an absent length.
        if (!double.IsNaN(options.Length) && !IsFinite(options.Length))
            throw new SkinRenderException("chain input \"length\" must be finite");
        if (!IsFinite(options.Length))
            throw new SkinRenderException("chain geometry and progress must be finite");
        var elapsed = double.IsNaN(options.T) ? 0 : options.T;
        if (!IsFinite(elapsed))
            throw new SkinRenderException("chain input \"t\" must be finite");
        inputs.Set(SkinInput.Length, Math.Max(0, options.Length));
        inputs.Set(SkinInput.Elapsed, elapsed);
    }

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
