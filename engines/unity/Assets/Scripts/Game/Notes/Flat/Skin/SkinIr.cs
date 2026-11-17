// Skin compiler — ports packages/notes/src/skin/{runtime,compile}.ts from the
// closure-based TS interpreter to precompiled immutable object graphs:
// value expressions, tracks, gates and merged frame trees are built once at
// load time; per-frame evaluation allocates nothing.
// PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;

public class SkinCompileException : Exception
{
    public SkinCompileException(string message) : base(message)
    {
    }
}

public class SkinRenderException : Exception
{
    public SkinRenderException(string message) : base(message)
    {
    }
}

/// <summary>A numeric field: a constant, an expression (property binding), or absent
/// (NaN const, no expression) — absent fields evaluate to 0 and are omitted from dumps.</summary>
public readonly struct SkinField
{
    public readonly double Const;
    public readonly SkinExpr Expr;

    public SkinField(double value)
    {
        Const = value;
        Expr = null;
    }

    public SkinField(SkinExpr expr)
    {
        Const = double.NaN;
        Expr = expr;
    }

    public static readonly SkinField Absent = new SkinField(double.NaN);

    public bool Present => Expr != null || !double.IsNaN(Const);

    public double Eval(SkinEnv env) => Expr == null ? (double.IsNaN(Const) ? 0 : Const) : Expr.Eval(env);
}

/// <summary>Compiled value expressions — the tape of compileNum with const folding.</summary>
public abstract class SkinExpr
{
    public abstract double Eval(SkinEnv env);
}

public sealed class SkinConstExpr : SkinExpr
{
    public readonly double Value;

    public SkinConstExpr(double value)
    {
        Value = value;
    }

    public override double Eval(SkinEnv env) => Value;
}

public sealed class SkinInputExpr : SkinExpr
{
    public readonly SkinInput Input;

    public SkinInputExpr(SkinInput input)
    {
        Input = input;
    }

    public override double Eval(SkinEnv env) => env.Inputs.Get(Input);
}

public sealed class SkinTrackExpr : SkinExpr
{
    public readonly int Index;

    public SkinTrackExpr(int index)
    {
        Index = index;
    }

    public override double Eval(SkinEnv env) => env.TrackValue(Index);
}

public sealed class SkinLerpExpr : SkinExpr
{
    public readonly SkinExpr From, To, Progress;

    public SkinLerpExpr(SkinExpr from, SkinExpr to, SkinExpr progress)
    {
        From = from;
        To = to;
        Progress = progress;
    }

    public override double Eval(SkinEnv env)
    {
        var t = Progress.Eval(env);
        return From.Eval(env) + (To.Eval(env) - From.Eval(env)) * t;
    }
}

public sealed class SkinAddExpr : SkinExpr
{
    public readonly SkinExpr[] Terms;

    public SkinAddExpr(SkinExpr[] terms)
    {
        Terms = terms;
    }

    public override double Eval(SkinEnv env)
    {
        var sum = 0.0;
        foreach (var term in Terms) sum += term.Eval(env);
        return sum;
    }
}

public sealed class SkinMultiplyExpr : SkinExpr
{
    public readonly SkinExpr[] Terms;

    public SkinMultiplyExpr(SkinExpr[] terms)
    {
        Terms = terms;
    }

    public override double Eval(SkinEnv env)
    {
        var product = 1.0;
        foreach (var term in Terms) product *= term.Eval(env);
        return product;
    }
}

public sealed class SkinClampExpr : SkinExpr
{
    public readonly SkinExpr Value;
    public readonly double Min, Max;

    public SkinClampExpr(SkinExpr value, double min, double max)
    {
        Value = value;
        Min = min;
        Max = max;
    }

    public override double Eval(SkinEnv env)
    {
        var v = Value.Eval(env);
        return Math.Min(Max, Math.Max(Min, v));
    }
}

/// <summary>A time reference: a constant or a named input (holdLanding / exitRewindEnd).</summary>
public readonly struct SkinTimeSpec
{
    public readonly double Const;
    public readonly SkinInput? Input;

    public SkinTimeSpec(double value)
    {
        Const = value;
        Input = null;
    }

    public SkinTimeSpec(SkinInput input)
    {
        Const = double.NaN;
        Input = input;
    }

    public static SkinTimeSpec From(SkinTime time) =>
        time.IsNumber ? new SkinTimeSpec(time.Value) : new SkinTimeSpec(time.Input.Value);

    public double Resolve(SkinInputTable inputs) =>
        !Input.HasValue ? Const : inputs.GetTime(Input.Value);
}

// ------------------------------------------------------------------ ease

public sealed class SkinCompiledEase
{
    public readonly Func<double, double> Fn;
    public readonly bool HasWindow;
    public readonly SkinTimeSpec WindowFrom, WindowTo;

    public SkinCompiledEase(Func<double, double> fn)
    {
        Fn = fn;
    }

    public SkinCompiledEase(Func<double, double> fn, SkinTimeSpec from, SkinTimeSpec to)
    {
        Fn = fn;
        HasWindow = true;
        WindowFrom = from;
        WindowTo = to;
    }

    public double Eval(double phi, SkinInputTable inputs, double segmentK)
    {
        // Plain: the easing input is the key segment's progress. Windowed: the raw
        // clock read through the ease's own window (the imperative ease(seg(p, a, b)) idiom).
        if (!HasWindow) return Fn(segmentK);
        var a = WindowFrom.Resolve(inputs);
        var b = WindowTo.Resolve(inputs);
        return Fn(FlatEase.Seg(phi, a, b));
    }
}

// ------------------------------------------------------------------ colors

public enum SkinColorKind
{
    Hex,
    Ring,
    Theme,
    Grade,
    Family,
    Track,
}

public readonly struct SkinColorBinding
{
    public readonly SkinColorKind Kind;
    public readonly int ColorRef; // authored string ref (Hex)
    public readonly int Shade; // numeric shade level
    public readonly SkinGrade Grade;
    public readonly FlatFamily Family;
    public readonly int TrackIndex;

    public SkinColorBinding(SkinColorKind kind, int colorRef, int shade, SkinGrade grade, FlatFamily family, int trackIndex)
    {
        Kind = kind;
        ColorRef = colorRef;
        Shade = shade;
        Grade = grade;
        Family = family;
        TrackIndex = trackIndex;
    }

    public SkinColor Resolve(SkinEnv env)
    {
        switch (Kind)
        {
            case SkinColorKind.Hex:
                return new SkinColor(env.Skin.RefValue(ColorRef), env.Skin.ColorRefs[ColorRef]);
            case SkinColorKind.Ring:
                return SkinPalette.RingTone(env.Ctx.Palettes.Note, Shade);
            case SkinColorKind.Theme:
            {
                // ctx.grade (bound by judgement slots) selects the grade ramp, else the note palette.
                var theme = env.Ctx.Grade.HasValue && env.Ctx.Palettes.Grades != null
                    ? env.Ctx.Palettes.Grades.Palette(env.Ctx.Grade.Value)
                    : env.Ctx.Palettes.Note;
                return SkinPalette.SampleScale(theme, Shade);
            }
            case SkinColorKind.Grade:
                return SkinPalette.SampleScale(env.Ctx.Palettes.Grades.Palette(Grade), Shade);
            case SkinColorKind.Family:
                return SkinPalette.SampleScale(env.Ctx.Palettes.Family(Family, env.Ctx.Direction), Shade);
            case SkinColorKind.Track:
                return env.TrackColor(TrackIndex);
            default:
                throw new SkinRenderException("invalid color binding");
        }
    }
}

// ------------------------------------------------------------------ tracks

public sealed class SkinCompiledTrack
{
    public bool IsColor;
    public SkinTrackForm Form;
    public SkinExpr Driver;

    // Keys form
    public SkinTimeSpec[] KeyTimes;
    public double[] KeyValues;
    public SkinColorBinding[] KeyColors;
    public SkinCompiledEase[] KeyEases; // index i+1 shapes the segment arriving at key i+1
    public bool DynamicTimes;
    public bool HasRate;
    public double Rate = 1;
    public SkinTimeSpec Offset;

    // Ease form
    public SkinCompiledEase Ease;

    // Bump form
    public (SkinTimeSpec From, SkinTimeSpec To)[] Windows;
    public double Low, High;

    // Damped form
    public SkinTimeSpec DampedFrom, DampedTo;
    public double Damping;

    // Fade form
    public double Drop, Fall, Power;

    /// <summary>The track's local clock phase (after driver substitution and rate/offset).</summary>
    public double Phase(double phi, SkinInputTable inputs, SkinEnv env)
    {
        var effective = Driver == null ? phi : Driver.Eval(env);
        if (Form != SkinTrackForm.Keys) return effective;
        return LocalPhase(effective, inputs);
    }

    private double LocalPhase(double phi, SkinInputTable inputs)
    {
        if (!HasRate) return phi;
        var local = phi * Rate + Offset.Resolve(inputs);
        // plain `%` (an extra +1 wrap would flip the phase at 0.999…9 + 1)
        return local % 1;
    }

    public double At(double phi, SkinInputTable inputs, SkinEnv env)
    {
        var evaluateStart = SkinPaintStats.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        try
        {
            return AtInner(phi, inputs, env);
        }
        finally
        {
            if (SkinPaintStats.Enabled)
                SkinPaintStats.EvaluateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - evaluateStart;
        }
    }

    private double AtInner(double phi, SkinInputTable inputs, SkinEnv env)
    {
        var effective = Driver == null ? phi : Driver.Eval(env);
        switch (Form)
        {
            case SkinTrackForm.Keys:
            {
                var local = LocalPhase(effective, inputs);
                if (DynamicTimes) ValidateDynamicTimes(inputs);
                var keys = KeyTimes;
                if (local <= keys[0].Resolve(inputs)) return KeyValue(0, env);
                var last = keys.Length - 1;
                if (local >= keys[last].Resolve(inputs)) return KeyValue(last, env);
                // Scan segments from the end: at a boundary the LAST containing segment
                // owns the sample (keeps discontinuous handoffs on their code-accurate side).
                for (var i = last - 1; i >= 0; i--)
                {
                    var a = keys[i].Resolve(inputs);
                    var b = keys[i + 1].Resolve(inputs);
                    if (local < a || local > b) continue;
                    var k = FlatEase.Seg(local, a, b);
                    var ease = KeyEases[i + 1];
                    var eased = ease == null ? k : ease.Eval(local, inputs, k);
                    return IsColor
                        ? 0 // color tracks resolve through AtColor
                        : FlatEase.Lerp(KeyValues[i], KeyValues[i + 1], eased);
                }
                return KeyValue(last, env);
            }
            case SkinTrackForm.Ease:
                return Ease.Fn == null ? effective : Ease.Fn(effective);
            case SkinTrackForm.Bump:
            {
                var v = 0.0;
                foreach (var window in Windows)
                    v = Math.Max(v, FlatEase.Bump(effective, window.From.Resolve(inputs), window.To.Resolve(inputs)));
                return FlatEase.Lerp(Low, High, v);
            }
            case SkinTrackForm.Damped:
            {
                var u = FlatEase.Seg(effective, DampedFrom.Resolve(inputs), DampedTo.Resolve(inputs));
                var d = (1 - Math.Exp(-Damping * u)) / (1 - Math.Exp(-Damping));
                return FlatEase.Lerp(Low, High, d);
            }
            case SkinTrackForm.Fade:
                return effective < 0 ? 1
                    : effective >= 1 ? 0
                    : Math.Max(0, Drop - Fall * Math.Pow(effective, Power));
            default:
                throw new SkinRenderException("invalid track form");
        }
    }

    public SkinColor AtColor(double phi, SkinInputTable inputs, SkinEnv env)
    {
        var evaluateStart = SkinPaintStats.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        try
        {
            return AtColorInner(phi, inputs, env);
        }
        finally
        {
            if (SkinPaintStats.Enabled)
                SkinPaintStats.EvaluateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - evaluateStart;
        }
    }

    private SkinColor AtColorInner(double phi, SkinInputTable inputs, SkinEnv env)
    {
        var effective = Driver == null ? phi : Driver.Eval(env);
        var local = LocalPhase(effective, inputs);
        if (DynamicTimes) ValidateDynamicTimes(inputs);
        var keys = KeyTimes;
        if (local <= keys[0].Resolve(inputs)) return KeyColors[0].Resolve(env);
        var last = keys.Length - 1;
        if (local >= keys[last].Resolve(inputs)) return KeyColors[last].Resolve(env);
        for (var i = last - 1; i >= 0; i--)
        {
            var a = keys[i].Resolve(inputs);
            var b = keys[i + 1].Resolve(inputs);
            if (local < a || local > b) continue;
            var k = FlatEase.Seg(local, a, b);
            var ease = KeyEases[i + 1];
            var eased = ease == null ? k : ease.Eval(local, inputs, k);
            return SkinColorMath.Mix(KeyColors[i].Resolve(env), KeyColors[i + 1].Resolve(env), eased);
        }
        return KeyColors[last].Resolve(env);
    }

    private double KeyValue(int index, SkinEnv env) => IsColor ? 0 : KeyValues[index];

    private void ValidateDynamicTimes(SkinInputTable inputs)
    {
        var previous = double.NegativeInfinity;
        foreach (var key in KeyTimes)
        {
            var at = key.Resolve(inputs);
            if (at < 0 || at > 1 || at <= previous)
                throw new SkinRenderException("resolved key times must increase strictly within 0..1");
            previous = at;
        }
    }

    public void ValidateDynamicWindows(SkinInputTable inputs)
    {
        foreach (var window in Windows)
        {
            if (window.From.Resolve(inputs) >= window.To.Resolve(inputs))
                throw new SkinRenderException("invalid resolved track window");
        }
    }
}

// ------------------------------------------------------------------ transforms & gates

public sealed class SkinTransformExpr
{
    public readonly SkinExpr X, Y, Rotate, Scale, ScaleX, ScaleY;

    public SkinTransformExpr(SkinExpr x, SkinExpr y, SkinExpr rotate, SkinExpr scale, SkinExpr scaleX, SkinExpr scaleY)
    {
        X = x;
        Y = y;
        Rotate = rotate;
        Scale = scale;
        ScaleX = scaleX;
        ScaleY = scaleY;
    }

    public bool HasAny => X != null || Y != null || Rotate != null || Scale != null || ScaleX != null || ScaleY != null;
}

public sealed class SkinCompiledGate
{
    public int TrackIndex = -1;
    public bool Phase;
    public bool HasFrom, HasUntil;
    public SkinTimeSpec From, Until;
    public bool FromInclusive, UntilInclusive;

    public bool Open(double phi, SkinEnv env)
    {
        double value;
        if (TrackIndex < 0) value = phi;
        else if (Phase) value = env.Phase(TrackIndex);
        else value = env.TrackValue(TrackIndex);
        if (HasFrom)
        {
            var from = From.Resolve(env.Inputs);
            if (!(FromInclusive ? value >= from : value > from)) return false;
        }
        if (HasUntil)
        {
            var until = Until.Resolve(env.Inputs);
            if (!(UntilInclusive ? value <= until : value < until)) return false;
        }
        return true;
    }
}

// ------------------------------------------------------------------ merged frames

public sealed class CompiledFrameNode
{
    public SkinFrameKind Kind;
    public SkinField Cx, Cy, R, Start, End, X, Y, W, H, Rx, X1, Y1, X2, Y2, Width, Assemble, Spin, StrokeWidth, Opacity;
    public bool CxPresent, CyPresent, RxPresent;
    public bool HasOpacity;
    public bool HasStrokeWidth;
    public SkinColorBinding? Fill;
    public SkinColorBinding? Stroke;
    public bool HasCap, HasJoin;
    public FlatCap Cap;
    public FlatJoin Join;
    public bool Closed;
    public int Segments;
    public SkinEdgeDoc Edge;
    public Rgba EdgeRgba;
    public bool IsAdd;
    // dash: static (from frame) or expressions (from properties)
    public double[] StaticDash;
    public SkinField[] DashExpr;
    public SkinField DashOffset;
    public bool HasDashOffset;
    // poly
    public SkinField[] PointX, PointY;
    public int PointCount;
    // group
    public List<CompiledFrameNode> Children;
    public SkinTransformExpr Transform; // group frames: static or merged from properties
    public SkinClipExpr Clip;
    public bool Isolate;
    // image
    public string Src;
}

public sealed class SkinClipExpr
{
    public bool IsCircle;
    public SkinField Cx, Cy, R, X, Y, W, H, Rx;
}

public sealed class CompiledLayer
{
    public bool Windowed;
    public double At, Until;
    public SkinCompiledGate Gate;
    // frame source
    public CompiledFrameNode Frame;
    public bool WrapTransform; // leaf frame + properties.transform → wrapper group
    public SkinTransformExpr WrapperTransform;
    // animation source
    public string AnimReference;
    public CompiledSkinAnimation ResolvedAnimation;
    public bool HasSample;
    public SkinExpr SampleFrom, SampleTo;
    public double SampleDuration = -1; // fps-sequences: frames.length / fps; else parent clock duration
    public SkinExpr AnimOpacity;
    public SkinTransformExpr AnimTransform;
    public bool Add;
    public CompiledLayer[] Children;
    public CompiledLayer Otherwise;
    public bool UsesClip, UsesImages, UsesCurvedDash;
}

public sealed class CompiledSkinAnimation
{
    public string Id;
    public CompiledSkin Owner;
    public bool IsSequence;
    public double? Fps;
    public CompiledFrameNode[] SequenceFrames;
    public CompiledLayer[] Layers;
    public string[] TrackNames;
    public SkinCompiledTrack[] Tracks;
    public Dictionary<string, int> TrackIndex;
    public SkinExpr RootOpacity;
    public SkinTransformExpr RootTransform;
    public bool Isolate;
    public bool UsesClip, UsesImages, UsesCurvedDash, HasSequence;
}

public sealed class CompiledSkinSlot
{
    public string Key;
    public SkinSlotContract Contract;
    public CompiledSkinAnimation Animation;
    public SkinGrade? Grade; // bound grade (clear/miss paths and feedback)
    public bool IncludeStart;

    /// <summary>Resolved loop period in seconds: the binding's `period` override, else the contract duration.
    /// 0 = the loop freezes on its first frame.</summary>
    public double Period;

    public string Note;
    public bool UsesUnsupported;

    /// <summary>True when the slot's animation emits image quads. Images only paint
    /// through the dedicated image sink (clear/feedback FX); every other draw path
    /// (heads, bodies, progress, drag actives) has no image sink and must treat
    /// such slots as unsupported so the classic/imperative fallback renders —
    /// otherwise the quads skip silently and the note partly disappears.</summary>
    public bool UsesImages => Animation?.UsesImages ?? false;
}

// ------------------------------------------------------------------ images

/// <summary>Pure half of a compiled image reference: the authored src and its PNG
/// bytes (loaded before compile). The UnityEngine half — the atlas page and the
/// packed UV rect — lives in SkinImage.cs as the other partial, so the pure-C#
/// compile (Tools/SkinProbe) never sees engine types.</summary>
public sealed partial class SkinImage
{
    public string Src; // authored ref (relative path or data: URI)
    public byte[] Bytes; // PNG bytes, loaded before compile
    public int Width, Height; // decoded size (for packing)
    // Packed atlas rect in Unity texture space (y-up): (U0, V0) = image bottom-left.
    public float U0, V0, U1, V1;

    /// <summary>True once the atlas pass assigned a non-empty rect.</summary>
    public bool Packed => U1 > U0;
}
