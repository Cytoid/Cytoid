// Port of cytoid-design packages/notes/src/skin/validate.ts — validates every
// asset (including unused ones) before any draw is exposed, and computes each
// animation's required input set for the slot availability check.
// PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;
using System.Linq;

public static class SkinValidator
{
    private static readonly Dictionary<SkinFrameKind, string[]> PropertyNames = new Dictionary<SkinFrameKind, string[]>
    {
        [SkinFrameKind.Image] = new[] { "x", "y", "w", "h" },
        [SkinFrameKind.Circle] = new[] { "cx", "cy", "r", "stroke", "strokeWidth", "fill" },
        [SkinFrameKind.Arc] = new[] { "cx", "cy", "r", "start", "end", "stroke", "strokeWidth", "dash", "dashOffset" },
        [SkinFrameKind.Rect] = new[] { "x", "y", "w", "h", "rx", "stroke", "strokeWidth", "fill" },
        [SkinFrameKind.Line] = new[] { "x1", "y1", "x2", "y2", "stroke", "strokeWidth", "dash", "dashOffset" },
        [SkinFrameKind.Poly] = new[] { "points", "stroke", "strokeWidth", "fill" },
        [SkinFrameKind.Ring] = new[] { "r", "width", "assemble", "spin", "stroke", "strokeWidth", "dash", "dashOffset" },
        [SkinFrameKind.Group] = new[] { "clip" },
    };

    private static readonly HashSet<SkinInput> Common = new HashSet<SkinInput>
    {
        SkinInput.Unit, SkinInput.Size, SkinInput.Heading, SkinInput.R, SkinInput.W, SkinInput.Inner,
        SkinInput.RingC, SkinInput.InnerHalf, SkinInput.Hair, SkinInput.RingW,
    };

    private static readonly HashSet<SkinInput> HoldInputs = new HashSet<SkinInput>
    {
        SkinInput.BracketR, SkinInput.BracketW, SkinInput.HoldLanding, SkinInput.GlyphW,
        SkinInput.CoreR, SkinInput.ProgressR, SkinInput.ProgressW,
    };

    private static readonly HashSet<SkinInput> FlickInputs = new HashSet<SkinInput>
    {
        SkinInput.FlickW, SkinInput.Rv, SkinInput.FlickInner, SkinInput.CoreMax, SkinInput.FrameOuter,
        SkinInput.Near, SkinInput.NearFar, SkinInput.ApproachNear, SkinInput.ArrowSize, SkinInput.ArrowTip,
        SkinInput.ArrowHalf,
    };

    private static readonly HashSet<SkinInput> BodyInputs = new HashSet<SkinInput>
    {
        SkinInput.Appear, SkinInput.Progress, SkinInput.Elapsed, SkinInput.Remaining,
        SkinInput.Duration, SkinInput.BodyWidth, SkinInput.DashPeriod,
    };

    private static readonly HashSet<SkinInput> ChainInputs = new HashSet<SkinInput>
    {
        SkinInput.Length, SkinInput.Elapsed,
    };

    private const int MaxNodes = 20_000;

    public sealed class Result
    {
        public readonly Dictionary<string, HashSet<SkinInput>> Inputs = new Dictionary<string, HashSet<SkinInput>>();
    }

    /// <summary>Per-animation validation context (the TS visit closure).</summary>
    private sealed class AnimationCheck
    {
        public readonly SkinDocument Skin;
        public readonly string Id;
        public readonly SkinAnimationDoc Animation;
        public readonly Result Result;
        public readonly HashSet<SkinInput> Required = new HashSet<SkinInput>();

        public AnimationCheck(SkinDocument skin, string id, SkinAnimationDoc animation, Result result)
        {
            Skin = skin;
            Id = id;
            Animation = animation;
            Result = result;
        }

        // inspect(): collect {input} references, verify {track} references and clamp bounds.
        public void Inspect(object value)
        {
            switch (value)
            {
                case SkinInputValue input:
                    Required.Add(input.Input);
                    return;
                case SkinTrackValue track:
                    if (!Animation.Tracks.ContainsKey(track.Name))
                        throw new SkinRenderException($"{Id}: unknown track \"{track.Name}\"");
                    return;
                case SkinLerpValue lerp:
                    Inspect(lerp.From);
                    Inspect(lerp.To);
                    Inspect(lerp.Progress);
                    return;
                case SkinAddValue add:
                    foreach (var term in add.Terms) Inspect(term);
                    return;
                case SkinMultiplyValue multiply:
                    foreach (var term in multiply.Terms) Inspect(term);
                    return;
                case SkinClampValue clamp:
                    if (clamp.Min > clamp.Max)
                        throw new SkinRenderException($"{Id}: invalid clamp bounds");
                    Inspect(clamp.Value);
                    return;
                default:
                    InspectDocument(value, this);
                    return;
            }
        }

        // checkTypes(): {track} bindings must match the numeric/color expectation of
        // their context (fill/stroke keyed color; everything else numeric; gates
        // numeric unless `phase`).
        public void CheckTypes(SkinValue value, bool color)
        {
            switch (value)
            {
                case SkinTrackValue track:
                    if (Animation.Tracks.TryGetValue(track.Name, out var target) &&
                        target.IsColor != color)
                        throw new SkinRenderException(
                            $"{Id}: expected {(color ? "color" : "numeric")} track \"{track.Name}\"");
                    return;
                case SkinLerpValue lerp:
                    CheckTypes(lerp.From, color);
                    CheckTypes(lerp.To, color);
                    CheckTypes(lerp.Progress, color);
                    return;
                case SkinAddValue add:
                    foreach (var term in add.Terms) CheckTypes(term, color);
                    return;
                case SkinMultiplyValue multiply:
                    foreach (var term in multiply.Terms) CheckTypes(term, color);
                    return;
                case SkinClampValue clamp:
                    CheckTypes(clamp.Value, color);
                    return;
            }
        }

        public void CheckProperties(SkinPropertiesDoc properties)
        {
            if (properties == null) return;
            CheckTypes(properties.Opacity, false);
            CheckTransform(properties.Transform);
            CheckTypes(properties.Cx, false); CheckTypes(properties.Cy, false); CheckTypes(properties.R, false);
            CheckTypes(properties.Start, false); CheckTypes(properties.End, false);
            CheckTypes(properties.X, false); CheckTypes(properties.Y, false);
            CheckTypes(properties.W, false); CheckTypes(properties.H, false); CheckTypes(properties.Rx, false);
            CheckTypes(properties.X1, false); CheckTypes(properties.Y1, false);
            CheckTypes(properties.X2, false); CheckTypes(properties.Y2, false);
            CheckTypes(properties.Width, false); CheckTypes(properties.Assemble, false);
            CheckTypes(properties.Spin, false); CheckTypes(properties.DashOffset, false);
            CheckTypes(properties.StrokeWidth, false);
            if (properties.Dash != null)
                foreach (var dash in properties.Dash) CheckTypes(dash, false);
            if (properties.Points != null)
                foreach (var point in properties.Points)
                {
                    CheckTypes(point.X, false);
                    CheckTypes(point.Y, false);
                }
            CheckClip(properties.Clip);
            if (properties.FillTrack != null)
                CheckTypes(new SkinTrackValue(properties.FillTrack), true);
            if (properties.StrokeTrack != null)
                CheckTypes(new SkinTrackValue(properties.StrokeTrack), true);

            void CheckTransform(SkinTransformDoc transform)
            {
                if (transform == null) return;
                CheckTypes(transform.X, false); CheckTypes(transform.Y, false); CheckTypes(transform.Rotate, false);
                CheckTypes(transform.Scale, false); CheckTypes(transform.ScaleX, false); CheckTypes(transform.ScaleY, false);
            }

            void CheckClip(SkinClipDoc clip)
            {
                if (clip == null) return;
                CheckTypes(clip.Cx, false); CheckTypes(clip.Cy, false); CheckTypes(clip.R, false);
                CheckTypes(clip.X, false); CheckTypes(clip.Y, false);
                CheckTypes(clip.W, false); CheckTypes(clip.H, false); CheckTypes(clip.Rx, false);
            }
        }

        public void CheckGate(SkinGateDoc gate)
        {
            if (gate?.Track == null || !Animation.Tracks.TryGetValue(gate.Track, out var track)) return;
            if (!gate.Phase && track.IsColor)
                throw new SkinRenderException($"{Id}: expected numeric track \"{gate.Track}\"");
        }
    }

    // Document-node arm of inspect() — Times, transforms, properties, gates, tracks, layers.
    private static void InspectDocument(object value, AnimationCheck check)
    {
        switch (value)
        {
            case SkinTime time:
                if (time.Input.HasValue) check.Required.Add(time.Input.Value);
                return;
            case SkinTransformDoc transform:
                check.Inspect(transform.X); check.Inspect(transform.Y); check.Inspect(transform.Rotate);
                check.Inspect(transform.Scale); check.Inspect(transform.ScaleX); check.Inspect(transform.ScaleY);
                return;
            case SkinPropertiesDoc properties:
                check.Inspect(properties.Opacity);
                check.Inspect(properties.Transform);
                check.Inspect(properties.Cx); check.Inspect(properties.Cy); check.Inspect(properties.R);
                check.Inspect(properties.Start); check.Inspect(properties.End);
                check.Inspect(properties.X); check.Inspect(properties.Y);
                check.Inspect(properties.W); check.Inspect(properties.H); check.Inspect(properties.Rx);
                check.Inspect(properties.X1); check.Inspect(properties.Y1);
                check.Inspect(properties.X2); check.Inspect(properties.Y2);
                check.Inspect(properties.Width); check.Inspect(properties.Assemble);
                check.Inspect(properties.Spin); check.Inspect(properties.DashOffset);
                check.Inspect(properties.StrokeWidth);
                if (properties.Dash != null) foreach (var dash in properties.Dash) check.Inspect(dash);
                if (properties.Points != null)
                    foreach (var point in properties.Points)
                    {
                        check.Inspect(point.X);
                        check.Inspect(point.Y);
                    }
                InspectClipDoc(properties.Clip, check);
                return;
            case SkinGateDoc gate:
                check.Inspect(gate.From);
                check.Inspect(gate.Until);
                return;
            case SkinTrackDoc track:
                foreach (var key in track.Keys)
                {
                    // key.at / window times also contribute input references
                    if (key.At.Input.HasValue) check.Required.Add(key.At.Input.Value);
                    if (key.HasWindow)
                    {
                        if (key.WindowFrom.Input.HasValue) check.Required.Add(key.WindowFrom.Input.Value);
                        if (key.WindowTo.Input.HasValue) check.Required.Add(key.WindowTo.Input.Value);
                    }
                }
                foreach (var window in track.Windows)
                {
                    if (window.Item1.Input.HasValue) check.Required.Add(window.Item1.Input.Value);
                    if (window.Item2.Input.HasValue) check.Required.Add(window.Item2.Input.Value);
                }
                if (track.From.Input.HasValue) check.Required.Add(track.From.Input.Value);
                if (track.To.Input.HasValue) check.Required.Add(track.To.Input.Value);
                check.Inspect(track.Driver);
                if (track.Offset.Input.HasValue) check.Required.Add(track.Offset.Input.Value);
                return;
            case SkinLayerDoc layer:
                if (layer.SampleFrom.Input.HasValue) check.Required.Add(layer.SampleFrom.Input.Value);
                if (layer.SampleTo.Input.HasValue) check.Required.Add(layer.SampleTo.Input.Value);
                return;
            case Dictionary<string, SkinTrackDoc> tracks:
                foreach (var pair in tracks) InspectDocument(pair.Value, check);
                return;
        }
    }

    private static void InspectClipDoc(SkinClipDoc clip, AnimationCheck check)
    {
        if (clip == null) return;
        check.Inspect(clip.Cx); check.Inspect(clip.Cy); check.Inspect(clip.R);
        check.Inspect(clip.X); check.Inspect(clip.Y);
        check.Inspect(clip.W); check.Inspect(clip.H); check.Inspect(clip.Rx);
    }

    public static Result Validate(SkinDocument skin)
    {
        var result = new Result();
        var costs = new Dictionary<string, int>();
        var active = new HashSet<string>();
        var heights = new Dictionary<string, int>();
        if (skin.Frames.Count > 4096 || skin.Animations.Count > 4096)
            throw new SkinRenderException("asset count budget exceeded");

        int Visit(string id, int depth)
        {
            if (depth > 32)
                throw new SkinRenderException("animation reference depth exceeds 32");
            if (active.Contains(id))
                throw new SkinRenderException($"animation reference cycle: {string.Join(" -> ", active.Append(id))}");
            if (costs.TryGetValue(id, out var cached)) return cached;
            if (!skin.Animations.TryGetValue(id, out var animation))
                throw new SkinRenderException($"unknown animation \"{id}\"");
            active.Add(id);
            var check = new AnimationCheck(skin, id, animation, result);
            var height = 1;

            foreach (var pair in animation.Tracks)
                ValidateTrack(pair.Value, $"{id}.{pair.Key}");
            InspectDocument(animation.Tracks, check);
            check.Inspect(animation.RootOpacity);
            InspectDocument(animation.RootTransform, check);
            if (animation.RootTransform != null)
                foreach (var expr in new[] { animation.RootTransform.X, animation.RootTransform.Y,
                             animation.RootTransform.Rotate, animation.RootTransform.Scale,
                             animation.RootTransform.ScaleX, animation.RootTransform.ScaleY })
                    check.CheckTypes(expr, false);
            check.CheckTypes(animation.RootOpacity, false);

            // Track dependency cycles / depth (driver → track references).
            var trackHeights = new Dictionary<string, int>();
            var activeTracks = new HashSet<string>();
            int VisitTrack(string name, int trackDepth)
            {
                if (trackDepth > 32)
                    throw new SkinRenderException($"{id}: track dependency depth exceeds 32");
                if (activeTracks.Contains(name))
                    throw new SkinRenderException($"{id}: track dependency cycle at \"{name}\"");
                if (trackHeights.TryGetValue(name, out var cachedHeight)) return cachedHeight;
                activeTracks.Add(name);
                var maxHeight = 1;
                if (animation.Tracks.TryGetValue(name, out var track))
                {
                    void Walk(SkinValue value)
                    {
                        switch (value)
                        {
                            case SkinTrackValue reference:
                                maxHeight = Math.Max(maxHeight, 1 + VisitTrack(reference.Name, trackDepth + 1));
                                return;
                            case SkinLerpValue lerp:
                                Walk(lerp.From); Walk(lerp.To); Walk(lerp.Progress);
                                return;
                            case SkinAddValue add:
                                foreach (var term in add.Terms) Walk(term);
                                return;
                            case SkinMultiplyValue multiply:
                                foreach (var term in multiply.Terms) Walk(term);
                                return;
                            case SkinClampValue clamp:
                                Walk(clamp.Value);
                                return;
                        }
                    }

                    Walk(track.Driver);
                }
                if (maxHeight > 32)
                    throw new SkinRenderException($"{id}: track dependency depth exceeds 32");
                activeTracks.Remove(name);
                trackHeights[name] = maxHeight;
                return maxHeight;
            }

            foreach (var name in animation.Tracks.Keys.ToList()) VisitTrack(name, 0);
            // Track drivers also type-check against numeric tracks.
            foreach (var pair in animation.Tracks)
                check.CheckTypes(pair.Value.Driver, false);

            int LayerCost(SkinLayerDoc layer, int nesting)
            {
                if (nesting > 32)
                    throw new SkinRenderException($"{id}: layer nesting exceeds 32");
                if (!double.IsNaN(layer.At) && !double.IsNaN(layer.Until) && layer.At >= layer.Until)
                    throw new SkinRenderException($"{id}: invalid layer interval");
                if (layer.Otherwise != null && layer.When == null)
                    throw new SkinRenderException($"{id}: otherwise requires when");
                if (layer.When != null && layer.When.Phase && layer.When.Track == null)
                    throw new SkinRenderException($"{id}: phase requires track");
                InspectDocument(layer, check);
                InspectDocument(layer.Properties, check);
                check.CheckProperties(layer.Properties);
                InspectDocument(layer.When, check);
                check.CheckGate(layer.When);

                var cost = 1;
                if (layer.Frame != null)
                {
                    if (!skin.Frames.TryGetValue(layer.Frame, out var frame))
                        throw new SkinRenderException($"{id}: unknown frame \"{layer.Frame}\"");
                    if (layer.Properties != null)
                    {
                        var allowed = new[] { "opacity", "transform" }.Concat(PropertyNames[frame.Kind]).ToArray();
                        foreach (var key in PropertyKeysOf(layer.Properties))
                            if (!allowed.Contains(key))
                                throw new SkinRenderException($"{id}: property \"{key}\" is invalid on {KindName(frame.Kind)}");
                    }

                    if (layer.Layers.Count > 0 && frame.Kind != SkinFrameKind.Group)
                        throw new SkinRenderException($"{id}: only group frames accept child layers");
                    var mergedStroke = layer.Properties?.Stroke ?? layer.Properties?.StrokeTrack ?? frame.Stroke;
                    var mergedHasStrokeWidth =
                        (layer.Properties?.StrokeWidth != null) || frame.HasStrokeWidth;
                    if ((frame.Kind == SkinFrameKind.Arc || frame.Kind == SkinFrameKind.Line ||
                         frame.Kind == SkinFrameKind.Ring) &&
                        (mergedStroke == null || (!mergedHasStrokeWidth && frame.Kind != SkinFrameKind.Ring)))
                        throw new SkinRenderException($"{id}: {KindName(frame.Kind)} requires stroke and strokeWidth");
                    cost += FrameCost(frame);
                }
                else
                {
                    if (layer.Layers.Count > 0)
                        throw new SkinRenderException($"{id}: animation references cannot append child layers");
                    if (layer.Properties != null)
                        foreach (var key in PropertyKeysOf(layer.Properties))
                            if (key != "opacity" && key != "transform")
                                throw new SkinRenderException($"{id}: animation instances only accept opacity and transform");
                    cost += Visit(layer.Animation, depth + 1);
                    height = Math.Max(height, 1 + heights[layer.Animation]);
                    if (height > 32)
                        throw new SkinRenderException($"{id}: animation reference depth exceeds 32");
                    foreach (var input in result.Inputs[layer.Animation]) check.Required.Add(input);
                }

                foreach (var child in layer.Layers) cost += LayerCost(child, nesting + 1);
                if (layer.Otherwise != null) cost += LayerCost(layer.Otherwise, nesting + 1);
                if (cost > MaxNodes)
                    throw new SkinRenderException($"{id}: expanded node budget exceeded");
                return cost;
            }

            var cost = 1;
            if (!animation.IsSequence)
            {
                foreach (var layer in animation.Layers) cost += LayerCost(layer, 0);
            }
            else
            {
                foreach (var name in animation.Frames)
                {
                    if (!skin.Frames.TryGetValue(name, out var frame))
                        throw new SkinRenderException($"{id}: unknown frame \"{name}\"");
                    ValidateSequenceFrame(frame, id);
                    cost = Math.Max(cost, 1 + FrameCost(frame));
                }
            }

            if (cost > MaxNodes)
                throw new SkinRenderException($"{id}: expanded node budget exceeded");
            active.Remove(id);
            costs[id] = cost;
            heights[id] = height;
            result.Inputs[id] = check.Required;
            return cost;
        }

        foreach (var id in skin.Animations.Keys.ToList()) Visit(id, 0);

        foreach (var pair in skin.Slots)
        {
            var contract = SkinSlots.Contract(pair.Key);
            if (contract == null)
                throw new SkinRenderException($"unknown slot \"{pair.Key}\"");
            if (!skin.Animations.ContainsKey(pair.Value.Animation))
                throw new SkinRenderException($"{pair.Key}: unknown animation \"{pair.Value.Animation}\"");
            foreach (var input in result.Inputs[pair.Value.Animation])
            {
                var allowed = Common.Contains(input)
                              || ((contract.Kind == NoteKind.Hold || contract.Kind == NoteKind.LongHold) && HoldInputs.Contains(input))
                              || (contract.Kind == NoteKind.Flick && FlickInputs.Contains(input))
                              || (contract.Path == "exit" && input == SkinInput.ExitRewindEnd);
                var bodyInput = contract.Path == "body" && (BodyInputs.Contains(input)
                    || (contract.Kind == NoteKind.Hold && input == SkinInput.Length)
                    || (contract.Kind == NoteKind.LongHold &&
                        (input == SkinInput.Top || input == SkinInput.Bottom || input == SkinInput.Collapse)));
                var chainInput = contract.Path == "chainLink" && ChainInputs.Contains(input);
                if (!allowed && !bodyInput && !chainInput)
                    throw new SkinRenderException($"{pair.Key}: input \"{SkinInputNames.Name(input)}\" is not available");
            }
        }
        return result;
    }

    private static IEnumerable<string> PropertyKeysOf(SkinPropertiesDoc properties)
    {
        if (properties.Opacity != null) yield return "opacity";
        if (properties.Transform != null) yield return "transform";
        if (properties.Cx != null) yield return "cx";
        if (properties.Cy != null) yield return "cy";
        if (properties.R != null) yield return "r";
        if (properties.Start != null) yield return "start";
        if (properties.End != null) yield return "end";
        if (properties.X != null) yield return "x";
        if (properties.Y != null) yield return "y";
        if (properties.W != null) yield return "w";
        if (properties.H != null) yield return "h";
        if (properties.Rx != null) yield return "rx";
        if (properties.X1 != null) yield return "x1";
        if (properties.Y1 != null) yield return "y1";
        if (properties.X2 != null) yield return "x2";
        if (properties.Y2 != null) yield return "y2";
        if (properties.Width != null) yield return "width";
        if (properties.Assemble != null) yield return "assemble";
        if (properties.Spin != null) yield return "spin";
        if (properties.Dash != null) yield return "dash";
        if (properties.DashOffset != null) yield return "dashOffset";
        if (properties.StrokeWidth != null) yield return "strokeWidth";
        if (properties.Fill != null || properties.FillTrack != null) yield return "fill";
        if (properties.Stroke != null || properties.StrokeTrack != null) yield return "stroke";
        if (properties.Points != null) yield return "points";
        if (properties.Clip != null) yield return "clip";
    }

    private static string KindName(SkinFrameKind kind) => kind switch
    {
        SkinFrameKind.Image => "image",
        SkinFrameKind.Circle => "circle",
        SkinFrameKind.Arc => "arc",
        SkinFrameKind.Rect => "rect",
        SkinFrameKind.Line => "line",
        SkinFrameKind.Poly => "poly",
        SkinFrameKind.Ring => "ring",
        SkinFrameKind.Group => "group",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static void ValidateSequenceFrame(SkinFrame frame, string id)
    {
        switch (frame.Kind)
        {
            case SkinFrameKind.Group:
                foreach (var child in frame.Children) ValidateSequenceFrame(child, id);
                break;
            case SkinFrameKind.Arc:
            case SkinFrameKind.Line:
            case SkinFrameKind.Ring:
                if (frame.Stroke == null || (frame.Kind != SkinFrameKind.Ring && !frame.HasStrokeWidth))
                    throw new SkinRenderException($"{id}: sequence frame requires stroke and strokeWidth");
                break;
        }
    }

    private static int FrameCost(SkinFrame frame)
    {
        switch (frame.Kind)
        {
            case SkinFrameKind.Group:
            {
                var cost = 1;
                foreach (var child in frame.Children) cost += FrameCost(child);
                return cost;
            }
            case SkinFrameKind.Ring:
                return 1 + (1 + (frame.Edge?.Bands.Count ?? 0)) * (1 + frame.Segments);
            default:
                return 1;
        }
    }

    private static void ValidateTrack(SkinTrackDoc track, string label)
    {
        if (track.Form == SkinTrackForm.Keys)
        {
            for (var i = 0; i < track.Keys.Count; i++)
            {
                var key = track.Keys[i];
                if (i == 0 && key.Ease != null)
                    throw new SkinRenderException($"{label}: first key cannot have ease");
                if (key.At.IsNumber)
                {
                    if (key.At.Value < 0 || key.At.Value > 1)
                        throw new SkinRenderException($"{label}: key time outside 0..1");
                    var previous = i > 0 ? (SkinTime?) track.Keys[i - 1].At : null;
                    if (previous.HasValue && previous.Value.IsNumber && previous.Value.Value >= key.At.Value)
                        throw new SkinRenderException($"{label}: key times must increase strictly");
                }

                if (key.HasWindow && key.WindowFrom.IsNumber && key.WindowTo.IsNumber &&
                    key.WindowFrom.Value >= key.WindowTo.Value)
                    throw new SkinRenderException($"{label}: invalid ease window");
            }
        }

        if (track.Form == SkinTrackForm.Damped && track.From.IsNumber && track.To.IsNumber &&
            track.From.Value >= track.To.Value)
            throw new SkinRenderException($"{label}: invalid damped window");
        if (track.Form == SkinTrackForm.Bump)
        {
            foreach (var window in track.Windows)
            {
                if (window.Item1.IsNumber && window.Item2.IsNumber && window.Item1.Value >= window.Item2.Value)
                    throw new SkinRenderException($"{label}: invalid bump window");
            }
        }
    }
}
