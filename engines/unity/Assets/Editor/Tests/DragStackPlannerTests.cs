using System.Collections.Generic;
using Cytoid.Storyboard;
using NUnit.Framework;

public class DragStackPlannerTests
{
    [Test]
    public void CoLocatedDragChildrenShareAStack()
    {
        var model = Model(
            Child(1, 0.5, 1f, next: 3),
            Child(2, 0.5, 1f, next: 4),
            Child(3, 0.5, 2f),
            Child(4, 0.5, 2f));

        var plan = DragStackPlanner.Build(model);

        Assert.That(plan.NoteIdToStackId.Count, Is.EqualTo(4));
        Assert.That(plan.NoteIdToStackId[1], Is.EqualTo(plan.NoteIdToStackId[2]));
        Assert.That(plan.NoteIdToStackId[3], Is.EqualTo(plan.NoteIdToStackId[4]));
        Assert.That(plan.NoteIdToStackId[1], Is.Not.EqualTo(plan.NoteIdToStackId[3]));
        Assert.That(plan.MaxSamePageDragStackHostCount, Is.EqualTo(2));
        Assert.That(plan.MaxSamePageDragLineCount, Is.EqualTo(1));
    }

    [Test]
    public void DragHeadsAreNeverStacked()
    {
        var model = Model(
            Head(1, 0.5, 1f, next: 3),
            Head(2, 0.5, 1f, next: 4),
            Child(3, 0.5, 2f),
            Child(4, 0.5, 2f));

        var plan = DragStackPlanner.Build(model);

        Assert.That(plan.NoteIdToStackId.ContainsKey(1), Is.False);
        Assert.That(plan.NoteIdToStackId.ContainsKey(2), Is.False);
        Assert.That(plan.NoteIdToStackId[3], Is.EqualTo(plan.NoteIdToStackId[4]));
    }

    [Test]
    public void DifferentIntroTimesDoNotStack()
    {
        var a = Child(1, 0.5, 1f);
        var b = Child(2, 0.5, 1f);
        a.intro_time = 0.1f;
        b.intro_time = 0.4f;
        var model = Model(a, b);

        var plan = DragStackPlanner.Build(model);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void DifferentVisualFieldsDoNotStack()
    {
        var a = Child(1, 0.5, 1f);
        var b = Child(2, 0.5, 1f);
        b.size = 2.0;
        var model = Model(a, b);

        var plan = DragStackPlanner.Build(model);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void IndependentStoryboardSignaturesDoNotStack()
    {
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f));
        var signatures = new Dictionary<int, string>
        {
            {1, "x=0.2"},
            {2, "x=0.8"}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void ControlledNoteDoesNotStackWithUncontrolledSibling()
    {
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f));
        var signatures = new Dictionary<int, string>
        {
            {1, "x=0.2"}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void IdenticalNoteControllersDoNotStack()
    {
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f));
        var signatures = new Dictionary<int, string>
        {
            {1, "dx=0.1"},
            {2, "dx=0.1"}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void UncontrolledPairStillStacksBesideAControlledNote()
    {
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f),
            Child(3, 0.5, 1f));
        var signatures = new Dictionary<int, string>
        {
            {3, "dx=0.1"}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId.ContainsKey(3), Is.False);
        Assert.That(plan.NoteIdToStackId[1], Is.EqualTo(plan.NoteIdToStackId[2]));
    }

    [Test]
    public void TriggerSpawnedControllersNeverShareAStack()
    {
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f));
        var signatures = new Dictionary<int, string>
        {
            {1, DragStackPlanner.TriggerSignaturePrefix + 1},
            {2, DragStackPlanner.TriggerSignaturePrefix + 2}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void DivergentNextNotesRefuseASharedStack()
    {
        var model = Model(
            Child(1, 0.5, 1f, next: 3),
            Child(2, 0.5, 1f, next: 4),
            Child(3, 0.2, 2f),
            Child(4, 0.8, 2f));

        var plan = DragStackPlanner.Build(model);
        var keyA = DragStackPlanner.MakeDragLineShareKey(
            model.note_map[1], model.note_map[3], plan.NoteIdToStackId);
        var keyB = DragStackPlanner.MakeDragLineShareKey(
            model.note_map[2], model.note_map[4], plan.NoteIdToStackId);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
        Assert.That(keyA, Is.Not.EqualTo(keyB));
        Assert.That(plan.MaxSamePageDragLineCount, Is.EqualTo(2));
    }

    [Test]
    public void IdenticalControllersOnNextNotesRefuseASharedStack()
    {
        // Successors with byte-identical controller fingerprints still diverge when a
        // trigger destroys exactly one controller by storyboard id; the shared host's
        // rotation would then point the surviving follower's drag line the wrong way.
        var model = Model(
            Child(1, 0.5, 1f, next: 3),
            Child(2, 0.5, 1f, next: 4),
            Child(3, 0.5, 2f),
            Child(4, 0.5, 2f));
        var signatures = new Dictionary<int, string>
        {
            {3, "dx=0.1"},
            {4, "dx=0.1"}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void OneControlledNextNoteRefuseASharedStack()
    {
        var model = Model(
            Child(1, 0.5, 1f, next: 3),
            Child(2, 0.5, 1f, next: 4),
            Child(3, 0.5, 2f),
            Child(4, 0.5, 2f));
        var signatures = new Dictionary<int, string>
        {
            {4, "dx=0.1"}
        };

        var plan = DragStackPlanner.Build(model, signatures);

        Assert.That(plan.NoteIdToStackId, Is.Empty);
    }

    [Test]
    public void UncontrolledNextNotesStillStack()
    {
        var model = Model(
            Child(1, 0.5, 1f, next: 3),
            Child(2, 0.5, 1f, next: 4),
            Child(3, 0.5, 2f),
            Child(4, 0.5, 2f));

        var plan = DragStackPlanner.Build(model);

        Assert.That(plan.NoteIdToStackId.ContainsKey(1), Is.True);
        Assert.That(plan.NoteIdToStackId[1], Is.EqualTo(plan.NoteIdToStackId[2]));
    }

    [Test]
    public void ShareKeyOneToThirtyFourDoesNotCollideWithTwoToThree()
    {
        // Historical collision of the 31-based hash: both edges hashed to 15822768,
        // letting the pool reuse one live line for both and recycle it early.
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f),
            Child(3, 0.5, 2f),
            Child(34, 0.5, 2f));

        var keyOneToThirtyFour = DragStackPlanner.MakeDragLineShareKey(
            model.note_map[1], model.note_map[34], noteIdToStackId: null);
        var keyTwoToThree = DragStackPlanner.MakeDragLineShareKey(
            model.note_map[2], model.note_map[3], noteIdToStackId: null);

        Assert.That(keyOneToThirtyFour, Is.Not.EqualTo(0));
        Assert.That(keyOneToThirtyFour, Is.Not.EqualTo(keyTwoToThree));
    }

    [Test]
    public void ShareKeysAreUniqueAcrossOrderedPairs()
    {
        var model = Model();
        var notes = new List<ChartModel.Note>();
        for (var id = 0; id <= 50; id++)
        {
            var note = Child(id, 0.5, 1f);
            notes.Add(note);
            model.note_list.Add(note);
            model.note_map[id] = note;
        }

        var seen = new HashSet<long>();
        for (var a = 0; a <= 50; a++)
        for (var b = 0; b <= 50; b++)
        {
            if (a == b) continue;
            var key = DragStackPlanner.MakeDragLineShareKey(model.note_map[a], model.note_map[b], noteIdToStackId: null);
            Assert.That(key, Is.Not.EqualTo(0), $"edge {a}->{b} lost its share key");
            Assert.That(seen.Add(key), Is.True, $"edges collide at {a}->{b}");
        }
    }

    [Test]
    public void ShareKeyStackedEndpointNeverCollidesWithUnstacked()
    {
        var model = Model(
            Child(1, 0.5, 1f),
            Child(2, 0.5, 1f),
            Child(3, 0.5, 2f));
        var stacked = new Dictionary<int, int> {{1, 1}, {2, 1}};

        var stackedFrom = DragStackPlanner.MakeDragLineShareKey(model.note_map[1], model.note_map[3], stacked);
        var unstackedFrom = DragStackPlanner.MakeDragLineShareKey(model.note_map[1], model.note_map[3], noteIdToStackId: null);

        Assert.That(stackedFrom, Is.Not.EqualTo(0));
        Assert.That(stackedFrom, Is.Not.EqualTo(unstackedFrom));
    }

    [Test]
    public void ShareKeySeparatesNoteTypesAndEndpointOrder()
    {
        // Same ids on both sides of each comparison so only the type bits can differ;
        // comparing notes with different ids would pass even without type segments.
        var sourceHead = Head(1, 0.5, 1f);
        var sourceChild = Child(1, 0.5, 1f);
        var destinationHead = Head(3, 0.5, 2f);
        var destinationChild = Child(3, 0.5, 2f);

        var headToChild = DragStackPlanner.MakeDragLineShareKey(sourceHead, destinationChild, noteIdToStackId: null);
        var childToChild = DragStackPlanner.MakeDragLineShareKey(sourceChild, destinationChild, noteIdToStackId: null);
        var childToHead = DragStackPlanner.MakeDragLineShareKey(sourceChild, destinationHead, noteIdToStackId: null);
        var reversed = DragStackPlanner.MakeDragLineShareKey(destinationChild, sourceChild, noteIdToStackId: null);

        Assert.That(headToChild, Is.Not.EqualTo(childToChild));
        Assert.That(childToChild, Is.Not.EqualTo(childToHead));
        Assert.That(childToChild, Is.Not.EqualTo(reversed));
    }

    [Test]
    public void UniformNoteControllersProduceMatchingSignatures()
    {
        var shared = Controller("same", 1, 0.1f);
        var copy = Controller("same-copy", 2, 0.1f);
        var signatures = DragStackPlanner.SignaturesFromNoteControllers(new[] {shared, copy});

        Assert.That(signatures[1], Is.EqualTo(signatures[2]));
        Assert.That(signatures[1].StartsWith(DragStackPlanner.TriggerSignaturePrefix), Is.False);
    }

    [Test]
    public void TriggerNoteControllersProduceUniqueSignatures()
    {
        var a = Controller("trig-a", 1, 0.1f);
        a.States[0].Time = float.MaxValue;
        var b = Controller("trig-b", 2, 0.1f);
        b.States[0].Time = float.MaxValue;

        var signatures = DragStackPlanner.SignaturesFromNoteControllers(new[] {a, b});

        Assert.That(signatures[1], Is.EqualTo(DragStackPlanner.TriggerSignaturePrefix + 1));
        Assert.That(signatures[2], Is.EqualTo(DragStackPlanner.TriggerSignaturePrefix + 2));
        Assert.That(signatures[1], Is.Not.EqualTo(signatures[2]));
    }

    static ChartModel Model(params ChartModel.Note[] notes)
    {
        var model = new ChartModel
        {
            page_list = new List<ChartModel.Page> {new ChartModel.Page()}
        };
        foreach (var note in notes)
        {
            model.note_list.Add(note);
            model.note_map[note.id] = note;
        }

        return model;
    }

    static ChartModel.Note Head(int id, double x, float startTime, int next = -1)
    {
        return Note(id, NoteType.DragHead, x, startTime, next);
    }

    static ChartModel.Note Child(int id, double x, float startTime, int next = -1)
    {
        return Note(id, NoteType.DragChild, x, startTime, next);
    }

    static ChartModel.Note Note(int id, NoteType type, double x, float startTime, int next)
    {
        return new ChartModel.Note
        {
            id = id,
            type = (int) type,
            page_index = 0,
            x = x,
            start_time = startTime,
            next_id = next,
            approach_rate = 1.0,
            style = 1
        };
    }

    static NoteController Controller(string id, int noteId, float xOffset)
    {
        return new NoteController
        {
            Id = id,
            States = new List<NoteControllerState>
            {
                new NoteControllerState
                {
                    Time = 0,
                    Note = noteId,
                    XOffset = xOffset
                }
            }
        };
    }
}
