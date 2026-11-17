// Skin compiler — turns a validated SkinDocument into the compiled object
// graph (SkinIr): folded expressions, tracks, gates, merged frame trees and
// the slot table with fallback inheritance, porting compile.ts field for
// field. PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class CompiledSkin
{
    public string Id, Name;

    /// <summary>The skin this one inherited incomplete kinds/feedback groups from
    /// (slot objects are reused, so its atlas must outlive this skin).</summary>
    internal CompiledSkin FallbackSkin;

    public Dictionary<string, CompiledSkinAnimation> Animations = new Dictionary<string, CompiledSkinAnimation>();
    public Dictionary<string, CompiledSkinSlot> Slots = new Dictionary<string, CompiledSkinSlot>();
    public Dictionary<NoteKind, bool> KindComplete = new Dictionary<NoteKind, bool>();
    public Dictionary<(NoteKind Kind, SkinFeedbackView View), bool> FeedbackComplete =
        new Dictionary<(NoteKind, SkinFeedbackView), bool>();
    public List<string> ColorRefs = new List<string>();
    private readonly Dictionary<string, int> colorRefIndex = new Dictionary<string, int>();
    private readonly List<Rgba> colorRefValues = new List<Rgba>();

    public Rgba RefValue(int index) => colorRefValues[index];

    public int RefOf(string authored)
    {
        if (colorRefIndex.TryGetValue(authored, out var index)) return index;
        index = ColorRefs.Count;
        colorRefIndex[authored] = index;
        ColorRefs.Add(authored);
        colorRefValues.Add(SkinColorMath.ParseHexRgba(authored));
        return index;
    }

    public CompiledSkinSlot Slot(NoteKind kind, string path) =>
        Slots.TryGetValue($"{SkinTimingTokens.KindKey(kind)}:{path}", out var slot) ? slot : null;

    /// <summary>Feedback clip of one note kind (compile.ts skin[kind].feedback).</summary>
    public CompiledSkinSlot Feedback(NoteKind kind, SkinFeedbackView view, SkinGrade grade, SkinTiming? timing)
    {
        var key = SkinTimingTokens.FeedbackSlotKey(kind, view, grade, timing);
        return Slots.TryGetValue(key, out var slot) ? slot : null;
    }

    /// <summary>Every image referenced by this skin's compiled frames, keyed by the
    /// authored src. Bytes are filled from the compile-time provider; the atlas
    /// page and UV rect are assigned later on the main thread (SkinImage.cs).</summary>
    public readonly Dictionary<string, SkinImage> Images = new Dictionary<string, SkinImage>();

    /// <summary>The compiled image for an authored src (null when absent).</summary>
    public SkinImage ImageOf(string src) =>
        src != null && Images.TryGetValue(src, out var image) ? image : null;

}

/// <summary>tokens.ts + skin-flat palette defaults — the flat look's constants.
/// The game derives its runtime palettes from game config instead; the probe and
/// the editor tests use these for the same values (flat's authored meta palette
/// equalled them, and the meta section is no longer read).</summary>
public static class DefaultSkinPalettes
{
    public static readonly Dictionary<SkinGrade, string> GradeColors = new Dictionary<SkinGrade, string>
    {
        [SkinGrade.Perfect] = "#5BC0EB",
        [SkinGrade.Great] = "#FDE74C",
        [SkinGrade.Good] = "#9BC53D",
        [SkinGrade.Bad] = "#E55934",
        [SkinGrade.Miss] = "#6B6F7A",
    };

    public static double Hue(FlatFamily family, FlatDirection direction)
    {
        var up = direction == FlatDirection.Up;
        switch (family)
        {
            case FlatFamily.LongHold:
                return 88;
            case FlatFamily.Drag:
            case FlatFamily.DropDrag:
                return 160;
            default:
                return up ? 247 : 20;
        }
    }

    public static SkinNotePalette Family(FlatFamily family, FlatDirection direction, string ring = null) =>
        SkinPalette.FromHue(Hue(family, direction), new SkinPalette.Options { Ring = ring });
}

public static class SkinCompiler
{
    /// <summary>Feedback modes compiled per note kind (compile.ts FeedbackSkin).</summary>
    internal static readonly SkinFeedbackView[] FeedbackViews =
    {
        SkinFeedbackView.Judgement,
        SkinFeedbackView.Timing,
        SkinFeedbackView.Combined,
    };

    // ------------------------------------------------------------- expressions

    private static SkinExpr Expr(SkinValue doc, CompileContext context)
    {
        switch (doc)
        {
            case SkinConstValue constant:
                return new SkinConstExpr(constant.Value);
            case SkinInputValue input:
                return new SkinInputExpr(input.Input);
            case SkinTrackValue track:
                return new SkinTrackExpr(context.TrackIndexOf(track.Name));
            case SkinLerpValue lerp:
            {
                var from = Expr(lerp.From, context);
                var to = Expr(lerp.To, context);
                var progress = Expr(lerp.Progress, context);
                if (from is SkinConstExpr a && to is SkinConstExpr b && progress is SkinConstExpr t)
                    return new SkinConstExpr(a.Value + (b.Value - a.Value) * t.Value);
                return new SkinLerpExpr(from, to, progress);
            }
            case SkinAddValue add:
            {
                var terms = add.Terms.Select(term => Expr(term, context)).ToArray();
                if (terms.All(term => term is SkinConstExpr))
                    return new SkinConstExpr(terms.Sum(term => ((SkinConstExpr) term).Value));
                return new SkinAddExpr(terms);
            }
            case SkinMultiplyValue multiply:
            {
                var terms = multiply.Terms.Select(term => Expr(term, context)).ToArray();
                if (terms.All(term => term is SkinConstExpr))
                {
                    var product = 1.0;
                    foreach (var term in terms) product *= ((SkinConstExpr) term).Value;
                    return new SkinConstExpr(product);
                }
                return new SkinMultiplyExpr(terms);
            }
            case SkinClampValue clamp:
            {
                var inner = Expr(clamp.Value, context);
                if (inner is SkinConstExpr v)
                    return new SkinConstExpr(Math.Min(clamp.Max, Math.Max(clamp.Min, v.Value)));
                return new SkinClampExpr(inner, clamp.Min, clamp.Max);
            }
            default:
                throw new SkinCompileException("invalid value expression");
        }
    }

    private sealed class CompileContext
    {
        public readonly Dictionary<string, int> TrackIndex;

        public CompileContext(Dictionary<string, int> trackIndex)
        {
            TrackIndex = trackIndex;
        }

        public int TrackIndexOf(string name)
        {
            if (!TrackIndex.TryGetValue(name, out var index))
                throw new SkinCompileException($"unknown track \"{name}\"");
            return index;
        }
    }

    // ------------------------------------------------------------- ease

    private static SkinCompiledEase Ease(SkinEaseSpec spec, SkinTimeSpec? windowFrom = null, SkinTimeSpec? windowTo = null)
    {
        Func<double, double> fn = spec.Type switch
        {
            "linear" => FlatEase.Linear,
            "inQuad" => FlatEase.InQuad,
            "outQuad" => FlatEase.OutQuad,
            "inSine" => FlatEase.InSine,
            "outSine" => FlatEase.OutSine,
            "inOutSine" => FlatEase.InOutSine,
            "inOutQuad" => FlatEase.InOutQuad,
            "inCubic" => FlatEase.InCubic,
            "outCubic" => FlatEase.OutCubic,
            "inOutCubic" => FlatEase.InOutCubic,
            "outQuart" => FlatEase.OutQuart,
            "outQuint" => FlatEase.OutQuint,
            "inExpo" => FlatEase.InExpo,
            "outExpo" => FlatEase.OutExpo,
            "step" => t => t < 1 ? 0 : 1,
            "outBack" => t => 1 + (spec.Amount + 1) * Math.Pow(t - 1, 3) + spec.Amount * Math.Pow(t - 1, 2),
            "inPow" => t => Math.Pow(t, spec.Power),
            "outPow" => t => 1 - Math.Pow(1 - t, spec.Power),
            "bezier" => FlatEase.CubicBezier(spec.X1, spec.Y1, spec.X2, spec.Y2),
            "compose" => null,
            _ => throw new SkinCompileException($"invalid ease \"{spec.Type}\""),
        };
        if (spec.Type == "compose")
        {
            var outer = Ease(spec.Outer).Fn;
            var inner = Ease(spec.Inner).Fn;
            fn = t => outer(inner(t));
        }
        if (windowFrom.HasValue && windowTo.HasValue)
            return new SkinCompiledEase(fn, windowFrom.Value, windowTo.Value);
        return new SkinCompiledEase(fn);
    }

    // ------------------------------------------------------------- tracks

    private static SkinCompiledTrack Track(SkinTrackDoc doc, CompileContext context, CompiledSkin skin)
    {
        var track = new SkinCompiledTrack
        {
            Form = doc.Form,
            IsColor = doc.IsColor,
            Driver = doc.Driver != null ? Expr(doc.Driver, context) : null,
        };
        if (doc.Form == SkinTrackForm.Keys)
        {
            var count = doc.Keys.Count;
            track.KeyTimes = new SkinTimeSpec[count];
            track.KeyEases = new SkinCompiledEase[count];
            if (doc.IsColor)
            {
                track.KeyColors = doc.Keys.Select(key => ColorBinding(key.Color, skin)).ToArray();
            }
            else
            {
                track.KeyValues = doc.Keys.Select(key => key.Value).ToArray();
            }
            var dynamic = false;
            for (var i = 0; i < count; i++)
            {
                var key = doc.Keys[i];
                track.KeyTimes[i] = SkinTimeSpec.From(key.At);
                if (!key.At.IsNumber) dynamic = true;
                if (i > 0 && key.Ease != null)
                {
                    var from = key.HasWindow ? (SkinTimeSpec?) SkinTimeSpec.From(key.WindowFrom) : null;
                    var to = key.HasWindow ? (SkinTimeSpec?) SkinTimeSpec.From(key.WindowTo) : null;
                    if (from.HasValue && to.HasValue)
                    {
                        if (!key.WindowFrom.IsNumber || !key.WindowTo.IsNumber) dynamic = true;
                        track.KeyEases[i] = Ease(key.Ease, from, to);
                    }
                    else
                    {
                        track.KeyEases[i] = Ease(key.Ease);
                    }
                }
            }
            track.DynamicTimes = dynamic;
            track.HasRate = doc.HasRate || doc.HasOffset;
            if (doc.HasRate) track.Rate = doc.Rate;
            if (doc.HasOffset) track.Offset = SkinTimeSpec.From(doc.Offset);
        }
        else if (doc.Form == SkinTrackForm.Ease)
        {
            track.Ease = Ease(doc.Ease);
        }
        else if (doc.Form == SkinTrackForm.Bump)
        {
            track.Low = double.IsNaN(doc.Low) ? 0 : doc.Low;
            track.High = double.IsNaN(doc.High) ? 1 : doc.High;
            track.Windows = doc.Windows
                .Select(window => (SkinTimeSpec.From(window.Item1), SkinTimeSpec.From(window.Item2)))
                .ToArray();
            track.DynamicTimes = doc.Windows.Any(window =>
                !window.Item1.IsNumber || !window.Item2.IsNumber);
        }
        else if (doc.Form == SkinTrackForm.Damped)
        {
            track.Low = double.IsNaN(doc.Low) ? 0 : doc.Low;
            track.High = double.IsNaN(doc.High) ? 1 : doc.High;
            track.DampedFrom = SkinTimeSpec.From(doc.From);
            track.DampedTo = SkinTimeSpec.From(doc.To);
            track.Damping = doc.Damping;
            track.DynamicTimes = !doc.From.IsNumber || !doc.To.IsNumber;
        }
        else if (doc.Form == SkinTrackForm.Fade)
        {
            track.Drop = doc.Drop;
            track.Fall = doc.Fall;
            track.Power = doc.Power;
        }
        return track;
    }

    // ------------------------------------------------------------- colors

    private static SkinColorBinding ColorBinding(string text, CompiledSkin skin)
    {
        if (text.StartsWith("#"))
            return new SkinColorBinding(SkinColorKind.Hex, skin.RefOf(text), 0, 0, 0, -1);
        var split = text.Split(':');
        var type = split[0];
        var shade = int.Parse(split[1]);
        if (type == "ring")
            return new SkinColorBinding(SkinColorKind.Ring, -1, shade, 0, 0, -1);
        if (type == "theme")
            return new SkinColorBinding(SkinColorKind.Theme, -1, shade, 0, 0, -1);
        foreach (var grade in SkinTimingTokens.Grades)
        {
            if (type != SkinTimingTokens.GradeKey(grade)) continue;
            return new SkinColorBinding(SkinColorKind.Grade, -1, shade, grade, 0, -1);
        }
        return new SkinColorBinding(SkinColorKind.Family, -1, shade, 0, SkinJson.FamilyOf(type), -1);
    }

    private static SkinColorBinding? ColorBindingOf(string authored, string trackName, CompileContext context, CompiledSkin skin)
    {
        if (authored != null) return ColorBinding(authored, skin);
        if (trackName != null)
            return new SkinColorBinding(SkinColorKind.Track, -1, 0, 0, 0, context.TrackIndexOf(trackName));
        return null;
    }

    // ------------------------------------------------------------- transforms / clips

    private static SkinExpr OptionalExpr(SkinValue doc, CompileContext context) =>
        doc != null ? Expr(doc, context) : null;

    private static SkinTransformExpr Transform(SkinTransformDoc doc, CompileContext context) =>
        doc == null
            ? null
            : new SkinTransformExpr(
                OptionalExpr(doc.X, context), OptionalExpr(doc.Y, context), OptionalExpr(doc.Rotate, context),
                OptionalExpr(doc.Scale, context), OptionalExpr(doc.ScaleX, context), OptionalExpr(doc.ScaleY, context));

    private static SkinTransformExpr StaticTransform(SkinTransformDoc doc)
    {
        if (doc == null) return null;
        SkinExpr Field(SkinValue value) => value is SkinConstValue constant ? new SkinConstExpr(constant.Value) : null;
        return new SkinTransformExpr(
            Field(doc.X), Field(doc.Y), Field(doc.Rotate),
            Field(doc.Scale), Field(doc.ScaleX), Field(doc.ScaleY));
    }

    private static SkinClipExpr Clip(SkinClipDoc doc, CompileContext context)
    {
        if (doc == null) return null;
        return new SkinClipExpr
        {
            IsCircle = doc.IsCircle,
            Cx = Field(doc.Cx, context), Cy = Field(doc.Cy, context), R = Field(doc.R, context),
            X = Field(doc.X, context), Y = Field(doc.Y, context),
            W = Field(doc.W, context), H = Field(doc.H, context), Rx = Field(doc.Rx, context),
        };
    }

    private static SkinField Field(SkinValue doc, CompileContext context) =>
        doc == null ? SkinField.Absent
            : doc is SkinConstValue constant ? new SkinField(constant.Value)
            : new SkinField(Expr(doc, context));

    private static (SkinField Field, bool Present) OptionalField(double frameValue, SkinValue property, CompileContext context)
    {
        if (property != null) return (Field(property, context), true);
        return double.IsNaN(frameValue) ? (SkinField.Absent, false) : (new SkinField(frameValue), true);
    }

    // ------------------------------------------------------------- merged frames

    private static CompiledFrameNode MergedFrame(SkinFrame frame, SkinPropertiesDoc properties, CompileContext context, CompiledSkin skin)
    {
        var node = new CompiledFrameNode { Kind = frame.Kind };
        if (properties != null)
        {
            node.Opacity = properties.Opacity != null ? new SkinField(Expr(properties.Opacity, context)) : new SkinField(0);
            node.HasOpacity = properties.Opacity != null;
        }
        if (!double.IsNaN(frame.Opacity))
        {
            if (properties?.Opacity == null)
            {
                node.Opacity = new SkinField(frame.Opacity);
                node.HasOpacity = true;
            }
        }
        node.IsAdd = frame.IsAdd;

        if (frame.Kind == SkinFrameKind.Image)
        {
            // Rect rides the shared x/y/w/h fields (w/h validated nonneg by the
            // parser); opacity/isAdd merged above. The src resolves through
            // skin.Images at draw time.
            node.Src = frame.Src;
            node.X = MergeField(new SkinField(frame.X), properties?.X, context);
            node.Y = MergeField(new SkinField(frame.Y), properties?.Y, context);
            node.W = MergeField(new SkinField(frame.W), properties?.W, context);
            node.H = MergeField(new SkinField(frame.H), properties?.H, context);
            return node;
        }

        switch (frame.Kind)
        {
            case SkinFrameKind.Circle:
                node.R = MergeField(new SkinField(frame.R), properties?.R, context);
                (node.Cx, node.CxPresent) = OptionalField(frame.Cx, properties?.Cx, context);
                (node.Cy, node.CyPresent) = OptionalField(frame.Cy, properties?.Cy, context);
                node.Fill = ColorBindingOf(null, null, context, skin);
                node.Stroke = ColorBindingOf(null, null, context, skin);
                ApplyStrokeFill(node, frame, properties, context, skin);
                return node;
            case SkinFrameKind.Arc:
                node.R = MergeField(new SkinField(frame.R), properties?.R, context);
                node.Start = MergeField(new SkinField(frame.Start), properties?.Start, context);
                node.End = MergeField(new SkinField(frame.End), properties?.End, context);
                (node.Cx, node.CxPresent) = OptionalField(frame.Cx, properties?.Cx, context);
                (node.Cy, node.CyPresent) = OptionalField(frame.Cy, properties?.Cy, context);
                ApplyStroke(node, frame, properties, context, skin);
                return node;
            case SkinFrameKind.Rect:
                node.X = MergeField(new SkinField(frame.X), properties?.X, context);
                node.Y = MergeField(new SkinField(frame.Y), properties?.Y, context);
                node.W = MergeField(new SkinField(frame.W), properties?.W, context);
                node.H = MergeField(new SkinField(frame.H), properties?.H, context);
                (node.Rx, node.RxPresent) = OptionalField(frame.Rx, properties?.Rx, context);
                ApplyStrokeFill(node, frame, properties, context, skin);
                node.HasJoin = frame.HasJoin;
                node.Join = frame.Join;
                node.HasCap = frame.HasCap;
                node.Cap = frame.Cap;
                return node;
            case SkinFrameKind.Line:
                node.X1 = MergeField(new SkinField(frame.X1), properties?.X1, context);
                node.Y1 = MergeField(new SkinField(frame.Y1), properties?.Y1, context);
                node.X2 = MergeField(new SkinField(frame.X2), properties?.X2, context);
                node.Y2 = MergeField(new SkinField(frame.Y2), properties?.Y2, context);
                ApplyStroke(node, frame, properties, context, skin);
                node.HasCap = frame.HasCap;
                node.Cap = frame.Cap;
                node.HasJoin = frame.HasJoin;
                node.Join = frame.Join;
                return node;
            case SkinFrameKind.Poly:
            {
                var points = properties?.Points;
                if (points != null)
                {
                    node.PointCount = points.Count;
                    node.PointX = points.Select(point => Field(point.X, context)).ToArray();
                    node.PointY = points.Select(point => Field(point.Y, context)).ToArray();
                }
                else
                {
                    node.PointCount = frame.PolyPoints.Count;
                    node.PointX = frame.PolyPoints.Select(point => new SkinField(point[0])).ToArray();
                    node.PointY = frame.PolyPoints.Select(point => new SkinField(point[1])).ToArray();
                }
                node.Closed = frame.Closed;
                ApplyStrokeFill(node, frame, properties, context, skin);
                node.HasJoin = frame.HasJoin;
                node.Join = frame.Join;
                node.HasCap = frame.HasCap;
                node.Cap = frame.Cap;
                return node;
            }
            case SkinFrameKind.Ring:
                node.R = MergeField(new SkinField(frame.R), properties?.R, context);
                node.Width = MergeField(new SkinField(frame.Width), properties?.Width, context);
                node.Assemble = MergeField(new SkinField(frame.Assemble), properties?.Assemble, context);
                node.Spin = MergeField(SkinField.Absent, properties?.Spin, context);
                node.Segments = frame.Segments;
                node.Edge = frame.Edge;
                if (frame.Edge != null) node.EdgeRgba = SkinColorMath.ParseHexRgb(frame.Edge.Color);
                node.Stroke = ColorBindingOf(frame.Stroke, properties?.Stroke ?? properties?.StrokeTrack, context, skin);
                node.HasStrokeWidth = frame.HasStrokeWidth || properties?.StrokeWidth != null;
                node.StrokeWidth = MergeOptional(frame.HasStrokeWidth ? new SkinField(frame.StrokeWidth) : SkinField.Absent,
                    properties?.StrokeWidth, context, 1);
                return node;
            case SkinFrameKind.Group:
            {
                node.Children = new List<CompiledFrameNode>(frame.Children.Count);
                foreach (var child in frame.Children)
                    node.Children.Add(MergedFrame(child, null, context, skin));
                // Group properties merge: transform/clip/opacity replace the frame's own.
                node.Transform = properties?.Transform != null
                    ? Transform(properties.Transform, context)
                    : StaticTransform(frame.Transform);
                node.Clip = properties?.Clip != null ? Clip(properties.Clip, context) : Clip(frame.Clip, context);
                node.Isolate = frame.Isolate;
                return node;
            }
            default:
                throw new SkinCompileException($"unsupported frame kind {frame.Kind}");
        }
    }

    private static SkinField MergeField(SkinField frameField, SkinValue property, CompileContext context) =>
        property != null ? Field(property, context) : frameField;

    private static SkinField MergeOptional(SkinField frameField, SkinValue property, CompileContext context, double unused) =>
        MergeField(frameField, property, context);

    private static void ApplyStroke(CompiledFrameNode node, SkinFrame frame, SkinPropertiesDoc properties,
        CompileContext context, CompiledSkin skin)
    {
        node.Stroke = ColorBindingOf(frame.Stroke, properties?.Stroke ?? properties?.StrokeTrack, context, skin);
        node.HasStrokeWidth = frame.HasStrokeWidth || properties?.StrokeWidth != null;
        node.StrokeWidth = MergeOptional(frame.HasStrokeWidth ? new SkinField(frame.StrokeWidth) : SkinField.Absent,
            properties?.StrokeWidth, context, 1);
        ApplyDash(node, frame, properties, context);
        node.HasCap = frame.HasCap;
        node.Cap = frame.Cap;
        node.HasJoin = frame.HasJoin;
        node.Join = frame.Join;
    }

    private static void ApplyStrokeFill(CompiledFrameNode node, SkinFrame frame, SkinPropertiesDoc properties,
        CompileContext context, CompiledSkin skin)
    {
        ApplyStroke(node, frame, properties, context, skin);
        node.Fill = ColorBindingOf(frame.Fill, properties?.Fill ?? properties?.FillTrack, context, skin);
    }

    private static void ApplyDash(CompiledFrameNode node, SkinFrame frame, SkinPropertiesDoc properties, CompileContext context)
    {
        var dash = properties?.Dash;
        if (dash != null)
        {
            node.DashExpr = dash.Select(value => Field(value, context)).ToArray();
        }
        else if (frame.Dash != null)
        {
            node.StaticDash = frame.Dash;
        }
        if (properties?.DashOffset != null)
        {
            node.DashOffset = Field(properties.DashOffset, context);
            node.HasDashOffset = true;
        }
        else if (frame.HasDashOffset)
        {
            node.DashOffset = new SkinField(frame.DashOffset);
            node.HasDashOffset = true;
        }
    }

    // ------------------------------------------------------------- layers

    private sealed class AnimationCompileScope
    {
        public readonly Dictionary<string, int> TrackIndex = new Dictionary<string, int>();
    }

    private static CompiledLayer Layer(SkinLayerDoc doc, AnimationCompileScope scope, CompiledSkin skin, SkinDocument document)
    {
        var context = new CompileContext(scope.TrackIndex);
        var layer = new CompiledLayer
        {
            Windowed = !double.IsNaN(doc.At) || !double.IsNaN(doc.Until),
            At = double.IsNaN(doc.At) ? 0 : doc.At,
            Until = double.IsNaN(doc.Until) ? 1 : doc.Until,
            Add = doc.IsAdd,
        };
        if (doc.When != null)
        {
            layer.Gate = new SkinCompiledGate
            {
                TrackIndex = doc.When.Track != null ? context.TrackIndexOf(doc.When.Track) : -1,
                Phase = doc.When.Phase,
                HasFrom = doc.When.HasFrom,
                From = SkinTimeSpec.From(doc.When.From),
                HasUntil = doc.When.HasUntil,
                Until = SkinTimeSpec.From(doc.When.Until),
                FromInclusive = doc.When.FromInclusive,
                UntilInclusive = doc.When.UntilInclusive,
            };
        }
        if (doc.Frame != null)
        {
            var frame = document.Frames[doc.Frame];
            var properties = doc.Properties;
            layer.WrapTransform = frame.Kind != SkinFrameKind.Group && properties?.Transform != null;
            layer.WrapperTransform = layer.WrapTransform ? Transform(properties.Transform, context) : null;
            // The merged primitive omits the wrapped transform (it rides the wrapper group).
            layer.Frame = MergedFrame(frame, layer.WrapTransform ? WithoutTransform(properties) : properties, context, skin);
            layer.Children = doc.Layers.Count > 0 ? doc.Layers.Select(child => Layer(child, scope, skin, document)).ToArray() : null;
        }
        else
        {
            layer.AnimReference = doc.Animation;
            layer.HasSample = doc.HasSampleFrom || doc.HasSampleTo;
            layer.SampleFrom = doc.HasSampleFrom ? TimeExpr(doc.SampleFrom, context) : new SkinConstExpr(0);
            layer.SampleTo = doc.HasSampleTo ? TimeExpr(doc.SampleTo, context) : new SkinConstExpr(1);
            if (doc.Properties != null)
            {
                layer.AnimOpacity = doc.Properties.Opacity != null ? Expr(doc.Properties.Opacity, context) : null;
                layer.AnimTransform = doc.Properties.Transform != null ? Transform(doc.Properties.Transform, context) : null;
            }
            layer.Otherwise = doc.Otherwise != null ? Layer(doc.Otherwise, scope, skin, document) : null;
            if (doc.Layers.Count > 0)
                throw new SkinCompileException("animation references cannot append child layers");
        }
        if (doc.Otherwise != null && doc.Frame != null)
            layer.Otherwise = Layer(doc.Otherwise, scope, skin, document);
        return layer;
    }

    private static SkinExpr TimeExpr(SkinTime time, CompileContext context) =>
        time.IsNumber ? new SkinConstExpr(time.Value) : (SkinExpr) new SkinInputExpr(time.Input.Value);

    private static SkinPropertiesDoc WithoutTransform(SkinPropertiesDoc properties)
    {
        if (properties == null) return null;
        var copy = new SkinPropertiesDoc
        {
            Opacity = properties.Opacity,
            Cx = properties.Cx, Cy = properties.Cy, R = properties.R,
            Start = properties.Start, End = properties.End,
            X = properties.X, Y = properties.Y, W = properties.W, H = properties.H, Rx = properties.Rx,
            X1 = properties.X1, Y1 = properties.Y1, X2 = properties.X2, Y2 = properties.Y2,
            Width = properties.Width, Assemble = properties.Assemble, Spin = properties.Spin,
            DashOffset = properties.DashOffset, StrokeWidth = properties.StrokeWidth,
            Fill = properties.Fill, Stroke = properties.Stroke,
            FillTrack = properties.FillTrack, StrokeTrack = properties.StrokeTrack,
            Points = properties.Points, Dash = properties.Dash, Clip = properties.Clip,
        };
        return copy; // Transform intentionally dropped
    }

    // ------------------------------------------------------------- animations

    public static CompiledSkin Compile(SkinDocument document, CompiledSkin fallback = null,
        Dictionary<string, byte[]> imageBytes = null)
    {
        var validation = SkinValidator.Validate(document);
        var skin = new CompiledSkin
        {
            Id = document.Id,
            Name = document.Name,
            FallbackSkin = fallback,
        };
        var scopes = new Dictionary<string, AnimationCompileScope>();
        foreach (var pair in document.Animations)
        {
            var scope = new AnimationCompileScope();
            var index = 0;
            foreach (var trackName in pair.Value.Tracks.Keys)
            {
                scope.TrackIndex[trackName] = index++;
            }
            scopes[pair.Key] = scope;
        }
        foreach (var pair in document.Animations)
        {
            var id = pair.Key;
            var animation = pair.Value;
            var scope = scopes[id];
            var context = new CompileContext(scope.TrackIndex);
            var compiled = new CompiledSkinAnimation
            {
                Id = id,
                Owner = skin,
                IsSequence = animation.IsSequence,
                Fps = animation.Fps,
                TrackNames = animation.Tracks.Keys.ToArray(),
                TrackIndex = new Dictionary<string, int>(scope.TrackIndex),
                RootOpacity = animation.RootOpacity != null ? Expr(animation.RootOpacity, context) : null,
                RootTransform = Transform(animation.RootTransform, context),
                Isolate = animation.Isolate,
            };
            compiled.Tracks = new SkinCompiledTrack[compiled.TrackNames.Length];
            for (var i = 0; i < compiled.TrackNames.Length; i++)
                compiled.Tracks[i] = Track(animation.Tracks[compiled.TrackNames[i]], context, skin);
            if (animation.IsSequence)
            {
                compiled.SequenceFrames = animation.Frames
                    .Select(name => MergedFrame(document.Frames[name], null, context, skin))
                    .ToArray();
                compiled.HasSequence = true;
            }
            else
            {
                compiled.Layers = animation.Layers.Select(layer => Layer(layer, scope, skin, document)).ToArray();
            }
            skin.Animations[id] = compiled;
        }
        // Resolve animation references + capability flags now that all animations exist.
        foreach (var compiled in skin.Animations.Values)
            FinalizeAnimation(compiled, skin, document);
        foreach (var compiled in skin.Animations.Values)
            compiled.HasSequence = compiled.HasSequence || DetectSequence(compiled, skin, new HashSet<string>());

        CollectImages(skin, imageBytes);
        BuildSlots(document, skin, fallback);
        return skin;
    }

    /// <summary>Walks every compiled animation (sequence frames + layer trees; animation
    /// references resolve to animations already in the table) and registers each
    /// referenced image src, attaching the provider's bytes when available.</summary>
    private static void CollectImages(CompiledSkin skin, Dictionary<string, byte[]> imageBytes)
    {
        foreach (var animation in skin.Animations.Values)
        {
            if (animation.SequenceFrames != null)
                foreach (var frame in animation.SequenceFrames)
                    CollectFrameImages(frame, skin, imageBytes);
            if (animation.Layers == null) continue;
            foreach (var layer in animation.Layers)
                CollectLayerImages(layer, skin, imageBytes);
        }
    }

    private static void CollectLayerImages(CompiledLayer layer, CompiledSkin skin, Dictionary<string, byte[]> imageBytes)
    {
        if (layer.Frame != null) CollectFrameImages(layer.Frame, skin, imageBytes);
        if (layer.Children != null)
            foreach (var child in layer.Children)
                CollectLayerImages(child, skin, imageBytes);
        if (layer.Otherwise != null) CollectLayerImages(layer.Otherwise, skin, imageBytes);
    }

    private static void CollectFrameImages(CompiledFrameNode frame, CompiledSkin skin, Dictionary<string, byte[]> imageBytes)
    {
        if (frame == null) return;
        if (frame.Kind == SkinFrameKind.Image && !skin.Images.ContainsKey(frame.Src))
        {
            byte[] bytes = null;
            imageBytes?.TryGetValue(frame.Src, out bytes);
            skin.Images[frame.Src] = new SkinImage { Src = frame.Src, Bytes = bytes };
        }
        if (frame.Children == null) return;
        foreach (var child in frame.Children)
            CollectFrameImages(child, skin, imageBytes);
    }

    private static void FinalizeAnimation(CompiledSkinAnimation animation, CompiledSkin skin, SkinDocument document)
    {
        if (animation.SequenceFrames != null)
        {
            var flags = new CompiledLayer();
            foreach (var frame in animation.SequenceFrames) MarkFrameFlags(frame, flags);
            animation.UsesImages |= flags.UsesImages;
            animation.UsesClip |= flags.UsesClip;
            animation.UsesCurvedDash |= flags.UsesCurvedDash;
        }
        if (animation.Layers == null) return;
        foreach (var layer in animation.Layers)
        {
            FinalizeLayer(layer, skin, document);
            animation.UsesImages |= layer.UsesImages;
            animation.UsesClip |= layer.UsesClip;
            animation.UsesCurvedDash |= layer.UsesCurvedDash;
        }
    }

    private static void FinalizeLayer(CompiledLayer layer, CompiledSkin skin, SkinDocument document)
    {
        if (layer.AnimReference != null)
        {
            if (!skin.Animations.TryGetValue(layer.AnimReference, out var source))
                throw new SkinCompileException($"unknown animation \"{layer.AnimReference}\"");
            layer.ResolvedAnimation = source;
            // Sequences with fps ret ime to their natural duration; everything else
            // inherits the parent clock's duration.
            layer.SampleDuration = source.IsSequence && source.Fps.HasValue
                ? source.SequenceFrames.Length / source.Fps.Value
                : -1;
            layer.UsesImages |= source.UsesImages;
            layer.UsesClip |= source.UsesClip;
            layer.UsesCurvedDash |= source.UsesCurvedDash;
        }
        MarkFrameFlags(layer.Frame, layer);
        if (layer.Children != null)
            foreach (var child in layer.Children)
            {
                FinalizeLayer(child, skin, document);
                layer.UsesImages |= child.UsesImages;
                layer.UsesClip |= child.UsesClip;
                layer.UsesCurvedDash |= child.UsesCurvedDash;
            }
        if (layer.Otherwise != null)
        {
            FinalizeLayer(layer.Otherwise, skin, document);
            layer.UsesImages |= layer.Otherwise.UsesImages;
            layer.UsesClip |= layer.Otherwise.UsesClip;
            layer.UsesCurvedDash |= layer.Otherwise.UsesCurvedDash;
        }
    }

    private static void MarkFrameFlags(CompiledFrameNode frame, CompiledLayer flags)
    {
        if (frame == null) return;
        if (frame.Kind == SkinFrameKind.Image) flags.UsesImages = true;
        if (frame.Kind == SkinFrameKind.Group)
        {
            if (frame.Clip != null) flags.UsesClip = true;
            foreach (var child in frame.Children) MarkFrameFlags(child, flags);
        }
        var hasDash = frame.StaticDash != null || frame.DashExpr != null;
        if (hasDash && (frame.Kind == SkinFrameKind.Arc || frame.Kind == SkinFrameKind.Poly || frame.Kind == SkinFrameKind.Ring))
            flags.UsesCurvedDash = true;
    }

    private static bool DetectSequence(CompiledSkinAnimation animation, CompiledSkin skin, HashSet<string> visiting)
    {
        visiting.Add(animation.Id);
        var result = animation.IsSequence;
        if (animation.Layers != null)
        {
            foreach (var layer in animation.Layers)
            {
                if (VisitLayer(layer)) result = true;
                if (result) break;
            }
        }
        visiting.Remove(animation.Id);
        return result;

        bool VisitLayer(CompiledLayer layer)
        {
            if (layer.ResolvedAnimation != null && !visiting.Contains(layer.ResolvedAnimation.Id) &&
                DetectSequence(layer.ResolvedAnimation, skin, visiting))
                return true;
            if (layer.Children != null)
            {
                foreach (var child in layer.Children)
                {
                    if (VisitLayer(child)) return true;
                }
            }
            return layer.Otherwise != null && VisitLayer(layer.Otherwise);
        }
    }

    // ------------------------------------------------------------- slots

    private static void BuildSlots(SkinDocument document, CompiledSkin skin, CompiledSkin fallback)
    {
        foreach (var pair in document.Slots)
        {
            var contract = SkinSlots.Contract(pair.Key);
            if (pair.Value.Period.HasValue && contract.Mode != SkinClipMode.Loop)
                throw new SkinCompileException($"{pair.Key}: period is only valid on loop slots");
            var animation = skin.Animations[pair.Value.Animation];
            var path = contract.Path;
            var grade = path.StartsWith("clear:")
                ? SkinJson.GradeOf(path.Substring(6))
                : path == "miss" ? (SkinGrade?) SkinGrade.Miss : contract.Grade;
            var slot = new CompiledSkinSlot
            {
                Key = pair.Key,
                Contract = contract,
                Animation = animation,
                Grade = grade,
                Period = pair.Value.Period ?? contract.Duration,
                Note = pair.Value.Note,
            };
            skin.Slots[pair.Key] = slot;
        }

        // Kind completeness (compile.ts required slots); incomplete kinds inherit the
        // entire group from the fallback.
        foreach (var kind in SkinTimingTokens.NoteKinds)
        {
            var required = new List<string> { "enter", "clear:perfect", "clear:great", "clear:good", "clear:bad" };
            if (kind == NoteKind.Hold || kind == NoteKind.LongHold) required.Add("body");
            if (kind == NoteKind.DragHead || kind == NoteKind.ClickDragHead) required.Add("chainLink");
            var hasHold = new[] { "press", "loopUnder", "loopOver", "progress" }
                .Any(path => document.Slots.ContainsKey($"{SkinTimingTokens.KindKey(kind)}:{path}"));
            if (hasHold)
            {
                required.Add("press");
                required.Add("progress");
            }
            var missing = required.Where(path => !document.Slots.ContainsKey($"{SkinTimingTokens.KindKey(kind)}:{path}")).ToList();
            if (missing.Count > 0)
            {
                if (fallback == null)
                    throw new SkinCompileException(
                        $"{SkinTimingTokens.KindKey(kind)}: missing required slots: {string.Join(", ", missing)}");
                InheritKind(skin, fallback, kind);
                skin.KindComplete[kind] = fallback.KindComplete.TryGetValue(kind, out var kindComplete) && kindComplete;
                // An inherited design carries its feedback groups wholesale, exactly like
                // compile.ts assigning designs[kind] = fallback[kind].
                foreach (var view in SkinCompiler.FeedbackViews)
                    skin.FeedbackComplete[(kind, view)] =
                        fallback.FeedbackComplete.TryGetValue((kind, view), out var inheritedComplete) && inheritedComplete;
                continue;
            }
            skin.KindComplete[kind] = true;

            // Feedback groups inherit per (kind, view) — compile.ts feedbackOf(kind).
            foreach (var view in SkinCompiler.FeedbackViews)
            {
                var bindings = view == SkinFeedbackView.Judgement
                    ? SkinTimingTokens.Grades.Select(grade => (grade, (SkinTiming?) null))
                    : from grade in SkinTimingTokens.TimedGrades
                    from timing in SkinTimingTokens.Timings
                    select (grade, (SkinTiming?) timing);
                var feedbackMissing = new List<string>();
                foreach (var (grade, timing) in bindings)
                {
                    var key = SkinTimingTokens.FeedbackSlotKey(kind, view, grade, timing);
                    if (!document.Slots.ContainsKey(key)) feedbackMissing.Add(key);
                }
                if (feedbackMissing.Count > 0)
                {
                    if (fallback == null)
                        throw new SkinCompileException(
                            $"{SkinTimingTokens.KindKey(kind)}: feedback:{SkinTimingTokens.ViewKey(view)}: missing required slots: {string.Join(", ", feedbackMissing)}");
                    InheritFeedback(skin, fallback, kind, view);
                    skin.FeedbackComplete[(kind, view)] =
                        fallback.FeedbackComplete.TryGetValue((kind, view), out var feedbackComplete) && feedbackComplete;
                    continue;
                }
                skin.FeedbackComplete[(kind, view)] = true;
            }
        }

        foreach (var slot in skin.Slots.Values)
            // Clips are CPU-clipped at emission; only curved dashes are globally
            // unsupported. Image slots stay loadable: paths WITH an image sink
            // (clear/feedback FX) paint them, paths without one gate on the slot's
            // UsesImages at draw time (classic/imperative fallback). UsesImages
            // remains diagnostic here (see Tools/SkinProbe).
            slot.UsesUnsupported = slot.Animation.UsesCurvedDash;
    }

    private static void InheritKind(CompiledSkin skin, CompiledSkin fallback, NoteKind kind)
    {
        var prefix = $"{SkinTimingTokens.KindKey(kind)}:";
        foreach (var pair in fallback.Slots.Where(pair => pair.Key.StartsWith(prefix)).ToList())
            skin.Slots[pair.Key] = pair.Value;
    }

    private static void InheritFeedback(CompiledSkin skin, CompiledSkin fallback, NoteKind kind, SkinFeedbackView view)
    {
        var prefix = $"{SkinTimingTokens.KindKey(kind)}:feedback:{SkinTimingTokens.ViewKey(view)}:";
        foreach (var pair in fallback.Slots.Where(pair => pair.Key.StartsWith(prefix)).ToList())
            skin.Slots[pair.Key] = pair.Value;
    }
}
