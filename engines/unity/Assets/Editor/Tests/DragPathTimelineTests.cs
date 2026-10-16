using System;
using System.Reflection;
using System.Runtime.Serialization;
using Cytoid.Storyboard;
using Cytoid.Storyboard.Notes;
using NUnit.Framework;
using UnityEngine;

public class DragPathTimelineTests
{
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(6)]
    [TestCase(7)]
    public void FallingNodesDefineAFlatPathAtTheirOwnChartTimes(int type)
    {
        var timeline = new NotePositionTimeline();
        // Once Forgotten 36 -> 37, default AR. Values are in note-local units.
        var a = Note(36, 29.603504f, new Vector3(.875f, -.8f));
        var b = Note(37, 29.691610f, new Vector3(.75f, -.7f));
        a.type = b.type = type;
        timeline.Register(Fall(a, 1.4f, 0f, 1.375f), float.NegativeInfinity);
        timeline.Register(Fall(b, 1.4f, 0f, 1.375f), float.NegativeInfinity);
        var from = timeline.GetEndpoint(a, null);
        var to = timeline.GetEndpoint(b, null);
        for (var i = 0; i <= 20; i++)
        {
            var time = Mathf.Lerp(a.start_time, b.start_time, i / 20f);
            var head = DragPathGeometry.HeadPosition(from, to, time, a.start_time, b.start_time);
            Assert.That(head.y, Is.EqualTo(0f).Within(1e-6f));
        }
        Assert.That(a.Override.Y, Is.Null, "sampling must not mutate the live override");
        Assert.That(b.Override.Y, Is.Null);
    }

    [Test]
    public void NonzeroLandingHeightIsPreserved()
    {
        var n = Note(925, 106.575f, Vector3.zero);
        var timeline = new NotePositionTimeline();
        timeline.Register(Fall(n, 1.4f, .5f, 2.175f), float.NegativeInfinity);
        Assert.That(timeline.GetEndpoint(n, null).y, Is.EqualTo(.5f));
    }

    [Test]
    public void DifferentHeadAndChildTargetsStillFormADiagonalPath()
    {
        var a = Note(870, 10f, Vector3.zero);
        var b = Note(871, 11f, Vector3.zero);
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(870, State(0, .85f, 1f)), float.NegativeInfinity);
        timeline.Register(Track(871, State(9, .925f, .625f), State(11, .5f, .5f)), float.NegativeInfinity);
        var head = DragPathGeometry.HeadPosition(timeline.GetEndpoint(a, null),
            timeline.GetEndpoint(b, null), 10.5f, 10, 11);
        AssertVector(head, new Vector3(.675f, .75f));
    }

    [Test]
    public void SamplingUsesTheSameEasingAsTheLiveController()
    {
        var a = State(0, 0, 0);
        a.Easing = EasingFunction.Ease.EaseOutQuad;
        var b = State(2, 1, 1);
        var ovr = new ChartModel.Note.NoteOverride();
        NotePositionSampler.Apply(ovr, a, b, 1);
        Assert.That(ovr.X, Is.EqualTo(.75f));
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, a, b), float.NegativeInfinity);
        AssertVector(timeline.GetEndpoint(Note(1, 1, Vector3.zero), null), new Vector3(.75f, .75f));
    }

    [Test]
    public void MultipliersAndOffsetsAreConvertedBeforeAbsoluteOverrides()
    {
        var chart = EmptyChart();
        var note = Note(1, 1, new Vector3(-.25f, -.5f));
        note.x = note.y = .25f;
        var state = new NoteControllerState
        {
            Time = 0, XMultiplier = 2, XOffset = .1f, YMultiplier = 2, YOffset = .1f
        };
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, state), float.NegativeInfinity);
        AssertVector(timeline.GetEndpoint(note, chart), new Vector3(.1f, .2f));
        timeline.Register(Track(1, State(0, .8f, .9f)), 0);
        AssertVector(timeline.GetEndpoint(note, chart), new Vector3(.8f, .9f));
    }

    [Test]
    public void ExplicitDisableRestoresTheChartCoordinate()
    {
        var a = State(0, .5f, .8f);
        var b = new NoteControllerState {Time = 1, OverrideX = false, OverrideY = false};
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, a, b), float.NegativeInfinity);
        AssertVector(timeline.GetEndpoint(Note(1, 2, new Vector3(.2f, .3f)), null), new Vector3(.2f, .3f));
    }

    [Test]
    public void LastStateAtTheSameTimestampWins()
    {
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, State(0, 0, 1), State(1, 0, 1), State(1, 0, 0)), float.NegativeInfinity);
        Assert.That(timeline.GetEndpoint(Note(1, 1, Vector3.zero), null).y, Is.Zero);
    }

    [Test]
    public void ResetStateWithoutPositionWritesRetainsTheLastPosition()
    {
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, State(0, .3f, .4f), new NoteControllerState {Time = 1}), float.NegativeInfinity);
        AssertVector(timeline.GetEndpoint(Note(1, 2, Vector3.zero), null), new Vector3(.3f, .4f));
    }

    [Test]
    public void ControllerOrderIsResolvedPerChannel()
    {
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, State(0, .2f, .3f)), float.NegativeInfinity);
        timeline.Register(Track(1, new NoteControllerState {Time = 0, OverrideY = true, Y = World(.9f)}), 0);
        AssertVector(timeline.GetEndpoint(Note(1, 1, Vector3.zero), null), new Vector3(.2f, .9f));
    }

    [Test]
    public void StoppedControllerHoldsItsLastValueWithoutInventingFutureWrites()
    {
        var timeline = new NotePositionTimeline();
        var track = Track(1, State(0, 0, 0), State(2, 2, 2));
        timeline.Register(track, 0);
        var note = Note(1, 2, Vector3.zero);
        AssertVector(timeline.GetEndpoint(note, null), new Vector3(2, 2));
        timeline.Stop(track, .5f);
        AssertVector(timeline.GetEndpoint(note, null), new Vector3(.5f, .5f));
    }

    [Test]
    public void ActiveEarlierControllerCanTakeOverAfterLaterControllerStops()
    {
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, State(0, .1f, .1f)), 0);
        var later = Track(1, State(.2f, .9f, .9f));
        timeline.Register(later, .2f);
        timeline.Stop(later, .5f);
        AssertVector(timeline.GetEndpoint(Note(1, 1, Vector3.zero), null), new Vector3(.1f, .1f));
    }

    [Test]
    public void LateSpawnCannotRewriteAnAlreadyPassedEndpoint()
    {
        var timeline = new NotePositionTimeline();
        var note = Note(1, 1, new Vector3(.2f, .3f));
        AssertVector(timeline.GetEndpoint(note, null), note.position);
        timeline.Register(Track(1, State(0, .9f, .9f)), 2);
        AssertVector(timeline.GetEndpoint(note, null), note.position);
    }

    [Test]
    public void TriggeredTrackUpdatesAnEndpointStillInTheFuture()
    {
        var timeline = new NotePositionTimeline();
        var note = Note(1, 3, Vector3.zero);
        AssertVector(timeline.GetEndpoint(note, null), Vector3.zero);
        timeline.Register(Track(1, State(2, .7f, .8f)), 2);
        AssertVector(timeline.GetEndpoint(note, null), new Vector3(.7f, .8f));
    }

    [Test]
    public void DestroyStateStopsSamplingAtItsBoundary()
    {
        var timeline = new NotePositionTimeline();
        timeline.Register(Track(1, State(0, .2f, .3f),
            new NoteControllerState {Time = 1, Destroy = true}, State(2, .9f, .9f)), 0);
        AssertVector(timeline.GetEndpoint(Note(1, 3, Vector3.zero), null), new Vector3(.2f, .3f));
    }

    [Test]
    public void ClearRemovesTrackHistoryAndCachedEndpoints()
    {
        var timeline = new NotePositionTimeline();
        var note = Note(1, 1, Vector3.zero);
        timeline.Register(Track(1, State(0, .8f, .9f)), 0);
        timeline.GetEndpoint(note, null);
        timeline.Clear();
        AssertVector(timeline.GetEndpoint(note, null), Vector3.zero);
    }

    [Test]
    public void UncontrolledNodesKeepTheirBakedPositionIncludingPageFunctions()
    {
        var note = Note(1, 1, new Vector3(.2f, -.6f, 2));
        // Deliberately differs from the baked coordinate; do not rebuild from legacy y.
        note.y = .9f;
        AssertVector(new NotePositionTimeline().GetEndpoint(note, null), note.position);
    }

    private static Chart EmptyChart()
    {
        var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
        foreach (var pair in new[] {("baseSize", 1f), ("screenRatio", 1f),
                     ("horizontalRatio", .5f), ("verticalRatio", 1f)})
            typeof(Chart).GetField(pair.Item1, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(chart, pair.Item2);
        return chart;
    }

    private static ChartModel.Note Note(int id, float time, Vector3 position)
        => new ChartModel.Note {id = id, start_time = time, position = position};

    private static UnitFloat World(float value) => new UnitFloat(value, ReferenceUnit.World, false, false);

    private static NoteControllerState State(float time, float x, float y)
        => new NoteControllerState {Time = time, OverrideX = true, X = World(x), OverrideY = true, Y = World(y)};

    private static NoteController Track(int id, params NoteControllerState[] states)
    {
        var track = new NoteController();
        foreach (var state in states) {state.Note = id; track.States.Add(state);}
        return track;
    }

    private static NoteController Fall(ChartModel.Note note, float from, float to, float duration)
        => Track(note.id, new NoteControllerState {Note = note.id, Time = note.start_time - duration,
                OverrideY = true, Y = World(from)},
            new NoteControllerState {Note = note.id, Time = note.start_time, OverrideY = true, Y = World(to)});

    private static void AssertVector(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(1e-6f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(1e-6f));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(1e-6f));
    }
}
