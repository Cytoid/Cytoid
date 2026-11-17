using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Profiling;

/// <summary>
/// Unity boundary of the hot-updatable skin system (Design PR #33, schema v2).
/// Owns the compiled skin registry: the built-in flat.skin.json is compiled on
/// a background thread at startup (pure C#, no UnityEngine API in the compile
/// path) and swapped in atomically. Draw calls run through SkinPainter on the
/// main thread with zero steady-state allocation (verified by Tools/SkinProbe).
///
/// SKIN-ONLY FLAT PATH — the "experimental flat note design" setting is this
/// runtime: when on, every flat visual draws from skin slots. There is no
/// imperative flat geometry and no classic fallback: a game whose skin is
/// unavailable (Ready == false, e.g. a failed compile) refuses to load —
/// Game.Initialize throws before the note pool initializes and Game.Start
/// surfaces the load-error dialog. Within a loaded game, a missing or
/// capability-unsupported slot renders nothing rather than deferring to
/// classic renderers.
///
/// Engine/skin responsibility contract (per the C# skin architecture review):
///   skin owns:  enter size curve, enter fade-in, head exit fade + rewind
///               playback (the exit slot's fade track and exitRewindEnd input)
///   engine keeps: position/rotation, storyboard SizeMultiplier and opacity,
///               chart/model opacity, GlobalNoteOpacityMultiplier,
///               Override.OpacityMultiplier, HideNotes, visibility windows,
///               pooling and colliders
///   removed with the imperative flat path: ScaleAt/EnterScale curve blending,
///               the 2× intro opacity ramp, head ExitAlpha/ExitRewind, the
///               in-geometry MISS judgement text (feedback labels live in the
///               skin's feedback slots via the clear FX integration)/// </summary>
public static class FlatSkinRuntime
{
    private const string BuiltInSkinPath = "Skins/flat.skin.json";
    private const int MaxAtlasSize = 2048;

    private static volatile CompiledSkin current;
    private static Task compileTask;
    private static bool loadAttempted;
    // Captured at WarmUp (main thread): the built-in compile runs on a worker and
    // posts its atlas build back here.
    private static SynchronizationContext mainThread;
    private static readonly object CompileLock = new object();

    // Atlas pages keyed by owning skin: pages die with the registry (a swapped-out
    // skin's pages are destroyed at the swap; ClearCaches sweeps the rest). No
    // refcounting — inherited slots resolve images through their OWNING skin, so
    // a fallback skin's pages live as long as that skin is reachable as fallback.
    private static readonly Dictionary<CompiledSkin, Texture2D[]> Atlases =
        new Dictionary<CompiledSkin, Texture2D[]>();

    public static CompiledSkin Current => current;

    public static bool Ready => current != null;

    /// <summary>The built-in skin's load task (parse → validate → compile → swap),
    /// or null when not started. Await it — never Wait() it on Unity's main
    /// thread: the Android path completes via UnityWebRequest on that thread.</summary>
    public static Task WhenReady() => compileTask;

    /// <summary>Non-blocking readiness gate for the synchronous TryLoad* APIs:
    /// a pending built-in load rejects the call instead of deadlocking the main
    /// thread (desktop Task.Wait would also freeze a frame; Android never
    /// completes under a blocked main thread).</summary>
    private static bool BuiltInReadyForLoad()
    {
        if (compileTask == null || compileTask.IsCompleted) return true;
        Debug.LogWarning("[FlatSkinRuntime] built-in skin still loading — TryLoad rejected; retry after FlatSkinRuntime.Ready");
        return false;
    }

    /// <summary>Starts the background compile of the built-in skin (idempotent).
    /// On Android StreamingAssets lives inside the APK (jar:file:…): the JSON —
    /// and every image file the file system cannot serve — is fetched with a
    /// UnityWebRequest on the main thread BEFORE the compile task spawns; the
    /// compile itself stays pure C# on a worker.</summary>
    public static void WarmUp()
    {
        lock (CompileLock)
        {
            if (loadAttempted) return;
            loadAttempted = true;
            mainThread = SynchronizationContext.Current;
            var path = Path.Combine(Application.streamingAssetsPath, BuiltInSkinPath);
            var baseDir = Path.GetDirectoryName(path);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                // Files serve both JSON and images here; bytes load on the compile
                // worker (no main-thread cost when the skin has no images).
                compileTask = Task.Run(() => CompileBuiltIn(json, baseDir, null));
                return;
            }

            var request = UnityWebRequest.Get(path);
            var operation = request.SendWebRequest();
            // TryLoad awaits compileTask — it must complete only when the built-in
            // compile (JSON + images) has finished, however many hops it takes.
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            compileTask = completion.Task;
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[FlatSkinRuntime] built-in skin download failed ({request.error}); gameplay with the flat design will refuse to load");
                        completion.TrySetResult(true);
                        return;
                    }

                    var json = request.downloadHandler.text;
                    PrefetchImageBytes(json, baseDir, fetched =>
                        Task.Run(() => CompileBuiltIn(json, baseDir, fetched))
                            .ContinueWith(_ => completion.TrySetResult(true)));
                }
                finally
                {
                    request.Dispose();
                }
            };
        }
    }

    /// <summary>Scans the document for image frame srcs on the main thread and fetches
    /// every file-backed one via UnityWebRequest (APK-resident files are invisible
    /// to File.Exists). Data URIs are skipped — the compile worker decodes them.
    /// Invokes done exactly once, on the main thread.</summary>
    private static void PrefetchImageBytes(string json, string baseDir, Action<Dictionary<string, byte[]>> done)
    {
        var srcs = new List<string>();
        try
        {
            ScanImageSrcs(JObject.Parse(json), srcs);
        }
        catch (Exception)
        {
            done(null); // malformed JSON: the compile reports the real error
            return;
        }
        Dictionary<string, byte[]> fetched = null;
        var pending = 0;
        foreach (var src in srcs)
        {
            if (src.StartsWith("data:")) continue;
            if (fetched == null) fetched = new Dictionary<string, byte[]>();
            if (fetched.ContainsKey(src)) continue;
            fetched[src] = null; // reserve; overwritten on success
            pending++;
            var request = UnityWebRequest.Get(Path.Combine(baseDir, src));
            request.SendWebRequest().completed += _ =>
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                        fetched[src] = request.downloadHandler.data;
                    else
                        Debug.LogWarning($"[FlatSkinRuntime] skin image '{src}' fetch failed ({request.error})");
                    if (--pending == 0) done(fetched);
                }
                finally
                {
                    request.Dispose();
                }
            };
        }
        if (pending == 0) done(null);
    }

    /// <summary>Depth-first scan for image frame srcs (frames can nest inline in groups).</summary>
    private static void ScanImageSrcs(JToken token, List<string> srcs)
    {
        if (token is JObject obj)
        {
            if (obj["type"]?.ToString() == "image" && obj["src"] != null)
            {
                var src = obj["src"].ToString();
                if (!string.IsNullOrEmpty(src)) srcs.Add(src);
            }
            foreach (var property in obj.Properties())
                ScanImageSrcs(property.Value, srcs);
        }
        else if (token is JArray array)
        {
            foreach (var item in array) ScanImageSrcs(item, srcs);
        }
    }

    /// <summary>Worker-side image byte loading: file-backed srcs read relative to the
    /// skin JSON's directory, data URIs decode inline, APK-fetched bytes pass
    /// through. Null when the skin references no images (the common case).</summary>
    private static Dictionary<string, byte[]> ImageBytesForCompile(string json, string baseDir,
        Dictionary<string, byte[]> prefetched)
    {
        var srcs = new List<string>();
        try
        {
            ScanImageSrcs(JObject.Parse(json), srcs);
        }
        catch (Exception)
        {
            return null; // malformed JSON: SkinJson.Parse reports the real error
        }
        if (srcs.Count == 0) return null;
        var bytes = new Dictionary<string, byte[]>(srcs.Count);
        foreach (var src in srcs)
        {
            if (prefetched != null && prefetched.TryGetValue(src, out var fetched) && fetched != null)
            {
                bytes[src] = fetched;
                continue;
            }
            if (src.StartsWith("data:image/png;base64,"))
            {
                try { bytes[src] = Convert.FromBase64String(src.Substring(22)); }
                catch (FormatException) { /* invalid payload: the image stays missing */ }
            }
            else
            {
                var path = Path.Combine(baseDir, src);
                if (File.Exists(path))
                {
                    try { bytes[src] = File.ReadAllBytes(path); }
                    catch (IOException) { /* unreadable: the image stays missing */ }
                }
            }
        }
        return bytes.Count > 0 ? bytes : null;
    }

    private static void CompileBuiltIn(string json, string baseDir, Dictionary<string, byte[]> prefetched)
    {
        try
        {
            var document = SkinJson.Parse(json);
            // No fallback: the built-in skin defines the defaults itself.
            var skin = SkinCompiler.Compile(document, null, ImageBytesForCompile(json, baseDir, prefetched));
            current = skin;
            Debug.Log($"[FlatSkinRuntime] compiled built-in skin '{skin.Id}' " +
                      $"({skin.Animations.Count} animations, {skin.Slots.Count} slots)");
            // The atlas is a main-thread resource and the compile ran on a worker:
            // post the build back. If it fails (size cap) the images clear and
            // image quads skip silently — the skin keeps its vector parts.
            if (skin.Images.Count > 0)
                mainThread?.Post(_ =>
                {
                    if (ReferenceEquals(current, skin) && !BuildAtlas(skin)) skin.Images.Clear();
                }, null);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FlatSkinRuntime] built-in skin failed to compile; gameplay with the flat design will refuse to load: {e.Message}");
        }
    }

    /// <summary>Compiles a user-supplied skin JSON against the built-in as fallback
    /// (incomplete kinds / feedback groups inherit from it) and swaps it in.
    /// Image files resolve against StreamingAssets (read on the compile worker);
    /// APK-resident files cannot be fetched synchronously here, so data URIs are
    /// the portable form for user skins on Android. An image atlas over the size
    /// cap rejects the skin (registry unchanged).</summary>
    public static bool TryLoad(string json)
    {
        try
        {
            WarmUp();
            if (!BuiltInReadyForLoad()) return false;
            var document = SkinJson.Parse(json);
            var skin = SkinCompiler.Compile(document, current,
                ImageBytesForCompile(json, Application.streamingAssetsPath, null));
            return SwapCompiledSkin(skin);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FlatSkinRuntime] rejected skin: {e.Message}");
            return false;
        }
    }

    /// <summary>Loads a skin JSON from an absolute path with its sibling directory as
    /// the image base (editor/desktop tooling and tests; mirrors FlatParity's
    /// optional-golden convention — callers skip when the design checkout with
    /// the baked branch is absent).</summary>
    public static bool TryLoadFromFile(string absolutePath)
    {
        try
        {
            WarmUp();
            if (!BuiltInReadyForLoad()) return false;
            if (!File.Exists(absolutePath)) return false;
            var json = File.ReadAllText(absolutePath);
            var skin = SkinCompiler.Compile(SkinJson.Parse(json), current,
                ImageBytesForCompile(json, Path.GetDirectoryName(absolutePath), null));
            return SwapCompiledSkin(skin);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FlatSkinRuntime] rejected skin: {e.Message}");
            return false;
        }
    }

    /// <summary>Loads a skin JSON stored under StreamingAssets (editor/desktop tooling
    /// and test path): file-backed images read directly from the skin's directory
    /// and the atlas builds inline on this (main) thread.</summary>
    public static bool TryLoadFromStreamingAssets(string relativePath)
    {
        try
        {
            WarmUp();
            if (!BuiltInReadyForLoad()) return false;
            var path = Path.Combine(Application.streamingAssetsPath, relativePath);
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            var skin = SkinCompiler.Compile(SkinJson.Parse(json), current,
                ImageBytesForCompile(json, Path.GetDirectoryName(path), null));
            return SwapCompiledSkin(skin);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[FlatSkinRuntime] rejected skin {relativePath}: {e.Message}");
            return false;
        }
    }

    /// <summary>Builds the atlas for a freshly compiled skin, destroys the replaced
    /// skin's page and swaps the registry. Shared by both load paths.</summary>
    private static bool SwapCompiledSkin(CompiledSkin skin)
    {
        if (!BuildAtlas(skin))
        {
            Debug.LogWarning($"[FlatSkinRuntime] skin '{skin.Id}' rejected: an image exceeds one {MaxAtlasSize}x{MaxAtlasSize} atlas page");
            DestroyAtlas(skin);
            return false;
        }
        // The replaced skin's atlas stays alive: the new skin's inherited slots
        // keep Animation.Owner on it (InheritKind/InheritFeedback reuse slot
        // objects), and in-flight FlatClearFx snapshots hold the same
        // references. Atlases are bounded by TryLoad swaps per session and
        // reaped in ClearCaches (new game) outside the current lineage.
        current = skin;
        return true;
    }

    public static bool Supports(NoteKind kind) =>
        current != null && current.KindComplete.TryGetValue(kind, out var complete) && complete;

    /// <summary>True when the kind's head can be painted by the skin this frame.</summary>
    public static bool CanPaintHead(NoteKind kind)
    {
        if (current == null) return false;
        if (!Supports(kind)) return false;
        // Drop notes have no exit; everything else needs a clean exit + enter slot.
        // Head draws have no image sink, so image slots gate heads to nothing
        // drawn (CanPaintHead false) instead of dropping quads on a sink that
        // cannot show them.
        var enter = current.Slot(kind, "enter");
        if (enter == null || enter.UsesUnsupported || enter.UsesImages) return false;
        if (kind == NoteKind.DropClick || kind == NoteKind.DropDrag) return true;
        var exit = current.Slot(kind, "exit");
        return exit != null && !exit.UsesUnsupported && !exit.UsesImages;
    }

    // ------------------------------------------------------------- atlas

    /// <summary>The compiled skin's first atlas page (null until built / when
    /// imageless). Full page sets ride the SkinImage.Texture of packed images.</summary>
    public static Texture2D AtlasOf(CompiledSkin skin) =>
        skin != null && Atlases.TryGetValue(skin, out var pages) && pages.Length > 0 ? pages[0] : null;

    /// <summary>Packs every compiled image into one or more atlas pages on the main
    /// thread: shelf-packed rows (height-descending), 2 px edge-extruded gutters
    /// against bilinear bleed (a transparent pad lerps toward black and fringes
    /// every image — replicating border texels keeps the filter inside the same
    /// color), no power-of-two requirement, each page capped at MaxAtlasSize²
    /// (the memory bound — only a single image larger than a page rejects the
    /// skin). Each SkinImage gets its rect in Unity texture space (y-up:
    /// (U0, V0) is the image's bottom-left) and Texture = its page. Images
    /// without bytes are left unpacked (their quads skip at draw). All images
    /// decode into temporary staging textures, blitted via Get/SetPixels32 (both
    /// bottom-row-first — upright orientation is preserved). Idempotent.</summary>
    public static bool BuildAtlas(CompiledSkin skin)
    {
        if (skin == null || Atlases.ContainsKey(skin) || skin.Images.Count == 0) return true;
        var staging = new List<(SkinImage image, Texture2D texture)>(skin.Images.Count);
        foreach (var image in skin.Images.Values)
        {
            if (image.Bytes == null) continue; // missing bytes: its quads skip at draw
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(image.Bytes))
            {
                Debug.LogWarning($"[FlatSkinRuntime] skin image '{image.Src}' failed to decode");
                DestroyTexture(texture);
                continue;
            }
            image.Width = texture.width;
            image.Height = texture.height;
            staging.Add((image, texture));
        }
        if (staging.Count == 0)
        {
            skin.Images.Clear(); // nothing drawable: drop the entries so draws skip fast
            return true;
        }

        // Shelf-pack, tallest first so rows stay tight; overflow rolls to a new
        // page. Each cell is the image plus a full gutter ring on every side.
        const int gutter = 2;
        staging.Sort((a, b) => b.texture.height != a.texture.height
            ? b.texture.height.CompareTo(a.texture.height)
            : string.CompareOrdinal(a.image.Src, b.image.Src));
        var placed = new List<(SkinImage image, Texture2D texture, int page, int x, int y, int w, int h)>(staging.Count);
        var pageWidths = new List<int> { gutter };
        var pageHeights = new List<int> { gutter };
        var page = 0;
        var cursorX = gutter;
        var cursorY = gutter;
        var rowHeight = 0;
        foreach (var (image, texture) in staging)
        {
            var w = texture.width;
            var h = texture.height;
            if (w > MaxAtlasSize - 2 * gutter || h > MaxAtlasSize - 2 * gutter) return Reject();
            var cellW = w + 2 * gutter;
            var cellH = h + 2 * gutter;
            if (cursorX + cellW - gutter > MaxAtlasSize)
            {
                cursorX = gutter;
                cursorY += rowHeight + gutter;
                rowHeight = 0;
            }
            if (cursorY + cellH - gutter > MaxAtlasSize)
            {
                page++;
                pageWidths.Add(gutter);
                pageHeights.Add(gutter);
                cursorX = gutter;
                cursorY = gutter;
                rowHeight = 0;
            }
            // (x, y) = the IMAGE rect inside its cell; the ring around it is gutter.
            placed.Add((image, texture, page, cursorX, cursorY, w, h));
            if (cellH > rowHeight) rowHeight = cellH;
            cursorX += cellW;
            if (cursorX + gutter > pageWidths[page]) pageWidths[page] = cursorX + gutter;
            if (cursorY + h + gutter + gutter > pageHeights[page]) pageHeights[page] = cursorY + h + 2 * gutter;
        }

        var pages = new Texture2D[pageWidths.Count];
        for (var p = 0; p < pages.Length; p++)
            pages[p] = new Texture2D(pageWidths[p], pageHeights[p], TextureFormat.RGBA32, false);
        foreach (var (image, texture, p, x, y, w, h) in placed)
        {
            var block = texture.GetPixels32();
            pages[p].SetPixels32(x, y, w, h, block);
            ExtrudeEdges(pages[p], x, y, w, h, gutter, block);
            image.U0 = x / (float) pageWidths[p];
            image.V0 = y / (float) pageHeights[p];
            image.U1 = (x + w) / (float) pageWidths[p];
            image.V1 = (y + h) / (float) pageHeights[p];
            image.Texture = pages[p];
        }
        foreach (var atlas in pages)
            atlas.Apply(false, true);
        foreach (var (_, texture) in staging)
            DestroyTexture(texture);
        Atlases[skin] = pages;
        return true;

        bool Reject()
        {
            foreach (var (_, texture) in staging)
                DestroyTexture(texture);
            return false;
        }
    }

    /// <summary>Replicates the image's border texels into the gutter ring around its
    /// atlas cell, so bilinear filtering at the UV boundary stays on the edge color
    /// (no dark fringe from transparent padding, no bleed from neighbours).</summary>
    private static void ExtrudeEdges(Texture2D page, int x, int y, int w, int h, int g, Color32[] block)
    {
        var left = new Color32[h];
        var right = new Color32[h];
        for (var i = 0; i < h; i++)
        {
            left[i] = block[i * w];
            right[i] = block[i * w + w - 1];
        }
        var bottom = new Color32[w];
        var top = new Color32[w];
        for (var i = 0; i < w; i++)
        {
            bottom[i] = block[i];
            top[i] = block[(h - 1) * w + i];
        }
        for (var k = 1; k <= g; k++)
        {
            page.SetPixels32(x - k, y, 1, h, left);
            page.SetPixels32(x + w - 1 + k, y, 1, h, right);
            page.SetPixels32(x, y - k, w, 1, bottom);
            page.SetPixels32(x, y + h - 1 + k, w, 1, top);
        }

        var cornerBl = block[0];
        var cornerBr = block[w - 1];
        var cornerTl = block[(h - 1) * w];
        var cornerTr = block[(h - 1) * w + w - 1];
        for (var ky = 1; ky <= g; ky++)
        for (var kx = 1; kx <= g; kx++)
        {
            page.SetPixels32(x - kx, y - ky, 1, 1, Single(cornerBl));
            page.SetPixels32(x + w - 1 + kx, y - ky, 1, 1, Single(cornerBr));
            page.SetPixels32(x - kx, y + h - 1 + ky, 1, 1, Single(cornerTl));
            page.SetPixels32(x + w - 1 + kx, y + h - 1 + ky, 1, 1, Single(cornerTr));
        }
        return;

        static Color32[] Single(Color32 color)
        {
            var array = new Color32[1];
            array[0] = color;
            return array;
        }
    }

    private static void DestroyAtlas(CompiledSkin skin)
    {
        if (skin == null || !Atlases.Remove(skin, out var pages)) return;
        foreach (var atlas in pages)
            DestroyTexture(atlas);
    }

    /// <summary>Editor-safe texture teardown: play mode destroys (deferred); edit-mode
    /// tests need DestroyImmediate (Destroy logs an error there).</summary>
    private static void DestroyTexture(Texture2D texture)
    {
        if (texture == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return;
        }
#endif
        UnityEngine.Object.Destroy(texture);
    }

    // ------------------------------------------------------------- context

    private static readonly Dictionary<(Game, FlatFamily, FlatDirection, int, int), SkinPaletteContext> ContextCache =
        new Dictionary<(Game, FlatFamily, FlatDirection, int, int), SkinPaletteContext>();

    private static Game contextGame;

    public static void ClearCaches()
    {
        ContextCache.Clear();
        contextGame = null;
        // Keep the current skin's fallback lineage alive (inherited slots'
        // Animation.Owner points into it); reap everything else.
        var lineage = new HashSet<CompiledSkin>();
        for (var skin = current; skin != null; skin = skin.FallbackSkin) lineage.Add(skin);
        var stale = new List<CompiledSkin>();
        foreach (var entry in Atlases)
            if (!lineage.Contains(entry.Key)) stale.Add(entry.Key);
        foreach (var skin in stale) DestroyAtlas(skin);
    }

    /// <summary>Builds (and caches per game) the palette context from game config:
    /// player/chart fill seed → note palette; ring colors; grade effect colors.</summary>
    // NOTE: the skin document's meta.palette section is tolerated but ignored —
    // game-config colours unconditionally own this palette. The old priority
    // chain (chart/storyboard > player explicit > skin meta > flat defaults)
    // was dropped along with the meta fields: flat's meta equalled the game
    // defaults, so only third-party skins ever noticed the gap.
    public static SkinPaletteContext PaletteContextOf(Game game, FlatFamily family, FlatDirection direction,
        Color seed, Color ring)
    {
        if (contextGame != game)
        {
            ContextCache.Clear();
            contextGame = game;
        }
        var seedRgba = (Color32) seed;
        var ringRgba = (Color32) ring;
        var key = (game, family, direction, Packed(seedRgba), Packed(ringRgba));
        if (ContextCache.TryGetValue(key, out var cached)) return cached;
        var grades = new Dictionary<SkinGrade, string>();
        foreach (var grade in SkinTimingTokens.Grades)
            grades[grade] = HexOf(game.Config.NoteGradeEffectColors[ToGrade(grade)]);
        var context = new SkinPaletteContext
        {
            // The ring colour is part of the palette, not just the cache key:
            // without Options.Ring the palette falls back to the neutral band and
            // chart ring_color / player ring settings silently do nothing.
            Note = SkinPalette.FromHex(HexOf(seed), new SkinPalette.Options { Ring = HexOf(ring) }),
            Grades = new SkinGradePalettes(grades),
            Family = (lookupFamily, lookupDirection) =>
                SkinPalette.FromHex(HexOf(FamilySeed(game, lookupFamily, lookupDirection))),
        };
        ContextCache[key] = context;
        return context;
    }

    private static Color FamilySeed(Game game, FlatFamily family, FlatDirection direction)
    {
        // Family lookups sample the game's configured fill colour for that family.
        // Approximation pending the palette-priority contract with Design: custom
        // player colours hex-derive (derivePalette(hex)); defaults are close to the
        // design's hue table because the shipped theme matches it.
        var type = FamilyType(family);
        var index = direction == FlatDirection.Down ? 0 : 1;
        return game.Config.NoteFillColors[type][index];
    }

    private static NoteType FamilyType(FlatFamily family) => family switch
    {
        FlatFamily.Click => NoteType.Click,
        FlatFamily.Hold => NoteType.Hold,
        FlatFamily.LongHold => NoteType.LongHold,
        FlatFamily.Flick => NoteType.Flick,
        FlatFamily.Drag => NoteType.DragHead,
        FlatFamily.ClickDrag => NoteType.CDragChild,
        FlatFamily.DropClick => NoteType.DropClick,
        FlatFamily.DropDrag => NoteType.DropDrag,
        _ => NoteType.Click,
    };

    private static NoteGrade ToGrade(SkinGrade grade) => grade switch
    {
        SkinGrade.Perfect => NoteGrade.Perfect,
        SkinGrade.Great => NoteGrade.Great,
        SkinGrade.Good => NoteGrade.Good,
        SkinGrade.Bad => NoteGrade.Bad,
        _ => NoteGrade.Miss,
    };

    private static int Packed(Color32 c) => c.r | c.g << 8 | c.b << 16 | c.a << 24;

    private static string HexOf(Color c)
    {
        var rgba = (Color32) c;
        return $"#{rgba.r:X2}{rgba.g:X2}{rgba.b:X2}";
    }

    /// <summary>Draw context for a note head — evaluation happens in design pixels
    /// (unit 128, scale 1); the mesh converts to world units. Heading is 0: drag
    /// chains rotate the note transform, frames are authored at 0.
    /// Fill variant reuses the caller's context: the per-frame hot path allocates nothing.</summary>
    public static void FillHeadContext(SkinDrawContext context, NoteKind kind, SkinPaletteContext palettes,
        bool alternativeColor, double approach)
    {
        context.Kind = kind;
        context.RingStroke = FlatTokens.Unit * FlatTokens.StrokeRing;
        context.HairStroke = FlatTokens.Unit * FlatTokens.StrokeHair;
        context.Palettes = palettes;
        context.Grade = null;
        context.Direction = alternativeColor ? FlatDirection.Down : FlatDirection.Up;
        context.BodyDirection = context.Direction;
        context.Heading = 0;
        context.Approach = approach;
        context.Size = FlatTokens.Unit * FlatTokens.Size(kind);
        context.Unit = FlatTokens.Unit;
    }

    /// <summary>Snapshot variant for detached exit visuals (allocated once at capture).</summary>
    public static SkinDrawContext HeadContextOf(Note note, FlatFamily family, SkinPaletteContext palettes,
        double approach)
    {
        var context = new SkinDrawContext();
        FillHeadContext(context, FlatNoteRendererBase.KindFor(note.Type), palettes,
            note.Model.UseAlternativeColor(), approach);
        return context;
    }

    // ------------------------------------------------------------ profiling

    // Static markers: zero overhead when the profiler is off, no per-call allocation.
    internal static readonly ProfilerMarker BindingMarker =
        new ProfilerMarker(ProfilerCategory.Scripts, "Skin.Binding");
    internal static readonly ProfilerMarker PaintMarker =
        new ProfilerMarker(ProfilerCategory.Scripts, "Skin.Paint");
    internal static readonly ProfilerMarker UploadCopyMarker =
        new ProfilerMarker(ProfilerCategory.Scripts, "Skin.UploadCopy");
}
