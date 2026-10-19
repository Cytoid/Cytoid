// Numeric parity checker: replays the TS-generated golden.json inputs against
// the pure C# port (Assets/Scripts/Game/Notes/Flat/{Palette,Mesh}). Run:
//   dotnet run --project Tools/FlatParity
// Regenerate golden values:
//   cd ../cytoid-design/packages/notes && npx tsx ../Cytoid/Tools/FlatParity/golden.ts

using System.Text.Json;

var fail = 0;
var pass = 0;
var missing = new List<string>();

void Check(string name, bool ok, string detail = "")
{
    if (ok) pass++;
    else { fail++; Console.WriteLine($"FAIL {name} {detail}"); }
}

bool Close(double a, double b, double tol = 1e-4) => Math.Abs(a - b) <= tol;

bool HexClose(string a, string b, double tol = 1e-4)
{
    var ca = FlatColor.ParseHex(a);
    var cb = FlatColor.ParseHex(b);
    return Close(ca.R, cb.R, tol) && Close(ca.G, cb.G, tol) && Close(ca.B, cb.B, tol);
}

// Golden replay needs the generated golden.json (not committed — regenerate it
// from the cytoid-design checkout). Without it the suite still runs every
// self-contained property/invariant check and reports the replay as skipped;
// pass --require-golden to make a missing golden.json a failure instead.
var requireGolden = args.Contains("--require-golden");
JsonDocument? goldenDoc = File.Exists("Tools/FlatParity/golden.json")
    ? JsonDocument.Parse(File.ReadAllText("Tools/FlatParity/golden.json"))
    : null;
if (goldenDoc == null)
{
    var note = "golden.json not found — golden replay skipped, self-contained checks only." +
        " Regenerate: cd <cytoid-design>/packages/notes && npx tsx <this-repo>/Tools/FlatParity/golden.ts";
    if (requireGolden)
    {
        Console.Error.WriteLine("--require-golden: " + note);
        return 1;
    }
    Console.Error.WriteLine("note: " + note);
}

// Stage 1a sources — color / ease / tokens / palette
if (File.Exists("Assets/Scripts/Game/Notes/Flat/Palette/FlatColor.cs"))
{
    if (goldenDoc != null)
    {
    var golden = goldenDoc.RootElement;

    FlatPalette ParseRampName(string name)
    {
        var parts = name.Split('/');
        var fam = parts[0] switch
        {
            "click" => FlatFamily.Click, "hold" => FlatFamily.Hold, "long-hold" => FlatFamily.LongHold,
            "flick" => FlatFamily.Flick, "drag" => FlatFamily.Drag, "click-drag" => FlatFamily.ClickDrag,
            "drop-click" => FlatFamily.DropClick, _ => FlatFamily.DropDrag,
        };
        var dir = parts[1] == "up" ? FlatDirection.Up : FlatDirection.Down;
        return FlatPaletteDerive.Default(fam, dir);
    }

    try
    {
        // OKLCH forward
        foreach (var c in golden.GetProperty("oklch").EnumerateArray())
        {
            var o = FlatColor.RgbToOklch(FlatColor.ParseHex(c.GetProperty("hex").GetString()!));
            var w = c.GetProperty("oklch");
            Check($"oklch {c.GetProperty("hex")}", Close(o.L, w.GetProperty("l").GetDouble()) &&
                  Close(o.C, w.GetProperty("c").GetDouble()) && Close(o.H, w.GetProperty("h").GetDouble(), 1e-3),
                  $"got L{o.L} C{o.C} H{o.H}");
        }

        // OKLCH inverse (incl. gamut mapping)
        foreach (var c in golden.GetProperty("toRgb").EnumerateArray())
        {
            var w = c.GetProperty("oklch");
            var rgb = FlatColor.OklchToRgb(new Oklch(w.GetProperty("l").GetDouble(), w.GetProperty("c").GetDouble(), w.GetProperty("h").GetDouble()));
            Check($"toRgb l={w.GetProperty("l")}", HexClose(FlatColor.ToHex(rgb), c.GetProperty("hex").GetString()!),
                  $"got {FlatColor.ToHex(rgb)} want {c.GetProperty("hex")}");
        }

        // Ramps
        foreach (var ramp in golden.GetProperty("ramps").EnumerateObject())
        {
            var p = ramp.Name.StartsWith("seed:")
                ? FlatPaletteDerive.FromSeed(FlatColor.ParseHex(ramp.Name["seed:".Length..]))
                : ParseRampName(ramp.Name);
            var shades = new[] { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };
            foreach (var s in shades)
                Check($"ramp {ramp.Name}[{s}]", HexClose(FlatColor.ToHex(p.Shade(s)), ramp.Value.GetProperty(s.ToString()).GetString()!),
                      $"got {FlatColor.ToHex(p.Shade(s))} want {ramp.Value.GetProperty(s.ToString())}");
            Check($"ramp {ramp.Name} ring", HexClose(FlatColor.ToHex(p.RingColor), ramp.Value.GetProperty("ring").GetString()!));
            Check($"ramp {ramp.Name} ink", HexClose(FlatColor.ToHex(p.Ink), ramp.Value.GetProperty("ink").GetString()!));
            Check($"ramp {ramp.Name} hue", Close(p.Hue, ramp.Value.GetProperty("hue").GetDouble(), 1e-3), $"got {p.Hue}");
        }

        // tone()
        foreach (var t in golden.GetProperty("tone").EnumerateArray())
        {
            var ramp = t.GetProperty("ramp").GetString()!;
            if (ramp != "click/up") continue;
            var p = FlatPaletteDerive.Default(FlatFamily.Click, FlatDirection.Up);
            var step = t.GetProperty("step").GetDouble();
            Check($"tone {ramp} {step}", HexClose(FlatColor.ToHex(p.Tone(step)), t.GetProperty("hex").GetString()!),
                  $"got {FlatColor.ToHex(p.Tone(step))} want {t.GetProperty("hex")}");
        }

        // ringTone()
        foreach (var t in golden.GetProperty("ringTone").EnumerateArray())
        {
            var p = FlatPaletteDerive.Default(FlatFamily.Click, FlatDirection.Up);
            var k = t.GetProperty("k").GetDouble();
            Check($"ringTone k={k}", HexClose(FlatColor.ToHex(p.RingTone(k)), t.GetProperty("hex").GetString()!),
                  $"got {FlatColor.ToHex(p.RingTone(k))} want {t.GetProperty("hex")}");
        }

        // Ease functions
        foreach (var e in golden.GetProperty("ease").EnumerateArray())
        {
            var fn = e.GetProperty("fn").GetString()!;
            var x = e.GetProperty("x").GetDouble();
            var v = e.GetProperty("v").GetDouble();
            var got = fn switch
            {
                "linear" => FlatEase.Linear(x), "inQuad" => FlatEase.InQuad(x), "outQuad" => FlatEase.OutQuad(x),
                "outSine" => FlatEase.OutSine(x), "inSine" => FlatEase.InSine(x), "inOutQuad" => FlatEase.InOutQuad(x),
                "inCubic" => FlatEase.InCubic(x), "outCubic" => FlatEase.OutCubic(x), "inOutCubic" => FlatEase.InOutCubic(x),
                "outQuart" => FlatEase.OutQuart(x), "outExpo" => FlatEase.OutExpo(x), "inExpo" => FlatEase.InExpo(x),
                "outBack" => FlatEase.OutBack(x), "outBack(2)" => FlatEase.OutBack(x, 2),
                _ => double.NaN,
            };
            Check($"ease {fn}({x})", Close(got, v), $"got {got} want {v}");
        }

        // cubic-bezier + size curves
        var bez = FlatEase.CubicBezier(0.7, 0, 0.75, 1);
        foreach (var b in golden.GetProperty("bezier").EnumerateArray())
            Check($"bezier({b.GetProperty("x")})", Close(bez(b.GetProperty("x").GetDouble()), b.GetProperty("v").GetDouble()));

        foreach (var s in golden.GetProperty("sizeCurve").EnumerateArray())
        {
            var kind = s.GetProperty("kind").GetString()!;
            var settle = FlatTokens.SizeCurve(kind == "click" ? SizeKind.Click : SizeKind.DragHead);
            var curve = FlatEase.RiseSettleCurve(settle, FlatTokens.SizeCurveShape);
            var got = curve(s.GetProperty("p").GetDouble());
            Check($"sizeCurve {kind} p={s.GetProperty("p")}", Close(got, s.GetProperty("v").GetDouble()), $"got {got} want {s.GetProperty("v")}");
        }

        // helpers
        foreach (var h in golden.GetProperty("helpers").EnumerateArray())
        {
            var fn = h.GetProperty("fn").GetString()!;
            var argv = h.GetProperty("args").EnumerateArray().Select(a => a.GetDouble()).ToArray();
            var v = h.GetProperty("v").GetDouble();
            var got = fn switch
            {
                "seg" => FlatEase.Seg(argv[0], argv[1], argv[2]),
                "bump" => FlatEase.Bump(argv[0], argv[1], argv[2]),
                "lerp" => FlatEase.Lerp(argv[0], argv[1], argv[2]),
                "clamp" => FlatEase.Clamp(argv[0], argv[1], argv[2]),
                "clamp01" => FlatEase.Clamp01(argv[0]),
                "ranged" => FlatEase.Ranged(argv[0], argv[1], argv[2], FlatEase.Linear),
                _ => double.NaN,
            };
            Check($"{fn}({string.Join(',', argv)})", Close(got, v), $"got {got} want {v}");
        }

        // mulberry32
        foreach (var r in golden.GetProperty("rng").EnumerateArray())
        {
            var rng = new FlatEase.Rng((uint) r.GetProperty("seed").GetInt32());
            var i = 0;
            foreach (var want in r.GetProperty("values").EnumerateArray())
                Check($"rng({r.GetProperty("seed")})[{i++}]", Close(rng.Next(), want.GetDouble()));
        }

        // mix / parse
        foreach (var m in golden.GetProperty("mix").EnumerateArray())
            Check($"mix {m.GetProperty("a")} {m.GetProperty("b")} {m.GetProperty("t")}",
                  HexClose(FlatColor.ToHex(FlatColor.Mix(FlatColor.ParseHex(m.GetProperty("a").GetString()!), FlatColor.ParseHex(m.GetProperty("b").GetString()!), m.GetProperty("t").GetDouble())), m.GetProperty("hex").GetString()!),
                  $"want {m.GetProperty("hex")}");
        foreach (var p in golden.GetProperty("parse").EnumerateArray())
        {
            var rgb = FlatColor.ParseHex(p.GetProperty("hex").GetString()!);
            var w = p.GetProperty("rgb");
            Check($"parse {p.GetProperty("hex")}", Close(rgb.R, w[0].GetDouble()) && Close(rgb.G, w[1].GetDouble()) && Close(rgb.B, w[2].GetDouble()));
        }
    }
    catch (NotImplementedException)
    {
        missing.Add("Flat/Palette still has unimplemented stubs");
    }
    }
}
else
{
    missing.Add("Flat/Palette (FlatColor.cs not found — stage 1a not started)");
}

// Stage 1b sources — hold entrance parity + geometry property tests
if (File.Exists("Assets/Scripts/Game/Notes/Flat/Mesh/FlatGeom.cs"))
{
    try
    {
        if (goldenDoc != null)
        {
        var holdGolden = goldenDoc.RootElement;
        foreach (var c in holdGolden.GetProperty("holdEase").EnumerateArray())
            Check($"hold InOutSine x={c.GetProperty("x")}",
                Close(FlatEase.InOutSine(c.GetProperty("x").GetDouble()), c.GetProperty("v").GetDouble(), 1e-9));
        foreach (var c in holdGolden.GetProperty("holdSize").EnumerateArray())
            Check($"hold size p={c.GetProperty("p")}",
                Close(FlatHoldDesign.Size(c.GetProperty("p").GetDouble(), c.GetProperty("approach").GetDouble()),
                    c.GetProperty("v").GetDouble(), 1e-9));
        foreach (var c in holdGolden.GetProperty("holdLanding").EnumerateArray())
        {
            var approach = c.GetProperty("approach").GetDouble();
            var land = c.GetProperty("land").GetDouble();
            Check($"hold bracket landing approach={approach}", Close(FlatHoldDesign.BracketLandP(approach), land, 1e-12));
            Check($"hold size before landing approach={approach}", Close(FlatHoldDesign.Size(land - 0.01, approach), c.GetProperty("before").GetDouble(), 1e-9));
            Check($"hold size at landing approach={approach}", Close(FlatHoldDesign.Size(land, approach), c.GetProperty("at").GetDouble(), 1e-9));
            Check($"hold bracket rests at landing approach={approach}", FlatHoldDesign.BracketSpin(land, approach) == 0);
        }
        foreach (var c in holdGolden.GetProperty("holdBracket").EnumerateArray())
        {
            var p = c.GetProperty("p").GetDouble();
            var approach = c.GetProperty("approach").GetDouble();
            Check($"hold bracket spin p={p}", Close(FlatHoldDesign.BracketSpin(p, approach), c.GetProperty("spin").GetDouble(), 1e-9));
            Check($"hold bracket fixed radius p={p}", Close(FlatTokens.Unit / 2 * FlatHoldDesign.BracketRadius, c.GetProperty("radius").GetDouble(), 1e-9));
            Check($"hold bracket width p={p}", Close(FlatTokens.Unit * FlatTokens.StrokeHair * FlatHoldDesign.BracketWidth, c.GetProperty("width").GetDouble(), 1e-9));
            Check($"hold bracket fade p={p}", Close(FlatEase.Seg(p, FlatHoldDesign.BracketFrom, FlatHoldDesign.BracketFadeTo), c.GetProperty("alpha").GetDouble(), 1e-9));
        }
        foreach (var c in holdGolden.GetProperty("bodyReveal").EnumerateArray())
        {
            var reveal = FlatHoldDesign.BodyReveal(c.GetProperty("appear").GetDouble());
            Check($"body reveal stretch appear={c.GetProperty("appear")}", Close(reveal.Stretch, c.GetProperty("stretch").GetDouble(), 1e-9));
            Check($"body reveal alpha appear={c.GetProperty("appear")}", Close(reveal.Alpha, c.GetProperty("alpha").GetDouble(), 1e-9));
        }
        foreach (var c in holdGolden.GetProperty("holdWake").EnumerateArray())
            Check($"hold wake p={c.GetProperty("p")}",
                Close(FlatHoldDesign.Wake(c.GetProperty("p").GetDouble()), c.GetProperty("v").GetDouble(), 1e-9));

        // Source scene -> expected mesh, independent of FlatText's layout and animation.
        // Golden vertexCount/samples describe TS poly vertices before tessellation.
        void CheckTextGeometry(string name, JsonElement geometry, FlatMeshBuilder actual,
            double alpha = 1, double offsetX = 0, double offsetY = 0, double z = 0)
        {
            var expected = new FlatMeshBuilder();
            var sourcePoints = new List<(double X, double Y)>();
            foreach (var node in geometry.GetProperty("nodes").EnumerateArray())
            {
                var pts = node.GetProperty("points").EnumerateArray().Select(v => v.GetDouble()).ToArray();
                for (var i = 0; i < pts.Length; i += 2)
                {
                    sourcePoints.Add((pts[i], pts[i + 1]));
                    pts[i] += offsetX;
                    pts[i + 1] += offsetY;
                }
                var c = FlatColor.ParseHex(node.GetProperty("color").GetString()!);
                c = new Rgba(c.R, c.G, c.B, (float)(node.GetProperty("alpha").GetDouble() * alpha));
                var width = node.GetProperty("width").GetDouble();
                if (width == 0) FlatGeom.PolyFill(expected, pts, c, z);
                else FlatGeom.PolyStroke(expected, pts, node.GetProperty("closed").GetBoolean(), width, c, FlatJoin.Miter, FlatCap.Square, z);
            }
            var pointsMatch = sourcePoints.Count == geometry.GetProperty("vertexCount").GetInt32();
            foreach (var sample in geometry.GetProperty("samples").EnumerateArray())
            {
                var p = sourcePoints[sample.GetProperty("i").GetInt32()];
                pointsMatch &= Close(p.X, sample.GetProperty("point")[0].GetDouble(), 1e-12) &&
                    Close(p.Y, sample.GetProperty("point")[1].GetDouble(), 1e-12);
            }
            Check(name + " source vertex count / exact point samples", pointsMatch);
            Check(name + " mesh vertex count / topology", actual.VertCount == expected.VertCount && actual.Tris.SequenceEqual(expected.Tris),
                $"got {actual.VertCount}, want {expected.VertCount}");
            var match = actual.Verts.Count == expected.Verts.Count;
            var firstMismatch = "";
            for (var i = 0; match && i < actual.Verts.Count; i++)
            {
                match = Close(actual.Verts[i], expected.Verts[i], i % 7 < 2 ? 2e-5 : 1e-6);
                if (!match) firstMismatch = $"vertex {i / 7} channel {i % 7}: got {actual.Verts[i]}, want {expected.Verts[i]}";
            }
            Check(name + " all mesh positions / widths / colours / opacity", match, firstMismatch);
        }
        var textTokens = holdGolden.GetProperty("textTokens");
        Check("judgement cap height / centre", Close(FlatJudgement.CapHeight(), textTokens.GetProperty("capHeight").GetDouble(), 1e-12) && FlatJudgement.Offset() == (0, 0));
        foreach (var grade in textTokens.GetProperty("grades").EnumerateObject())
        {
            Check("grade token " + grade.Name, HexClose(FlatColor.ToHex(FlatTokens.GradeColor(grade.Name)), grade.Value.GetString()!));
            Check("grade duration " + grade.Name, Close(FlatTokens.Time.ClearDuration(grade.Name), textTokens.GetProperty("durations").GetProperty(grade.Name).GetDouble(), 1e-12));
        }
        foreach (var c in textTokens.GetProperty("showsTiming").EnumerateArray())
        {
            var grade = c.GetProperty("grade").GetString()!;
            Check("shows timing " + grade, FlatTiming.ShowsTiming(grade) == c.GetProperty("value").GetBoolean());
            if (!FlatTiming.ShowsTiming(grade))
            {
                var empty = new FlatMeshBuilder();
                FlatTiming.Draw(empty, "fast", grade, 0.1, false);
                Check("untimed grade emits nothing " + grade, empty.VertCount == 0);
            }
        }
        foreach (var c in textTokens.GetProperty("timingOf").EnumerateArray())
            Check("timing of " + c.GetProperty("error"), FlatTiming.TimingOf(c.GetProperty("error").GetDouble()) == c.GetProperty("value").GetString());
        foreach (var c in holdGolden.GetProperty("textMeasure").EnumerateArray())
            Check("measure text " + c.GetProperty("text"), Close(FlatText.MeasureText(c.GetProperty("text").GetString()!, c.GetProperty("H").GetDouble(),
                c.GetProperty("gap").ValueKind == JsonValueKind.Null ? null : c.GetProperty("gap").GetDouble()), c.GetProperty("width").GetDouble(), 1e-10));
        foreach (var c in holdGolden.GetProperty("textDraw").EnumerateArray())
        {
            var mesh = new FlatMeshBuilder();
            var t = c.GetProperty("t").GetDouble();
            FlatText.DrawText(mesh, c.GetProperty("text").GetString()!, c.GetProperty("H").GetDouble(), FlatTokens.GradeColor("perfect"),
                gap: c.GetProperty("gap").GetDouble(), pop: new GlyphPop(t, 0.02, 0.035, 0.2, 2));
            CheckTextGeometry($"drawText {c.GetProperty("text")} t={t}", c.GetProperty("geometry"), mesh);
        }
        foreach (var c in holdGolden.GetProperty("textGlyphs").EnumerateArray())
        {
            var mesh = new FlatMeshBuilder();
            FlatText.DrawText(mesh, c.GetProperty("text").GetString()!, 27, FlatColor.ParseHex("#123456"), gap: 4.2, stroke: 3.7);
            CheckTextGeometry("glyph " + c.GetProperty("text"), c.GetProperty("geometry"), mesh);
        }
        foreach (var c in holdGolden.GetProperty("judgementDraw").EnumerateArray())
        {
            foreach (var alpha in new[] { 1.0, 0.37 })
            {
                var mesh = new FlatMeshBuilder();
                var grade = c.GetProperty("grade").GetString()!;
                Rgba? color = c.GetProperty("override").ValueKind == JsonValueKind.Null ? null : FlatColor.ParseHex(c.GetProperty("override").GetString()!);
                FlatJudgement.Draw(mesh, grade, c.GetProperty("t").GetDouble(), color, alpha, z: -0.125);
                CheckTextGeometry($"judgement {grade} t={c.GetProperty("t")} override={c.GetProperty("override")} alpha={alpha}", c.GetProperty("geometry"), mesh, alpha, z: -0.125);
            }
        }
        foreach (var c in holdGolden.GetProperty("timingSlots").EnumerateArray())
        {
            var stacked = c.GetProperty("slot").GetString() == "stacked";
            var unit = c.GetProperty("unit").GetDouble();
            var offset = FlatTiming.Offset(stacked, unit);
            Check($"timing slot {stacked} unit={unit}", Close(FlatTiming.CapHeight(stacked, unit), c.GetProperty("H").GetDouble(), 1e-12) &&
                Close(offset.X, c.GetProperty("offset").GetProperty("x").GetDouble(), 1e-12) && Close(offset.Y, c.GetProperty("offset").GetProperty("y").GetDouble(), 1e-12));
        }
        foreach (var c in holdGolden.GetProperty("timingLabels").EnumerateArray())
        {
            var mesh = new FlatMeshBuilder();
            var timing = c.GetProperty("timing").GetString()!;
            FlatTiming.DrawLabel(mesh, timing, c.GetProperty("H").GetDouble(), FlatTokens.GradeColor(timing == "fast" ? "bad" : "good"));
            CheckTextGeometry("timing label " + timing, c.GetProperty("geometry"), mesh);
        }
        foreach (var c in holdGolden.GetProperty("timingDraw").EnumerateArray())
        {
            foreach (var alpha in new[] { 1.0, 0.37 })
            {
                var mesh = new FlatMeshBuilder();
                var stacked = c.GetProperty("slot").GetString() == "stacked";
                var timing = c.GetProperty("timing").GetString()!;
                var grade = c.GetProperty("grade").GetString()!;
                FlatTiming.Draw(mesh, timing, grade, c.GetProperty("t").GetDouble(), stacked, alpha);
                // renderTiming is placed; Draw is local, so compare with the source
                // offset removed in double precision before mesh tessellation.
                var offset = FlatTiming.Offset(stacked);
                CheckTextGeometry($"timing {timing}/{grade}/{stacked} t={c.GetProperty("t")} alpha={alpha}",
                    c.GetProperty("geometry"), mesh, alpha, -offset.X, -offset.Y);
            }
        }
        }
        var appendMesh = new FlatMeshBuilder();
        appendMesh.Vert(123, 456, 7, new Rgba(1, 0, 0));
        FlatJudgement.Draw(appendMesh, "great", 0.1);
        FlatTiming.Draw(appendMesh, "slow", "good", 0.1, true);
        Check("text appends without resetting earlier geometry", appendMesh.VertCount > 1 && appendMesh.Verts[0] == 123 && appendMesh.Verts[1] == 456 && appendMesh.Verts[2] == 7);

        var b = new FlatMeshBuilder();
        var white = new Rgba(1, 1, 1, 1);
        var tint = new Rgba(0.2f, 0.4f, 0.6f, 0.35f);
        const double depth = -0.125;
        var feather = FlatGeom.EdgeFeather;
        var inset = feather / 2;
        Check("edge feather public contract", Close(feather, 0.5));

        double Area(FlatMeshBuilder mesh, float alpha, bool solidOnly = true)
        {
            double area = 0;
            for (var i = 0; i < mesh.Tris.Count; i += 3)
            {
                var a = mesh.Tris[i] * 7;
                var c = mesh.Tris[i + 1] * 7;
                var d = mesh.Tris[i + 2] * 7;
                var aa = mesh.Verts[a + 6];
                var ca = mesh.Verts[c + 6];
                var da = mesh.Verts[d + 6];
                if (solidOnly && (!Close(aa, alpha, 1e-6) || !Close(ca, alpha, 1e-6) || !Close(da, alpha, 1e-6))) continue;
                var triangle = Math.Abs(((double) mesh.Verts[c] - mesh.Verts[a]) * (mesh.Verts[d + 1] - mesh.Verts[a + 1]) -
                    ((double) mesh.Verts[c + 1] - mesh.Verts[a + 1]) * (mesh.Verts[d] - mesh.Verts[a])) / 2;
                area += triangle * (solidOnly ? 1 : (aa + ca + da) / (3 * alpha));
            }
            return area;
        }

        void NearArea(string name, double area, double expected, double relativeTolerance = 0.01)
        {
            Check(name, Math.Abs(area - expected) <= Math.Max(1e-5, expected * relativeTolerance), $"got {area}, want {expected}");
        }

        void CheckMesh(string name, FlatMeshBuilder mesh, bool binaryAlpha = true, bool requireClearEdge = true)
        {
            var valid = mesh.VertCount > 0 && mesh.Verts.Count % 7 == 0 && mesh.Tris.Count > 0 && mesh.Tris.Count % 3 == 0;
            var clear = 0;
            var full = 0;
            for (var i = 0; i < mesh.VertCount; i++)
            {
                var vertex = i * 7;
                for (var j = 0; j < 7; j++) valid &= float.IsFinite(mesh.Verts[vertex + j]);
                valid &= Close(mesh.Verts[vertex + 2], depth) && Close(mesh.Verts[vertex + 3], tint.R) &&
                    Close(mesh.Verts[vertex + 4], tint.G) && Close(mesh.Verts[vertex + 5], tint.B);
                var alpha = mesh.Verts[vertex + 6];
                valid &= alpha >= 0 && alpha <= tint.A;
                if (alpha == 0) clear++;
                if (alpha == tint.A) full++;
                if (binaryAlpha) valid &= alpha == 0 || alpha == tint.A;
            }
            for (var i = 0; i < mesh.Tris.Count; i += 3)
            {
                var a = mesh.Tris[i];
                var c = mesh.Tris[i + 1];
                var d = mesh.Tris[i + 2];
                var indicesValid = a >= 0 && a < mesh.VertCount && c >= 0 && c < mesh.VertCount && d >= 0 && d < mesh.VertCount;
                valid &= indicesValid;
                if (!indicesValid) continue;
                var cross = ((double) mesh.Verts[c * 7] - mesh.Verts[a * 7]) *
                        ((double) mesh.Verts[d * 7 + 1] - mesh.Verts[a * 7 + 1]) -
                    ((double) mesh.Verts[c * 7 + 1] - mesh.Verts[a * 7 + 1]) *
                        ((double) mesh.Verts[d * 7] - mesh.Verts[a * 7]);
                valid &= cross > 0;
            }
            Check(name + " finite / winding / rgba / z / feather alpha", valid && (!requireClearEdge || clear > 0) && (!binaryAlpha || full > 0));
        }

        bool HasPoint(FlatMeshBuilder mesh, double x, double y, float alpha)
        {
            for (var i = 0; i < mesh.VertCount; i++)
                if (Close(mesh.Verts[i * 7], x, 2e-5) && Close(mesh.Verts[i * 7 + 1], y, 2e-5) && Close(mesh.Verts[i * 7 + 6], alpha, 1e-6))
                    return true;
            return false;
        }

        // Independent angle walk of actual outer vertices (not just a copy of the density formula).
        void CheckSagitta(string name, FlatMeshBuilder mesh, double cx, double cy, double radius,
            double start = 0, double sweep = Math.PI * 2)
        {
            var angles = new List<double>();
            var fullCircle = Close(Math.Abs(sweep), Math.PI * 2);
            for (var i = 0; i < mesh.VertCount; i++)
            {
                var dx = mesh.Verts[i * 7] - cx;
                var dy = mesh.Verts[i * 7 + 1] - cy;
                if (mesh.Verts[i * 7 + 6] != 0 || !Close(Math.Sqrt(dx * dx + dy * dy), radius, 2e-5)) continue;
                var angle = (Math.Sign(sweep) * (Math.Atan2(dx, -dy) - start)) % (Math.PI * 2);
                if (angle < -1e-6) angle += Math.PI * 2;
                if (fullCircle || angle <= Math.Abs(sweep) + 1e-6) angles.Add(Math.Max(0, angle));
            }
            angles.Sort();
            var maxSagitta = 0.0;
            for (var i = 1; i < angles.Count; i++)
                maxSagitta = Math.Max(maxSagitta, radius * (1 - Math.Cos((angles[i] - angles[i - 1]) / 2)));
            if (fullCircle && angles.Count > 0)
                maxSagitta = Math.Max(maxSagitta, radius * (1 - Math.Cos((Math.PI * 2 - angles[angles.Count - 1] + angles[0]) / 2)));
            Check(name + " outer contour sagitta", angles.Count >= 3 && maxSagitta <= 0.10001, $"got {maxSagitta}");
        }

        List<(double Start, double End)> DashRuns(FlatMeshBuilder mesh,
            double originX = 0, double originY = 0, double directionX = 1, double directionY = 0)
        {
            var intervals = new List<(double Start, double End)>();
            for (var i = 0; i < mesh.Tris.Count; i += 3)
            {
                var lo = double.PositiveInfinity;
                var hi = double.NegativeInfinity;
                for (var j = 0; j < 3; j++)
                {
                    var vertex = mesh.Tris[i + j] * 7;
                    var distance = (mesh.Verts[vertex] - originX) * directionX + (mesh.Verts[vertex + 1] - originY) * directionY;
                    lo = Math.Min(lo, distance);
                    hi = Math.Max(hi, distance);
                }
                intervals.Add((lo, hi));
            }
            intervals.Sort((a, c) => a.Start.CompareTo(c.Start));
            var runs = new List<(double Start, double End)>();
            foreach (var interval in intervals)
            {
                if (runs.Count == 0 || interval.Start > runs[runs.Count - 1].End + 1e-5) runs.Add(interval);
                else
                {
                    var previous = runs[runs.Count - 1];
                    runs[runs.Count - 1] = (previous.Start, Math.Max(previous.End, interval.End));
                }
            }
            return runs;
        }

        foreach (var radius in new[] { 1.0, 6, 9, 40, 81, 128 })
        {
            b.Reset();
            FlatGeom.Disk(b, 3, -7, radius, tint, depth);
            var segments = (b.VertCount - 1) / 2;
            Check($"disk two rings r={radius}", b.VertCount == 1 + segments * 2 && b.Tris.Count / 3 == segments * 3);
            NearArea($"disk opaque area r={radius}", Area(b, tint.A), Math.PI * Math.Pow(radius - inset, 2));
            NearArea($"disk alpha-weighted area r={radius}", Area(b, tint.A, false), Math.PI * radius * radius, 0.02);
            Check($"disk symmetric feather r={radius}", HasPoint(b, 3, -7 - radius + inset, tint.A) && HasPoint(b, 3, -7 - radius - inset, 0));
            CheckMesh($"disk r={radius}", b);
            CheckSagitta($"disk r={radius}", b, 3, -7, radius + inset);
        }

        foreach (var sample in new[] { (R: 40.0, W: 6.0), (R: 64.0, W: 10.9), (R: 81.0, W: 2.8), (R: 117.0, W: 6.8) })
        {
            b.Reset();
            FlatGeom.Ring(b, 3, -7, sample.R, sample.W, tint, depth);
            var radius = sample.R;
            var half = sample.W / 2;
            var segments = ExpectedSegments(radius + half + inset);
            Check($"ring four radii / three strips r={radius}", b.VertCount == 4 * (segments + 1) && b.Tris.Count / 3 == segments * 6);
            NearArea($"ring opaque area r={radius}", Area(b, tint.A), 2 * Math.PI * radius * (sample.W - feather));
            NearArea($"ring alpha-weighted area r={radius}", Area(b, tint.A, false), 2 * Math.PI * radius * sample.W);
            Check($"ring four radial alpha levels r={radius}", HasPoint(b, 3, -7 - (radius - half - inset), 0) &&
                HasPoint(b, 3, -7 - (radius - half + inset), tint.A) && HasPoint(b, 3, -7 - (radius + half - inset), tint.A) &&
                HasPoint(b, 3, -7 - (radius + half + inset), 0));
            // Each contour is f/2 from the geometric edge; the 0→full transition is f wide.
            Check($"ring feather width r={radius}", Close(b.Verts[1] - b.Verts[8], feather, 1e-5) &&
                Close(b.Verts[15] - b.Verts[22], feather, 1e-5));
            CheckMesh($"ring r={radius}", b);
            CheckSagitta($"ring r={radius}", b, 3, -7, radius + half + inset);
        }
        b.Reset();
        FlatGeom.Ring(b, 0, 0, 40, 100, tint, depth);
        NearArea("thick ring opaque disk area", Area(b, tint.A), Math.PI * Math.Pow(90 - inset, 2));
        CheckMesh("thick ring", b);
        CheckSagitta("thick ring", b, 0, 0, 90 + inset);

        foreach (var sweep in new[] { Math.PI / 2, -Math.PI / 2, 0.03, -0.03 })
        {
            foreach (var round in new[] { false, true })
            {
                const double start = 0.37;
                const double radius = 80;
                const double width = 6;
                b.Reset();
                FlatGeom.Arc(b, 3, -7, radius, start, start + sweep, width, tint, round, depth);
                var outer = radius + width / 2 + inset;
                var coverage = Math.Abs(sweep) + (round ? 0 : 2 * Math.Asin(inset / outer));
                var segments = Math.Max(4, (int) Math.Ceiling(ExpectedSegments(outer) * coverage / (2 * Math.PI)));
                var radialVertexCount = 4 * (segments + 1);
                Check($"arc radial bands sweep={sweep} round={round}", b.VertCount >= radialVertexCount &&
                    b.Tris.Count / 3 >= segments * 6);
                foreach (var endpoint in new[] { 0, segments })
                {
                    var vertex = endpoint * 4 * 7;
                    // The solid/rim pair straddles the endpoint plane by ±f/2.
                    // Their midpoint preserves the original endpoint angle.
                    var x = (b.Verts[vertex + 14] + b.Verts[vertex + 21]) / 2.0;
                    var y = (b.Verts[vertex + 15] + b.Verts[vertex + 22]) / 2.0;
                    var expectedAngle = endpoint == 0 ? start : start + sweep;
                    Check($"arc endpoint angle sweep={sweep} round={round} end={endpoint}",
                        Close(Math.Atan2(x - 3, -(y + 7)), expectedAngle, 1e-6) &&
                        Close(Math.Sqrt((x - 3) * (x - 3) + (y + 7) * (y + 7)), 83, 0.001));
                }
                var opaqueWidth = width - feather;
                var expected = round ? radius * Math.Abs(sweep) * opaqueWidth + Math.PI * Math.Pow(opaqueWidth / 2, 2) :
                    (radius * Math.Abs(sweep) - feather) * opaqueWidth;
                NearArea($"arc opaque area sweep={sweep} round={round}", Area(b, tint.A), expected, 0.015);
                var outerStart = start - (round ? 0 : Math.Sign(sweep) * Math.Asin(inset / outer));
                CheckSagitta($"arc sweep={sweep} round={round}", b, 3, -7, outer, outerStart, Math.Sign(sweep) * coverage);
                CheckMesh($"arc sweep={sweep} round={round}", b);
            }
        }
        foreach (var direction in new[] { 1, -1 })
        {
            b.Reset();
            FlatGeom.Arc(b, 0, 0, 80, 0, direction * Math.PI / 2, 6, tint, true, depth);
            Check($"round arc cap tangent direction={direction}", HasPoint(b, -direction * (3 - inset), -80, tint.A) &&
                HasPoint(b, -direction * (3 + inset), -80, 0));
        }
        b.Reset();
        FlatGeom.Arc(b, 0, 0, 80, 0, 6 * Math.PI, 6, tint, true, depth);
        var arcVertices = b.VertCount;
        var arcTriangles = b.Tris.Count;
        var arcArea = Area(b, tint.A);
        b.Reset();
        FlatGeom.Ring(b, 0, 0, 80, 6, tint, depth);
        Check("full-circle arc suppresses caps / matches ring", b.VertCount == arcVertices && b.Tris.Count == arcTriangles && Close(Area(b, tint.A), arcArea));

        foreach (var radius in new[] { 0.0, 10, 30, 100 })
        {
            b.Reset();
            FlatGeom.RoundedRect(b, 3, -7, 100, 60, radius, tint, depth);
            var innerRadius = Math.Max(0, Math.Min(radius, 30) - inset);
            NearArea($"rounded rect opaque area rx={radius}", Area(b, tint.A),
                (100 - feather) * (60 - feather) - (4 - Math.PI) * innerRadius * innerRadius);
            var runs = DashRuns(b);
            Check($"rounded rect outer bounds rx={radius}", runs.Count == 1 && Close(runs[0].Start, 3 - inset) && Close(runs[0].End, 103 + inset));
            var topSolidX = radius == 0 ? 103 - inset : 103 - Math.Min(radius, 30);
            Check($"rounded rect straight edge feather rx={radius}", HasPoint(b, topSolidX, -7 + inset, tint.A) &&
                HasPoint(b, 103 - Math.Min(radius, 30), -7 - inset, 0));
            CheckMesh($"rounded rect rx={radius}", b);
        }
        b.Reset();
        FlatGeom.RoundedRect(b, -18.2, -60, 36.4, 120, 18.2, tint, depth);
        NearArea("hold capsule opaque area", Area(b, tint.A), (36.4 - feather) * (120 - feather) - (4 - Math.PI) * Math.Pow(18.2 - inset, 2));
        CheckSagitta("hold capsule top-right", b, 0, -60 + 18.2, 18.2 + inset, 0, Math.PI / 2);
        CheckMesh("hold capsule", b);
        b.Reset();
        FlatGeom.RoundedRect(b, -20, -20, 40, 40, 100, tint, depth);
        NearArea("clamped rounded rect becomes disk", Area(b, tint.A), Math.PI * Math.Pow(20 - inset, 2));
        CheckSagitta("rounded rect circle", b, 0, 0, 20 + inset);

        foreach (var sign in new[] { 1, -1 })
        {
            var corner = new[] { 0.0, 0, 30, 0, 30, sign * 30.0 };
            foreach (var join in new[] { FlatJoin.Miter, FlatJoin.Bevel, FlatJoin.Round })
            {
                b.Reset();
                FlatGeom.PolyStroke(b, corner, false, 4, tint, join, FlatCap.Butt, depth);
                var innerHalf = 2 - inset;
                var expected = (60 - feather) * (4 - feather);
                if (join == FlatJoin.Bevel) expected -= innerHalf * innerHalf / 2;
                if (join == FlatJoin.Round) expected -= innerHalf * innerHalf * (1 - Math.PI / 4);
                NearArea($"{join} corner opaque area sign={sign}", Area(b, tint.A), expected, 0.001);
                if (join == FlatJoin.Miter)
                    Check($"miter paired offset intersections sign={sign}", HasPoint(b, 30 + innerHalf, -sign * innerHalf, tint.A) &&
                        HasPoint(b, 30 + 2 + inset, -sign * (2 + inset), 0) && HasPoint(b, 30 - innerHalf, sign * innerHalf, tint.A));
                else
                    Check($"{join} paired outer endpoints sign={sign}", HasPoint(b, 30, -sign * innerHalf, tint.A) &&
                        HasPoint(b, 30, -sign * (2 + inset), 0));
                CheckMesh($"{join} corner sign={sign}", b);
            }
        }
        b.Reset();
        FlatGeom.PolyStroke(b, new[] { 0.0, 0, 30, 0, 0, 1 }, false, 4, tint, FlatJoin.Miter, FlatCap.Butt, depth);
        var maxX = double.NegativeInfinity;
        for (var i = 0; i < b.VertCount; i++) maxX = Math.Max(maxX, b.Verts[i * 7]);
        Check("acute miter clamps both contours to bevel", maxX <= 32 + inset);
        CheckMesh("clamped miter", b);
        var diamond = new[] { 0.0, -20, 20, 0, 0, 20, -20, 0 };
        foreach (var cap in new[] { FlatCap.Butt, FlatCap.Round, FlatCap.Square })
        {
            b.Reset();
            FlatGeom.PolyStroke(b, diamond, true, 4, tint, FlatJoin.Miter, cap, depth);
            NearArea($"closed diamond opaque area / ignores {cap} caps", Area(b, tint.A), 4 * Math.Sqrt(800) * (4 - feather), 1e-5);
            CheckMesh($"closed diamond {cap}", b);
        }
        b.Reset();
        FlatGeom.PolyStroke(b, new[] { 0.0, 0, 0, 0, 30, 0, 30, 0, 30, 30 }, false, 4, tint, FlatJoin.Miter, FlatCap.Butt, depth);
        NearArea("poly stroke skips consecutive duplicates", Area(b, tint.A), (60 - feather) * (4 - feather), 1e-5);
        b.Reset();
        FlatGeom.PolyStroke(b, new[] { 0.0, -20, 20, 0, 0, 20, -20, 0, 0, -20 }, true, 4, tint, FlatJoin.Miter, FlatCap.Round, depth);
        NearArea("poly stroke skips repeated closure", Area(b, tint.A), 4 * Math.Sqrt(800) * (4 - feather), 1e-5);

        foreach (var cap in new[] { FlatCap.Butt, FlatCap.Round, FlatCap.Square })
        {
            b.Reset();
            FlatGeom.Line(b, 3, 7, 33, 47, 4, tint, cap, depth);
            var expected = cap == FlatCap.Butt ? (50 - feather) * (4 - feather) : cap == FlatCap.Square ?
                (50 + 4 - feather) * (4 - feather) : 50 * (4 - feather) + Math.PI * Math.Pow(2 - inset, 2);
            NearArea($"diagonal {cap} line opaque area", Area(b, tint.A), expected, 0.001);
            NearArea($"diagonal {cap} line alpha-weighted area", Area(b, tint.A, false), cap == FlatCap.Butt ? 200 : cap == FlatCap.Square ? 216 : 200 + Math.PI * 4);
            var runs = DashRuns(b, 3, 7, 0.6, 0.8);
            var extension = (cap == FlatCap.Butt ? 0 : 2) + inset;
            Check($"diagonal {cap} line cap extent", runs.Count == 1 && Close(runs[0].Start, -extension, 1e-5) && Close(runs[0].End, 50 + extension, 1e-5));
            CheckMesh($"diagonal {cap} line", b);
            var lineArea = Area(b, tint.A);
            var lineVertexCount = b.VertCount;
            b.Reset();
            FlatGeom.PolyStroke(b, new[] { 3.0, 7, 33, 47 }, false, 4, tint, FlatJoin.Miter, cap, depth);
            Check($"two-point poly stroke matches {cap} line", Close(Area(b, tint.A), lineArea) && b.VertCount == lineVertexCount);
        }
        b.Reset();
        FlatGeom.Line(b, 0, 0, 20, 0, 4, tint, FlatCap.Butt, depth);
        Check("butt cap is symmetric / all four corners feathered", b.VertCount == 8 && b.Tris.Count / 3 == 10 &&
            HasPoint(b, inset, 2 - inset, tint.A) && HasPoint(b, -inset, 2 + inset, 0) &&
            HasPoint(b, 20 - inset, -2 + inset, tint.A) && HasPoint(b, 20 + inset, -2 - inset, 0));
        b.Reset();
        FlatGeom.Line(b, 0, 0, 20, 0, 4, tint, FlatCap.Square, depth);
        Check("square cap opaque / transparent extensions", HasPoint(b, -2 + inset, 2 - inset, tint.A) && HasPoint(b, -2 - inset, 2 + inset, 0));

        b.Reset();
        FlatGeom.DashedLine(b, 0, 0, 25, 0, 4, tint, new[] { 10.0, 10 }, 0, FlatCap.Butt, depth);
        var truncated = DashRuns(b);
        Check("dash starts exactly at zero / centered AA band", truncated.Count == 2 && Close(truncated[0].Start, -inset) && Close(truncated[0].End, 10 + inset));
        Check("dash truncates at segment end / centered AA band", truncated.Count == 2 && Close(truncated[1].Start, 20 - inset) && Close(truncated[1].End, 25 + inset));
        NearArea("truncated dash opaque area", Area(b, tint.A), (15 - 2 * feather) * (4 - feather), 1e-5);
        CheckMesh("truncated dash", b);
        foreach (var offset in new[] { 0.0, 5, 25, -15, -5 })
        {
            b.Reset();
            FlatGeom.DashedLine(b, 0, 0, 200, 0, 4, tint, new[] { 10.0, 10 }, offset, FlatCap.Butt, depth);
            var runs = DashRuns(b);
            var expectedCount = offset == -5 ? 11 : 10;
            var first = offset == 0 || offset == -5 ? -inset : 5 - inset;
            var last = offset == 0 ? 190 + inset : offset == -5 ? 200 + inset : 195 + inset;
            Check($"dash phase / count offset={offset}", runs.Count == expectedCount && Close(runs[0].Start, first) && Close(runs[runs.Count - 1].End, last));
            NearArea($"dash alpha-weighted duty offset={offset}", Area(b, tint.A, false), 400, 0.01);
            CheckMesh($"dashes offset={offset}", b);
        }
        b.Reset();
        FlatGeom.DashedLine(b, 3, 7, 123, 167, 4, tint, new[] { 10.0, 10 }, 5, FlatCap.Butt, depth);
        var diagonal = DashRuns(b, 3, 7, 0.6, 0.8);
        Check("dash follows diagonal path", diagonal.Count == 10 && Close(diagonal[0].Start, 5 - inset, 1e-5) && Close(diagonal[9].End, 195 + inset, 1e-5));
        CheckMesh("diagonal dashes", b);
        b.Reset();
        FlatGeom.DashedLine(b, 200, 0, 0, 0, 4, tint, new[] { 10.0, 10 }, 5, FlatCap.Butt, depth);
        var reverse = DashRuns(b, 200, 0, -1, 0);
        Check("dash follows reversed path", reverse.Count == 10 && Close(reverse[0].Start, 5 - inset) && Close(reverse[9].End, 195 + inset));
        b.Reset();
        FlatGeom.DashedLine(b, 0, 0, 28, 0, 4, tint, new[] { 8.0, 4, 2 }, 0, FlatCap.Butt, depth);
        var odd = DashRuns(b);
        Check("odd dash repeats with alternating parity", odd.Count == 3 && Close(odd[0].End, 8 + inset) &&
            Close(odd[1].Start, 12 - inset) && Close(odd[1].End, 14 + inset) && Close(odd[2].Start, 22 - inset) && Close(odd[2].End, 26 + inset));
        b.Reset();
        FlatGeom.DashedLine(b, 0, 0, 40, 0, 4, tint, new[] { 10.0 }, 0, FlatCap.Butt, depth);
        Check("single-value dash repeats twice", DashRuns(b).Count == 2);
        foreach (var pattern in new[] { Array.Empty<double>(), new[] { 0.0, 0 } })
        {
            b.Reset();
            FlatGeom.DashedLine(b, 0, 0, 25, 0, 4, tint, pattern, 0, FlatCap.Butt, depth);
            Check("empty / zero dash is solid", DashRuns(b).Count == 1);
            NearArea("empty / zero dash opaque area", Area(b, tint.A), (25 - feather) * (4 - feather), 1e-5);
        }
        b.Reset();
        FlatGeom.DashedLine(b, 0, 0, 30, 0, 4, tint, new[] { 5.0, 0, 5, 10 }, 0, FlatCap.Butt, depth);
        Check("zero gap keeps touching runs continuous", DashRuns(b).Count == 2);
        NearArea("zero-gap dash has no internal cap feather", Area(b, tint.A), 2 * (10 - feather) * (4 - feather), 1e-5);
        b.Reset();
        FlatGeom.DashedLine(b, 0, 0, 25, 0, 4, tint, new[] { 10.0, 0 }, 0, FlatCap.Butt, depth);
        Check("all-zero gaps merge into one line", b.VertCount == 8 && b.Tris.Count / 3 == 10);
        NearArea("merged zero-gap opaque area", Area(b, tint.A), (25 - feather) * (4 - feather), 1e-5);
        b.Reset();
        FlatGeom.DashedLine(b, 0, 0, 30, 0, 4, tint, new[] { 0.0, 10 }, 0, FlatCap.Butt, depth);
        Check("zero-length butt dashes emit no triangles", b.Tris.Count == 0);

        foreach (var points in new[] { diamond, new[] { -20.0, 0, 0, 20, 20, 0, 0, -20 },
            new[] { 0.0, -20, 20, 0, 0, 20, -20, 0, 0, -20 }, new[] { 0.0, -20, 0, -20, 20, 0, 0, 20, -20, 0 } })
        {
            b.Reset();
            FlatGeom.PolyFill(b, points, tint, depth);
            // Fills are hard-edged: one contour, full alpha to the geometric edge,
            // fan topology — canvas AA is only reproduced on strokes.
            NearArea("convex fill opaque area / winding / duplicates", Area(b, tint.A), 2 * 20 * 20, 1e-5);
            Check("convex fill reaches the geometric edge", HasPoint(b, 0, -20, tint.A) && !HasPoint(b, 0, -outerTipFeathered(), 0));
            Check("convex fill single contour topology", b.VertCount == 4 && b.Tris.Count / 3 == 2);
            CheckMesh("convex fill", b, true, false);
        }

        // Collapsed cores keep finite nonempty ramps and reduced coverage instead of
        // negative radii, inverted quads, or silently dropping subpixel primitives.
        foreach (var radius in new[] { 0.01, inset, inset + 0.01 })
        {
            b.Reset();
            FlatGeom.Disk(b, 0, 0, radius, tint, depth);
            CheckMesh($"tiny disk r={radius}", b, radius > inset);
            Check($"tiny disk remains visible r={radius}", Area(b, tint.A, false) > 0);
        }
        foreach (var width in new[] { 0.01, 0.2, feather, feather + 0.01 })
        {
            b.Reset();
            FlatGeom.Ring(b, 0, 0, 40, width, tint, depth);
            CheckMesh($"hairline ring w={width}", b, width >= feather);
            Check($"hairline ring ramps w={width}", Area(b, tint.A, false) > 0 && (width > feather || Area(b, tint.A) == 0));
            foreach (var cap in new[] { FlatCap.Butt, FlatCap.Round, FlatCap.Square })
            {
                b.Reset();
                FlatGeom.Line(b, 0, 0, 20, 0, width, tint, cap, depth);
                CheckMesh($"hairline {cap} line w={width}", b, width >= feather);
                Check($"hairline {cap} line remains visible w={width}", Area(b, tint.A, false) > 0);
            }
        }
        foreach (var cap in new[] { FlatCap.Butt, FlatCap.Round, FlatCap.Square })
        {
            b.Reset();
            FlatGeom.DashedLine(b, 0, 0, 0.2, 0, 4, tint, new[] { 0.1, 1.0 }, 0, cap, depth);
            CheckMesh($"sub-feather {cap} dash", b, cap != FlatCap.Butt);
            Check($"sub-feather {cap} dash remains visible", Area(b, tint.A, false) > 0);
        }
        b.Reset();
        FlatGeom.RoundedRect(b, 0, 0, 0.1, 3, 0.05, tint, depth);
        CheckMesh("thin rounded rect", b, false);
        b.Reset();
        FlatGeom.PolyFill(b, new[] { 0.0, 0, 0.1, 0, 0.1, 3, 0, 3 }, tint, depth);
        CheckMesh("thin convex fill", b, true, false);
        b.Reset();
        FlatGeom.Arc(b, 0, 0, 40, 0, 0.001, 0.2, tint, false, depth);
        CheckMesh("short hairline butt arc", b, false);
        foreach (var join in new[] { FlatJoin.Miter, FlatJoin.Bevel, FlatJoin.Round })
        {
            b.Reset();
            FlatGeom.PolyStroke(b, new[] { 0.0, 0, 20, 0, 20, 20 }, false, 0.2, tint, join, FlatCap.Round, depth);
            CheckMesh($"hairline {join} stroke", b, false);
        }

        b.Reset();
        FlatGeom.Arc(b, 0, 0, 40, 0, 0.001, 4, tint, false, depth);
        CheckMesh("sub-feather wide butt arc", b, false);
        Check("sub-feather butt arc collapses its plateau without folded strips", Area(b, (float) (tint.A * 0.04 / feather)) < 1e-6 &&
            Area(b, tint.A, false) > 0);
        foreach (var width in new[] { 6.8, 100.0 })
        {
            b.Reset();
            FlatGeom.Line(b, 0, 0, 200, 0, width, tint, FlatCap.Round, depth);
            CheckSagitta($"start round cap w={width}", b, 0, 0, width / 2 + inset, Math.PI, Math.PI);
            CheckSagitta($"end round cap w={width}", b, 200, 0, width / 2 + inset, 0, Math.PI);
        }
        b.Reset();
        FlatGeom.Disk(b, 0, 0, 9, new Rgba(tint.R, tint.G, tint.B, 0), depth);
        var transparent = b.Tris.Count > 0;
        for (var i = 0; i < b.VertCount; i++) transparent &= b.Verts[i * 7 + 6] == 0;
        Check("transparent caller preserves geometry with zero alpha everywhere", transparent && TriArea(b) > 0);

        // Integration: a renderer scales every vertex alpha after accumulating parts.
        b.Reset();
        FlatGeom.Disk(b, 0, 0, 9, tint, depth);
        var firstDiskVertices = b.VertCount;
        FlatGeom.Disk(b, 30, 0, 9, tint, depth);
        var clearBefore = 0;
        var opacityCorrect = b.VertCount == firstDiskVertices * 2;
        for (var i = 0; i < b.VertCount; i++)
        {
            var alpha = b.Verts[i * 7 + 6];
            b.Verts[i * 7 + 6] *= 0.37f;
            if (alpha == 0)
            {
                clearBefore++;
                opacityCorrect &= b.Verts[i * 7 + 6] == 0;
            }
            else opacityCorrect &= Close(b.Verts[i * 7 + 6], tint.A * 0.37f, 1e-6);
        }
        Check("appended disks / caller opacity preserves zero feather alpha", opacityCorrect && clearBefore > 0);

        b.Reset();
        FlatGeom.Disk(b, 0, 0, 0, white);
        FlatGeom.Ring(b, 0, 0, 40, 0, white);
        FlatGeom.Arc(b, 0, 0, 40, 1, 1, 6, white, true);
        FlatGeom.RoundedRect(b, 0, 0, 0, 20, 4, white);
        FlatGeom.Line(b, 0, 0, 0, 0, 4, white, FlatCap.Butt);
        FlatGeom.DashedLine(b, 0, 0, 0, 0, 4, white, new[] { 10.0, 10 }, 5, FlatCap.Butt);
        FlatGeom.PolyStroke(b, new[] { 0.0, 0, 0, 0 }, false, 4, white, FlatJoin.Miter, FlatCap.Butt);
        FlatGeom.PolyFill(b, new[] { 0.0, 0, 1, 1 }, white);
        Check("zero-sized inputs emit no geometry", b.VertCount == 0 && b.Tris.Count == 0);
        var firstVertex = b.Vert(1, 2, depth, tint);
        var secondVertex = b.Vert(3, 4, depth, tint);
        var thirdVertex = b.Vert(5, 6, depth, tint);
        var fourthVertex = b.Vert(7, 8, depth, tint);
        b.Tri(firstVertex, secondVertex, thirdVertex);
        b.Quad(firstVertex, secondVertex, thirdVertex, fourthVertex);
        Check("builder stride / indices unchanged", firstVertex == 0 && secondVertex == 1 && thirdVertex == 2 && fourthVertex == 3 &&
            b.Verts.Count == 28 && Close(b.Verts[0], 1) && Close(b.Verts[1], 2) && Close(b.Verts[2], depth) &&
            Close(b.Verts[3], tint.R) && Close(b.Verts[4], tint.G) && Close(b.Verts[5], tint.B) && Close(b.Verts[6], tint.A));
        Check("builder append order unchanged", b.Tris.SequenceEqual(new[] { 0, 1, 2, 0, 1, 2, 0, 2, 3 }));
        var vertexCapacity = b.Verts.Capacity;
        var triangleCapacity = b.Tris.Capacity;
        b.Reset();
        Check("builder reset / capacity unchanged", b.VertCount == 0 && b.Tris.Count == 0 && b.Verts.Capacity == vertexCapacity && b.Tris.Capacity == triangleCapacity);
        Check("builder index restarts", b.Vert(0, 0, 0, white) == 0);
    }
    catch (NotImplementedException)
    {
        missing.Add("Flat/Mesh still has unimplemented stubs");
    }
}
else
{
    missing.Add("Flat/Mesh geometry (FlatGeom.cs not found — stage 1b not started)");
}

/// <summary>Density contract — keep in sync with FlatGeom.CircleSegments (sagitta ≤ 0.1 design px).</summary>
int ExpectedSegments(double r)
{
    var tol = Math.Min(1.0, 0.1 / Math.Max(r, 1e-9));
    return Math.Max(8, (int) Math.Ceiling(Math.PI / Math.Acos(1 - tol)));
}

/// <summary>Where the old feathered fill contour used to sit — must no longer exist.</summary>
double outerTipFeathered() => 20 + Math.Sqrt(2) * 0.25;

double TriArea(FlatMeshBuilder b)
{
    var v = b.Verts;
    double area = 0;
    for (var i = 0; i < b.Tris.Count; i += 3)
    {
        var (ax, ay) = (v[b.Tris[i] * 7], v[b.Tris[i] * 7 + 1]);
        var (bx, by) = (v[b.Tris[i + 1] * 7], v[b.Tris[i + 1] * 7 + 1]);
        var (cx, cy) = (v[b.Tris[i + 2] * 7], v[b.Tris[i + 2] * 7 + 1]);
        area += Math.Abs((bx - ax) * (cy - ay) - (cx - ax) * (by - ay)) / 2;
    }
    return area;
}

// Exit timeline parity (independent of the unchanged stage 1a/1b sections).
if (goldenDoc != null)
{
    var exitGolden = goldenDoc.RootElement;
foreach (var c in exitGolden.GetProperty("exitAlpha").EnumerateArray())
{
    var kind = c.GetProperty("kind").GetString();
    var curve = kind switch
    {
        "tap" => FlatTokens.Time.Exit.Tap,
        "holdHead" => FlatTokens.Time.Exit.HoldHead,
        "holdBody" => FlatTokens.Time.Exit.HoldBody,
        "dragChild" => FlatTokens.Time.Exit.DragChild,
        _ => throw new InvalidOperationException($"Unknown exit curve: {kind}"),
    };
    var t = c.GetProperty("t").GetDouble();
    Check($"exit alpha {kind} t={t}", Close(FlatEase.ExitAlpha(t, curve), c.GetProperty("v").GetDouble(), 1e-12));
}
foreach (var c in exitGolden.GetProperty("exitRewind").EnumerateArray())
{
    var t = c.GetProperty("t").GetDouble();
    var approach = c.GetProperty("approach").GetDouble();
    Check($"exit rewind t={t} approach={approach}",
        Close(FlatEase.ExitRewind(t, approach), c.GetProperty("v").GetDouble(), 1e-12));
}

}

// Self-contained sanity checks — invariants and round-trips, no golden needed.
// Bit-faithfulness to the TS still requires the golden replay above.
if (File.Exists("Assets/Scripts/Game/Notes/Flat/Palette/FlatColor.cs"))
{
    Check("sanity: parse/tohex roundtrip", HexClose("#35A7FF", FlatColor.ToHex(FlatColor.ParseHex("#35A7FF"))));
    Check("sanity: hex parse ignores alpha digits", HexClose("#35A7FF80", FlatColor.ToHex(FlatColor.ParseHex("#35A7FF80"))));
    var ok = FlatColor.RgbToOklch(FlatColor.ParseHex("#35A7FF"));
    var okBack = FlatColor.RgbToOklch(FlatColor.OklchToRgb(ok));
    Check("sanity: oklch roundtrip", Close(ok.L, okBack.L, 1e-3) && Close(ok.C, okBack.C, 1e-3) && Math.Abs(((okBack.H - ok.H + 540) % 360) - 180) < 1e-3);
    var clickUp = FlatColor.RgbToOklch(FlatPaletteDerive.FromHue(247).Shade(400));
    Check("sanity: derive keeps hue at shade 400", Math.Abs(((clickUp.H - 247 + 540) % 360) - 180) < 0.5 && clickUp.L is > 0.70 and < 0.84);
    var p247 = FlatPaletteDerive.FromHue(247);
    Check("sanity: tone clamps to endpoints",
        FlatColor.ToHex(p247.Tone(-5)) == FlatColor.ToHex(p247.Shade(50)) &&
        FlatColor.ToHex(p247.Tone(2000)) == FlatColor.ToHex(p247.Shade(950)));
    Check("sanity: lerp endpoints", FlatEase.Lerp(3, 7, 0) == 3 && FlatEase.Lerp(3, 7, 1) == 7);
    Check("sanity: judgement cap height ratio", Close(FlatJudgement.CapHeight(10), 1.2, 1e-9) && Close(FlatTiming.CapHeight(true, 10), 0.72, 1e-9));
    Check("sanity: timing grade semantics", FlatTiming.ShowsTiming("great") && !FlatTiming.ShowsTiming("miss") &&
        FlatTiming.TimingOf(-0.02) == "fast" && FlatTiming.TimingOf(0.02) == "slow");
    Check("sanity: measure text positive", FlatText.MeasureText("PERFECT", 10) > 0);
}

Console.WriteLine($"\n{pass} passed, {fail} failed{(missing.Count > 0 ? $", pending: {string.Join("; ", missing)}" : "")}");
return fail == 0 && missing.Count == 0 ? 0 : 1;
