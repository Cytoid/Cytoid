using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Cytoid.Storyboard;
using Cytoid.Storyboard.Sprites;
using NUnit.Framework;
using UnityEngine;

public class DragPathRuntimeTests
{
    private GameObject root;
    private DragPathTestGame game;
    private DragPathTestHead head;
    private ChartModel.Note a, b, c;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("DragPathRuntimeTests");
        game = root.AddComponent<DragPathTestGame>();
        var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
        chart.Model = new ChartModel();
        a = new ChartModel.Note {id = 1, type = 3, start_time = 0, next_id = 2, position = Vector3.zero};
        b = new ChartModel.Note {id = 2, type = 4, start_time = 1, next_id = 3, position = Vector3.right};
        c = new ChartModel.Note {id = 3, type = 4, start_time = 2, next_id = -1, position = Vector3.right * 2};
        foreach (var model in new[] {a, b, c}) chart.Model.note_map[model.id] = model;
        game.Configure(chart);
        head = root.AddComponent<DragPathTestHead>();
        Set(typeof(Note), head, "Game", game);
        Set(typeof(Note), head, "Chart", chart.Model);
        Set(typeof(Note), head, "Model", a);
        Set(typeof(DragHeadNote), head, "FromNoteModel", a);
        Set(typeof(DragHeadNote), head, "ToNoteModel", b);
        Set(typeof(DragHeadNote), head, "StartToNoteModel", b);
        Set(typeof(DragHeadNote), head, "EndNoteModel", c);
        game.ObjectPool.SpawnedNotes.Add(a.id, head);
    }

    [TearDown]
    public void TearDown()
    {
        UnitFloat.Storyboard = null;
        UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void HeadAdvancesAcrossMultipleUnspawnedChildrenInOneFrame()
    {
        game.Seek(1.5f);
        game.PrepareNotePositions();
        Assert.That(head.FromNoteModel, Is.SameAs(b));
        Assert.That(head.ToNoteModel, Is.SameAs(c));
        Assert.That(head.transform.localPosition.x, Is.EqualTo(1.5f));
        Assert.That(head.transform.localPosition.y, Is.Zero);
    }

    [Test]
    public void TriggerDuringJudgmentCannotSeparateLineAndHeadInTheSameFrame()
    {
        game.Seek(.5f);
        game.PrepareNotePositions();
        var track = new NoteController();
        track.States.Add(new NoteControllerState {Note = b.id, Time = 0,
            OverrideY = true, Y = new UnitFloat(1, ReferenceUnit.World, false, false)});
        game.Storyboard.Renderer.NotePositions.Register(track, .5f);
        var lineStart = DragPathGeometry.LineStart(Vector3.zero,
            game.GetDragPathPosition(a), game.GetDragPathPosition(b), game.Time, 0, 1);
        Assert.That(lineStart, Is.EqualTo(head.transform.localPosition));
        Assert.That(lineStart.y, Is.Zero);

        game.Seek(.6f);
        game.PrepareNotePositions();
        Assert.That(head.transform.localPosition.y, Is.EqualTo(.6f).Within(1e-6f));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void DestroyingATargetedControllerAlsoDestroysItsOwner(bool ownerFirst)
    {
        game.Seek(0);
        var main = game.Storyboard.Renderer;
        var owner = new NoteController {Id = "owner"};
        owner.States.Add(new NoteControllerState {Note = a.id, Time = 10});
        var borrower = new NoteController {Id = "borrower", TargetId = owner.Id};
        borrower.States.Add(new NoteControllerState {Note = b.id, Time = 0, Destroy = true});
        var ownerRenderer = new NoteControllerRenderer(main, owner);
        var borrowerRenderer = new NoteControllerRenderer(main, borrower) {Parent = ownerRenderer};
        var survivor = new NoteController {Id = "survivor"};
        survivor.States.Add(new NoteControllerState {Note = c.id, Time = 0,
            OverrideY = true, Y = new UnitFloat(.75f, ReferenceUnit.World, false, false)});
        var survivorRenderer = new NoteControllerRenderer(main, survivor);
        Set(typeof(NoteControllerRenderer), survivorRenderer, "Note", c);
        ownerRenderer.Children.Add(borrowerRenderer);
        main.ComponentRenderers.Add(owner.Id, ownerRenderer);
        main.ComponentRenderers.Add(borrower.Id, borrowerRenderer);
        main.ComponentRenderers.Add(survivor.Id, survivorRenderer);
        main.TypedComponentRenderers.Add(typeof(NoteController),
            ownerFirst
                ? new List<StoryboardComponentRenderer> {ownerRenderer, borrowerRenderer, survivorRenderer}
                : new List<StoryboardComponentRenderer> {borrowerRenderer, ownerRenderer, survivorRenderer});

        main.UpdateNoteControls();

        Assert.That(main.ComponentRenderers.Keys, Is.EquivalentTo(new[] {survivor.Id}));
        Assert.That(main.TypedComponentRenderers[typeof(NoteController)],
            Is.EqualTo(new[] {survivorRenderer}));
        Assert.That(ownerRenderer.IsDisposed, Is.True);
        Assert.That(borrowerRenderer.IsDisposed, Is.True);
        Assert.That(c.Override.Y, Is.EqualTo(.75f));
    }

    [Test]
    public void SimultaneousChildrenAreConsumedWithoutAnIntermediateFrame()
    {
        b.start_time = a.start_time;
        game.Seek(.5f);
        game.PrepareNotePositions();
        Assert.That(head.FromNoteModel, Is.SameAs(b));
        Assert.That(head.transform.localPosition.x, Is.EqualTo(1.25f));
    }

    private static void Set(System.Type type, object target, string name, object value)
        => type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SetValue(target, value);
}

// EditMode fixtures avoid starting the real content/audio loader.
public sealed class DragPathTestGame : Game
{
    protected override void Awake() { }
    protected override void Start() { }

    public void Configure(Chart chart)
    {
        Chart = chart;
        State = (GameState) FormatterServices.GetUninitializedObject(typeof(GameState));
        ObjectPool = new ObjectPool(this);
        Storyboard = new Cytoid.Storyboard.Storyboard(this, "{}");
    }

    public void Seek(float time) { Time = time; }
}

public sealed class DragPathTestHead : DragHeadNote
{
    // The fixture has no renderer or pooled lifetime to tear down.
    public new void OnDestroy() { }
}
