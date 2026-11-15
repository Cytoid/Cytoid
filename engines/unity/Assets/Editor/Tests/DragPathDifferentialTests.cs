using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Cytoid.Storyboard;
using Cytoid.Storyboard.Notes;
using Cytoid.Storyboard.Sprites;
using NUnit.Framework;
using UnityEngine;

public class DragPathDifferentialTests
{
    public static IEnumerable<int> Seeds => Enumerable.Range(0, 128);

    [TestCaseSource(nameof(Seeds))]
    public void EndpointAtJudgmentMatchesTheActualLiveResult(int seed)
    {
        var root = new GameObject("Differential probe " + seed);
        try
        {
            var random = new System.Random(seed);
            var game = root.AddComponent<DragPathTestGame>();
            var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
            chart.Model = new ChartModel();
            foreach (var pair in new[] {("baseSize", 1f), ("screenRatio", 1f),
                         ("horizontalRatio", .5f), ("verticalRatio", 1f)})
                typeof(Chart).GetField(pair.Item1, BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(chart, pair.Item2);
            var note = new ChartModel.Note {id = 2, start_time = 2, x = .25f, y = .25f,
                position = new Vector3(.25f, -.5f, .1f)};
            chart.Model.note_map.Add(note.id, note);
            game.Configure(chart);
            game.camera = root.AddComponent<Camera>();
            game.camera.orthographicSize = 1;
            var main = game.Storyboard.Renderer;
            var renderers = new List<StoryboardComponentRenderer>();
            for (var i = 0; i < 3; i++)
            {
                var controller = new NoteController {Id = "controller " + i};
                foreach (var time in new[] {-.5f, 0, .5f, .5f, 1, 1.5f, 2.5f})
                {
                    var state = new NoteControllerState {Note = note.id, Time = time,
                        OverrideX = Flag(random), X = Unit(random),
                        OverrideY = Flag(random), Y = Unit(random),
                        OverrideZ = Flag(random), Z = Unit(random),
                        XMultiplier = Number(random), YMultiplier = Number(random),
                        XOffset = Number(random), YOffset = Number(random)};
                    if (time == 1.5f && random.Next(5) == 0) state.Destroy = true;
                    controller.States.Add(state);
                }
                var renderer = new NoteControllerRenderer(main, controller);
                typeof(NoteControllerRenderer).GetProperty("Note").SetValue(renderer, note);
                main.ComponentRenderers.Add(controller.Id, renderer);
                main.NotePositions.Register(controller, float.NegativeInfinity);
                renderers.Add(renderer);
            }
            main.TypedComponentRenderers[typeof(NoteController)] = renderers;
            var frames = new[] {0, .17f, .61f, .88f, 1.03f, 1.41f, 1.79f, 2f};
            for (var frame = 0; frame < frames.Length; frame++)
            {
                game.Seek(frames[frame]);
                game.camera.orthographicSize = 1 + frame * .2f;
                if (frame == 3 || frame == 5)
                {
                    var id = "controller " + random.Next(3);
                    main.DestroyObjectsById(id);
                }
                main.UpdateNoteControls();
                var endpoint = main.NotePositions.GetEndpoint(note, chart);
                if (frame == frames.Length - 1)
                    Equal(endpoint, note.CalculatePosition(chart), "at the judgment frame");
            }
            var expected = note.CalculatePosition(chart);
            foreach (var id in main.ComponentRenderers.Keys.ToArray()) main.DestroyObjectsById(id);
            game.camera.orthographicSize = 9;
            Equal(main.NotePositions.GetEndpoint(note, chart), expected, "after all controllers stop");
        }
        finally
        {
            UnitFloat.Storyboard = null;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool? Flag(System.Random random)
    {
        var n = random.Next(4);
        return n < 2 ? (bool?) null : n == 2;
    }

    private static float? Number(System.Random random)
        => random.Next(3) == 0 ? (float?) (random.Next(-4, 9) / 4f) : null;

    private static UnitFloat Unit(System.Random random)
    {
        if (random.Next(4) == 0) return null;
        var unit = new[] {ReferenceUnit.World, ReferenceUnit.CameraY, ReferenceUnit.StageY}[random.Next(3)];
        return new UnitFloat(random.Next(-4, 9), unit, false, false);
    }

    private static void Equal(Vector3 actual, Vector3 expected, string context)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-5), "X " + context);
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-5), "Y " + context);
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(1e-5), "Z " + context);
    }
}
