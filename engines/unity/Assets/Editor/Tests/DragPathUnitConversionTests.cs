using System.Runtime.Serialization;
using Cytoid.Storyboard;
using Cytoid.Storyboard.Notes;
using NUnit.Framework;
using UnityEngine;

public class DragPathUnitConversionTests
{
    private GameObject root;
    private DragPathTestGame game;
    private ChartModel.Note note;
    private NotePositionTimeline timeline;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("DragPathUnitConversionTests");
        game = root.AddComponent<DragPathTestGame>();
        var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
        chart.Model = new ChartModel();
        game.Configure(chart);
        game.camera = root.AddComponent<Camera>();
        game.camera.orthographicSize = 1;
        UnitFloat.Storyboard = game.Storyboard;
        note = new ChartModel.Note {id = 1, start_time = 3};
        timeline = game.Storyboard.Renderer.NotePositions;
    }

    [TearDown]
    public void TearDown()
    {
        UnitFloat.Storyboard = null;
        UnityEngine.Object.DestroyImmediate(root);
    }

    [TestCase(ReferenceUnit.CameraX)]
    [TestCase(ReferenceUnit.CameraY)]
    [TestCase(ReferenceUnit.StageX)]
    [TestCase(ReferenceUnit.StageY)]
    public void FutureEndpointSamplingDoesNotPrimeTheLiveUnitCache(ReferenceUnit unit)
    {
        var future = new UnitFloat(1, unit, false, false);
        var active = State(2, future);
        Register(new NoteControllerState {Note = note.id, Time = 0, OverrideY = false}, active);
        timeline.GetEndpoint(note, game.Chart);

        game.camera.orthographicSize = 2;
        var live = new ChartModel.Note.NoteOverride();
        NotePositionSampler.Apply(live, active, active, 2);

        Assert.That(live.Y, Is.EqualTo(future.Convert()).Within(1e-6f));
    }

    [Test]
    public void CachedEndpointResamplesWhenTheConversionBasisChanges()
    {
        Register(State(2, CameraY(1)));
        Assert.That(timeline.GetEndpoint(note, game.Chart).y, Is.EqualTo(1));

        game.camera.orthographicSize = 2;

        Assert.That(timeline.GetEndpoint(note, game.Chart).y, Is.EqualTo(2));
    }

    [Test]
    public void EndpointRespectsAUnitAlreadyConvertedByTheLiveController()
    {
        var active = State(2, CameraY(1));
        Register(active);
        var live = new ChartModel.Note.NoteOverride();
        NotePositionSampler.Apply(live, active, active, 2);
        game.camera.orthographicSize = 2;

        Assert.That(timeline.GetEndpoint(note, game.Chart).y, Is.EqualTo(live.Y));
        Assert.That(live.Y, Is.EqualTo(1));
    }

    [Test]
    public void CachedEndpointTracksTheValueEventuallyFixedByTheLiveController()
    {
        var active = State(2, CameraY(1));
        Register(active);
        Assert.That(timeline.GetEndpoint(note, game.Chart).y, Is.EqualTo(1));
        game.camera.orthographicSize = 2;
        var live = new ChartModel.Note.NoteOverride();
        NotePositionSampler.Apply(live, active, active, 2);
        game.camera.orthographicSize = 3;

        Assert.That(live.Y, Is.EqualTo(2));
        Assert.That(timeline.GetEndpoint(note, game.Chart).y, Is.EqualTo(live.Y));
    }

    [Test]
    public void LiveSamplingAtTheSegmentStartDoesNotConvertTheNextState()
    {
        var from = State(0, new UnitFloat(0, ReferenceUnit.World, false, false));
        var to = State(2, CameraY(1));
        var live = new ChartModel.Note.NoteOverride();
        NotePositionSampler.Apply(live, from, to, 0);
        game.camera.orthographicSize = 2;

        NotePositionSampler.Apply(live, from, to, 1);

        Assert.That(live.Y, Is.EqualTo(1));
    }

    private void Register(params NoteControllerState[] states)
    {
        var controller = new NoteController();
        controller.States.AddRange(states);
        timeline.Register(controller, float.NegativeInfinity);
    }

    private NoteControllerState State(float time, UnitFloat y)
        => new NoteControllerState {Note = note.id, Time = time, OverrideY = true, Y = y};

    private static UnitFloat CameraY(float value)
        => new UnitFloat(value, ReferenceUnit.CameraY, false, false);
}
