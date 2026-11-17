// Skin document model — the parsed, pre-validation representation of a
// schema v2 skin JSON (frames / animations / slots / meta), mirroring
// packages/notes/src/skin/schema.ts types. PURE C#.

using System;
using System.Collections.Generic;

public sealed class SkinDocument
{
    public string Id;
    public string Name;
    public SkinMeta Meta;
    public Dictionary<string, SkinFrame> Frames = new Dictionary<string, SkinFrame>();
    public Dictionary<string, SkinAnimationDoc> Animations = new Dictionary<string, SkinAnimationDoc>();
    public Dictionary<string, SkinSlotDoc> Slots = new Dictionary<string, SkinSlotDoc>();
}

/// <summary>Intentionally empty. Skins may still carry a meta.palette/grades
/// section (parsed tolerantly, contents ignored): older third-party skins wrote
/// one, but no runtime ever consumed it — game-config colours always owned the
/// palette, so the fields only invited drift between probe and game.</summary>
public sealed class SkinMeta
{
}

// ----------------------------------------------------------------- values

/// <summary>Time — a number or an input reference (holdLanding / exitRewindEnd).</summary>
public readonly struct SkinTime
{
    public readonly double Value;
    public readonly SkinInput? Input;

    public SkinTime(double value)
    {
        Value = value;
        Input = null;
    }

    public SkinTime(SkinInput input)
    {
        Value = double.NaN;
        Input = input;
    }

    public bool IsNumber => !Input.HasValue;

    public static implicit operator SkinTime(double value) => new SkinTime(value);
}

public abstract class SkinValue
{
}

public sealed class SkinConstValue : SkinValue
{
    public readonly double Value;

    public SkinConstValue(double value)
    {
        Value = value;
    }
}

public sealed class SkinInputValue : SkinValue
{
    public readonly SkinInput Input;

    public SkinInputValue(SkinInput input)
    {
        Input = input;
    }
}

public sealed class SkinTrackValue : SkinValue
{
    public readonly string Name;

    public SkinTrackValue(string name)
    {
        Name = name;
    }
}

public sealed class SkinLerpValue : SkinValue
{
    public readonly SkinValue From, To, Progress;

    public SkinLerpValue(SkinValue from, SkinValue to, SkinValue progress)
    {
        From = from;
        To = to;
        Progress = progress;
    }
}

public sealed class SkinAddValue : SkinValue
{
    public readonly SkinValue[] Terms;

    public SkinAddValue(SkinValue[] terms)
    {
        Terms = terms;
    }
}

public sealed class SkinMultiplyValue : SkinValue
{
    public readonly SkinValue[] Terms;

    public SkinMultiplyValue(SkinValue[] terms)
    {
        Terms = terms;
    }
}

public sealed class SkinClampValue : SkinValue
{
    public readonly SkinValue Value;
    public readonly double Min, Max;

    public SkinClampValue(SkinValue value, double min, double max)
    {
        Value = value;
        Min = min;
        Max = max;
    }
}

// ----------------------------------------------------------------- ease

public sealed class SkinEaseSpec
{
    public string Type; // linear | inQuad | ... | outBack | inPow | outPow | bezier | compose | step
    public double Amount; // outBack
    public double Power; // inPow / outPow
    public double X1, Y1, X2, Y2; // bezier
    public SkinEaseSpec Outer, Inner; // compose
}

// ----------------------------------------------------------------- tracks

public sealed class SkinTrackKey
{
    public SkinTime At;
    public double Value; // numeric form
    public string Color; // color form (when IsColor)
    public bool IsColor;
    public SkinEaseSpec Ease;
    public SkinTime WindowFrom, WindowTo;
    public bool HasWindow;
}

public enum SkinTrackForm
{
    Keys,
    Ease,
    Bump,
    Damped,
    Fade,
}

public sealed class SkinTrackDoc
{
    public SkinTrackForm Form;
    public bool IsColor;
    public List<SkinTrackKey> Keys = new List<SkinTrackKey>();
    public SkinEaseSpec Ease;
    public List<(SkinTime From, SkinTime To)> Windows = new List<(SkinTime, SkinTime)>(); // bump
    public SkinTime From, To; // damped
    public double Damping, Low = double.NaN, High = double.NaN;
    public double Drop, Fall, Power; // fade
    public SkinTime Offset = new SkinTime(double.NaN);
    public bool HasOffset;
    public double Rate = double.NaN;
    public bool HasRate;
    public SkinValue Driver;
}

// ----------------------------------------------------------------- bindings

public sealed class SkinGateDoc
{
    public string Track;
    public bool Phase;
    public SkinTime From = new SkinTime(double.NaN), Until = new SkinTime(double.NaN);
    public bool FromInclusive, UntilInclusive;
    public bool HasFrom, HasUntil;
}

public sealed class SkinTransformDoc
{
    public SkinValue X, Y, Rotate, Scale, ScaleX, ScaleY;
}

public sealed class SkinClipDoc
{
    public bool IsCircle;
    // circle: cx?, cy?, r — rect: x, y, w, h, rx?
    public SkinValue Cx, Cy, R, X, Y, W, H, Rx;
}

public sealed class SkinPointDoc
{
    public SkinValue X, Y;
}

/// <summary>Layer/frame property overrides (expressions allowed).</summary>
public sealed class SkinPropertiesDoc
{
    public SkinValue Opacity;
    public SkinTransformDoc Transform;
    public SkinValue Cx, Cy, R, Start, End, X, Y, W, H, Rx;
    public SkinValue X1, Y1, X2, Y2, Width, Assemble, Spin, DashOffset, StrokeWidth;
    public string Fill, Stroke; // authored color strings
    public string FillTrack, StrokeTrack; // { track: name }
    public List<SkinPointDoc> Points;
    public List<SkinValue> Dash;
    public SkinClipDoc Clip;
}

// ----------------------------------------------------------------- frames

public enum SkinFrameKind
{
    Image,
    Circle,
    Arc,
    Rect,
    Line,
    Poly,
    Ring,
    Group,
}

public sealed class SkinEdgeBand
{
    public double Fraction, Opacity;
}

public sealed class SkinEdgeDoc
{
    public string Color; // #rrggbb
    public bool Inside = true; // placement 'inside' default
    public List<SkinEdgeBand> Bands = new List<SkinEdgeBand>();
}

public sealed class SkinFrame
{
    public SkinFrameKind Kind;
    // image
    public string Src;
    // common static geometry (leaf kinds)
    public double X, Y, W, H, Rx, Cx, Cy, R, Start, End, X1, Y1, X2, Y2, Width, Assemble, Spin, StrokeWidth;
    public bool HasStrokeWidth;
    public List<double[]> PolyPoints = new List<double[]>(); // static frame points {x, y}
    public bool Closed;
    public string Fill, Stroke; // authored colors
    public double[] Dash; // static dash array (stroke)
    public double DashOffset; // static dashOffset
    public bool HasDashOffset;
    public FlatCap Cap;
    public bool HasCap;
    public FlatJoin Join;
    public bool HasJoin;
    public int Segments; // ring
    public SkinEdgeDoc Edge; // ring
    public double Opacity = double.NaN;
    public bool IsAdd; // paint.blend 'add'
    // group
    public SkinTransformDoc Transform; // static numbers
    public SkinClipDoc Clip; // static numbers
    public bool Isolate;
    public List<SkinFrame> Children = new List<SkinFrame>();
}

// ----------------------------------------------------------------- animations & slots

public sealed class SkinLayerDoc
{
    public string Frame; // source { frame }
    public string Animation; // source { animation }
    public SkinTime SampleFrom = new SkinTime(double.NaN), SampleTo = new SkinTime(double.NaN);
    public bool HasSampleFrom, HasSampleTo;
    public double At = double.NaN, Until = double.NaN;
    public SkinPropertiesDoc Properties;
    public bool IsAdd;
    public List<SkinLayerDoc> Layers = new List<SkinLayerDoc>();
    public SkinGateDoc When;
    public SkinLayerDoc Otherwise;
}

public sealed class SkinAnimationDoc
{
    public bool IsSequence;
    public double? Fps;
    public List<string> Frames = new List<string>(); // sequence
    public List<SkinLayerDoc> Layers = new List<SkinLayerDoc>(); // layers
    public Dictionary<string, SkinTrackDoc> Tracks = new Dictionary<string, SkinTrackDoc>();
    public SkinValue RootOpacity;
    public SkinTransformDoc RootTransform;
    public bool Isolate;
}

public sealed class SkinSlotDoc
{
    public string Animation;

    /// <summary>Loop period override in seconds (loop slots only); null = the contract duration.
    /// Mirrors cytoid-design schema.ts Slot.period. 0 freezes the loop on its first frame.</summary>
    public double? Period;

    public string Note;
}
