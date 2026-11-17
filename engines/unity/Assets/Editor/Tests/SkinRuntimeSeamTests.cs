// Focused tests for the skin runtime's Unity seam: steady-state allocation,
// context reuse, fallback contracts and pool behaviour. These exercise the
// same call sequence as FlatNoteRendererBase.DrawSkinHead (FillHeadContext →
// SkinPainter.DrawNoteSlot → FlatNoteMesh fused upload) minus the Note/Game
// plumbing, which itself performs no per-frame allocation once cached.

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

public class SkinRuntimeSeamTests
{
    private GameObject meshRoot;
    private FlatNoteMesh mesh;
    private FlatMeshBuilder builder;

    [SetUp]
    public void SetUp()
    {
        CompileBuiltInSkin();
        meshRoot = new GameObject("SeamMesh");
        mesh = FlatNoteMesh.Create(meshRoot.transform, "Head", false, 0);
        builder = new FlatMeshBuilder();
    }

    [TearDown]
    public void TearDown()
    {
        // Edit mode: FlatNoteMesh.Dispose calls runtime Destroy — destroy the
        // GameObject tree instead (the orphan Mesh object is editor-GC'd).
        if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh.gameObject);
        UnityEngine.Object.DestroyImmediate(meshRoot);
    }

    internal static void CompileBuiltInSkin()
    {
        FlatSkinRuntime.WarmUp();
        // WarmUp compiles on a worker; seam tests need it now.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!FlatSkinRuntime.Ready && DateTime.UtcNow < deadline) Thread.Sleep(10);
        Assert.IsTrue(FlatSkinRuntime.Ready, "built-in skin must compile for seam tests");
    }

    internal static SkinPaletteContext SeamPalettes(CompiledSkin skin)
    {
        return new SkinPaletteContext
        {
            Note = DefaultSkinPalettes.Family(FlatFamily.Click, FlatDirection.Up),
            Grades = new SkinGradePalettes(DefaultSkinPalettes.GradeColors),
            Family = (family, direction) => DefaultSkinPalettes.Family(family, direction),
        };
    }

    internal static SkinDrawContext MakeSeamContext(CompiledSkin skin)
    {
        var context = new SkinDrawContext();
        FlatSkinRuntime.FillHeadContext(context, NoteKind.Click, SeamPalettes(skin),
            alternativeColor: false, approach: 1.2);
        return context;
    }

    /// <summary>The production hot path (context fill + paint + fused upload) must not
    /// allocate once warm — this is the seam the renderer calls every frame.</summary>
    [Test]
    public void PaintAndUploadAreSteadyStateZeroAllocation()
    {
        var skin = FlatSkinRuntime.Current;
        var slot = skin.Slot(NoteKind.Click, "enter");
        Assume.That(slot, Is.Not.Null);
        var context = MakeSeamContext(skin);
        var painter = new SkinPainter();
        var sinks = new SkinSinks(builder);
        var opacity = 1f;

        void Frame(double driver)
        {
            // Same sequence as DrawSkinHead + the fused upload in Render().
            builder.Reset();
            painter.DrawNoteSlot(slot, driver, context, sinks);
            mesh.Upload(builder, opacity, opacity, builder.VertCount);
        }

        for (var i = 0; i <= 60; i++) Frame(i / 60.0); // warm-up: pools, caches, mesh lists

        var gcBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var loop = 0; loop < 10; loop++)
        {
            for (var i = 0; i <= 60; i++) Frame(i / 60.0);
            var exit = skin.Slot(NoteKind.Click, "exit");
            for (var i = 0; i <= 30; i++)
            {
                builder.Reset();
                painter.DrawNoteSlot(exit, i / 100.0, context, sinks);
                mesh.Upload(builder, opacity, opacity, builder.VertCount);
            }
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - gcBefore;
        Assert.AreEqual(0L, allocated, $"seam allocated {allocated} bytes across 910 frames after warm-up");
    }

    /// <summary>Steady-driver skip precondition: the same (slot, driver, context) must
    /// produce byte-identical geometry, so the renderer may skip the rebuild.</summary>
    [Test]
    public void SameDriverProducesIdenticalGeometry()
    {
        var skin = FlatSkinRuntime.Current;
        var context = MakeSeamContext(skin);
        var painter = new SkinPainter();
        var a = new FlatMeshBuilder();
        var b = new FlatMeshBuilder();
        foreach (var driver in new[] { 0.0, 0.37, 1.0 })
        {
            a.Reset();
            b.Reset();
            painter.DrawNoteSlot(skin.Slot(NoteKind.Click, "enter"), driver, context, new SkinSinks(a));
            painter.DrawNoteSlot(skin.Slot(NoteKind.Click, "enter"), driver, context, new SkinSinks(b));
            Assert.AreEqual(a.Verts.Count, b.Verts.Count, $"vertex count at p={driver}");
            for (var i = 0; i < a.Verts.Count; i++)
                Assert.AreEqual(a.Verts[i], b.Verts[i], $"vertex {i} at p={driver}");
            CollectionAssert.AreEqual(a.Tris, b.Tris, $"triangles at p={driver}");
        }
    }

    /// <summary>Fallback contract: a slot whose dynamic key times resolve out of order
    /// throws on every draw (deterministic), so the renderer's catch + once-only
    /// skinBroken gate is required and sufficient — retries never succeed silently.</summary>
    [Test]
    public void PoisonedExitSlotThrowsDeterministically()
    {
        // click:exit with a key at {input: exitRewindEnd}; with a long approach the
        // resolved time lands outside 0..1 → the runtime validation throws at draw.
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        var poison = Newtonsoft.Json.Linq.JObject.Parse(@"
{
  ""type"": ""layers"",
  ""tracks"": { ""fade"": { ""keys"": [
    { ""at"": 0, ""value"": 1 },
    { ""at"": { ""input"": ""exitRewindEnd"" }, ""value"": 0.5 },
    { ""at"": 1, ""value"": 0 } ] } },
  ""properties"": { ""opacity"": { ""track"": ""fade"" } },
  ""layers"": [ { ""source"": { ""frame"": ""frame1"" } } ]
}");
        document["animations"]["poison"] = poison;
        document["slots"]["click:exit"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = "poison" };
        var skin = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), FlatSkinRuntime.Current);
        var slot = skin.Slot(NoteKind.Click, "exit");
        Assert.NotNull(slot);
        var context = MakeSeamContext(skin);
        context.Approach = 0.1; // exitRewindEnd = 1 - 0.3/0.1 < 0 → resolved key time outside 0..1
        var painter = new SkinPainter();
        var builder = new FlatMeshBuilder();
        Assert.Throws<SkinRenderException>(() => painter.DrawNoteSlot(slot, 0.05, context, new SkinSinks(builder)));
        Assert.Throws<SkinRenderException>(() => painter.DrawNoteSlot(slot, 0.05, context, new SkinSinks(builder)));
    }

    /// <summary>Image frames now compile (this used to assert rejection). Compile must
    /// collect the referenced image into skin.Images, drop the capability gate, and
    /// the painter must emit one white quad into the dedicated image sink with UVs
    /// mapped for Unity's y-up texture space: design-space top-left samples
    /// (u0, v1), bottom-right (u1, v0).</summary>
    [Test]
    public void ImageFramePaintsQuadWithYUpUvMapping()
    {
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["frames"]["img"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "image",
            ["src"] = "assets-baked/__test.png",
            ["x"] = -10.0, ["y"] = -5.0, ["w"] = 20.0, ["h"] = 10.0,
        };
        document["animations"]["imgAnim"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "layers",
            ["layers"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
            {
                ["source"] = new Newtonsoft.Json.Linq.JObject { ["frame"] = "img" },
            }),
        };
        document["slots"]["click:enter"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = "imgAnim" };
        // 4×4 opaque red PNG — bytes go straight into the compile stash; the UV
        // contract needs no atlas, so the packed rect is authored by hand.
        var png = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 0, 0, 255);
        png.SetPixels32(pixels);
        png.Apply();
        var bytes = png.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(png);
        var skin = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), FlatSkinRuntime.Current,
            new Dictionary<string, byte[]> { ["assets-baked/__test.png"] = bytes });
        var image = skin.ImageOf("assets-baked/__test.png");
        Assert.IsNotNull(image);
        CollectionAssert.AreEqual(bytes, image.Bytes, "compile must attach the provider's bytes");
        var slot = skin.Slot(NoteKind.Click, "enter");
        Assert.IsFalse(slot.UsesUnsupported, "images are no longer capability-gated");
        image.UvRect = new Rect(0.25f, 0.5f, 0.5f, 0.25f); // (u0, v0) = (0.25, 0.5); (u1, v1) = (0.75, 0.75)

        var painter = new SkinPainter();
        var imageSink = new FlatMeshBuilder();
        painter.DrawNoteSlot(slot, 0.5, MakeSeamContext(skin), new SkinSinks(builder, null, imageSink));

        Assert.IsTrue(painter.UsedImages, "the draw must report image usage");
        Assert.AreEqual(4, imageSink.VertCount, "one quad per image frame");
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 2, 3 }, imageSink.Tris, "quad triangulation");
        Assert.AreEqual(8, painter.LastImageUv.Count, "2 floats per vertex (4 verts × 2)");
        // Vertex order TL, TR, BR, BL; top edge samples v1, bottom edge v0.
        var expectedUv = new[] { 0.25f, 0.75f, 0.75f, 0.75f, 0.75f, 0.5f, 0.25f, 0.5f };
        for (var i = 0; i < expectedUv.Length; i++)
            Assert.AreEqual(expectedUv[i], painter.LastImageUv[i], $"uv[{i}]");
        // Identity root matrix: the design rect corners land untransformed
        // (vertex order TL, TR, BR, BL; stride-7 flat layout).
        var expectedCorners = new[] { (-10f, -5f), (10f, -5f), (10f, 5f), (-10f, 5f) };
        for (var v = 0; v < 4; v++)
        {
            Assert.AreEqual(expectedCorners[v].Item1, imageSink.Verts[v * 7], $"vertex {v} x");
            Assert.AreEqual(expectedCorners[v].Item2, imageSink.Verts[v * 7 + 1], $"vertex {v} y");
        }
        // White tint with full straight alpha (the slot sets no opacity).
        Assert.AreEqual(1f, imageSink.Verts[3], "r");
        Assert.AreEqual(1f, imageSink.Verts[4], "g");
        Assert.AreEqual(1f, imageSink.Verts[5], "b");
        Assert.AreEqual(1f, imageSink.Verts[6], "a");
    }

    /// <summary>Registry contract update (this used to assert rejection): a skin with
    /// images now loads via TryLoad, swapping the registry and building its atlas.
    /// The data-URI src exercises the worker-side decode path end to end.</summary>
    [Test]
    public void ImageSkinLoadsAndSwapsRegistry()
    {
        var png = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var green = new Color32(0, 255, 0, 255);
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = green;
        png.SetPixels32(pixels);
        png.Apply();
        var dataUri = "data:image/png;base64," + Convert.ToBase64String(png.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(png);
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["frames"]["img"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "image",
            ["src"] = dataUri,
            ["x"] = -8.0, ["y"] = -8.0, ["w"] = 16.0, ["h"] = 16.0,
        };
        document["animations"]["imgAnim"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "layers",
            ["layers"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
            {
                ["source"] = new Newtonsoft.Json.Linq.JObject { ["frame"] = "img" },
            }),
        };
        document["slots"]["click:enter"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = "imgAnim" };
        var before = FlatSkinRuntime.Current;
        Assert.IsTrue(FlatSkinRuntime.TryLoad(document.ToString()), "image skins must load");
        try
        {
            var skin = FlatSkinRuntime.Current;
            Assert.AreNotSame(before, skin, "registry must swap");
            Assert.AreEqual(4, skin.ImageOf(dataUri).Width, "atlas build must record the decoded size");
            Assert.IsNotNull(FlatSkinRuntime.AtlasOf(skin), "atlas page must be cached per skin");
            Assert.IsTrue(skin.ImageOf(dataUri).Packed, "the image must be packed");
        }
        finally
        {
            // Restore the original registry state for other tests (and drop the atlas).
            typeof(FlatSkinRuntime)
                .GetField("current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, before);
            FlatSkinRuntime.ClearCaches();
        }
    }

    /// <summary>Heads have no image sink, so a skin whose enter/exit slots emit image
    /// quads must gate heads to nothing drawn (CanPaintHead false) instead of
    /// dropping the head quads on a sink that cannot show them. The painter-level
    /// image path stays covered by ImageFramePaintsQuadWithYUpUvMapping; this pins
    /// the renderer-level gate.</summary>
    [Test]
    public void ImageHeadSlotsGateHeadsOff()
    {
        var png = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 255, 0, 255);
        png.SetPixels32(pixels);
        png.Apply();
        var dataUri = "data:image/png;base64," + Convert.ToBase64String(png.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(png);
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["frames"]["img"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "image",
            ["src"] = dataUri,
            ["x"] = -8.0, ["y"] = -8.0, ["w"] = 16.0, ["h"] = 16.0,
        };
        document["animations"]["imgAnim"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "layers",
            ["layers"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
            {
                ["source"] = new Newtonsoft.Json.Linq.JObject { ["frame"] = "img" },
            }),
        };
        document["slots"]["click:enter"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = "imgAnim" };
        // Compile-only: the flag must ride the slot without any engine involvement.
        var compiled = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), FlatSkinRuntime.Current);
        var enter = compiled.Slot(NoteKind.Click, "enter");
        Assert.IsTrue(enter.UsesImages, "compile must expose image usage on the slot");
        Assert.IsFalse(enter.UsesUnsupported, "images alone must not flip the global gate");

        var before = FlatSkinRuntime.Current;
        Assert.IsTrue(FlatSkinRuntime.TryLoad(document.ToString()), "image skins must still load");
        try
        {
            Assert.IsFalse(FlatSkinRuntime.CanPaintHead(NoteKind.Click),
                "image enter slots must gate heads to nothing drawn, not drop the quads");
            // Kind availability is unchanged (Supports is about slot completeness);
            // only the head/body gates consult UsesImages.
            Assert.IsTrue(FlatSkinRuntime.Supports(NoteKind.Click), "image skins still complete the kind");
        }
        finally
        {
            typeof(FlatSkinRuntime)
                .GetField("current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, before);
            FlatSkinRuntime.ClearCaches();
        }
    }

    /// <summary>Baked-skin integration (skip-if-missing): when the generated image
    /// skin ships under StreamingAssets it must load, compile with images, build an
    /// atlas and draw its feedback slots over the full clock with image vertices
    /// and no exceptions.</summary>
    [Test]
    public void BakedSkinWithImagesDrawsFeedback()
    {
        // The baked variant now lives in the cytoid-design repo (branch
        // bake-image-skin, packages/skin-flat/src). Same convention as the
        // FlatParity golden: absent checkout/branch → Ignore, never fail.
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CYTOID_DESIGN_DIR"),
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "cytoid-design")),
            "/tmp/cytoid-design",
        };
        var bakedPath = candidates
            .Where(dir => !string.IsNullOrEmpty(dir))
            .Select(dir => Path.Combine(dir, "packages", "skin-flat", "src", "flat-baked.skin.json"))
            .FirstOrDefault(File.Exists);
        if (bakedPath == null)
            Assert.Ignore("flat-baked.skin.json not found — clone cytoid-design (branch bake-image-skin) or set CYTOID_DESIGN_DIR");
        var before = FlatSkinRuntime.Current;
        Assert.IsTrue(FlatSkinRuntime.TryLoadFromFile(bakedPath), "baked skin must load");
        try
        {
            var skin = FlatSkinRuntime.Current;
            Assert.Greater(skin.Images.Count, 0, "baked skin must reference images");
            Assert.IsNotNull(FlatSkinRuntime.AtlasOf(skin), "atlas must build");
            var context = MakeSeamContext(skin);
            var painter = new SkinPainter();
            var imageSink = new FlatMeshBuilder();
            var maxImageVerts = 0;
            foreach (var grade in new[] { SkinGrade.Perfect, SkinGrade.Great, SkinGrade.Good, SkinGrade.Bad, SkinGrade.Miss })
            {
                var slot = skin.Feedback(NoteKind.Click, SkinFeedbackView.Judgement, grade, null);
                if (slot == null) continue;
                for (var t = 0d; t <= slot.Contract.Duration; t += slot.Contract.Duration / 16)
                {
                    builder.Reset();
                    imageSink.Reset();
                    Assert.DoesNotThrow(() =>
                        painter.DrawFeedbackSlot(slot, t, context, new SkinSinks(builder, null, imageSink)));
                    maxImageVerts = Math.Max(maxImageVerts, imageSink.VertCount);
                }
            }
            Assert.Greater(maxImageVerts, 0, "baked feedback must emit image vertices");
        }
        finally
        {
            typeof(FlatSkinRuntime)
                .GetField("current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, before);
            FlatSkinRuntime.ClearCaches();
        }
    }

    /// <summary>The hold renderer's per-frame slot queries ride SkinSlotCache:
    /// resolving with an unchanged skin must be allocation-free (CompiledSkin.Slot()
    /// interpolates a key string per call), and a skin swap must re-resolve to the
    /// new instance's slots — a whole-kind inheritance hands over the fallback's
    /// slot objects, so the reference swap is observable through them.</summary>
    [Test]
    public void HoldSlotCacheIsSteadyStateAllocationFreeAndReswaps()
    {
        var baseJson = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var fallback = SkinCompiler.Compile(SkinJson.Parse(baseJson));
        Assert.IsNotNull(fallback.Slot(NoteKind.Hold, "press"), "flat must ship hold press/loopUnder slots");

        // Variant: hold:press removed → the hold kind inherits wholesale from the
        // fallback, so the new skin's Press must become the fallback's slot object.
        var document = Newtonsoft.Json.Linq.JObject.Parse(baseJson);
        ((Newtonsoft.Json.Linq.JObject) document["slots"]).Remove("hold:press");
        var variant = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), fallback);

        var cache = new SkinSlotCache();
        cache.Resolve(fallback, NoteKind.Hold);
        Assert.AreSame(fallback.Slot(NoteKind.Hold, "press"), cache.Press);
        Assert.AreSame(fallback.Slot(NoteKind.Hold, "loopUnder"), cache.LoopUnder);
        Assert.AreSame(fallback.Slot(NoteKind.Hold, "progress"), cache.Progress);
        Assert.AreSame(fallback.Slot(NoteKind.Hold, "body"), cache.Body);

        var gcBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) cache.Resolve(fallback, NoteKind.Hold);
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - gcBefore,
            "steady-state Resolve must not allocate");

        cache.Resolve(variant, NoteKind.Hold);
        Assert.AreSame(fallback.Slot(NoteKind.Hold, "press"), cache.Press,
            "a skin swap must re-resolve — the inherited kind hands over the fallback's slots");
        cache.Resolve(null, NoteKind.Hold);
        Assert.IsNull(cache.Press, "a null skin clears the slots so callers fall back");
    }

    /// <summary>FlatClearFx splits clear and feedback across two image sinks (the
    /// burst must stay below the label regardless of medium). The painter must
    /// re-base its UV/quad lists whenever a draw begins against a freshly emptied
    /// image sink, so each sink uploads index-aligned with exactly its own role's
    /// quads — otherwise the second role would inherit the first's UV offsets.
    /// Uses two image enter slots on different kinds with one shared painter.</summary>
    [Test]
    public void PainterRebasesImageListsPerEmptyImageSink()
    {
        var png = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 255, 0, 255);
        png.SetPixels32(pixels);
        png.Apply();
        var dataUri = "data:image/png;base64," + Convert.ToBase64String(png.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(png);
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["frames"]["img"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "image",
            ["src"] = dataUri,
            ["x"] = -8.0, ["y"] = -8.0, ["w"] = 16.0, ["h"] = 16.0,
        };
        foreach (var kindKey in new[] { "click", "hold" })
        {
            document["animations"][kindKey + "ImgAnim"] = new Newtonsoft.Json.Linq.JObject
            {
                ["type"] = "layers",
                ["layers"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
                {
                    ["source"] = new Newtonsoft.Json.Linq.JObject { ["frame"] = "img" },
                }),
            };
            document["slots"][kindKey + ":enter"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = kindKey + "ImgAnim" };
        }
        var skin = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), FlatSkinRuntime.Current,
            new Dictionary<string, byte[]> { [dataUri] = null });
        var clickSlot = skin.Slot(NoteKind.Click, "enter");
        var holdSlot = skin.Slot(NoteKind.Hold, "enter");
        Assert.IsNotNull(clickSlot);
        Assert.IsNotNull(holdSlot);
        // Packed derives from the UV rect (U1 > U0) — a full-rect rect packs it.
        skin.ImageOf(dataUri).UvRect = new Rect(0f, 0f, 1f, 1f);

        var painter = new SkinPainter();
        var context = MakeSeamContext(skin);
        var vectorA = new FlatMeshBuilder();
        var imageSinkA = new FlatMeshBuilder();
        painter.DrawNoteSlot(clickSlot, 0.5, context, new SkinSinks(vectorA, null, imageSinkA));
        Assert.AreEqual(4, imageSinkA.VertCount, "first draw emits one quad");
        Assert.AreEqual(1, painter.LastImageQuads.Count, "first draw's painter list holds its own quad");

        // Second draw against a DIFFERENT, freshly emptied image sink: the painter
        // must re-base — the list now describes sink B only, and B's four vertices
        // map to the list's four uv pairs from index 0.
        var vectorB = new FlatMeshBuilder();
        var imageSinkB = new FlatMeshBuilder();
        painter.DrawNoteSlot(holdSlot, 0.5, context, new SkinSinks(vectorB, null, imageSinkB));
        Assert.AreEqual(4, imageSinkB.VertCount, "second draw emits one quad");
        Assert.AreEqual(1, painter.LastImageQuads.Count,
            "the list must re-base per empty sink, not accumulate across roles");
        Assert.AreEqual(8, painter.LastImageUv.Count, "uv pairs realign with sink B's four vertices");
    }

    /// <summary>All four FX vector meshes (feedback + clear trios) must follow the
    /// play-time world scale — the clear pair was missed once and rendered the
    /// design-pixel geometry at scale 1, massively oversizing the built-in burst.
    /// Drives the internal ApplyWorldScale directly: Play()'s Game plumbing is
    /// out of reach for edit-mode tests, but the scale fan-out is the contract.</summary>
    [Test]
    public void FxVectorMeshesFollowPlayWorldScale()
    {
        var go = new GameObject("FxClearScale");
        var fx = go.AddComponent<FlatClearFx>();
        // Initialize is internal to Assembly-CSharp; the edit suite drives it via
        // reflection (same as ApplyWorldScale below).
        typeof(FlatClearFx).GetMethod("Initialize",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance)?.Invoke(fx, null);
        try
        {
            var worldScaleField = typeof(FlatNoteMesh).GetField("worldScale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assume.That(worldScaleField, Is.Not.Null);
            var apply = typeof(FlatClearFx).GetMethod("ApplyWorldScale",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            Assume.That(apply, Is.Not.Null);
            FlatNoteMesh MeshOf(string name)
            {
                var field = typeof(FlatClearFx).GetField(name,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assume.That(field, Is.Not.Null);
                return (FlatNoteMesh) field.GetValue(fx);
            }

            apply.Invoke(fx, new object[] { 0.5f });
            foreach (var name in new[] { "normalMesh", "additiveMesh", "clearNormalMesh", "clearAdditiveMesh" })
                Assert.AreEqual(0.5f, (float) worldScaleField.GetValue(MeshOf(name)),
                    $"{name} must follow the play scale");

            // Pool reuse: a second play at another zoom must update every mesh again.
            apply.Invoke(fx, new object[] { 0.25f });
            foreach (var name in new[] { "normalMesh", "additiveMesh", "clearNormalMesh", "clearAdditiveMesh" })
                Assert.AreEqual(0.25f, (float) worldScaleField.GetValue(MeshOf(name)),
                    $"{name} must not keep a stale scale");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    /// <summary>Group clips (rect, the hold-body shape) constrain the emitted geometry:
    /// a child rect far larger than the clip must come out entirely inside it.</summary>
    [Test]
    public void RectClipConstrainsEmittedGeometry()
    {
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["frames"]["clipChild"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "rect",
            ["x"] = -1000.0, ["y"] = -1000.0, ["w"] = 2000.0, ["h"] = 2000.0,
            ["fill"] = "#FFFFFF",
        };
        document["frames"]["clipGroup"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "group",
            ["clip"] = new Newtonsoft.Json.Linq.JObject
            {
                ["type"] = "rect",
                ["x"] = -10.0, ["y"] = -5.0, ["w"] = 20.0, ["h"] = 10.0,
            },
            ["children"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
            {
                ["type"] = "rect",
                ["x"] = -1000.0, ["y"] = -1000.0, ["w"] = 2000.0, ["h"] = 2000.0,
                ["fill"] = "#FFFFFF",
            }),
        };
        document["animations"]["clipAnim"] = new Newtonsoft.Json.Linq.JObject
        {
            ["type"] = "layers",
            ["layers"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
            {
                ["source"] = new Newtonsoft.Json.Linq.JObject { ["frame"] = "clipGroup" },
            }),
        };
        document["slots"]["click:enter"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = "clipAnim" };
        var skin = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), FlatSkinRuntime.Current);
        var slot = skin.Slot(NoteKind.Click, "enter");
        Assert.IsFalse(slot.UsesUnsupported, "clips are no longer capability-gated");
        var context = MakeSeamContext(skin);
        var builder = new FlatMeshBuilder();
        new SkinPainter().DrawNoteSlot(slot, 0.5, context, new SkinSinks(builder));
        Assert.Greater(builder.VertCount, 0, "clipped geometry must still emit");
        for (var i = 0; i < builder.VertCount; i++)
        {
            Assert.GreaterOrEqual(builder.Verts[i * 7], -10.0001, $"vertex {i} x");
            Assert.LessOrEqual(builder.Verts[i * 7], 10.0001, $"vertex {i} x");
            Assert.GreaterOrEqual(builder.Verts[i * 7 + 1], -5.0001, $"vertex {i} y");
            Assert.LessOrEqual(builder.Verts[i * 7 + 1], 5.0001, $"vertex {i} y");
        }
    }

    /// <summary>Context reuse: FillHeadContext overwrites every field (no stale state
    /// between pooled notes) and does not allocate.</summary>
    [Test]
    public void FillHeadContextOverwritesWithoutAllocation()
    {
        var skin = FlatSkinRuntime.Current;
        var context = new SkinDrawContext();
        var flickPalettes = new SkinPaletteContext
        {
            Note = DefaultSkinPalettes.Family(FlatFamily.Flick, FlatDirection.Up),
            Grades = new SkinGradePalettes(DefaultSkinPalettes.GradeColors),
            Family = (family, direction) => DefaultSkinPalettes.Family(family, direction),
        };
        FlatSkinRuntime.FillHeadContext(context, NoteKind.Flick, flickPalettes,
            alternativeColor: true, approach: 3.0);
        var first = (Kind: context.Kind, Alt: context.Direction, Approach: context.Approach, Size: context.Size);

        var clickPalettes = new SkinPaletteContext
        {
            Note = DefaultSkinPalettes.Family(FlatFamily.Click, FlatDirection.Up),
            Grades = new SkinGradePalettes(DefaultSkinPalettes.GradeColors),
            Family = (family, direction) => DefaultSkinPalettes.Family(family, direction),
        };
        var gcBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
            FlatSkinRuntime.FillHeadContext(context, NoteKind.Click, clickPalettes, false, 1.2);
        Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - gcBefore, "FillHeadContext must not allocate");

        Assert.AreEqual(NoteKind.Click, context.Kind);
        Assert.AreEqual(FlatDirection.Up, context.Direction);
        Assert.AreEqual(1.2, context.Approach);
        Assert.AreEqual(128 * FlatTokens.Size(NoteKind.Click), context.Size);
        Assert.AreNotEqual(first.Kind, context.Kind, "previous note's kind must be overwritten");
    }

    /// <summary>Painter survives a hot skin swap (registry replacement) without stale
    /// per-painter state — the registry swap pattern used by TryLoad.</summary>
    [Test]
    public void PainterSurvivesSkinSwap()
    {
        var original = FlatSkinRuntime.Current;
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        Assert.IsTrue(FlatSkinRuntime.TryLoad(json), "identical skin must reload cleanly");
        try
        {
            var skin = FlatSkinRuntime.Current;
            Assert.AreNotSame(original, skin);
            var context = MakeSeamContext(skin);
            var painter = new SkinPainter();
            var builder = new FlatMeshBuilder();
            painter.DrawNoteSlot(skin.Slot(NoteKind.Click, "enter"), 0.5, context, new SkinSinks(builder));
            Assert.Greater(builder.VertCount, 0, "painter must draw with the swapped skin");
        }
        finally
        {
            // Restore the original registry state for other tests.
            typeof(FlatSkinRuntime)
                .GetField("current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, original);
        }
    }

    /// <summary>F3: skins inheriting image slots from their fallback keep the
    /// fallback's atlas alive — inherited slot objects keep Animation.Owner on the
    /// fallback skin, so a swapped-out (or cached-out) fallback must not lose its
    /// pages while the current skin still references them.</summary>
    [Test]
    public void InheritedImageSlotsKeepFallbackAtlasAlive()
    {
        CompileBuiltInSkin();
        var png = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 255, 0, 255);
        png.SetPixels32(pixels);
        png.Apply();
        var dataUri = "data:image/png;base64," + Convert.ToBase64String(png.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(png);
        var baseJson = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));

        Newtonsoft.Json.Linq.JObject ImageSkinDoc()
        {
            var document = Newtonsoft.Json.Linq.JObject.Parse(baseJson);
            document["frames"]["img"] = new Newtonsoft.Json.Linq.JObject
            {
                ["type"] = "image", ["src"] = dataUri,
                ["x"] = -8.0, ["y"] = -8.0, ["w"] = 16.0, ["h"] = 16.0,
            };
            document["animations"]["imgAnim"] = new Newtonsoft.Json.Linq.JObject
            {
                ["type"] = "layers",
                ["layers"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject
                {
                    ["source"] = new Newtonsoft.Json.Linq.JObject { ["frame"] = "img" },
                }),
            };
            document["slots"]["click:enter"] = new Newtonsoft.Json.Linq.JObject { ["animation"] = "imgAnim" };
            return document;
        }

        var restore = FlatSkinRuntime.Current;
        try
        {
            // A: image slot on click:enter. B: inherits everything from A.
            Assert.IsTrue(FlatSkinRuntime.TryLoad(ImageSkinDoc().ToString()), "skin A must load");
            var a = FlatSkinRuntime.Current;
            var aAtlas = FlatSkinRuntime.AtlasOf(a);
            Assert.IsNotNull(aAtlas, "A must have an atlas");

            var bDoc = new Newtonsoft.Json.Linq.JObject
            {
                ["schemaVersion"] = 2, ["id"] = "b", ["name"] = "B",
                ["frames"] = new Newtonsoft.Json.Linq.JObject(),
                ["animations"] = new Newtonsoft.Json.Linq.JObject(),
                ["slots"] = new Newtonsoft.Json.Linq.JObject(),
            };
            Assert.IsTrue(FlatSkinRuntime.TryLoad(bDoc.ToString()), "skin B must load via fallback inheritance");
            var b = FlatSkinRuntime.Current;
            var inherited = b.Slot(NoteKind.Click, "enter");
            Assert.IsNotNull(inherited, "click:enter inherited from A");
            Assert.AreSame(a, inherited.Animation.Owner, "inherited slot keeps the fallback owner");
            Assert.IsTrue(aAtlas, "A's atlas must stay alive after the swap");
            // ClearCaches (new game) must keep the whole current lineage.
            FlatSkinRuntime.ClearCaches();
            Assert.IsTrue(aAtlas, "A's atlas survives ClearCaches while B's lineage references it");
        }
        finally
        {
            typeof(FlatSkinRuntime).GetField("current", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, restore);
        }
    }

    /// <summary>F4: textured uploads are allocation-free once the UV buffer is warm
    /// (FlatClearFx reuses its per-instance buffer; the mesh path must not allocate
    /// per call either).</summary>
    [Test]
    public void TexturedUploadIsSteadyStateZeroAllocation()
    {
        var root = new GameObject("UvAllocRoot");
        var mesh = FlatNoteMesh.Create(root.transform, "Head", false, 0);
        var builder = new FlatMeshBuilder();
        builder.Vert(0, 0, 0, new Rgba(1, 1, 1, 1));
        builder.Vert(16, 0, 0, new Rgba(1, 1, 1, 1));
        builder.Vert(0, 16, 0, new Rgba(1, 1, 1, 1));
        builder.Tri(0, 1, 2);
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var uv = new Vector2[builder.VertCount];
        try
        {
            mesh.UploadTextured(builder, uv, texture, 1f);
            mesh.UploadTextured(builder, uv, texture, 1f);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++) mesh.UploadTextured(builder, uv, texture, 1f);
            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before,
                "UploadTextured must not allocate once warm (FlatClearFx reuses its UV buffer)");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(mesh.gameObject);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>Feedback-mode selection must mirror the TS renderFeedback contract:
    /// timed grades with a judged timing get Combined/Timing per the judgement-text
    /// setting; PERFECT and MISS never carry the label (judgement-only when the
    /// text is on); absent timing degrades to judgement-only; both settings off →
    /// no feedback slot. Pure function — no Unity objects involved.</summary>
    [Test]
    public void FeedbackSelectionMatchesRenderFeedbackContract()
    {
        // PERFECT and MISS are judgement-only regardless of the timing flags.
        Assert.AreEqual((SkinFeedbackView.Judgement, SkinGrade.Perfect, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(true, true, true, SkinGrade.Perfect));
        Assert.AreEqual((SkinFeedbackView.Judgement, SkinGrade.Miss, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(true, true, true, SkinGrade.Miss));
        Assert.AreEqual((SkinFeedbackView.Judgement, SkinGrade.Perfect, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(true, false, false, SkinGrade.Perfect));
        // Timed grade with both settings on → combined view (timing binds at lookup).
        Assert.AreEqual((SkinFeedbackView.Combined, SkinGrade.Great, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(true, true, true, SkinGrade.Great));
        // Timed grade with the judgement text off → timing-only.
        Assert.AreEqual((SkinFeedbackView.Timing, SkinGrade.Good, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(false, true, true, SkinGrade.Good));
        // Both settings off → no feedback slot at all.
        Assert.IsNull(FlatClearFx.SelectFeedback(false, false, true, SkinGrade.Great));
        // Timing label off → timed grade falls back to judgement-only.
        Assert.AreEqual((SkinFeedbackView.Judgement, SkinGrade.Great, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(true, false, true, SkinGrade.Great));
        // No timing judged → timed grade falls back to judgement-only.
        Assert.AreEqual((SkinFeedbackView.Judgement, SkinGrade.Great, SkinTiming.Fast),
            FlatClearFx.SelectFeedback(true, true, false, SkinGrade.Great));
    }

    /// <summary>Feedback is bound per note kind, not globally: every kind compiles
    /// its own three groups, and a fallback inherits exactly the missing
    /// (kind, mode) group without replacing the kind's own groups.</summary>
    [Test]
    public void FeedbackCompilesPerNoteKindAndInheritsPerMode()
    {
        var baseJson = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var fallback = SkinCompiler.Compile(SkinJson.Parse(baseJson));
        foreach (var kind in SkinTimingTokens.NoteKinds)
        {
            foreach (var view in new[] { SkinFeedbackView.Judgement, SkinFeedbackView.Timing, SkinFeedbackView.Combined })
                Assert.IsTrue(fallback.FeedbackComplete[(kind, view)], $"{kind} {view} must be complete");
            Assert.IsNotNull(fallback.Feedback(kind, SkinFeedbackView.Judgement, SkinGrade.Perfect, null),
                $"{kind}:feedback:judgement:perfect must compile");
            Assert.IsNotNull(fallback.Feedback(kind, SkinFeedbackView.Combined, SkinGrade.Great, SkinTiming.Fast),
                $"{kind}:feedback:combined:great:fast must compile");
        }
        // Drop compiles its own clips; the authored lift is not shared with click.
        Assert.AreNotSame(fallback.Feedback(NoteKind.Click, SkinFeedbackView.Judgement, SkinGrade.Perfect, null),
            fallback.Feedback(NoteKind.DropClick, SkinFeedbackView.Judgement, SkinGrade.Perfect, null),
            "drop judgement must not reuse the click slot");

        // A skin missing one kind's timing group inherits that group only.
        var document = Newtonsoft.Json.Linq.JObject.Parse(baseJson);
        var slots = (Newtonsoft.Json.Linq.JObject) document["slots"];
        foreach (var name in slots.Properties().Select(property => property.Name)
                     .Where(name => name.StartsWith("drop-click:feedback:timing:")).ToList())
            slots.Remove(name);
        var skin = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), fallback);
        Assert.IsTrue(skin.FeedbackComplete[(NoteKind.DropClick, SkinFeedbackView.Timing)],
            "a group served from the fallback counts as complete, like KindComplete");
        Assert.AreSame(fallback.Feedback(NoteKind.DropClick, SkinFeedbackView.Timing, SkinGrade.Great, SkinTiming.Fast),
            skin.Feedback(NoteKind.DropClick, SkinFeedbackView.Timing, SkinGrade.Great, SkinTiming.Fast),
            "missing timing group inherits from the fallback");
        Assert.AreNotSame(fallback.Feedback(NoteKind.DropClick, SkinFeedbackView.Judgement, SkinGrade.Perfect, null),
            skin.Feedback(NoteKind.DropClick, SkinFeedbackView.Judgement, SkinGrade.Perfect, null),
            "the kind's own judgement group is kept");

        // Without a fallback the missing group is a compile error naming the kind.
        var error = Assert.Throws<SkinCompileException>(() => SkinCompiler.Compile(SkinJson.Parse(document.ToString())));
        Assert.That(error.Message, Does.Contain("drop-click: feedback:timing: missing required slots"));
    }

    /// <summary>The chain-link slot (cytoid-design 6559409): the raw template spans the
    /// full 0..length along +y with the Cytoid tile's width, duty and source-side
    /// gap; the sustain clock is a no-op on flat's frozen pose; the engine's
    /// lead/trail window cuts along y only.</summary>
    [Test]
    public void ChainLinkDrawsRawTemplateAndClipsToEngineWindow()
    {
        var skin = FlatSkinRuntime.Current;
        foreach (var kind in new[] { NoteKind.DragHead, NoteKind.ClickDragHead })
        {
            var slot = skin.Slot(kind, "chainLink");
            Assert.IsNotNull(slot, $"{kind} must ship a chainLink slot");
            Assert.AreEqual(0.0, slot.Period, $"{kind} binds a frozen pose (period 0)");
            Assert.IsFalse(slot.UsesUnsupported, "chain links must paint (no curved dash)");
            var context = new SkinDrawContext();
            FlatSkinRuntime.FillHeadContext(context, kind, SeamPalettes(skin),
                alternativeColor: false, approach: 1.2);
            var painter = new SkinPainter();
            var chainBuilder = new FlatMeshBuilder();

            chainBuilder.Reset();
            painter.DrawChainLinkSlot(slot, new SkinInputs.ChainOptions { Length = 300, T = 0 },
                context, new SkinSinks(chainBuilder));
            Assert.Greater(chainBuilder.VertCount, 0, "the template must emit geometry");
            // Stroke tessellation extends butt caps by ~0.25 px — tolerance covers it.
            var halfWidth = 128 * 0.0811 / 2;
            var dash = halfWidth; // exact 50 % duty: dash = gap = half the width
            for (var i = 0; i < chainBuilder.VertCount; i++)
            {
                Assert.LessOrEqual(Math.Abs(chainBuilder.Verts[i * 7]), halfWidth + 0.5, $"{kind} vertex {i} x");
                var y = chainBuilder.Verts[i * 7 + 1];
                Assert.GreaterOrEqual(y, dash - 0.5, $"{kind} vertex {i} y (gap at the source)");
                Assert.LessOrEqual(y, 300.5, $"{kind} vertex {i} y (butt cap at the destination)");
            }

            // Frozen pose: t only feeds the elapsed input — flat's period 0 wrap
            // keeps the pattern bit-identical across the clock.
            var frozen = new FlatMeshBuilder();
            painter.DrawChainLinkSlot(slot, new SkinInputs.ChainOptions { Length = 300, T = 1e6 },
                context, new SkinSinks(frozen));
            Assert.IsTrue(chainBuilder.Verts.SequenceEqual(frozen.Verts),
                $"{kind} template pose must not move across the sustain clock");

            // Engine window (stage.ts parity): the rect clip cuts along y only.
            chainBuilder.Reset();
            painter.DrawChainLinkSlot(slot, new SkinInputs.ChainOptions { Length = 300, T = 0 },
                context, new SkinSinks(chainBuilder), 90, 210, context.Unit * 16);
            Assert.Greater(chainBuilder.VertCount, 0, "the window keeps a slice of the strip");
            for (var i = 0; i < chainBuilder.VertCount; i++)
            {
                var y = chainBuilder.Verts[i * 7 + 1];
                Assert.GreaterOrEqual(y, 89.99, $"{kind} windowed vertex {i} y");
                Assert.LessOrEqual(y, 210.01, $"{kind} windowed vertex {i} y");
            }
        }
    }

    /// <summary>chainLink is a required slot on both chain kinds: a skin missing it
    /// inherits the whole kind from the fallback, or fails compilation naming it.</summary>
    [Test]
    public void ChainLinkIsRequiredAndInheritsWholeKind()
    {
        var baseJson = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var fallback = SkinCompiler.Compile(SkinJson.Parse(baseJson));
        Assert.IsNotNull(fallback.Slot(NoteKind.DragHead, "chainLink"));
        Assert.IsNotNull(fallback.Slot(NoteKind.ClickDragHead, "chainLink"));

        var document = Newtonsoft.Json.Linq.JObject.Parse(baseJson);
        ((Newtonsoft.Json.Linq.JObject) document["slots"]).Remove("drag-head:chainLink");
        var skin = SkinCompiler.Compile(SkinJson.Parse(document.ToString()), fallback);
        Assert.AreSame(fallback.Slot(NoteKind.DragHead, "chainLink"), skin.Slot(NoteKind.DragHead, "chainLink"),
            "an incomplete kind inherits the fallback wholesale");
        Assert.AreNotSame(fallback.Slot(NoteKind.ClickDragHead, "chainLink"), skin.Slot(NoteKind.ClickDragHead, "chainLink"),
            "the complete kind keeps its own slot");

        var error = Assert.Throws<SkinCompileException>(() => SkinCompiler.Compile(SkinJson.Parse(document.ToString())));
        Assert.That(error.Message, Does.Contain("drag-head: missing required slots: chainLink"));
    }

    /// <summary>The meta section is accepted but opaque: unknown keys, stale palette
    /// fields and arbitrary contents all load without error, and nothing is read
    /// back (SkinMeta is an empty type — game config owns the runtime palette).</summary>
    [Test]
    public void MetaSectionIsToleratedAndIgnored()
    {
        var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Skins/flat.skin.json"));
        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["meta"] = new Newtonsoft.Json.Linq.JObject
        {
            ["palette"] = new Newtonsoft.Json.Linq.JObject
            {
                ["chroma"] = 0.9,
                ["hue"] = new Newtonsoft.Json.Linq.JObject { ["click"] = new Newtonsoft.Json.Linq.JObject { ["up"] = 1, ["down"] = 2 } },
                ["ring"] = "#123456",
                ["grade"] = new Newtonsoft.Json.Linq.JObject { ["perfect"] = "#ABCDEF" },
                // Stale or future fields must not reject the skin.
                ["somethingNew"] = true,
            },
            ["grades"] = new Newtonsoft.Json.Linq.JObject { ["perfect"] = "#FFFFFF" },
            ["totallyUnknown"] = new Newtonsoft.Json.Linq.JArray { 1, 2, 3 },
        };
        var parsed = SkinJson.Parse(document.ToString());
        Assert.IsNotNull(parsed.Meta, "meta stays schema-visible as an empty object");
        Assert.DoesNotThrow(() => SkinCompiler.Compile(parsed), "ignored meta contents must not affect compilation");

        // A non-object meta still fails the shape check (schema contract).
        document["meta"] = "legacy-string";
        Assert.Throws<SkinParseException>(() => SkinJson.Parse(document.ToString()));
    }
}
