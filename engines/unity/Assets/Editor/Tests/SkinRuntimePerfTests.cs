// Skin runtime performance harness — A/B the note head hot path in the
// editor (SMOKE/RELATIVE numbers only: editor-loop timing is not device data;
// the zero-GC verdict IS valid because it measures the seam calls directly).
//
//   Unity -batchmode -projectPath . -runTests -testPlatform Editor \
//          -testResults skin-perf-results.xml -logFile skin-perf.log
//          -testFilter "SkinRuntimePerfTests"      (harness only)
//
// Modes (all in one run, three phases over the same fixed scene):
//   skin-full  — FillHeadContext + SkinPainter.DrawNoteSlot + fused
//                FlatNoteMesh.Upload (the production seam, minus Note plumbing)
//   skin-eval  — painter only, no upload (attribution: interpretation vs
//                tessellation-transfer-upload)
//
// (The old imperative DrawEnter comparator died with the imperative flat path;
// the skin runtime is the only flat implementation now.)
//
// Scene: 60 concurrent click heads under an orthographic camera rendered into
// a RenderTexture in batchmode (draw/SetPass counters tick). ⅓ entering
// (p sweep), ⅓ steady at p = 1 (rebuild-skip steady state), ⅓ exiting
// (exit driver sweep; the exit slot owns rewind + fade).
// Output: skin-perf-report.json in the project root (SKIN_PERF_JSON overrides).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

public class SkinRuntimePerfTests
{
    private const int WarmupFrames = 150;
    private const int MeasureFrames = 300;
    private const int NoteCount = 60;
    private const double Approach = 1.2;

    private GameObject sceneRoot;
    private Camera camera;
    private RenderTexture renderTexture;
    private readonly FlatMeshBuilder scratch = new FlatMeshBuilder();

    [SetUp]
    public void SetUp()
    {
        SkinRuntimeSeamTests.CompileBuiltInSkin();
        sceneRoot = new GameObject("SkinPerf");
        var camGo = new GameObject("SkinPerfCam");
        camGo.transform.SetParent(sceneRoot.transform, false);
        camera = camGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
        camera.enabled = false; // manual Render() only — deterministic per-frame cost
    }

    [TearDown]
    public void TearDown()
    {
        if (renderTexture != null)
        {
            camera.targetTexture = null;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
        UnityEngine.Object.DestroyImmediate(sceneRoot);
        SkinPaintStats.Enabled = false;
    }

    private sealed class SimNote
    {
        public FlatNoteMesh Mesh;
        public int State; // 0 enter, 1 steady, 2 exit
        public double EnterP;
        public double ExitT;

        /// <summary>Processed = painted by the workload (steady skip applies identically in
        /// every phase); Uploads = actual mesh uploads (0 in skin-eval by design).</summary>
        public double Processed;
        public double Uploads;
    }

    [UnityTest]
    public IEnumerator PerfHarness_ImperativeVsSkinRuntime()
    {
        var skin = FlatSkinRuntime.Current;
        var enterSlot = skin.Slot(NoteKind.Click, "enter");
        var exitSlot = skin.Slot(NoteKind.Click, "exit");
        Assume.That(enterSlot, Is.Not.Null);
        Assume.That(exitSlot, Is.Not.Null);
        Assume.That(FlatSkinRuntime.CanPaintHead(NoteKind.Click), Is.True);

        var palettes = new SkinPaletteContext
        {
            Note = DefaultSkinPalettes.Family(FlatFamily.Click, FlatDirection.Up),
            Grades = new SkinGradePalettes(DefaultSkinPalettes.GradeColors),
            Family = (family, direction) => DefaultSkinPalettes.Family(family, direction),
        };
        var painter = new SkinPainter();
        var context = new SkinDrawContext();

        var notes = new List<SimNote>(NoteCount);
        for (var i = 0; i < NoteCount; i++)
        {
            var mesh = FlatNoteMesh.Create(sceneRoot.transform, "Head" + i, false, 0);
            mesh.SetWorldScale(0.03f); // design px → world, like a mid-zoom note
            var angle = i * Math.PI * 2 / NoteCount;
            mesh.transform.localPosition = new Vector3(
                (float) (Math.Cos(angle) * 5f), (float) (Math.Sin(angle) * 4f), 0f);
            notes.Add(new SimNote { Mesh = mesh, State = i % 3 });
        }

        // Manual RT render so draw/SetPass counters tick in batchmode — only when a
        // GPU is present (-nographics crashes on camera.Render; the run stays a pure
        // CPU measurement there, with zero draw/setpass samples by design).
        var canRender = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
        if (Application.isBatchMode && canRender)
        {
            renderTexture = new RenderTexture(1152, 640, 24, RenderTextureFormat.Default) { name = "SkinPerfRt" };
            renderTexture.Create();
            camera.targetTexture = renderTexture;
        }

        var report = new JObject
        {
            ["environment"] = $"{Application.unityVersion} editor batch={Application.isBatchMode}",
            ["notes"] = NoteCount,
            ["measureFrames"] = MeasureFrames,
            ["warning"] = "editor smoke/relative numbers; not device data",
        };

        yield return MeasurePhase("skin-full", report, notes, frame =>
        {
            foreach (var note in notes)
            {
                if (note.State == 1 && note.Processed > 0) continue; // same skip contract as the renderer
                scratch.Reset();
                var opacity = 1f;
                if (note.State == 2)
                {
                    // The exit slot owns rewind + fade; the engine envelope stays at 1.
                    FlatSkinRuntime.FillHeadContext(context, NoteKind.Click, palettes, false, Approach);
                    painter.DrawNoteSlot(exitSlot, note.ExitT, context, new SkinSinks(scratch));
                }
                else
                {
                    FlatSkinRuntime.FillHeadContext(context, NoteKind.Click, palettes, false, Approach);
                    painter.DrawNoteSlot(enterSlot, note.EnterP, context, new SkinSinks(scratch));
                }

                note.Mesh.Upload(scratch, opacity, opacity, scratch.VertCount);
                note.Processed++;
                note.Uploads++;
            }
        });

        // Identical paint workload and steady-skip contract as skin-full — minus
        // the upload. Processed counts stay comparable; Uploads stays 0.
        yield return MeasurePhase("skin-eval", report, notes, frame =>
        {
            foreach (var note in notes)
            {
                if (note.State == 1 && note.Processed > 0) continue;
                scratch.Reset();
                painter.DrawNoteSlot(note.State == 2 ? exitSlot : enterSlot,
                    note.State == 2 ? note.ExitT : note.EnterP, context, new SkinSinks(scratch));
                note.Processed++;
            }
        });

        var path = Environment.GetEnvironmentVariable("SKIN_PERF_JSON") ?? "skin-perf-report.json";
        File.WriteAllText(Path.GetFullPath(path), report.ToString(Newtonsoft.Json.Formatting.Indented));
        UnityEngine.Debug.Log("[SkinPerf] report written to " + Path.GetFullPath(path));
    }

    private IEnumerator MeasurePhase(string name, JObject report, List<SimNote> notes, Action<int> frameBody)
    {
        var drawCalls = TryRecorder(ProfilerCategory.Render, "Draw Calls Count", "Draw Calls", "Render.DrawCalls");
        var setPass = TryRecorder(ProfilerCategory.Render, "SetPass Calls Count", "SetPass Calls", "Render.SetPass Calls");
        var gcFrame = TryRecorder(ProfilerCategory.Memory, "GC Allocated In Frame", "GC.Allocated In Frame");

        foreach (var note in notes)
        {
            note.EnterP = 0;
            note.ExitT = 0;
            note.Processed = 0;
            note.Uploads = 0;
        }

        SkinPaintStats.Enabled = true;
        for (var f = 0; f < WarmupFrames; f++)
        {
            AdvanceDrivers(notes, f);
            frameBody(f);
            if (camera.targetTexture != null) camera.Render();
            if (f % 10 == 0) yield return null; // let the editor loop breathe; not measured
        }

        SkinPaintStats.Reset();
        foreach (var note in notes) { note.Processed = 0; note.Uploads = 0; } // measure-window counters only
        var phase = new JObject();
        report[name] = phase;
        var threadGcBefore = GC.GetAllocatedBytesForCurrentThread();
        var framesWithSeamGc = 0;
        double drawSamples = 0, setPassSamples = 0, gcRecorderSamples = 0, seamGcBytes = 0;
        long bodyTicks = 0;
        for (var f = 0; f < MeasureFrames; f++)
        {
            AdvanceDrivers(notes, f);
            // Timing and GC attribution cover ONLY the seam calls (+ render) — the
            // editor-frame wait between iterations stays outside both windows.
            var frameGc = GC.GetAllocatedBytesForCurrentThread();
            var frameStart = Stopwatch.GetTimestamp();
            frameBody(f);
            var seamDelta = GC.GetAllocatedBytesForCurrentThread() - frameGc;
            if (seamDelta > 0)
            {
                framesWithSeamGc++;
                seamGcBytes += seamDelta;
            }
            if (camera.targetTexture != null) camera.Render();
            bodyTicks += Stopwatch.GetTimestamp() - frameStart;
            drawSamples += Current(drawCalls);
            setPassSamples += Current(setPass);
            gcRecorderSamples += Current(gcFrame);
            yield return null;
        }
        SkinPaintStats.Enabled = false;

        var wallMs = bodyTicks * 1000.0 / Stopwatch.Frequency;
        var threadGc = GC.GetAllocatedBytesForCurrentThread() - threadGcBefore;
        phase["wallMs"] = Round(wallMs);
        phase["msPerFrame"] = Round(wallMs / MeasureFrames);
        phase["threadGcBytesPerFrame"] = Round(threadGc / (double) MeasureFrames);
        phase["framesWithSeamGc"] = framesWithSeamGc;
        phase["seamGcBytesPerFrame"] = Round(seamGcBytes / MeasureFrames);
        double processed = 0, uploads = 0;
        foreach (var note in notes)
        {
            processed += note.Processed;
            uploads += note.Uploads;
        }
        phase["processed"] = processed;
        phase["uploads"] = uploads;
        phase["drawCallsPerFrame"] = Round(drawSamples / MeasureFrames);
        phase["setPassPerFrame"] = Round(setPassSamples / MeasureFrames);
        phase["gcRecorderPerFrame"] = Round(gcRecorderSamples / MeasureFrames);
        phase["paintMs"] = Round(SkinPaintStats.Milliseconds(SkinPaintStats.PaintTicks));
        phase["evaluateMs"] = Round(SkinPaintStats.Milliseconds(SkinPaintStats.EvaluateTicks));
        phase["tessellateMs"] = Round(SkinPaintStats.Milliseconds(SkinPaintStats.TessellateTicks));
        phase["paintCount"] = SkinPaintStats.PaintCount;
        phase["leafCount"] = SkinPaintStats.LeafCount;

        drawCalls?.Dispose();
        setPass?.Dispose();
        gcFrame?.Dispose();
        UnityEngine.Debug.Log($"[SkinPerf] {name}: {wallMs / MeasureFrames:F4} ms/frame, " +
                              $"seam GC {seamGcBytes / MeasureFrames:F1} B/frame ({framesWithSeamGc} frames)");
    }

    private static double Round(double v) => Math.Round(v, 3);

    private static void AdvanceDrivers(List<SimNote> notes, int frame)
    {
        // ⅓ sweep the enter window, ⅓ hold the steady pose, ⅓ sweep the exit curve.
        foreach (var note in notes)
        {
            switch (note.State)
            {
                case 0:
                    note.EnterP = (frame % 300) / 300.0;
                    break;
                case 2:
                    note.ExitT = (frame % 90) / 300.0; // 0..0.3 s, the tap exit window
                    break;
            }
        }
    }

    private static double Current(ProfilerRecorder? recorder) => recorder.HasValue && recorder.Value.Valid ? recorder.Value.LastValue : 0;

    private static ProfilerRecorder? TryRecorder(ProfilerCategory category, params string[] names)
    {
        foreach (var name in names)
        {
            ProfilerRecorder recorder;
            try
            {
                recorder = new ProfilerRecorder(category, name, 1);
                if (recorder.Valid) return recorder;
                recorder.Dispose();
            }
            catch (ArgumentException)
            {
                // name not present in this Unity version — try the next spelling
            }
        }
        return null;
    }
}
