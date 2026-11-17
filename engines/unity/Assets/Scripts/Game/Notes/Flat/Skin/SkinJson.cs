// Strict skin JSON reader — a field-for-field port of the zod schema in
// packages/notes/src/skin/schema.ts (strictObject: unknown keys are rejected).
// Errors surface as SkinParseException("invalid skin: <path> — <message>"),
// mirroring parseSkin's first-issue report. PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class SkinParseException : Exception
{
    public SkinParseException(string message) : base(message)
    {
    }
}

public static class SkinJson
{
    private static readonly string[] Families =
        { "click", "hold", "long-hold", "flick", "drag", "click-drag", "drop-click", "drop-drag" };

    private static readonly string[] GradeNames = { "perfect", "great", "good", "bad", "miss" };

    private static readonly int[] Shades = { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };

    private static readonly string[] Reserved =
    {
        "constructor", "toString", "toLocaleString", "valueOf", "hasOwnProperty",
        "isPrototypeOf", "propertyIsEnumerable", "__proto__",
    };

    private static readonly string[] CapNames = { "butt", "round", "square" };
    private static readonly string[] JoinNames = { "miter", "round", "bevel" };
    private static readonly string[] EaseTypes =
    {
        "linear", "inQuad", "outQuad", "inSine", "outSine", "inOutSine", "inOutQuad",
        "inCubic", "outCubic", "inOutCubic", "outQuart", "outQuint", "inExpo", "outExpo", "step",
    };

    // ------------------------------------------------------------- helpers

    private static SkinParseException Fail(string path, string message) =>
        new SkinParseException($"invalid skin: {path} — {message}");

    private static void RejectUnknown(JObject obj, IEnumerable<string> known, string path)
    {
        foreach (var key in obj.Properties())
        {
            if (known.Contains(key.Name)) continue;
            throw Fail(path, $"unrecognized key \"{key.Name}\"");
        }
    }

    private static JObject Obj(JToken token, string path)
    {
        if (token is JObject obj) return obj;
        throw Fail(path, "expected object");
    }

    private static JToken Field(JObject obj, string name, string path)
    {
        return obj.TryGetValue(name, out var value) ? value : null;
    }

    private static bool HasField(JObject obj, string name) => obj.ContainsKey(name);

    private static double Number(JToken token, string path)
    {
        if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
            throw Fail(path, "expected number");
        var value = (double) token;
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw Fail(path, "expected finite number");
        return value;
    }

    private static double Number(JObject obj, string name, string path) => Number(Field(obj, name, path), $"{path}.{name}");

    private static double OptionalNumber(JObject obj, string name, string path, double fallback = double.NaN)
    {
        var token = Field(obj, name, path);
        return token == null || token.Type == JTokenType.Null ? fallback : Number(token, $"{path}.{name}");
    }

    private static bool OptionalBool(JObject obj, string name, string path, bool fallback = false)
    {
        var token = Field(obj, name, path);
        if (token == null || token.Type == JTokenType.Null) return fallback;
        if (token.Type != JTokenType.Boolean) throw Fail($"{path}.{name}", "expected boolean");
        return (bool) token;
    }

    private static string String(JObject obj, string name, string path)
    {
        var token = Field(obj, name, path);
        if (token == null || token.Type != JTokenType.String) throw Fail($"{path}.{name}", "expected string");
        return (string) token;
    }

    private static string NameString(JToken token, string path)
    {
        if (token.Type != JTokenType.String) throw Fail(path, "expected string");
        var value = (string) token;
        if (value.Length < 1 || value.Length > 128 || Reserved.Contains(value))
            throw Fail(path, "reserved identifier");
        return value;
    }

    private static void CheckColor(string text, string path)
    {
        if (IsHex(text)) return;
        var split = text.Split(':');
        if (split.Length == 2)
        {
            var (type, level) = (split[0], split[1]);
            var knownTypes = new[] { "ring", "theme" }.Concat(Families).Concat(GradeNames);
            if (knownTypes.Contains(type) && Shades.Any(shade => shade.ToString() == level))
                return;
        }
        throw Fail(path, "expected Hex or color type:shade (50, 100, …, 900, 950)");
    }

    internal static bool IsHex(string text)
    {
        if (text == null || text.Length < 2 || text[0] != '#') return false;
        for (var i = 1; i < text.Length; i++)
        {
            var c = text[i];
            var digit = c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
            if (!digit) return false;
        }
        var len = text.Length - 1;
        return len == 3 || len == 4 || len == 6 || len == 8;
    }

    // ------------------------------------------------------------- time & value

    private static SkinTime Time(JToken token, string path)
    {
        if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            return new SkinTime(Number(token, path));
        var obj = Obj(token, path);
        RejectUnknown(obj, new[] { "input" }, path);
        var input = String(obj, "input", path);
        if (input != "holdLanding" && input != "exitRewindEnd")
            throw Fail($"{path}.input", "invalid enum value");
        return new SkinTime(input == "holdLanding" ? SkinInput.HoldLanding : SkinInput.ExitRewindEnd);
    }

    internal static SkinValue Value(JToken token, string path)
    {
        switch (token.Type)
        {
            case JTokenType.Float:
            case JTokenType.Integer:
                return new SkinConstValue(Number(token, path));
            case JTokenType.Object:
                break;
            default:
                throw Fail(path, "invalid value");
        }
        var obj = (JObject) token;
        if (obj.ContainsKey("input"))
        {
            RejectUnknown(obj, new[] { "input" }, path);
            var name = String(obj, "input", path);
            if (!SkinInputNames.TryParse(name, out var input))
                throw Fail($"{path}.input", $"invalid enum value. Expected one of {string.Join(" | ", "…")}");
            return new SkinInputValue(input);
        }
        if (obj.ContainsKey("track"))
        {
            RejectUnknown(obj, new[] { "track" }, path);
            return new SkinTrackValue(NameString(Field(obj, "track", path), $"{path}.track"));
        }
        if (obj.ContainsKey("lerp"))
        {
            RejectUnknown(obj, new[] { "lerp" }, path);
            var lerp = Obj(Field(obj, "lerp", path), $"{path}.lerp");
            RejectUnknown(lerp, new[] { "from", "to", "progress" }, $"{path}.lerp");
            return new SkinLerpValue(
                Value(Field(lerp, "from", path), $"{path}.lerp.from"),
                Value(Field(lerp, "to", path), $"{path}.lerp.to"),
                Value(Field(lerp, "progress", path), $"{path}.lerp.progress"));
        }
        if (obj.ContainsKey("add")) return SumValue(obj, "add", path);
        if (obj.ContainsKey("multiply")) return SumValue(obj, "multiply", path);
        if (obj.ContainsKey("clamp"))
        {
            RejectUnknown(obj, new[] { "clamp" }, path);
            var clamp = Obj(Field(obj, "clamp", path), $"{path}.clamp");
            RejectUnknown(clamp, new[] { "value", "min", "max" }, $"{path}.clamp");
            var min = OptionalNumber(clamp, "min", $"{path}.clamp", 0);
            var max = OptionalNumber(clamp, "max", $"{path}.clamp", 1);
            return new SkinClampValue(Value(Field(clamp, "value", path), $"{path}.clamp.value"), min, max);
        }
        throw Fail(path, "invalid value");
    }

    private static SkinValue SumValue(JObject obj, string key, string path)
    {
        RejectUnknown(obj, new[] { key }, path);
        var array = Field(obj, key, path) as JArray;
        if (array == null) throw Fail($"{path}.{key}", "expected array");
        if (array.Count < 1 || array.Count > 64)
            throw Fail($"{path}.{key}", "array must contain between 1 and 64 elements");
        var terms = new SkinValue[array.Count];
        for (var i = 0; i < array.Count; i++)
            terms[i] = Value(array[i], $"{path}.{key}[{i}]");
        return key == "add" ? (SkinValue) new SkinAddValue(terms) : new SkinMultiplyValue(terms);
    }

    // ------------------------------------------------------------- ease

    private static SkinEaseSpec Ease(JToken token, string path)
    {
        var obj = Obj(token, path);
        var type = String(obj, "type", path);
        switch (type)
        {
            case "outBack":
                RejectUnknown(obj, new[] { "type", "amount" }, path);
                return new SkinEaseSpec { Type = type, Amount = Number(obj, "amount", path) };
            case "inPow":
            case "outPow":
                RejectUnknown(obj, new[] { "type", "power" }, path);
                var power = Number(obj, "power", path);
                if (power <= 0) throw Fail($"{path}.power", "number must be positive");
                return new SkinEaseSpec { Type = type, Power = power };
            case "bezier":
                RejectUnknown(obj, new[] { "type", "x1", "y1", "x2", "y2" }, path);
                var x1 = Number(obj, "x1", path);
                var x2 = Number(obj, "x2", path);
                if (x1 < 0 || x1 > 1) throw Fail($"{path}.x1", "number must be between 0 and 1");
                if (x2 < 0 || x2 > 1) throw Fail($"{path}.x2", "number must be between 0 and 1");
                return new SkinEaseSpec
                {
                    Type = type,
                    X1 = x1,
                    Y1 = Number(obj, "y1", path),
                    X2 = x2,
                    Y2 = Number(obj, "y2", path),
                };
            case "compose":
                RejectUnknown(obj, new[] { "type", "outer", "inner" }, path);
                return new SkinEaseSpec
                {
                    Type = type,
                    Outer = Ease(Field(obj, "outer", path), $"{path}.outer"),
                    Inner = Ease(Field(obj, "inner", path), $"{path}.inner"),
                };
            default:
                if (!EaseTypes.Contains(type)) throw Fail($"{path}.type", "invalid enum value");
                RejectUnknown(obj, new[] { "type" }, path);
                return new SkinEaseSpec { Type = type };
        }
    }

    // ------------------------------------------------------------- track

    private static readonly string[] KeysFieldsColor = { "keys", "driver", "offset", "rate" };
    private static readonly string[] KeysFieldsNumber = { "keys", "offset", "rate", "driver" };

    private static SkinTrackDoc Track(JToken token, string path)
    {
        var obj = Obj(token, path);
        var hasKeys = obj.ContainsKey("keys");
        if (hasKeys)
        {
            var keys = Field(obj, "keys", path) as JArray;
            if (keys == null) throw Fail($"{path}.keys", "expected array");
            if (keys.Count < 1 || keys.Count > 4096)
                throw Fail($"{path}.keys", "array must contain between 1 and 4096 elements");
            // The union's first variant (color) matches when the first value is a string.
            var firstValue = Obj(keys[0], $"{path}.keys[0]").GetValue("value");
            var isColor = firstValue != null && firstValue.Type == JTokenType.String;
            RejectUnknown(obj, isColor ? KeysFieldsColor : KeysFieldsNumber, path);
            var track = new SkinTrackDoc { Form = SkinTrackForm.Keys, IsColor = isColor };
            for (var i = 0; i < keys.Count; i++)
            {
                var keyObj = Obj(keys[i], $"{path}.keys[{i}]");
                RejectUnknown(keyObj, new[] { "at", "value", "ease", "window" }, $"{path}.keys[{i}]");
                var key = new SkinTrackKey { At = Time(Field(keyObj, "at", path), $"{path}.keys[{i}].at"), IsColor = isColor };
                var valueToken = Field(keyObj, "value", path);
                if (isColor)
                {
                    if (valueToken.Type != JTokenType.String) throw Fail($"{path}.keys[{i}].value", "expected string");
                    key.Color = (string) valueToken;
                    CheckColor(key.Color, $"{path}.keys[{i}].value");
                }
                else
                {
                    key.Value = Number(valueToken, $"{path}.keys[{i}].value");
                }
                var easeToken = Field(keyObj, "ease", path);
                if (easeToken != null) key.Ease = Ease(easeToken, $"{path}.keys[{i}].ease");
                var windowToken = Field(keyObj, "window", path);
                if (windowToken != null)
                {
                    var window = Obj(windowToken, $"{path}.keys[{i}].window");
                    RejectUnknown(window, new[] { "from", "to" }, $"{path}.keys[{i}].window");
                    key.HasWindow = true;
                    key.WindowFrom = Time(Field(window, "from", path), $"{path}.keys[{i}].window.from");
                    key.WindowTo = Time(Field(window, "to", path), $"{path}.keys[{i}].window.to");
                }
                track.Keys.Add(key);
            }
            ReadTimingExtras(obj, track, path);
            return track;
        }
        if (obj.ContainsKey("ease"))
        {
            RejectUnknown(obj, new[] { "ease", "driver" }, path);
            return new SkinTrackDoc { Form = SkinTrackForm.Ease, Ease = Ease(Field(obj, "ease", path), $"{path}.ease"), Driver = OptionalDriver(obj, path) };
        }
        if (obj.ContainsKey("curve"))
        {
            var curve = String(obj, "curve", path);
            switch (curve)
            {
                case "bump":
                {
                    RejectUnknown(obj, new[] { "curve", "windows", "low", "high", "driver" }, path);
                    var windows = Field(obj, "windows", path) as JArray;
                    if (windows == null) throw Fail($"{path}.windows", "expected array");
                    if (windows.Count < 1 || windows.Count > 128)
                        throw Fail($"{path}.windows", "array must contain between 1 and 128 elements");
                    var track = new SkinTrackDoc { Form = SkinTrackForm.Bump };
                    foreach (var windowToken in windows)
                    {
                        var window = Obj(windowToken, $"{path}.windows");
                        RejectUnknown(window, new[] { "from", "to" }, $"{path}.windows");
                        track.Windows.Add((
                            Time(Field(window, "from", path), $"{path}.windows.from"),
                            Time(Field(window, "to", path), $"{path}.windows.to")));
                    }
                    ReadLowHigh(obj, track, path);
                    track.Driver = OptionalDriver(obj, path);
                    return track;
                }
                case "damped":
                {
                    RejectUnknown(obj, new[] { "curve", "from", "to", "damping", "low", "high", "driver" }, path);
                    var damping = Number(obj, "damping", path);
                    if (damping <= 0) throw Fail($"{path}.damping", "number must be positive");
                    var track = new SkinTrackDoc
                    {
                        Form = SkinTrackForm.Damped,
                        From = Time(Field(obj, "from", path), $"{path}.from"),
                        To = Time(Field(obj, "to", path), $"{path}.to"),
                        Damping = damping,
                    };
                    ReadLowHigh(obj, track, path);
                    track.Driver = OptionalDriver(obj, path);
                    return track;
                }
                case "fade":
                {
                    RejectUnknown(obj, new[] { "curve", "drop", "fall", "power", "driver" }, path);
                    var drop = Number(obj, "drop", path);
                    var fall = Number(obj, "fall", path);
                    var power = Number(obj, "power", path);
                    if (drop < 0 || drop > 1) throw Fail($"{path}.drop", "number must be between 0 and 1");
                    if (fall < 0) throw Fail($"{path}.fall", "number must be nonnegative");
                    if (power <= 0) throw Fail($"{path}.power", "number must be positive");
                    return new SkinTrackDoc { Form = SkinTrackForm.Fade, Drop = drop, Fall = fall, Power = power, Driver = OptionalDriver(obj, path) };
                }
                default:
                    throw Fail($"{path}.curve", "invalid enum value");
            }
        }
        throw Fail(path, "invalid track");
    }

    private static void ReadTimingExtras(JObject obj, SkinTrackDoc track, string path)
    {
        if (obj.ContainsKey("offset"))
        {
            track.HasOffset = true;
            track.Offset = Time(Field(obj, "offset", path), $"{path}.offset");
        }
        if (obj.ContainsKey("rate"))
        {
            track.HasRate = true;
            track.Rate = Number(obj, "rate", path);
        }
        track.Driver = OptionalDriver(obj, path);
    }

    private static void ReadLowHigh(JObject obj, SkinTrackDoc track, string path)
    {
        if (obj.ContainsKey("low")) track.Low = Number(obj, "low", path);
        if (obj.ContainsKey("high")) track.High = Number(obj, "high", path);
    }

    private static SkinValue OptionalDriver(JObject obj, string path)
    {
        return obj.ContainsKey("driver") ? Value(Field(obj, "driver", path), $"{path}.driver") : null;
    }

    // ------------------------------------------------------------- gate / transform / properties

    private static SkinGateDoc Gate(JToken token, string path)
    {
        var obj = Obj(token, path);
        RejectUnknown(obj, new[] { "track", "phase", "from", "until", "fromInclusive", "untilInclusive" }, path);
        var gate = new SkinGateDoc();
        if (obj.ContainsKey("track")) gate.Track = NameString(Field(obj, "track", path), $"{path}.track");
        gate.Phase = OptionalBool(obj, "phase", path);
        if (obj.ContainsKey("from"))
        {
            gate.HasFrom = true;
            gate.From = Time(Field(obj, "from", path), $"{path}.from");
        }
        if (obj.ContainsKey("until"))
        {
            gate.HasUntil = true;
            gate.Until = Time(Field(obj, "until", path), $"{path}.until");
        }
        gate.FromInclusive = OptionalBool(obj, "fromInclusive", path);
        gate.UntilInclusive = OptionalBool(obj, "untilInclusive", path);
        return gate;
    }

    private static SkinTransformDoc Transform(JToken token, string path)
    {
        var obj = Obj(token, path);
        RejectUnknown(obj, new[] { "x", "y", "rotate", "scale", "scaleX", "scaleY" }, path);
        return new SkinTransformDoc
        {
            X = OptionalExpr(obj, "x", path),
            Y = OptionalExpr(obj, "y", path),
            Rotate = OptionalExpr(obj, "rotate", path),
            Scale = OptionalExpr(obj, "scale", path),
            ScaleX = OptionalExpr(obj, "scaleX", path),
            ScaleY = OptionalExpr(obj, "scaleY", path),
        };
    }

    private static SkinValue OptionalExpr(JObject obj, string name, string path) =>
        obj.ContainsKey(name) ? Value(Field(obj, name, path), $"{path}.{name}") : null;

    private static SkinClipDoc ClipExpr(JToken token, string path)
    {
        var obj = Obj(token, path);
        var type = String(obj, "type", path);
        if (type == "circle")
        {
            RejectUnknown(obj, new[] { "type", "cx", "cy", "r" }, path);
            return new SkinClipDoc { IsCircle = true, Cx = OptionalExpr(obj, "cx", path), Cy = OptionalExpr(obj, "cy", path), R = RequiredExpr(obj, "r", path) };
        }
        if (type != "rect") throw Fail($"{path}.type", "invalid enum value");
        RejectUnknown(obj, new[] { "type", "x", "y", "w", "h", "rx" }, path);
        return new SkinClipDoc
        {
            X = RequiredExpr(obj, "x", path),
            Y = RequiredExpr(obj, "y", path),
            W = RequiredExpr(obj, "w", path),
            H = RequiredExpr(obj, "h", path),
            Rx = OptionalExpr(obj, "rx", path),
        };
    }

    private static SkinValue RequiredExpr(JObject obj, string name, string path)
    {
        if (!obj.ContainsKey(name)) throw Fail($"{path}.{name}", "required");
        return Value(Field(obj, name, path), $"{path}.{name}");
    }

    private static readonly string[] PropertyKeys =
    {
        "opacity", "transform", "cx", "cy", "r", "start", "end", "x", "y", "w", "h", "rx",
        "x1", "y1", "x2", "y2", "width", "assemble", "spin", "dash", "dashOffset", "strokeWidth",
        "fill", "stroke", "points", "clip",
    };

    private static SkinPropertiesDoc Properties(JToken token, string path)
    {
        var obj = Obj(token, path);
        RejectUnknown(obj, PropertyKeys, path);
        var properties = new SkinPropertiesDoc
        {
            Opacity = OptionalExpr(obj, "opacity", path),
            Transform = obj.ContainsKey("transform") ? Transform(Field(obj, "transform", path), $"{path}.transform") : null,
            Cx = OptionalExpr(obj, "cx", path),
            Cy = OptionalExpr(obj, "cy", path),
            R = OptionalExpr(obj, "r", path),
            Start = OptionalExpr(obj, "start", path),
            End = OptionalExpr(obj, "end", path),
            X = OptionalExpr(obj, "x", path),
            Y = OptionalExpr(obj, "y", path),
            W = OptionalExpr(obj, "w", path),
            H = OptionalExpr(obj, "h", path),
            Rx = OptionalExpr(obj, "rx", path),
            X1 = OptionalExpr(obj, "x1", path),
            Y1 = OptionalExpr(obj, "y1", path),
            X2 = OptionalExpr(obj, "x2", path),
            Y2 = OptionalExpr(obj, "y2", path),
            Width = OptionalExpr(obj, "width", path),
            Assemble = OptionalExpr(obj, "assemble", path),
            Spin = OptionalExpr(obj, "spin", path),
            DashOffset = OptionalExpr(obj, "dashOffset", path),
            StrokeWidth = OptionalExpr(obj, "strokeWidth", path),
            Clip = obj.ContainsKey("clip") ? ClipExpr(Field(obj, "clip", path), $"{path}.clip") : null,
        };
        foreach (var key in new[] { "fill", "stroke" })
        {
            if (!obj.ContainsKey(key)) continue;
            var value = Field(obj, key, path);
            if (value.Type == JTokenType.String)
            {
                var text = (string) value;
                CheckColor(text, $"{path}.{key}");
                if (key == "fill") properties.Fill = text;
                else properties.Stroke = text;
            }
            else
            {
                var binding = Obj(value, $"{path}.{key}");
                RejectUnknown(binding, new[] { "track" }, $"{path}.{key}");
                var track = NameString(Field(binding, "track", path), $"{path}.{key}.track");
                if (key == "fill") properties.FillTrack = track;
                else properties.StrokeTrack = track;
            }
        }
        if (obj.ContainsKey("points"))
        {
            var points = Field(obj, "points", path) as JArray;
            if (points == null) throw Fail($"{path}.points", "expected array");
            if (points.Count > 4096) throw Fail($"{path}.points", "array must contain at most 4096 elements");
            properties.Points = new List<SkinPointDoc>(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                var point = Obj(points[i], $"{path}.points[{i}]");
                RejectUnknown(point, new[] { "x", "y" }, $"{path}.points[{i}]");
                properties.Points.Add(new SkinPointDoc
                {
                    X = RequiredExpr(point, "x", $"{path}.points[{i}]"),
                    Y = RequiredExpr(point, "y", $"{path}.points[{i}]"),
                });
            }
        }
        if (obj.ContainsKey("dash"))
        {
            var dash = Field(obj, "dash", path) as JArray;
            if (dash == null) throw Fail($"{path}.dash", "expected array");
            if (dash.Count < 1 || dash.Count > 128)
                throw Fail($"{path}.dash", "array must contain between 1 and 128 elements");
            properties.Dash = new List<SkinValue>(dash.Count);
            for (var i = 0; i < dash.Count; i++)
                properties.Dash.Add(Value(dash[i], $"{path}.dash[{i}]"));
        }
        return properties;
    }

    // ------------------------------------------------------------- frames

    private static readonly string[] StrokeKeys = { "dash", "dashOffset", "stroke", "strokeWidth", "cap", "join" };

    private static SkinFrame Frame(JToken token, string path)
    {
        var obj = Obj(token, path);
        var type = String(obj, "type", path);
        var frame = new SkinFrame();
        switch (type)
        {
            case "image":
            {
                RejectUnknown(obj, new[] { "type", "src", "x", "y", "w", "h", "opacity", "blend" }, path);
                var src = String(obj, "src", path);
                if (!IsImageSource(src)) throw Fail($"{path}.src", "expected a relative PNG path or PNG data URI");
                frame.Kind = SkinFrameKind.Image;
                frame.Src = src;
                frame.X = Number(obj, "x", path);
                frame.Y = Number(obj, "y", path);
                frame.W = NonNegative(obj, "w", path);
                frame.H = NonNegative(obj, "h", path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "circle":
            {
                RejectUnknown(obj, new[] { "type", "r", "cx", "cy", "opacity", "blend" }.Concat(StrokeKeys).Append("fill"), path);
                frame.Kind = SkinFrameKind.Circle;
                frame.R = Number(obj, "r", path);
                frame.Cx = OptionalNumber(obj, "cx", path);
                frame.Cy = OptionalNumber(obj, "cy", path);
                ReadStrokeFill(obj, frame, path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "arc":
            {
                RejectUnknown(obj, new[] { "type", "r", "start", "end", "cx", "cy", "opacity", "blend" }.Concat(StrokeKeys), path);
                frame.Kind = SkinFrameKind.Arc;
                frame.R = Number(obj, "r", path);
                frame.Start = Number(obj, "start", path);
                frame.End = Number(obj, "end", path);
                frame.Cx = OptionalNumber(obj, "cx", path);
                frame.Cy = OptionalNumber(obj, "cy", path);
                ReadStroke(obj, frame, path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "rect":
            {
                RejectUnknown(obj, new[] { "type", "x", "y", "w", "h", "rx", "opacity", "blend" }.Concat(StrokeKeys).Append("fill"), path);
                frame.Kind = SkinFrameKind.Rect;
                frame.X = Number(obj, "x", path);
                frame.Y = Number(obj, "y", path);
                frame.W = Number(obj, "w", path);
                frame.H = Number(obj, "h", path);
                frame.Rx = OptionalNumber(obj, "rx", path);
                ReadStrokeFill(obj, frame, path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "line":
            {
                RejectUnknown(obj, new[] { "type", "x1", "y1", "x2", "y2", "opacity", "blend" }.Concat(StrokeKeys), path);
                frame.Kind = SkinFrameKind.Line;
                frame.X1 = Number(obj, "x1", path);
                frame.Y1 = Number(obj, "y1", path);
                frame.X2 = Number(obj, "x2", path);
                frame.Y2 = Number(obj, "y2", path);
                ReadStroke(obj, frame, path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "poly":
            {
                RejectUnknown(obj, new[] { "type", "points", "closed", "opacity", "blend" }.Concat(StrokeKeys).Append("fill"), path);
                frame.Kind = SkinFrameKind.Poly;
                var points = Field(obj, "points", path) as JArray;
                if (points == null) throw Fail($"{path}.points", "expected array");
                if (points.Count > 4096) throw Fail($"{path}.points", "array must contain at most 4096 elements");
                foreach (var pointToken in points)
                {
                    var point = Obj(pointToken, $"{path}.points");
                    RejectUnknown(point, new[] { "x", "y" }, $"{path}.points");
                    frame.PolyPoints.Add(new[]
                    {
                        Number(point, "x", $"{path}.points"),
                        Number(point, "y", $"{path}.points"),
                    });
                }
                frame.Closed = OptionalBool(obj, "closed", path);
                ReadStrokeFill(obj, frame, path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "ring":
            {
                RejectUnknown(obj, new[] { "type", "r", "width", "segments", "assemble", "spin", "edge", "opacity", "blend" }.Concat(StrokeKeys), path);
                frame.Kind = SkinFrameKind.Ring;
                frame.R = Number(obj, "r", path);
                frame.Width = Number(obj, "width", path);
                var segments = (int) Number(obj, "segments", path);
                if (segments != Number(obj, "segments", path) || segments < 1 || segments > 256)
                    throw Fail($"{path}.segments", "expected int between 1 and 256");
                frame.Segments = segments;
                frame.Assemble = Number(obj, "assemble", path);
                frame.Spin = OptionalNumber(obj, "spin", path);
                if (obj.ContainsKey("edge"))
                {
                    var edge = Obj(Field(obj, "edge", path), $"{path}.edge");
                    RejectUnknown(edge, new[] { "color", "placement", "bands" }, $"{path}.edge");
                    var color = String(edge, "color", path);
                    if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[\\da-f]{6}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        throw Fail($"{path}.edge.color", "invalid string");
                    var edgeDoc = new SkinEdgeDoc { Color = color };
                    if (edge.ContainsKey("placement"))
                    {
                        var placement = String(edge, "placement", path);
                        if (placement != "inside" && placement != "outside") throw Fail($"{path}.edge.placement", "invalid enum value");
                        edgeDoc.Inside = placement == "inside";
                    }
                    var bands = Field(edge, "bands", path) as JArray;
                    if (bands == null) throw Fail($"{path}.edge.bands", "expected array");
                    if (bands.Count > 128) throw Fail($"{path}.edge.bands", "array must contain at most 128 elements");
                    foreach (var bandToken in bands)
                    {
                        var band = Obj(bandToken, $"{path}.edge.bands");
                        RejectUnknown(band, new[] { "fraction", "opacity" }, $"{path}.edge.bands");
                        var fraction = Number(band, "fraction", path);
                        var opacity = Number(band, "opacity", path);
                        if (fraction < 0) throw Fail($"{path}.edge.bands.fraction", "number must be nonnegative");
                        if (opacity < 0 || opacity > 1) throw Fail($"{path}.edge.bands.opacity", "number must be between 0 and 1");
                        edgeDoc.Bands.Add(new SkinEdgeBand { Fraction = fraction, Opacity = opacity });
                    }
                    frame.Edge = edgeDoc;
                }
                ReadStroke(obj, frame, path);
                ReadPaint(obj, frame, path);
                return frame;
            }
            case "group":
                RejectUnknown(obj, new[] { "type", "opacity", "blend", "transform", "clip", "isolate", "children" }, path);
                frame.Kind = SkinFrameKind.Group;
                ReadPaint(obj, frame, path);
                if (obj.ContainsKey("transform"))
                {
                    var transform = Obj(Field(obj, "transform", path), $"{path}.transform");
                    RejectUnknown(transform, new[] { "x", "y", "rotate", "scale", "scaleX", "scaleY" }, $"{path}.transform");
                    frame.Transform = new SkinTransformDoc
                    {
                        X = StaticTransformNumber(transform, "x", path),
                        Y = StaticTransformNumber(transform, "y", path),
                        Rotate = StaticTransformNumber(transform, "rotate", path),
                        Scale = StaticTransformNumber(transform, "scale", path),
                        ScaleX = StaticTransformNumber(transform, "scaleX", path),
                        ScaleY = StaticTransformNumber(transform, "scaleY", path),
                    };
                }
                if (obj.ContainsKey("clip")) frame.Clip = ClipStatic(Field(obj, "clip", path), $"{path}.clip");
                frame.Isolate = OptionalBool(obj, "isolate", path);
                if (obj.ContainsKey("children"))
                {
                    var children = Field(obj, "children", path) as JArray;
                    if (children == null) throw Fail($"{path}.children", "expected array");
                    if (children.Count > 4096) throw Fail($"{path}.children", "array must contain at most 4096 elements");
                    foreach (var child in children) frame.Children.Add(Frame(child, $"{path}.children"));
                }
                return frame;
            default:
                throw Fail($"{path}.type", "invalid enum value");
        }
    }

    private static double NonNegative(JObject obj, string name, string path)
    {
        var value = Number(obj, name, path);
        if (value < 0) throw Fail($"{path}.{name}", "number must be nonnegative");
        return value;
    }

    private static SkinValue StaticTransformNumber(JObject transform, string name, string path) =>
        transform.ContainsKey(name)
            ? new SkinConstValue(Number(transform, name, path))
            : null;

    private static SkinClipDoc ClipStatic(JToken token, string path)
    {
        var obj = Obj(token, path);
        var clip = ClipExpr(token, path); // validates keys/types; expressions here must be constants
        foreach (var field in new[] { clip.Cx, clip.Cy, clip.R, clip.X, clip.Y, clip.W, clip.H, clip.Rx })
        {
            if (field != null && !(field is SkinConstValue))
                throw Fail(path, "frames accept numeric values only");
        }
        return clip;
    }

    private static void ReadPaint(JObject obj, SkinFrame frame, string path)
    {
        var opacity = OptionalNumber(obj, "opacity", path);
        if (!double.IsNaN(opacity)) frame.Opacity = opacity;
        if (obj.ContainsKey("blend"))
        {
            var blend = String(obj, "blend", path);
            if (blend != "normal" && blend != "add") throw Fail($"{path}.blend", "invalid enum value");
            frame.IsAdd = blend == "add";
        }
    }

    private static void ReadStrokeFill(JObject obj, SkinFrame frame, string path)
    {
        ReadStroke(obj, frame, path);
        if (obj.ContainsKey("fill"))
        {
            var fill = String(obj, "fill", path);
            CheckColor(fill, $"{path}.fill");
            frame.Fill = fill;
        }
    }

    private static void ReadStroke(JObject obj, SkinFrame frame, string path)
    {
        if (obj.ContainsKey("stroke"))
        {
            var stroke = String(obj, "stroke", path);
            CheckColor(stroke, $"{path}.stroke");
            frame.Stroke = stroke;
        }
        if (obj.ContainsKey("strokeWidth"))
        {
            frame.StrokeWidth = NonNegative(obj, "strokeWidth", path);
            frame.HasStrokeWidth = true;
        }
        if (obj.ContainsKey("dash"))
        {
            var dash = Field(obj, "dash", path) as JArray;
            if (dash == null) throw Fail($"{path}.dash", "expected array");
            if (dash.Count < 1 || dash.Count > 128)
                throw Fail($"{path}.dash", "array must contain between 1 and 128 elements");
            var values = new double[dash.Count];
            for (var i = 0; i < dash.Count; i++)
            {
                values[i] = Number(dash[i], $"{path}.dash[{i}]");
                if (values[i] < 0) throw Fail($"{path}.dash[{i}]", "number must be nonnegative");
            }
            frame.Dash = values;
        }
        if (obj.ContainsKey("dashOffset"))
        {
            frame.DashOffset = Number(obj, "dashOffset", path);
            frame.HasDashOffset = true;
        }
        if (obj.ContainsKey("cap"))
        {
            var cap = String(obj, "cap", path);
            var index = Array.IndexOf(CapNames, cap);
            if (index < 0) throw Fail($"{path}.cap", "invalid enum value");
            frame.Cap = (FlatCap) index;
            frame.HasCap = true;
        }
        if (obj.ContainsKey("join"))
        {
            var join = String(obj, "join", path);
            var index = Array.IndexOf(JoinNames, join);
            if (index < 0) throw Fail($"{path}.join", "invalid enum value");
            frame.Join = (FlatJoin) index;
            frame.HasJoin = true;
        }
    }

    internal static bool IsImageSource(string src)
    {
        if (src == null) return false;
        if (System.Text.RegularExpressions.Regex.IsMatch(src, "^data:image/png;base64,[A-Za-z0-9+/]+={0,2}$"))
            return true;
        return src.Length > 0 && src.Length <= 48_000_000 &&
               !src.Contains(':') && !src.Contains('\\') && !src.Contains('%') && !src.Contains('?') && !src.Contains('#') &&
               !src.StartsWith("/") && !src.Split('/').Contains("..") && src.ToLowerInvariant().EndsWith(".png");
    }

    // ------------------------------------------------------------- layers

    private static SkinLayerDoc Layer(JToken token, string path)
    {
        var obj = Obj(token, path);
        RejectUnknown(obj, new[] { "source", "at", "until", "properties", "blend", "layers", "when", "otherwise" }, path);
        var layer = new SkinLayerDoc();
        var source = Obj(Field(obj, "source", path), $"{path}.source");
        if (source.ContainsKey("frame"))
        {
            RejectUnknown(source, new[] { "frame" }, $"{path}.source");
            layer.Frame = NameString(Field(source, "frame", path), $"{path}.source.frame");
        }
        else
        {
            RejectUnknown(source, new[] { "animation", "sample" }, $"{path}.source");
            layer.Animation = NameString(Field(source, "animation", path), $"{path}.source.animation");
            if (source.ContainsKey("sample"))
            {
                var sample = Obj(Field(source, "sample", path), $"{path}.source.sample");
                RejectUnknown(sample, new[] { "from", "to" }, $"{path}.source.sample");
                if (sample.ContainsKey("from"))
                {
                    layer.HasSampleFrom = true;
                    layer.SampleFrom = Time(Field(sample, "from", path), $"{path}.source.sample.from");
                }
                if (sample.ContainsKey("to"))
                {
                    layer.HasSampleTo = true;
                    layer.SampleTo = Time(Field(sample, "to", path), $"{path}.source.sample.to");
                }
            }
        }
        if (obj.ContainsKey("at"))
        {
            layer.At = Number(obj, "at", path);
            if (layer.At < 0 || layer.At > 1) throw Fail($"{path}.at", "number must be between 0 and 1");
        }
        if (obj.ContainsKey("until"))
        {
            layer.Until = Number(obj, "until", path);
            if (layer.Until < 0 || layer.Until > 1) throw Fail($"{path}.until", "number must be between 0 and 1");
        }
        if (obj.ContainsKey("properties")) layer.Properties = Properties(Field(obj, "properties", path), $"{path}.properties");
        if (obj.ContainsKey("blend"))
        {
            var blend = String(obj, "blend", path);
            if (blend != "normal" && blend != "add") throw Fail($"{path}.blend", "invalid enum value");
            layer.IsAdd = blend == "add";
        }
        if (obj.ContainsKey("layers"))
        {
            var layers = Field(obj, "layers", path) as JArray;
            if (layers == null) throw Fail($"{path}.layers", "expected array");
            if (layers.Count > 4096) throw Fail($"{path}.layers", "array must contain at most 4096 elements");
            foreach (var child in layers) layer.Layers.Add(Layer(child, $"{path}.layers"));
        }
        if (obj.ContainsKey("when")) layer.When = Gate(Field(obj, "when", path), $"{path}.when");
        if (obj.ContainsKey("otherwise")) layer.Otherwise = Layer(Field(obj, "otherwise", path), $"{path}.otherwise");
        return layer;
    }

    // ------------------------------------------------------------- document

    public static SkinDocument Parse(string json)
    {
        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (Exception e)
        {
            throw new SkinParseException($"invalid skin: document — {e.Message}");
        }
        return Parse(root);
    }

    public static SkinDocument Parse(JObject root)
    {
        var path = "";
        RejectUnknown(root, new[] { "schemaVersion", "id", "name", "meta", "frames", "animations", "slots" }, path);
        if (root.TryGetValue("schemaVersion", out var version))
        {
            if (version.Type != JTokenType.Integer || (long) version != 2)
                throw Fail("schemaVersion", "invalid literal value, expected 2");
        }
        else throw Fail("", "schemaVersion is required");
        var document = new SkinDocument
        {
            Id = root.ContainsKey("id") ? NameString(Field(root, "id", path), "id") : throw Fail("id", "required"),
            Name = String(root, "name", "name"),
        };
        if (root.ContainsKey("meta")) document.Meta = Meta(Field(root, "meta", path), "meta");
        var frames = root.ContainsKey("frames") ? Obj(Field(root, "frames", path), "frames") : new JObject();
        foreach (var property in frames.Properties())
        {
            var key = NameString(JToken.FromObject(property.Name), "frames");
            document.Frames[key] = Frame(property.Value, $"frames.{key}");
        }
        var animations = root.ContainsKey("animations") ? Obj(Field(root, "animations", path), "animations") : new JObject();
        foreach (var property in animations.Properties())
        {
            var key = NameString(JToken.FromObject(property.Name), "animations");
            document.Animations[key] = Animation(property.Value, $"animations.{key}");
        }
        var slots = root.ContainsKey("slots") ? Obj(Field(root, "slots", path), "slots") : new JObject();
        foreach (var property in slots.Properties())
        {
            var key = NameString(JToken.FromObject(property.Name), "slots");
            var slot = Obj(property.Value, $"slots.{key}");
            RejectUnknown(slot, new[] { "animation", "period", "note" }, $"slots.{key}");
            var binding = new SkinSlotDoc { Animation = NameString(Field(slot, "animation", path), $"slots.{key}.animation") };
            if (slot.ContainsKey("period"))
                binding.Period = NonNegative(slot, "period", $"slots.{key}");
            if (slot.ContainsKey("note"))
            {
                var note = String(slot, "note", $"slots.{key}");
                if (note.Length > 4096) throw Fail($"slots.{key}.note", "string must contain at most 4096 elements");
                binding.Note = note;
            }
            document.Slots[key] = binding;
        }
        return document;
    }

    /// <summary>Meta is accepted but opaque: it must be an object, and nothing
    /// inside it is read or shape-checked. The palette/grades fields it once
    /// carried were never consumed by any runtime (game-config colours always
    /// won), so keeping their parsing only preserved a probe-vs-game drift.</summary>
    private static SkinMeta Meta(JToken token, string path)
    {
        Obj(token, path);
        return new SkinMeta();
    }

    internal static FlatFamily FamilyOf(string name)
    {
        var index = Array.IndexOf(Families, name);
        if (index < 0) throw new SkinParseException($"invalid skin: unknown family \"{name}\"");
        return (FlatFamily) index;
    }

    internal static SkinGrade GradeOf(string name)
    {
        var index = Array.IndexOf(GradeNames, name);
        if (index < 0) throw new SkinParseException($"invalid skin: unknown grade \"{name}\"");
        return (SkinGrade) index;
    }

    private static SkinAnimationDoc Animation(JToken token, string path)
    {
        var obj = Obj(token, path);
        var type = String(obj, "type", path);
        var animation = new SkinAnimationDoc { IsSequence = type == "sequence" };
        if (type != "layers" && type != "sequence") throw Fail($"{path}.type", "invalid enum value");
        var commonKeys = new[] { "type", "tracks", "properties", "isolate" };
        RejectUnknown(obj, type == "layers"
            ? commonKeys.Append("layers").ToArray()
            : commonKeys.Append("fps").Append("frames").ToArray(), path);
        if (obj.ContainsKey("tracks"))
        {
            var tracks = Obj(Field(obj, "tracks", path), $"{path}.tracks");
            foreach (var property in tracks.Properties())
            {
                var name = NameString(JToken.FromObject(property.Name), $"{path}.tracks");
                animation.Tracks[name] = Track(property.Value, $"{path}.tracks.{property.Name}");
            }
        }
        if (obj.ContainsKey("properties"))
        {
            var properties = Obj(Field(obj, "properties", path), $"{path}.properties");
            RejectUnknown(properties, new[] { "opacity", "transform" }, $"{path}.properties");
            animation.RootOpacity = OptionalExpr(properties, "opacity", $"{path}.properties");
            if (properties.ContainsKey("transform"))
                animation.RootTransform = Transform(Field(properties, "transform", path), $"{path}.properties.transform");
        }
        animation.Isolate = OptionalBool(obj, "isolate", path);
        if (type == "layers")
        {
            var layers = Field(obj, "layers", path) as JArray;
            if (layers == null) throw Fail($"{path}.layers", "expected array");
            if (layers.Count > 4096) throw Fail($"{path}.layers", "array must contain at most 4096 elements");
            foreach (var layer in layers) animation.Layers.Add(Layer(layer, $"{path}.layers"));
        }
        else
        {
            if (obj.ContainsKey("fps"))
            {
                var fps = Number(obj, "fps", path);
                if (fps <= 0) throw Fail($"{path}.fps", "number must be positive");
                animation.Fps = fps;
            }
            var frames = Field(obj, "frames", path) as JArray;
            if (frames == null) throw Fail($"{path}.frames", "expected array");
            if (frames.Count < 1 || frames.Count > 4096)
                throw Fail($"{path}.frames", "array must contain between 1 and 4096 elements");
            foreach (var frame in frames) animation.Frames.Add(NameString(frame, $"{path}.frames"));
        }
        return animation;
    }

}
