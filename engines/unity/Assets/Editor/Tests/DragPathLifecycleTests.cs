using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Cytoid.Storyboard;
using Cytoid.Storyboard.Notes;
using Cytoid.Storyboard.Sprites;
using NUnit.Framework;
using UnityEngine;

public class DragPathLifecycleTests
{
    private GameObject root;
    private DragPathTestGame game;
    private ChartModel.Note ownerNote, target;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("DragPathLifecycleTests");
        game = root.AddComponent<DragPathTestGame>();
        var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
        chart.Model = new ChartModel();
        ownerNote = new ChartModel.Note {id = 1, start_time = 4};
        target = new ChartModel.Note {id = 2, start_time = 4};
        chart.Model.note_map.Add(1, ownerNote);
        chart.Model.note_map.Add(2, target);
        game.Configure(chart);
    }

    [TearDown]
    public void TearDown()
    {
        UnitFloat.Storyboard = null;
        UnityEngine.Object.DestroyImmediate(root);
    }

    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    [TestCase(true, false)]
    public void CascadeDestructionMustPreserveTheLastActuallyAppliedCoordinate(
        bool startsAtDestruction, bool ownerFirst)
    {
        var owner = new NoteController {Id = "owner"};
        owner.States.Add(new NoteControllerState {Note = 1, Time = 0});
        owner.States.Add(new NoteControllerState {Note = 1, Time = 2, Destroy = true});
        var borrower = new NoteController {Id = "borrower", TargetId = "owner"};
        borrower.States.Add(State(startsAtDestruction ? 2 : 0, startsAtDestruction ? 100 : 0));
        borrower.States.Add(State(4, startsAtDestruction ? 100 : 4));
        var ownerRenderer = Renderer(owner, ownerNote);
        var borrowerRenderer = Renderer(borrower, target);
        borrowerRenderer.Parent = ownerRenderer;
        ownerRenderer.Children.Add(borrowerRenderer);
        game.Storyboard.Renderer.TypedComponentRenderers[typeof(NoteController)] =
            ownerFirst
                ? new List<StoryboardComponentRenderer> {ownerRenderer, borrowerRenderer}
                : new List<StoryboardComponentRenderer> {borrowerRenderer, ownerRenderer};
        game.Seek(1);
        game.Storyboard.Renderer.UpdateNoteControls();
        var expectedY = startsAtDestruction ? (float?) null : 1f;
        Assert.That(target.Override.Y, Is.EqualTo(expectedY));

        game.Seek(2);
        game.Storyboard.Renderer.UpdateNoteControls();
        Assert.That(borrowerRenderer.IsDisposed, Is.True);
        if (!ownerFirst) expectedY = startsAtDestruction ? 100 : 2;
        Assert.That(target.Override.Y, Is.EqualTo(expectedY));

        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart).y,
            Is.EqualTo(target.CalculatePosition(game.Chart).y),
            "disposed controls must not invent a write at the disposal frame");
    }

    [Test]
    public void APartialResetKeepsTheLastActualWriteWhileAnotherChannelContinues()
    {
        var controller = new NoteController {Id = "partial reset"};
        controller.States.Add(State(0, 0));
        controller.States.Add(new NoteControllerState {Note = 2, Time = 1, Y = World(100),
            OverrideX = true, X = World(2)});
        controller.States.Add(new NoteControllerState {Note = 2, Time = 4,
            OverrideX = true, X = World(8)});
        var renderer = Renderer(controller, target);
        Use(renderer);
        Tick(.25f);
        Assert.That(target.Override.Y, Is.EqualTo(25));
        // Prime the future endpoint before the reset becomes the live state.
        game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart);
        Tick(1);
        Tick(2);

        var endpoint = game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart);
        Assert.That(endpoint.x, Is.EqualTo(8));
        Assert.That(endpoint.y, Is.EqualTo(25));

        game.Storyboard.Renderer.DestroyObjectsById(controller.Id);
        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart),
            Is.EqualTo(target.CalculatePosition(game.Chart)));
    }

    [Test]
    public void DestroyAfterTheLivePassRetainsThatFramesWrite()
    {
        var controller = new NoteController {Id = "trigger destroy"};
        controller.States.Add(State(0, 0));
        controller.States.Add(State(4, 4));
        var renderer = Renderer(controller, target);
        Use(renderer);
        Tick(1);
        Tick(2);

        game.Storyboard.Renderer.OnTrigger(new Trigger {Destroy = new List<string> {controller.Id}});

        Assert.That(target.Override.Y, Is.EqualTo(2));
        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart).y, Is.EqualTo(2));
    }

    [Test]
    public void StoppedControllersMergeOnlyTheirOwnChannelsAndLastWriteTimes()
    {
        var first = new NoteController {Id = "first"};
        first.States.Add(new NoteControllerState {Note = 2, Time = 0,
            OverrideX = true, X = World(3), OverrideY = true, Y = World(4)});
        var second = new NoteController {Id = "second"};
        second.States.Add(State(0, 9));
        second.States.Add(new NoteControllerState {Note = 2, Time = 1,
            OverrideX = true, X = World(5)});
        var firstRenderer = Renderer(first, target);
        var secondRenderer = Renderer(second, target);
        Use(firstRenderer, secondRenderer);
        Tick(0);
        Assert.That(target.Override.Y, Is.EqualTo(9));
        Tick(1);
        Assert.That(target.Override.Y, Is.EqualTo(4));
        game.Storyboard.Renderer.DestroyObjectsById(first.Id);
        game.Storyboard.Renderer.DestroyObjectsById(second.Id);

        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart),
            Is.EqualTo(new Vector3(5, 4)));
    }

    [Test]
    public void ExplicitDisableIsAnActualWriteRetainedAfterDestruction()
    {
        var controller = new NoteController {Id = "disable"};
        controller.States.Add(State(0, 9));
        controller.States.Add(new NoteControllerState {Note = 2, Time = 1, OverrideY = false});
        var renderer = Renderer(controller, target);
        Use(renderer);
        Tick(0);
        Tick(1);
        game.Storyboard.Renderer.DestroyObjectsById(controller.Id);

        Assert.That(target.Override.Y, Is.Null);
        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart),
            Is.EqualTo(target.position));
    }

    [Test]
    public void ReusingAnIdDoesNotReuseTheStoppedInstancesLastWrite()
    {
        var first = new NoteController {Id = "reused"};
        first.States.Add(State(0, 9));
        Use(Renderer(first, target));
        Tick(0);
        game.Storyboard.Renderer.DestroyObjectsById(first.Id);
        var replacement = new NoteController {Id = first.Id};
        replacement.States.Add(State(1, 3));
        Use(Renderer(replacement, target));
        Tick(1);

        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart).y, Is.EqualTo(3));
    }

    [Test]
    public void APassedEndpointSurvivesLaterResetAndDestruction()
    {
        target.start_time = .25f;
        var controller = new NoteController {Id = "past"};
        controller.States.Add(State(0, 0));
        controller.States.Add(State(1, 4));
        controller.States.Add(new NoteControllerState {Note = 2, Time = 2});
        Use(Renderer(controller, target));
        Tick(0);
        var endpoint = game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart);
        Assert.That(endpoint.y, Is.EqualTo(1));
        Tick(1);
        Tick(2);
        game.Storyboard.Renderer.DestroyObjectsById(controller.Id);

        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart), Is.EqualTo(endpoint));
    }

    private void Use(params NoteControllerRenderer[] renderers)
        => game.Storyboard.Renderer.TypedComponentRenderers[typeof(NoteController)] =
            new List<StoryboardComponentRenderer>(renderers);

    [Test]
    public void LateRegistrationCannotReconstructAPassedObservedEndpoint()
    {
        target.start_time = 1.5f;
        var controller = new NoteController {Id = "observed past"};
        controller.States.Add(State(0, 0));
        controller.States.Add(new NoteControllerState {Note = 2, Time = 1, Y = World(100)});
        Use(Renderer(controller, target));
        Tick(.25f);
        Tick(1);
        var endpoint = game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart);
        Assert.That(endpoint.y, Is.EqualTo(25));
        Tick(2);
        game.Storyboard.Renderer.DestroyObjectsById(controller.Id);
        var late = new NoteController();
        late.States.Add(State(2, 99));

        game.Storyboard.Renderer.NotePositions.Register(late, 2);

        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart), Is.EqualTo(endpoint));
    }

    private void Tick(float time)
    {
        game.Seek(time);
        game.Storyboard.Renderer.UpdateNoteControls();
    }

    private static UnitFloat World(float value) => new UnitFloat(value, ReferenceUnit.World, false, false);

    [Test]
    public void SameTimestampResetMustNotInventAnEarlierWrite()
    {
        var storyboard = new Cytoid.Storyboard.Storyboard(game,
            @"{""note_controllers"":[{""id"":""reset"",""note"":2,""time"":0,
                ""override_y"":true,""y"":""world:0.8"",""states"":[{""time"":0,""reset"":true}]}]}");
        storyboard.Parse();
        var controller = storyboard.NoteControllers["reset"];
        Assert.That(controller.States.Count, Is.EqualTo(2));
        var renderer = Renderer(controller, target);
        game.Storyboard.Renderer.TypedComponentRenderers[typeof(NoteController)] =
            new List<StoryboardComponentRenderer> {renderer};
        game.Seek(0);
        game.Storyboard.Renderer.UpdateNoteControls();
        game.Seek(1);
        game.Storyboard.Renderer.UpdateNoteControls();
        Assert.That(target.Override.Y, Is.Null);

        Assert.That(game.Storyboard.Renderer.NotePositions.GetEndpoint(target, game.Chart),
            Is.EqualTo(target.CalculatePosition(game.Chart)),
            "FindStates always selects the reset state; the earlier state is never applied live");
    }

    private NoteControllerRenderer Renderer(NoteController controller, ChartModel.Note note)
    {
        var renderer = new NoteControllerRenderer(game.Storyboard.Renderer, controller);
        typeof(NoteControllerRenderer).GetProperty("Note", BindingFlags.Instance | BindingFlags.Public)
            .SetValue(renderer, note);
        game.Storyboard.Renderer.ComponentRenderers.Add(controller.Id, renderer);
        game.Storyboard.Renderer.NotePositions.Register(controller, float.NegativeInfinity);
        return renderer;
    }

    private static NoteControllerState State(float time, float y)
        => new NoteControllerState {Note = 2, Time = time, OverrideY = true,
            Y = new UnitFloat(y, ReferenceUnit.World, false, false)};
}
