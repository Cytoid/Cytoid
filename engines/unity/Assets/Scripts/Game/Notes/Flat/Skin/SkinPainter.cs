// Skin painter — the stateless VM. Walks the compiled animation graph per
// draw call with a preallocated environment pool, transform/opacity stacks
// and scratch tessellation, emitting straight into FlatMeshBuilders through
// FlatGeom (draw mode) or a JSON scene tree (dump mode for parity tests).
// Draw mode performs no per-frame allocations after warm-up.
//
// Documented deviations from the TS renderers (capability profile):
//  - isolate groups are approximated by folding opacity into the subtree
//    (matches the current Unity exit fade); no offscreen compositing yet.
//  - group clips are CPU-clipped at emission; image frames emit textured
//    quads into the optional image sink (no sink → quads skip) and ignore
//    group clips this round (no UV-space clipping yet).
//  - additive blend routes to a second mesh builder (same as FlatClearFx);
//    image quads always use the normal-blend image sink.
//  - square caps on arcs render as butt caps (no arc caller uses them).
// PURE C#: no UnityEngine references.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

/// <summary>Evaluation environment of one animation level: track memoization + inputs.</summary>
public sealed class SkinEnv
{
    public CompiledSkin Skin;
    public SkinInputTable Inputs;
    public SkinDrawContext Ctx;
    public double Phi;
    internal SkinCompiledTrack[] Tracks;
    internal string[] TrackNames;
    private double[] values = new double[8];
    private SkinColor[] colors = new SkinColor[8];
    private double[] phases = new double[8];
    private bool[] evaluated = new bool[8];
    private bool[] phaseEvaluated = new bool[8];
    private bool[] active = new bool[8];

    public void Reset(CompiledSkinAnimation animation, SkinInputTable inputs, SkinDrawContext ctx,
        CompiledSkin skin, double phi)
    {
        Skin = skin;
        Inputs = inputs;
        Ctx = ctx;
        Phi = phi;
        Tracks = animation.Tracks;
        TrackNames = animation.TrackNames;
        var count = Tracks.Length;
        if (values.Length < count)
        {
            values = new double[count];
            colors = new SkinColor[count];
            phases = new double[count];
            evaluated = new bool[count];
            phaseEvaluated = new bool[count];
            active = new bool[count];
        }
        else
        {
            Array.Clear(evaluated, 0, count);
            Array.Clear(phaseEvaluated, 0, count);
        }
    }

    public double TrackValue(int index)
    {
        if (evaluated[index]) return values[index];
        if (active[index]) throw CycleError(index);
        active[index] = true;
        values[index] = Tracks[index].At(Phi, Inputs, this);
        active[index] = false;
        evaluated[index] = true;
        return values[index];
    }

    public SkinColor TrackColor(int index)
    {
        if (evaluated[index]) return colors[index];
        if (active[index]) throw CycleError(index);
        active[index] = true;
        colors[index] = Tracks[index].AtColor(Phi, Inputs, this);
        active[index] = false;
        evaluated[index] = true;
        return colors[index];
    }

    public double Phase(int index)
    {
        if (phaseEvaluated[index]) return phases[index];
        phases[index] = Tracks[index].Phase(Phi, Inputs, this);
        phaseEvaluated[index] = true;
        return phases[index];
    }

    private SkinRenderException CycleError(int index) =>
        new SkinRenderException($"track cycle at \"{TrackNames[index]}\"");
}

/// <summary>Affine 2D transform in design-pixel space (+y down): canvas setTransform order
/// (translate · rotate · scale), applied to local geometry before the parent transform.</summary>
public readonly struct SkinMatrix
{
    public readonly double A, B, C, D, E, F;

    public SkinMatrix(double a, double b, double c, double d, double e, double f)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
        F = f;
    }

    public static readonly SkinMatrix Identity = new SkinMatrix(1, 0, 0, 1, 0, 0);

    public static SkinMatrix Compose(double x, double y, double rotate, double sx, double sy) =>
        Identity.Multiply(Translate(x, y)).Multiply(Rotate(rotate)).Multiply(Scale(sx, sy));

    public static SkinMatrix Translate(double x, double y) => new SkinMatrix(1, 0, 0, 1, x, y);

    public static SkinMatrix Rotate(double radians) =>
        new SkinMatrix(Math.Cos(radians), Math.Sin(radians), -Math.Sin(radians), Math.Cos(radians), 0, 0);

    public static SkinMatrix Scale(double sx, double sy) => new SkinMatrix(sx, 0, 0, sy, 0, 0);

    public SkinMatrix Multiply(in SkinMatrix local) => new SkinMatrix(
        A * local.A + C * local.B,
        B * local.A + D * local.B,
        A * local.C + C * local.D,
        B * local.C + D * local.D,
        A * local.E + C * local.F + E,
        B * local.E + D * local.F + F);

    public (double X, double Y) Apply(double x, double y) => (A * x + C * y + E, B * x + D * y + F);
}

public readonly struct SkinAnimationClock
{
    public readonly double Elapsed, Duration;

    public SkinAnimationClock(double elapsed, double duration)
    {
        Elapsed = elapsed;
        Duration = duration;
    }
}

/// <summary>Zero-allocation attribution counters for the paint hot path — enable from
/// the perf harness (Unity Profiler markers cover the same scopes on-device).
/// EvaluateTicks accumulates track/expr sampling, TessellateTicks the leaf draws
/// (param evaluation + FlatGeom + transform-append), PaintTicks whole slot paints.
/// Disabled, the cost is one branch per scope.</summary>
public static class SkinPaintStats
{
    public static bool Enabled;

    public static long PaintTicks, EvaluateTicks, TessellateTicks;
    public static long PaintCount, LeafCount;

    public static void Reset()
    {
        PaintTicks = EvaluateTicks = TessellateTicks = 0;
        PaintCount = LeafCount = 0;
    }

    public static double Milliseconds(long ticks) => ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
}

/// <summary>Target mesh builders: normal and additive (additive is null for note heads),
/// plus the optional textured image sink (provided only on the FX path — heads
/// draw without one, so image quads in their slots skip). A readonly struct so
/// hot-path calls pass it on the stack — no per-frame allocation.</summary>
public readonly struct SkinSinks
{
    public readonly FlatMeshBuilder Normal;
    public readonly FlatMeshBuilder Add;
    public readonly FlatMeshBuilder Image;

    public SkinSinks(FlatMeshBuilder normal, FlatMeshBuilder add = null, FlatMeshBuilder image = null)
    {
        Normal = normal;
        Add = add;
        Image = image;
    }
}

/// <summary>Resolved transform values (absent fields default; null when no field set).</summary>
public readonly struct SkinTransformValues
{
    public readonly bool Present;
    public readonly double X, Y, Rotate, Scale, ScaleX, ScaleY;
    public readonly bool HasX, HasY, HasRotate, HasScale, HasScaleX, HasScaleY;

    public SkinTransformValues(SkinTransformExpr transform, SkinEnv env)
    {
        Present = transform != null && transform.HasAny;
        if (!Present)
        {
            this = default;
            return;
        }
        HasX = transform.X != null;
        X = HasX ? transform.X.Eval(env) : 0;
        HasY = transform.Y != null;
        Y = HasY ? transform.Y.Eval(env) : 0;
        HasRotate = transform.Rotate != null;
        Rotate = HasRotate ? transform.Rotate.Eval(env) : 0;
        HasScale = transform.Scale != null;
        Scale = HasScale ? transform.Scale.Eval(env) : 1;
        HasScaleX = transform.ScaleX != null;
        ScaleX = (HasScaleX ? transform.ScaleX.Eval(env) : 1) * Scale;
        HasScaleY = transform.ScaleY != null;
        ScaleY = (HasScaleY ? transform.ScaleY.Eval(env) : 1) * Scale;
    }
}

public sealed class SkinPainter
{
    private const int MaxDepth = 40;

    private readonly SkinEnv[] envs = new SkinEnv[MaxDepth];
    private readonly SkinMatrix[] matrices = new SkinMatrix[MaxDepth];
    private readonly double[] opacities = new double[MaxDepth];
    private readonly bool[] additive = new bool[MaxDepth];
    private readonly FlatMeshBuilder scratch = new FlatMeshBuilder();
    private readonly double[] pointScratch = new double[8192];
    private double[] scratchDash = new double[16];

    // ---- group clips (rect / circle) as convex polygons in root design space.
    // Triangles are Sutherland-Hodgman clipped at emission; clipped edges are
    // hard (no feather) — the deviation from canvas coverage clipping.
    private const int MaxClips = 4;
    private const int MaxClipVerts = 16;
    private const int MaxClipPolyVerts = 64; // Sutherland-Hodgman can grow a subject ~2x per clip edge
    private readonly double[] clipX = new double[MaxClips * MaxClipVerts];
    private readonly double[] clipY = new double[MaxClips * MaxClipVerts];
    private readonly int[] clipVertCount = new int[MaxClips];
    private readonly int[] clipPushDepth = new int[MaxClips];
    private int clipCount;
    private readonly double[] polyX = new double[MaxClipPolyVerts];
    private readonly double[] polyY = new double[MaxClipPolyVerts];
    private readonly float[] polyR = new float[MaxClipPolyVerts];
    private readonly float[] polyG = new float[MaxClipPolyVerts];
    private readonly float[] polyB = new float[MaxClipPolyVerts];
    private readonly float[] polyA = new float[MaxClipPolyVerts];
    private readonly double[] outX = new double[MaxClipPolyVerts];
    private readonly double[] outY = new double[MaxClipPolyVerts];
    private readonly float[] outR = new float[MaxClipPolyVerts];
    private readonly float[] outG = new float[MaxClipPolyVerts];
    private readonly float[] outB = new float[MaxClipPolyVerts];
    private readonly float[] outA = new float[MaxClipPolyVerts];
    private readonly Dictionary<int, double[]> polyBuffers = new Dictionary<int, double[]>();
    private int depth;
    private CompiledSkin skin;
    private SkinSinks sinks;
    // Per-draw scratch (no steady-state allocation): resolved inputs + grade-bound context.
    private readonly SkinInputTable inputTable = new SkinInputTable();
    private readonly SkinDrawContext drawContext = new SkinDrawContext();

    // Dump mode (parity corpus): scene nodes are captured as JSON instead of tessellated.
    internal bool DumpMode;
    internal JObject DumpRoot;
    private readonly List<JObject> dumpStack = new List<JObject>(32);

    // Image quads: 2 floats per vertex, index-aligned with the image sink's
    // vertices (every vertex in that sink comes from an image quad, so the list
    // stays dense). Reset whenever a draw begins against a freshly emptied image
    // sink — callers that accumulate several slot draws into one builder per
    // frame (FlatClearFx) keep the list aligned across the whole frame.
    private readonly List<float> imageUv = new List<float>(64);
    // The image each emitted quad samples (quad-ordered, aligned with the sink in
    // blocks of 4 vertices) — multi-page atlases need the page identity at upload.
    private readonly List<SkinImage> imageQuads = new List<SkinImage>(16);

    /// <summary>True when the last draw emitted at least one image quad.</summary>
    public bool UsedImages { get; private set; }

    /// <summary>UVs of the image sink's vertices from the draws since the image
    /// sink was last empty (2 floats per vertex, vertex-index-aligned).</summary>
    public IReadOnlyList<float> LastImageUv => imageUv;

    /// <summary>The image (and thus atlas page) each emitted quad samples, quad-ordered
    /// and aligned with the image sink in blocks of 4 vertices.</summary>
    public IReadOnlyList<SkinImage> LastImageQuads => imageQuads;

    public SkinPainter()
    {
        for (var i = 0; i < MaxDepth; i++) envs[i] = new SkinEnv();
    }

    // =================================================================
    // Slot entry points (compile.ts clip wrappers)
    // =================================================================

    /// <summary>Note slot draw — x is the slot's driver (progress or seconds per contract mode).</summary>
    public void DrawNoteSlot(CompiledSkinSlot slot, double x, SkinDrawContext ctx, in SkinSinks targets)
    {
        if (double.IsNaN(x) || double.IsInfinity(x))
            throw new SkinRenderException($"{slot.Key}: driver must be finite");
        drawContext.CopyFrom(ctx);
        drawContext.Grade = slot.Grade;
        SkinInputs.Resolve(drawContext, inputTable);
        var contract = slot.Contract;
        inputTable.Set(SkinInput.ExitRewindEnd,
            1 - contract.Duration * SkinTimingTokens.Note.ExitRewind / Math.Max(1e-6, drawContext.Approach));
        var p = SkinSlots.SlotProgress(contract, x, slot.Period);
        var duration = contract.Mode == SkinClipMode.Normalized
            ? drawContext.Approach
            : contract.Mode == SkinClipMode.Loop ? slot.Period : contract.Duration;
        BeginDraw(slot, targets);
        var start = SkinPaintStats.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        DrawAnimation(slot.Animation, p, drawContext, inputTable, new SkinAnimationClock(p * duration, duration));
        if (SkinPaintStats.Enabled)
        {
            SkinPaintStats.PaintTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
            SkinPaintStats.PaintCount++;
        }
    }

    /// <summary>Feedback clip draw — t is seconds since the judgement.</summary>
    public void DrawFeedbackSlot(CompiledSkinSlot slot, double t, SkinDrawContext ctx, in SkinSinks targets)
    {
        if (double.IsNaN(t) || double.IsInfinity(t))
            throw new SkinRenderException($"{slot.Key}: driver must be finite");
        drawContext.CopyFrom(ctx);
        drawContext.Grade = slot.Grade;
        SkinInputs.Resolve(drawContext, inputTable);
        var duration = slot.Contract.Duration;
        BeginDraw(slot, targets);
        var start = SkinPaintStats.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        DrawAnimation(slot.Animation, t / duration, drawContext, inputTable, new SkinAnimationClock(t, duration));
        if (SkinPaintStats.Enabled)
        {
            SkinPaintStats.PaintTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
            SkinPaintStats.PaintCount++;
        }
    }

    /// <summary>Parametric hold / long-hold body draw (compile.ts design.body).</summary>
    public void DrawBodySlot(CompiledSkinSlot slot, SkinInputs.BodyOptions options, SkinDrawContext ctx,
        in SkinSinks targets)
    {
        drawContext.CopyFrom(ctx);
        drawContext.Kind = slot.Contract.Kind.Value;
        drawContext.Grade = null;
        SkinInputs.ResolveBody(drawContext, options, inputTable);
        var elapsed = inputTable.Get(SkinInput.Elapsed);
        var duration = inputTable.Get(SkinInput.Duration);
        var progress = inputTable.Get(SkinInput.Progress);
        BeginDraw(slot, targets);
        var start = SkinPaintStats.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        try
        {
        DrawBodyInner(slot, progress, drawContext, inputTable, elapsed, duration);
        }
        finally
        {
            if (SkinPaintStats.Enabled)
            {
                SkinPaintStats.PaintTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
                SkinPaintStats.PaintCount++;
            }
        }
    }

    /// <summary>Chain-link template draw (compile.ts design.chainLink): the raw 0..length
    /// strip along +y from the source note; the caller rotates it onto the link.
    /// Optional engine lead/trail window (template-space px; stage.ts parity —
    /// rect x ∈ [-halfWidth, halfWidth], y ∈ [windowY0, windowY1]); growth and
    /// retraction never reach the skin.</summary>
    public void DrawChainLinkSlot(CompiledSkinSlot slot, SkinInputs.ChainOptions options, SkinDrawContext ctx,
        in SkinSinks targets, double windowY0 = double.NaN, double windowY1 = double.NaN,
        double windowHalfWidth = double.NaN)
    {
        drawContext.CopyFrom(ctx);
        drawContext.Kind = slot.Contract.Kind.Value;
        drawContext.Grade = null;
        SkinInputs.ResolveChain(drawContext, options, inputTable);
        var t = inputTable.Get(SkinInput.Elapsed);
        var p = SkinSlots.SlotProgress(slot.Contract, t, slot.Period);
        BeginDraw(slot, targets);
        var hasWindow = !double.IsNaN(windowY0) && !double.IsNaN(windowY1) && clipCount < MaxClips;
        if (hasWindow) PushTemplateRectClip(-windowHalfWidth, windowY0, windowHalfWidth, windowY1);
        var start = SkinPaintStats.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        try
        {
            DrawAnimation(slot.Animation, p, drawContext, inputTable,
                new SkinAnimationClock(p * slot.Period, slot.Period));
        }
        finally
        {
            if (hasWindow) clipCount--;
            if (SkinPaintStats.Enabled)
            {
                SkinPaintStats.PaintTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
                SkinPaintStats.PaintCount++;
            }
        }
    }

    /// <summary>Engine-owned lead/trail window for chain links: a rect clip authored in
    /// template space (root matrix is identity at slot scope — same winding as
    /// PushClip's rect path).</summary>
    private void PushTemplateRectClip(double x0, double y0, double x1, double y1)
    {
        var baseIndex = clipCount * MaxClipVerts;
        clipX[baseIndex] = x0; clipY[baseIndex] = y0;
        clipX[baseIndex + 1] = x1; clipY[baseIndex + 1] = y0;
        clipX[baseIndex + 2] = x1; clipY[baseIndex + 2] = y1;
        clipX[baseIndex + 3] = x0; clipY[baseIndex + 3] = y1;
        clipVertCount[clipCount] = 4;
        clipPushDepth[clipCount] = depth;
        clipCount++;
    }

    private void DrawBodyInner(CompiledSkinSlot slot, double progress, SkinDrawContext drawContext,
        SkinInputTable inputTable, double elapsed, double duration)
    {
        // Hold templates extend along +y; the engine flips them for an upward body.
        if (slot.Contract.Kind == NoteKind.Hold && drawContext.BodyDirection == FlatDirection.Up)
        {
            if (DumpMode)
            {
                BeginGroup(null, FlipTransform, new SkinTransformValues(FlipTransform, null), false, false, null);
                DrawAnimation(slot.Animation, progress, drawContext, inputTable,
                    new SkinAnimationClock(elapsed, duration > 0 ? duration : 1));
                EndGroup();
            }
            else
            {
                PushTransform(SkinMatrix.Scale(1, -1));
                DrawAnimation(slot.Animation, progress, drawContext, inputTable,
                    new SkinAnimationClock(elapsed, duration > 0 ? duration : 1));
                depth--;
            }
        }
        else
        {
            DrawAnimation(slot.Animation, progress, drawContext, inputTable,
                new SkinAnimationClock(elapsed, duration > 0 ? duration : 1));
        }
    }

    private static readonly SkinTransformExpr FlipTransform =
        new SkinTransformExpr(null, null, null, null, null, new SkinConstExpr(-1));

    private void BeginDraw(CompiledSkinSlot slot, SkinSinks targets)
    {
        skin = slot.Animation.Owner;
        sinks = targets;
        depth = 0;
        matrices[0] = SkinMatrix.Identity;
        opacities[0] = 1;
        additive[0] = false;
        UsedImages = false;
        if (targets.Image != null && targets.Image.VertCount == 0)
        {
            imageUv.Clear();
            imageQuads.Clear();
        }
    }

    internal void BeginDump(CompiledSkin owner)
    {
        DumpMode = true;
        DumpRoot = null;
        dumpStack.Clear();
        clipCount = 0;
        depth = 0;
        matrices[0] = SkinMatrix.Identity;
        opacities[0] = 1;
        additive[0] = false;
        skin = owner;
        UsedImages = false;
        imageUv.Clear();
        imageQuads.Clear();
    }

    internal JObject EndDump()
    {
        DumpMode = false;
        return DumpRoot;
    }

    // =================================================================
    // Animation walk
    // =================================================================

    private void DrawAnimation(CompiledSkinAnimation animation, double p, SkinDrawContext ctx,
        SkinInputTable inputTable, in SkinAnimationClock clock)
    {
        if (depth >= MaxDepth - 2) throw new SkinRenderException("animation depth exceeds 40");
        var env = envs[depth];
        env.Reset(animation, inputTable, ctx, skin, p);
        var rootOpacity = animation.RootOpacity != null ? (double?) animation.RootOpacity.Eval(env) : null;
        var rootTransform = new SkinTransformValues(animation.RootTransform, env);
        BeginGroup(rootOpacity, animation.RootTransform, rootTransform, animation.Isolate, false, null);
        if (animation.IsSequence && p >= 0 && p <= 1)
        {
            var frames = animation.SequenceFrames;
            var position = !animation.Fps.HasValue
                ? Math.Max(0, p) * frames.Length
                : Math.Max(0, clock.Elapsed) * animation.Fps.Value;
            var index = Math.Min((int) Math.Floor(position), frames.Length - 1);
            DrawFrameNode(frames[index], env, p, clock, null);
        }
        else if (animation.Layers != null)
        {
            foreach (var layer in animation.Layers)
                DrawLayer(layer, env, p, clock);
        }
        EndGroup();
    }

    private void DrawLayer(CompiledLayer layer, SkinEnv env, double p, in SkinAnimationClock clock)
    {
        var at = layer.At;
        var until = layer.Until;
        if (layer.Windowed && (p < at || p > until || (p == until && until != 1)))
            return;
        if (layer.Gate != null && !layer.Gate.Open(p, env))
        {
            if (layer.Otherwise != null) DrawLayer(layer.Otherwise, env, p, clock);
            return;
        }
        var localClock = layer.Windowed
            ? new SkinAnimationClock(clock.Elapsed - at * clock.Duration, (until - at) * clock.Duration)
            : clock;
        var localP = layer.Windowed ? (p - at) / (until - at) : p;

        // TS draws the inner node, then wraps: blend group → (frame subtree |
        // animation + opacity/transform wrapper). Group nesting order in dumps:
        // blend wraps the animation-source wrapper.
        var openedBlend = layer.Add;
        if (openedBlend) BeginGroup(null, null, default, false, true, null);

        if (layer.Frame != null)
        {
            if (layer.WrapTransform)
            {
                var wrapper = new SkinTransformValues(layer.WrapperTransform, env);
                BeginGroup(null, layer.WrapperTransform, wrapper, false, false, null);
                DrawFrameNode(layer.Frame, env, localP, localClock, layer.Children);
                EndGroup();
            }
            else
            {
                DrawFrameNode(layer.Frame, env, localP, localClock, layer.Children);
            }
        }
        else
        {
            var local = layer.HasSample
                ? Math.Max(0, Math.Min(1, layer.SampleFrom.Eval(env) +
                                          (layer.SampleTo.Eval(env) - layer.SampleFrom.Eval(env)) * localP))
                : localP;
            var source = layer.ResolvedAnimation;
            var sampleDuration = layer.SampleDuration >= 0 ? layer.SampleDuration : clock.Duration;
            var elapsed = layer.HasSample ? local * sampleDuration : clock.Elapsed;
            var wrapperOpacity = layer.AnimOpacity != null ? (double?) layer.AnimOpacity.Eval(env) : null;
            var wrapperTransform = layer.AnimTransform != null
                ? new SkinTransformValues(layer.AnimTransform, env)
                : default;
            var openedWrapper = wrapperOpacity != null || layer.AnimTransform != null;
            if (openedWrapper)
                BeginGroup(wrapperOpacity, layer.AnimTransform, wrapperTransform, wrapperOpacity != null,
                    false, null);
            DrawAnimation(source, local, env.Ctx, env.Inputs, new SkinAnimationClock(elapsed, clock.Duration));
            if (openedWrapper) EndGroup();
        }

        if (openedBlend) EndGroup();
    }

    // =================================================================
    // Frame walk
    // =================================================================

    private void DrawFrameNode(CompiledFrameNode node, SkinEnv env, double p,
        in SkinAnimationClock clock, CompiledLayer[] children)
    {
        var opacity = node.HasOpacity ? node.Opacity.Eval(env) : 1.0;
        if (node.Kind == SkinFrameKind.Group)
        {
            var transform = new SkinTransformValues(node.Transform, env);
            BeginGroup(node.HasOpacity ? opacity : (double?) null, node.Transform, transform,
                node.Isolate, node.IsAdd, node.Clip, env);
            foreach (var child in node.Children) DrawFrameNode(child, env, p, clock, null);
            if (children != null)
                foreach (var child in children)
                    DrawLayer(child, env, p, clock);
            EndGroup();
            return;
        }
        if (SkinPaintStats.Enabled)
        {
            // Tessellation scope: leaf param evaluation + FlatGeom + transform-append.
            var leafStart = System.Diagnostics.Stopwatch.GetTimestamp();
            if (node.Kind == SkinFrameKind.Ring) DrawRing(node, env, opacity);
            else DrawLeaf(node, env, opacity);
            SkinPaintStats.TessellateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - leafStart;
            SkinPaintStats.LeafCount++;
            return;
        }
        if (node.Kind == SkinFrameKind.Ring)
        {
            DrawRing(node, env, opacity);
            return;
        }
        DrawLeaf(node, env, opacity);
    }

    private void DrawLeaf(CompiledFrameNode node, SkinEnv env, double opacity)
    {
        var alpha = opacities[depth] * opacity;
        var add = additive[depth] || node.IsAdd;
        var matrix = matrices[depth];
        var dump = DumpMode ? OpenDumpLeaf(node, env, opacity, add) : null;

        double[] dash = null;
        var dashOffset = 0.0;
        if (node.StaticDash != null || node.DashExpr != null)
        {
            var count = node.DashExpr?.Length ?? node.StaticDash.Length;
            if (!DumpMode)
            {
                EnsureScratchDash(count);
                dash = scratchDash;
            }
            else
            {
                dash = new double[count];
            }
            for (var i = 0; i < count; i++)
            {
                dash[i] = node.DashExpr != null
                    ? Math.Max(0, node.DashExpr[i].Eval(env))
                    : Math.Max(0, node.StaticDash[i]);
            }
            if (DumpMode)
            {
                var array = new JArray();
                foreach (var value in dash) array.Add(value);
                dump["dash"] = array;
            }
        }
        if (node.HasDashOffset)
        {
            dashOffset = node.DashOffset.Eval(env);
            if (DumpMode) dump["dashOffset"] = dashOffset;
        }

        switch (node.Kind)
        {
            case SkinFrameKind.Circle:
            {
                var r = Math.Max(0, node.R.Eval(env));
                var cx = node.Cx.Eval(env);
                var cy = node.Cy.Eval(env);
                if (DumpMode)
                {
                    DumpOptional(dump, "cx", node.CxPresent, cx);
                    DumpOptional(dump, "cy", node.CyPresent, cy);
                    dump["r"] = r;
                }
                if (node.Fill != null)
                {
                    var fill = node.Fill.Value.Resolve(env);
                    if (DumpMode) dump["fill"] = ColorString(fill);
                    var color = Tint(fill, alpha);
                    if (!DumpMode)
                    {
                        FlatGeom.Disk(scratch, cx, cy, r, color);
                        AppendTransformed(add, matrix);
                    }
                }
                if (node.Stroke != null)
                {
                    var stroke = node.Stroke.Value.Resolve(env);
                    var width = node.HasStrokeWidth ? node.StrokeWidth.Eval(env) : 1;
                    if (DumpMode)
                    {
                        dump["stroke"] = ColorString(stroke);
                        if (node.HasStrokeWidth) dump["strokeWidth"] = width;
                    }
                    else if (width > 0)
                    {
                        var color = Tint(stroke, alpha);
                        FlatGeom.Ring(scratch, cx, cy, r, width, color);
                        AppendTransformed(add, matrix);
                    }
                }
                break;
            }
            case SkinFrameKind.Arc:
            {
                var r = node.R.Eval(env);
                var start = node.Start.Eval(env);
                var end = node.End.Eval(env);
                var cx = node.Cx.Eval(env);
                var cy = node.Cy.Eval(env);
                var width = node.StrokeWidth.Eval(env);
                var stroke = node.Stroke.Value.Resolve(env);
                if (DumpMode)
                {
                    DumpOptional(dump, "cx", node.CxPresent, cx);
                    DumpOptional(dump, "cy", node.CyPresent, cy);
                    dump["r"] = r;
                    dump["start"] = start;
                    dump["end"] = end;
                    dump["stroke"] = ColorString(stroke);
                    dump["strokeWidth"] = width;
                    dump["cap"] = node.HasCap ? CapName(node.Cap) : "butt";
                }
                else if (width > 0 && Math.Abs(end - start) >= 1e-4)
                {
                    var color = Tint(stroke, alpha);
                    var cap = node.HasCap ? node.Cap : FlatCap.Butt;
                    if (dash != null)
                    {
                        // NO REALWORLD TEST: dashed arcs — sample the arc to a
                        // polyline and walk the dash pattern along it (canvas
                        // parity: each on-run is a stroked sub-path with the node cap).
                        var steps = Math.Max(2, (int) Math.Ceiling(Math.Abs(end - start) / 0.2));
                        if (pointScratch.Length < (steps + 1) * 2)
                            throw new SkinRenderException("dashed arc sampling budget exceeded");
                        for (var i = 0; i <= steps; i++)
                        {
                            var a = start + (end - start) * i / steps;
                            pointScratch[i * 2] = cx + Math.Sin(a) * r;
                            pointScratch[i * 2 + 1] = cy - Math.Cos(a) * r;
                        }
                        DashWalk(scratch, (steps + 1) * 2, false, dash, dashOffset, width, color, cap);
                        AppendTransformed(add, matrix);
                    }
                    else
                    {
                        FlatGeom.Arc(scratch, cx, cy, r, start, end, width, color, cap == FlatCap.Round);
                        if (cap == FlatCap.Square)
                        {
                            // NO REALWORLD TEST: square caps extend each arc end by
                            // half the stroke along the tangent ((cos a, sin a) for
                            // the clockwise parametrisation) — two butt lines.
                            var sx = cx + Math.Sin(start) * r;
                            var sy = cy - Math.Cos(start) * r;
                            var tx = cx + Math.Sin(end) * r;
                            var ty = cy - Math.Cos(end) * r;
                            var half = width / 2;
                            FlatGeom.Line(scratch, sx - Math.Cos(start) * half, sy - Math.Sin(start) * half, sx, sy, width, color, FlatCap.Butt);
                            FlatGeom.Line(scratch, tx, ty, tx + Math.Cos(end) * half, ty + Math.Sin(end) * half, width, color, FlatCap.Butt);
                        }
                        AppendTransformed(add, matrix);
                    }
                }
                break;
            }
            case SkinFrameKind.Rect:
            {
                var x = node.X.Eval(env);
                var y = node.Y.Eval(env);
                var w = node.W.Eval(env);
                var h = node.H.Eval(env);
                var rx = node.Rx.Eval(env);
                if (DumpMode)
                {
                    dump["x"] = x;
                    dump["y"] = y;
                    dump["w"] = w;
                    dump["h"] = h;
                    DumpOptional(dump, "rx", node.RxPresent, rx);
                }
                if (node.Fill != null)
                {
                    var fill = node.Fill.Value.Resolve(env);
                    if (DumpMode) dump["fill"] = ColorString(fill);
                    if (!DumpMode)
                    {
                        var color = Tint(fill, alpha);
                        FlatGeom.RoundedRect(scratch, x, y, w, h, rx, color);
                        AppendTransformed(add, matrix);
                    }
                }
                if (node.Stroke != null)
                {
                    var stroke = node.Stroke.Value.Resolve(env);
                    var width = node.HasStrokeWidth ? node.StrokeWidth.Eval(env) : 1;
                    if (DumpMode)
                    {
                        dump["stroke"] = ColorString(stroke);
                        if (node.HasStrokeWidth) dump["strokeWidth"] = width;
                    }
                    else if (width > 0 && w > 0 && h > 0)
                    {
                        var color = Tint(stroke, alpha);
                        var count = RectContour(x, y, w, h, rx);
                        FlatGeom.PolyStroke(scratch, pointScratch, count, true, width, color,
                            node.HasJoin ? node.Join : FlatJoin.Miter,
                            node.HasCap ? node.Cap : FlatCap.Butt);
                        AppendTransformed(add, matrix);
                    }
                }
                break;
            }
            case SkinFrameKind.Line:
            {
                var x1 = node.X1.Eval(env);
                var y1 = node.Y1.Eval(env);
                var x2 = node.X2.Eval(env);
                var y2 = node.Y2.Eval(env);
                var width = node.StrokeWidth.Eval(env);
                var stroke = node.Stroke.Value.Resolve(env);
                var cap = node.HasCap ? node.Cap : FlatCap.Butt;
                if (DumpMode)
                {
                    dump["x1"] = x1;
                    dump["y1"] = y1;
                    dump["x2"] = x2;
                    dump["y2"] = y2;
                    dump["stroke"] = ColorString(stroke);
                    dump["strokeWidth"] = width;
                    if (node.HasCap) dump["cap"] = CapName(node.Cap);
                }
                else if (width > 0)
                {
                    var color = Tint(stroke, alpha);
                    if (dash != null)
                        FlatGeom.DashedLine(scratch, x1, y1, x2, y2, width, color, dash, dashOffset, cap);
                    else
                        FlatGeom.Line(scratch, x1, y1, x2, y2, width, color, cap);
                    AppendTransformed(add, matrix);
                }
                break;
            }
            case SkinFrameKind.Poly:
            {
                var count = node.PointCount * 2;
                if (pointScratch.Length < count) throw new SkinRenderException("poly point budget exceeded");
                for (var i = 0; i < node.PointCount; i++)
                {
                    pointScratch[i * 2] = node.PointX[i].Eval(env);
                    pointScratch[i * 2 + 1] = node.PointY[i].Eval(env);
                }
                if (DumpMode)
                {
                    var points = new JArray();
                    for (var i = 0; i < node.PointCount; i++)
                        points.Add(new JArray(pointScratch[i * 2], pointScratch[i * 2 + 1]));
                    dump["points"] = points;
                    if (node.Closed) dump["closed"] = true;
                    if (node.Fill != null) dump["fill"] = ColorString(node.Fill.Value.Resolve(env));
                    if (node.Stroke != null) dump["stroke"] = ColorString(node.Stroke.Value.Resolve(env));
                    if (node.HasStrokeWidth) dump["strokeWidth"] = node.StrokeWidth.Eval(env);
                }
                if (node.Fill != null && !DumpMode)
                {
                    var color = Tint(node.Fill.Value.Resolve(env), alpha);
                    FlatGeom.PolyFill(scratch, PolyBuffer(node.PointCount), color);
                    AppendTransformed(add, matrix);
                }
                if (node.Stroke != null && !DumpMode)
                {
                    var width = node.HasStrokeWidth ? node.StrokeWidth.Eval(env) : 1;
                    if (width > 0)
                    {
                        var color = Tint(node.Stroke.Value.Resolve(env), alpha);
                        FlatGeom.PolyStroke(scratch, pointScratch, count, node.Closed, width, color,
                            node.HasJoin ? node.Join : FlatJoin.Miter,
                            node.HasCap ? node.Cap : FlatCap.Butt);
                        AppendTransformed(add, matrix);
                    }
                }
                break;
            }
            case SkinFrameKind.Image:
            {
                if (DumpMode)
                {
                    dump["src"] = node.Src;
                    dump["x"] = node.X.Eval(env);
                    dump["y"] = node.Y.Eval(env);
                    dump["w"] = node.W.Eval(env);
                    dump["h"] = node.H.Eval(env);
                    break;
                }
                // Image quads ride the dedicated image sink: without one (note heads
                // this validation phase) or without a packed atlas, skip silently.
                var image = skin?.ImageOf(node.Src);
                if (image == null || !image.Packed || sinks.Image == null) break;
                AppendImageQuad(node.X.Eval(env), node.Y.Eval(env), node.W.Eval(env), node.H.Eval(env),
                    new Rgba(1f, 1f, 1f, (float) alpha), image, matrix);
                break;
            }
        }
    }

    /// <summary>runtime.ts emitRing — the band, then the stepped edge bands on its outer silhouette.</summary>
    private void DrawRing(CompiledFrameNode node, SkinEnv env, double opacity)
    // NO REALWORLD TEST: ring dash support reads the node's dash bindings inline.
    {
        var r = node.R.Eval(env);
        var width = node.Width.Eval(env);
        var segments = node.Segments;
        var k = node.Assemble.Eval(env);
        var spin = node.Spin.Eval(env);
        var stroke = node.Stroke.Value.Resolve(env);
        var closed = k >= 0.999;
        var span = Math.PI * 2 / segments;
        var half = span * k / 2;
        var alpha = opacities[depth] * opacity;
        var matrix = matrices[depth];
        var add = additive[depth] || node.IsAdd;
        var edge = node.Edge;
        var bands = edge != null ? EdgeBands(width, edge) : null;
        var hasEdge = bands != null && bands.Count > 0;
        var edgeColor = edge != null ? new SkinColor(node.EdgeRgba, edge.Color) : default;
        double[] nodeDash = null;
        var nodeDashOffset = 0.0;
        if (node.DashExpr != null)
        {
            nodeDash = new double[node.DashExpr.Length];
            for (var i = 0; i < nodeDash.Length; i++) nodeDash[i] = Math.Max(0, node.DashExpr[i].Eval(env));
        }
        else if (node.StaticDash != null) nodeDash = node.StaticDash;
        if (node.HasDashOffset) nodeDashOffset = node.DashOffset.Eval(env);

        if (hasEdge && DumpMode) OpenDumpGroup(null, null, default, false, false, null);
        RingBand(r, width, stroke, 1, closed, span, half, spin, segments, opacity, alpha, matrix, add);
        if (hasEdge)
        {
            var outer = r + width / 2;
            for (var i = 0; i < bands.Count; i++)
                RingBand(outer + bands[i].Offset, bands[i].Width, edgeColor, bands[i].Opacity,
                    closed, span, half, spin, segments, opacity, alpha, matrix, add);
        }
        if (hasEdge && DumpMode) EndGroup();
    }

    /// <summary>One assembling-ring band: a closed circle or a group of arcs (cap butt).
    /// Explicit parameters — captured local functions would allocate per call.</summary>
    private void RingBand(double rr, double w, in SkinColor color, double o, bool closed, double span, double half,
        double spin, int segments, double ringOpacity, double alpha, in SkinMatrix matrix, bool add)
    {
        var nodeOpacity = ringOpacity * o;
        var tint = Tint(color, alpha * o);
        if (closed)
        {
            var radius = Math.Max(0, rr);
            if (DumpMode)
            {
                AddChild(new JObject
                {
                    ["t"] = "circle",
                    ["r"] = radius,
                    ["stroke"] = ColorString(color),
                    ["strokeWidth"] = w,
                    ["opacity"] = nodeOpacity,
                });
                return;
            }
            FlatGeom.Ring(scratch, 0, 0, radius, w, tint);
            AppendTransformed(add, matrix);
            return;
        }
        if (DumpMode)
        {
            var group = new JObject { ["t"] = "g", ["opacity"] = nodeOpacity, ["children"] = new JArray() };
            var children = (JArray) group["children"];
            for (var i = 0; i < segments; i++)
            {
                var center = spin + i * span;
                children.Add(new JObject
                {
                    ["t"] = "arc",
                    ["r"] = rr,
                    ["start"] = center - half,
                    ["end"] = center + half,
                    ["strokeWidth"] = w,
                    ["stroke"] = ColorString(color),
                    ["cap"] = "butt",
                });
            }
            AddChild(group);
            return;
        }
        for (var i = 0; i < segments; i++)
        {
            var center = spin + i * span;
            FlatGeom.Arc(scratch, 0, 0, rr, center - half, center + half, w, tint, false);
            AppendTransformed(add, matrix);
        }
    }

    internal readonly struct EdgeBandResult
    {
        public readonly double Offset, Width, Opacity;

        public EdgeBandResult(double offset, double width, double opacity)
        {
            Offset = offset;
            Width = width;
            Opacity = opacity;
        }
    }

    /// <summary>runtime.ts edgeBands — ported exactly, including the inside clamp and skip cursor advance.
    /// Writes into the painter's scratch list (cleared per call; capacity retained).</summary>
    private readonly List<EdgeBandResult> edgeBandScratch = new List<EdgeBandResult>(8);

    private List<EdgeBandResult> EdgeBands(double width, SkinEdgeDoc edge)
    {
        var output = edgeBandScratch;
        output.Clear();
        var inside = edge.Inside;
        var cursor = 0.0;
        foreach (var band in edge.Bands)
        {
            var w = width * Math.Max(0, band.Fraction);
            if (inside) w = Math.Min(w, width - cursor);
            if (!(w > 0) || !(band.Opacity > 0))
            {
                cursor += Math.Max(0, w);
                continue;
            }
            var c = cursor + w / 2;
            output.Add(new EdgeBandResult(inside ? -c : c, w, Math.Min(1, band.Opacity)));
            cursor += w;
        }
        return output;
    }

    // =================================================================
    // Group stack (transform / opacity / blend / dump nodes)
    // =================================================================

    private void BeginGroup(double? opacity, SkinTransformExpr transform, in SkinTransformValues values,
        bool isolate, bool add, SkinClipExpr clip, SkinEnv clipEnv = null)
    {
        var matrix = matrices[depth];
        if (values.Present)
            matrix = matrix.Multiply(SkinMatrix.Compose(values.X, values.Y, values.Rotate, values.ScaleX, values.ScaleY));
        matrices[depth + 1] = matrix;
        opacities[depth + 1] = opacities[depth] * (opacity ?? 1);
        additive[depth + 1] = add || additive[depth];
        depth++;
        if (clip != null && clipEnv != null && clipCount < MaxClips) PushClip(clip, clipEnv, matrices[depth]);
        if (DumpMode) OpenDumpGroup(opacity, transform, values, isolate, add, clip, clipEnv);
    }

    private void EndGroup()
    {
        depth--;
        while (clipCount > 0 && clipPushDepth[clipCount - 1] > depth) clipCount--;
        if (DumpMode && dumpStack.Count > 0) dumpStack.RemoveAt(dumpStack.Count - 1);
    }

    /// <summary>Builds the clip's convex polygon in root design space. The clip rect is
    /// authored in the group's local space — the same space children emit before
    /// their own transform — so corners ride the post-group matrix.</summary>
    private void PushClip(SkinClipExpr clip, SkinEnv env, in SkinMatrix matrix)
    {
        var baseIndex = clipCount * MaxClipVerts;
        var count = 0;
        if (clip.IsCircle)
        {
            const int segments = 16;
            var cx = clip.Cx.Eval(env);
            var cy = clip.Cy.Eval(env);
            var r = clip.R.Eval(env);
            for (var i = 0; i < segments; i++)
            {
                var a = Math.PI * 2 * i / segments;
                var (x, y) = matrix.Apply(cx + Math.Cos(a) * r, cy + Math.Sin(a) * r);
                clipX[baseIndex + count] = x;
                clipY[baseIndex + count] = y;
                count++;
            }
        }
        else
        {
            var x0 = clip.X.Eval(env);
            var y0 = clip.Y.Eval(env);
            var x1 = x0 + clip.W.Eval(env);
            var y1 = y0 + clip.H.Eval(env);
            var c0 = matrix.Apply(x0, y0);
            var c1 = matrix.Apply(x1, y0);
            var c2 = matrix.Apply(x1, y1);
            var c3 = matrix.Apply(x0, y1);
            clipX[baseIndex] = c0.X; clipY[baseIndex] = c0.Y;
            clipX[baseIndex + 1] = c1.X; clipY[baseIndex + 1] = c1.Y;
            clipX[baseIndex + 2] = c2.X; clipY[baseIndex + 2] = c2.Y;
            clipX[baseIndex + 3] = c3.X; clipY[baseIndex + 3] = c3.Y;
            count = 4;
        }
        // A flip matrix (e.g. the body's scaleY -1) reverses winding; the
        // inside test assumes counter-clockwise in y-down coords, so normalize.
        double area = 0;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            area += clipX[baseIndex + i] * clipY[baseIndex + j] - clipX[baseIndex + j] * clipY[baseIndex + i];
        }
        if (area < 0)
        {
            for (var i = 0; i < count / 2; i++)
            {
                var j = count - 1 - i;
                (clipX[baseIndex + i], clipX[baseIndex + j]) = (clipX[baseIndex + j], clipX[baseIndex + i]);
                (clipY[baseIndex + i], clipY[baseIndex + j]) = (clipY[baseIndex + j], clipY[baseIndex + i]);
            }
        }
        clipPushDepth[clipCount] = depth; // group depth that owns this clip
        clipVertCount[clipCount] = count;
        clipCount++;
    }

    /// <summary>True when p is inside the convex clip edge a→b (poly wound consistently).</summary>
    private static bool InsideClip(double ax, double ay, double bx, double by, double px, double py) =>
        (bx - ax) * (py - ay) - (by - ay) * (px - ax) >= 0;

    private static double ClipIntersect(double ax, double ay, double bx, double by,
        double cx2, double cy2, double dx2, double dy2)
    {
        var den = (bx - ax) * (dy2 - cy2) - (by - ay) * (dx2 - cx2);
        if (Math.Abs(den) < 1e-12) return 0;
        return ((cx2 - ax) * (dy2 - cy2) - (cy2 - ay) * (dx2 - cx2)) / den;
    }

    private void PushTransform(in SkinMatrix matrix)
    {
        matrices[depth + 1] = matrices[depth].Multiply(matrix);
        opacities[depth + 1] = opacities[depth];
        additive[depth + 1] = additive[depth];
        depth++;
    }

    // =================================================================
    // Emission: tessellate into scratch, then transform-append to the sink
    // =================================================================

    private void AppendTransformed(bool add, in SkinMatrix matrix)
    {
        var source = scratch;
        if (source.VertCount == 0) return;
        var target = add && sinks.Add != null ? sinks.Add : sinks.Normal;
        var targetStart = target.VertCount;
        var verts = source.Verts;
        var count = source.VertCount;
        if (clipCount == 0)
        {
            for (var i = 0; i < count; i++)
            {
                var o = i * 7;
                var (x, y) = matrix.Apply(verts[o], verts[o + 1]);
                target.Verts.Add((float) x);
                target.Verts.Add((float) y);
                target.Verts.Add(verts[o + 2]);
                target.Verts.Add(verts[o + 3]);
                target.Verts.Add(verts[o + 4]);
                target.Verts.Add(verts[o + 5]);
                target.Verts.Add(verts[o + 6]);
            }
            foreach (var index in source.Tris) target.Tris.Add(index + targetStart);
            source.Reset();
            return;
        }

        // Clip each transformed triangle against the active convex clip polygons
        // (Sutherland-Hodgman, colors interpolated), then fan-triangulate.
        for (var t2 = 0; t2 < source.Tris.Count; t2 += 3)
        {
            var poly = 0;
            for (var v = 0; v < 3; v++)
            {
                var o = source.Tris[t2 + v] * 7;
                var (x, y) = matrix.Apply(verts[o], verts[o + 1]);
                polyX[poly] = x; polyY[poly] = y;
                polyR[poly] = verts[o + 3]; polyG[poly] = verts[o + 4];
                polyB[poly] = verts[o + 5]; polyA[poly] = verts[o + 6];
                poly++;
            }
            for (var c = 0; c < clipCount && poly >= 3; c++)
            {
                var baseIndex = c * MaxClipVerts;
                var clipVerts = clipVertCount[c];
                for (var e = 0; e < clipVerts; e++)
                {
                    // Per-edge output starts fresh: the previous edge's result was
                    // already copied back into poly; accumulating here would splice
                    // stale outside vertices into the clipped polygon.
                    var outCount = 0;
                    var ax = clipX[baseIndex + e];
                    var ay = clipY[baseIndex + e];
                    var bx = clipX[baseIndex + (e + 1) % clipVerts];
                    var by = clipY[baseIndex + (e + 1) % clipVerts];
                    for (var s = 0; s < poly; s++)
                    {
                        var sNext = (s + 1) % poly;
                        var sx = polyX[s]; var sy = polyY[s];
                        var tx = polyX[sNext]; var ty = polyY[sNext];
                        var sIn = InsideClip(ax, ay, bx, by, sx, sy);
                        var tIn = InsideClip(ax, ay, bx, by, tx, ty);
                        if (sIn && tIn)
                        {
                            outX[outCount] = tx; outY[outCount] = ty;
                            outR[outCount] = polyR[sNext]; outG[outCount] = polyG[sNext];
                            outB[outCount] = polyB[sNext]; outA[outCount] = polyA[sNext];
                            outCount++;
                        }
                        else if (sIn != tIn)
                        {
                            var k = ClipIntersect(sx, sy, tx, ty, ax, ay, bx, by);
                            outX[outCount] = sx + (tx - sx) * k;
                            outY[outCount] = sy + (ty - sy) * k;
                            outR[outCount] = polyR[s] + (polyR[sNext] - polyR[s]) * (float) k;
                            outG[outCount] = polyG[s] + (polyG[sNext] - polyG[s]) * (float) k;
                            outB[outCount] = polyB[s] + (polyB[sNext] - polyB[s]) * (float) k;
                            outA[outCount] = polyA[s] + (polyA[sNext] - polyA[s]) * (float) k;
                            outCount++;
                            if (tIn)
                            {
                                outX[outCount] = tx; outY[outCount] = ty;
                                outR[outCount] = polyR[sNext]; outG[outCount] = polyG[sNext];
                                outB[outCount] = polyB[sNext]; outA[outCount] = polyA[sNext];
                                outCount++;
                            }
                        }
                    }
                    if (outCount < 3) { poly = 0; break; }
                    for (var cp = 0; cp < outCount; cp++)
                    {
                        polyX[cp] = outX[cp]; polyY[cp] = outY[cp];
                        polyR[cp] = outR[cp]; polyG[cp] = outG[cp];
                        polyB[cp] = outB[cp]; polyA[cp] = outA[cp];
                    }
                    poly = outCount;
                }
            }
            // Fan triangulate around vertex 0.
            var firstIndex = target.VertCount;
            for (var v = 0; v < poly; v++)
            {
                target.Verts.Add((float) polyX[v]);
                target.Verts.Add((float) polyY[v]);
                target.Verts.Add(0f);
                target.Verts.Add(polyR[v]);
                target.Verts.Add(polyG[v]);
                target.Verts.Add(polyB[v]);
                target.Verts.Add(polyA[v]);
            }
            for (var f = 1; f + 1 < poly; f++)
            {
                target.Tris.Add(firstIndex);
                target.Tris.Add(firstIndex + f);
                target.Tris.Add(firstIndex + f + 1);
            }
        }
        source.Reset();
    }

    /// <summary>Image leaf: one textured quad emitted straight into the image sink
    /// under the current matrix. Deviation from the vector path: group clips do
    /// not apply to images this round — textured quads would need UV-space
    /// clipping, so with active clips the quad still emits unclipped. Vertex
    /// order TL, TR, BR, BL; UVs append to the painter's list in the same order:
    /// design space is +y down, Unity UVs +y up, so the quad's top edge samples
    /// v1 and its bottom edge v0.</summary>
    private void AppendImageQuad(double x, double y, double w, double h, in Rgba color,
        SkinImage image, in SkinMatrix matrix)
    {
        var target = sinks.Image;
        var start = target.VertCount;
        var (tx0, ty0) = matrix.Apply(x, y);
        var (tx1, ty1) = matrix.Apply(x + w, y);
        var (tx2, ty2) = matrix.Apply(x + w, y + h);
        var (tx3, ty3) = matrix.Apply(x, y + h);
        target.Vert(tx0, ty0, 0, color);
        target.Vert(tx1, ty1, 0, color);
        target.Vert(tx2, ty2, 0, color);
        target.Vert(tx3, ty3, 0, color);
        target.Quad(start, start + 1, start + 2, start + 3);
        imageUv.Add(image.U0); imageUv.Add(image.V1); // top-left
        imageUv.Add(image.U1); imageUv.Add(image.V1); // top-right
        imageUv.Add(image.U1); imageUv.Add(image.V0); // bottom-right
        imageUv.Add(image.U0); imageUv.Add(image.V0); // bottom-left
        imageQuads.Add(image);
        UsedImages = true;
    }

    private void EnsureScratchDash(int count)
    {
        if (scratchDash.Length < count) scratchDash = new double[Math.Max(count, scratchDash.Length * 2)];
    }

    /// <summary>PolyFill reads the whole array — exact-size buffers cached per point count.</summary>
    private double[] PolyBuffer(int pointCount)
    {
        if (!polyBuffers.TryGetValue(pointCount, out var buffer))
        {
            buffer = new double[pointCount * 2];
            polyBuffers[pointCount] = buffer;
        }
        Array.Copy(pointScratch, buffer, pointCount * 2);
        return buffer;
    }

    private int RectContour(double x, double y, double w, double h, double rx)
    {
        rx = Math.Max(0, Math.Min(rx, Math.Min(w, h) / 2));
        if (rx <= 0)
        {
            var i = 0;
            pointScratch[i++] = x;
            pointScratch[i++] = y;
            pointScratch[i++] = x + w;
            pointScratch[i++] = y;
            pointScratch[i++] = x + w;
            pointScratch[i++] = y + h;
            pointScratch[i++] = x;
            pointScratch[i++] = y + h;
            return i;
        }
        // Corners sampled as real arcs (sagitta-bounded, like FlatGeom.RoundedRect):
        // the 2-sample chamfer turned large-radius capsule ends into octagon cuts.
        var count = 0;
        var cornerSegments = Math.Max(2, (int) Math.Ceiling(Math.PI / 4 /
            Math.Acos(Math.Max(-1.0, 1 - 0.25 / Math.Max(rx, 1e-9)))));
        for (var corner = 0; corner < 4; corner++)
        {
            double cx, cy;
            double a0;
            switch (corner)
            {
                case 0: cx = x + w - rx; cy = y + rx; a0 = -Math.PI / 2; break;
                case 1: cx = x + w - rx; cy = y + h - rx; a0 = 0; break;
                case 2: cx = x + rx; cy = y + h - rx; a0 = Math.PI / 2; break;
                default: cx = x + rx; cy = y + rx; a0 = Math.PI; break;
            }
            for (var i = 0; i <= cornerSegments; i++)
            {
                var a = a0 + (Math.PI / 2) * i / cornerSegments;
                pointScratch[count++] = cx + Math.Cos(a) * rx;
                pointScratch[count++] = cy + Math.Sin(a) * rx;
            }
        }
        return count;
    }

    /// <summary>NO REALWORLD TEST: walks a dash pattern along the polyline in
    /// pointScratch (length doubles), emitting one stroked Line per on-run.
    /// Pattern semantics mirror FlatGeom.DashedLine: odd patterns double, positive
    /// dashOffset shifts dash starts forward, period-0 falls back to a solid
    /// stroke. Runs close at vertices (no cross-segment merge) — the seam caps
    /// match a butt-joined polyline.</summary>
    private void DashWalk(FlatMeshBuilder b, int length, bool closed, double[] dash, double dashOffset,
        double width, Rgba color, FlatCap cap)
    {
        var pattern = dash.Length % 2 == 0 ? dash : dash.Concat(dash).ToArray();
        var period = 0.0;
        foreach (var d in pattern) period += d;
        if (period <= 0)
        {
            FlatGeom.PolyStroke(b, pointScratch, length, closed, width, color, FlatJoin.Miter, cap);
            return;
        }

        var phase = -dashOffset % period;
        if (phase < 0) phase += period;
        var idx = 0;
        while (phase >= pattern[idx])
        {
            phase -= pattern[idx];
            idx = (idx + 1) % pattern.Length;
        }
        var remain = pattern[idx] - phase;

        var segments = closed ? length / 2 : length / 2 - 1;
        for (var s = 0; s < segments; s++)
        {
            var x1 = pointScratch[s * 2];
            var y1 = pointScratch[s * 2 + 1];
            var x2 = pointScratch[(s + 1) * 2 % length];
            var y2 = pointScratch[(s + 1) * 2 + 1 % length];
            var dx = x2 - x1;
            var dy = y2 - y1;
            var d = Math.Sqrt(dx * dx + dy * dy);
            if (d <= 1e-9) continue;
            dx /= d;
            dy /= d;
            var pos = 0.0;
            double runStart = double.NaN, runEnd = 0;
            while (pos < d - 1e-9)
            {
                var step = Math.Min(remain, d - pos);
                if (idx % 2 == 0)
                {
                    if (double.IsNaN(runStart)) runStart = pos;
                    runEnd = pos + step;
                }
                else if (!double.IsNaN(runStart))
                {
                    EmitRun(x1, y1, dx, dy, runStart, runEnd);
                    runStart = double.NaN;
                }
                pos += step;
                remain -= step;
                if (remain <= 1e-12)
                {
                    idx = (idx + 1) % pattern.Length;
                    remain = pattern[idx];
                }
            }
            if (!double.IsNaN(runStart)) EmitRun(x1, y1, dx, dy, runStart, runEnd);
        }

        void EmitRun(double x1, double y1, double dx, double dy, double from, double to) =>
            FlatGeom.Line(b, x1 + dx * from, y1 + dy * from, x1 + dx * to, y1 + dy * to, width, color, cap);
    }

    private static Rgba Tint(in SkinColor color, double alpha) =>
        new Rgba(color.Value.R, color.Value.G, color.Value.B, (float) (color.Value.A * alpha));

    private static string CapName(FlatCap cap) => cap switch
    {
        FlatCap.Butt => "butt",
        FlatCap.Round => "round",
        FlatCap.Square => "square",
        _ => "butt",
    };

    private static string JoinName(FlatJoin join) => join switch
    {
        FlatJoin.Miter => "miter",
        FlatJoin.Round => "round",
        FlatJoin.Bevel => "bevel",
        _ => "miter",
    };

    private static string ColorString(in SkinColor color) =>
        color.Authored ?? SkinColorMath.ToHexString(color.Value);

    // =================================================================
    // Dump mode (parity corpus)
    // =================================================================

    private void OpenDumpGroup(double? opacity, SkinTransformExpr transform, in SkinTransformValues values,
        bool isolate, bool add, SkinClipExpr clip, SkinEnv env = null)
    {
        var node = new JObject { ["t"] = "g" };
        if (opacity != null) node["opacity"] = opacity;
        if (add) node["blend"] = "add";
        if (isolate) node["isolate"] = true;
        if (values.Present)
        {
            var tf = new JObject();
            if (values.HasX) tf["x"] = values.X;
            if (values.HasY) tf["y"] = values.Y;
            if (values.HasRotate) tf["rotate"] = values.Rotate;
            if (values.HasScale) tf["scale"] = values.Scale;
            if (values.HasScaleX) tf["scaleX"] = values.ScaleX;
            if (values.HasScaleY) tf["scaleY"] = values.ScaleY;
            node["tf"] = tf;
        }
        if (clip != null) node["clip"] = DumpClip(clip, env);
        node["children"] = new JArray();
        AddChild(node);
        dumpStack.Add(node);
    }

    private JObject DumpClip(SkinClipExpr clip, SkinEnv env)
    {
        var node = new JObject { ["type"] = clip.IsCircle ? "circle" : "rect" };
        if (clip.IsCircle)
        {
            if (clip.Cx.Present) node["cx"] = clip.Cx.Eval(env);
            if (clip.Cy.Present) node["cy"] = clip.Cy.Eval(env);
            node["r"] = clip.R.Eval(env);
        }
        else
        {
            node["x"] = clip.X.Eval(env);
            node["y"] = clip.Y.Eval(env);
            node["w"] = clip.W.Eval(env);
            node["h"] = clip.H.Eval(env);
            if (clip.Rx.Present) node["rx"] = clip.Rx.Eval(env);
        }
        return node;
    }

    private void AddChild(JObject node)
    {
        if (dumpStack.Count == 0)
        {
            DumpRoot = node;
            return;
        }
        ((JArray) dumpStack[dumpStack.Count - 1]["children"]).Add(node);
    }

    private JObject OpenDumpLeaf(CompiledFrameNode node, SkinEnv env, double opacity, bool add)
    {
        var dump = new JObject
        {
            ["t"] = node.Kind == SkinFrameKind.Circle ? "circle"
                : node.Kind == SkinFrameKind.Arc ? "arc"
                : node.Kind == SkinFrameKind.Rect ? "rect"
                : node.Kind == SkinFrameKind.Line ? "line"
                : node.Kind == SkinFrameKind.Image ? "image" : "poly",
        };
        if (node.HasOpacity) dump["opacity"] = opacity;
        if (add) dump["blend"] = "add";
        if (node.HasJoin && node.Kind != SkinFrameKind.Circle) dump["join"] = JoinName(node.Join);
        if (node.HasCap && (node.Kind == SkinFrameKind.Rect || node.Kind == SkinFrameKind.Line ||
                            node.Kind == SkinFrameKind.Poly))
            dump["cap"] = CapName(node.Cap);
        AddChild(dump);
        return dump;
    }

    private static void DumpOptional(JObject dump, string key, bool present, double value)
    {
        if (present) dump[key] = value;
    }
}
