using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using UnityEngine;

public class DropDragGuardTests
{
    [TestCase(4, 3.29670f, 3.62637f, 3.288872f, -3.28f, -3.276845f, -2.788882f)]
    [TestCase(24, 8.90109f, 9.56043f, 8.896207f, 0f, -0.234283f, -2.913804f)]
    [TestCase(46, 13.84614f, 14.50548f, 13.850166f, -0.656f, -0.534167f, -2.564023f)]
    [TestCase(47, 14.17581f, 14.50548f, 14.100184f, -1.968f, -1.921124f, -2.707683f)]
    public void RecordedFalseClicksAreBlocked(int id, float dragTime, float clickTime,
        float now, float landingX, float x, float y)
    {
        var guard = new DropDragGuard(id, dragTime, dragTime, 0, 1.3f,
            new Vector2(landingX, -3.288333f));
        Assert.That(DropNoteIsolation.BlocksSelect(guard, clickTime, 2.726251f,
            new Vector2(x, y), now, 0), Is.True);
    }

    [Test]
    public void OpeningClickRemainsAvailableForItsRealPress()
    {
        var guard = new DropDragGuard(4, 3.29670f, 3.29670f, 0, 1.3f,
            new Vector2(-3.28f, -3.288333f));
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 3.62637f, 2.726251f,
            guard.Landing, 3.638888f, 0), Is.False);
    }

    [Test]
    public void MidpointAndCoHitBoundariesProtectOnlyTheDragSide()
    {
        var guard = Guard(10f);
        Assert.That(Block(guard, 10.030f, 10f), Is.False, "30ms co-hit");
        Assert.That(Block(guard, 10.060f, 10.029f), Is.True);
        Assert.That(Block(guard, 10.060f, 10.030f), Is.True, "midpoint tie belongs to Drag");
        Assert.That(Block(guard, 10.060f, 10.031f), Is.False);
        Assert.That(Block(guard, 10.060f, 10.060f), Is.False, "on-time Click");
    }

    [Test]
    public void GuardUsesLandingRadiusAndSuccessfulLateWindow()
    {
        var guard = Guard(10f);
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 11f, 2f, new Vector2(2f, 0), 10f, 0), Is.True);
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 11f, 2f, new Vector2(2.001f, 0), 10f, 0), Is.False);
        Assert.That(Block(guard, 11f, 10.199f), Is.True);
        Assert.That(Block(Guard(0f), 1f, 0.200f), Is.False, "exact representable 200ms boundary");
        Assert.That(Block(guard, 11f, 10.201f), Is.False);
        Assert.That(Block(guard, 11f, 10.250f), Is.False, "300ms Miss threshold is not guard expiry");
    }

    [TestCase(-0.1f)]
    [TestCase(0.1f)]
    public void OffsetChangesMidpointButRawTimeStillControlsAdmission(float offset)
    {
        var guard = new DropDragGuard(1, 10f, 10f + offset, 0, 1f,
            Vector2.zero);
        Assert.That(Block(guard, 11f, 9.689f), Is.False);
        Assert.That(Block(guard, 11f, 9.691f), Is.True);
        var clickTime = guard.EffectiveTime + 0.060f;
        Assert.That(Block(guard, clickTime, guard.EffectiveTime + 0.020f), Is.True);
        Assert.That(Block(guard, clickTime, guard.EffectiveTime + 0.040f), Is.False);
    }

    [Test]
    public void FuturePageUsesItsOwnHalfDuration()
    {
        var guard = new DropDragGuard(1, 10f, 10f, 1, 0.2f,
            Vector2.zero);
        Assert.That(Block(guard, 11f, 9.89f), Is.False);
        Assert.That(Block(guard, 11f, 9.91f), Is.True);
        Assert.That(DropNoteIsolation.BlocksSelect(guard, 11f, 2f, Vector2.zero, 9.89f, 1), Is.True);
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void ActualFreshDragGateAllowsOnTimeClickAndKeepsOrdinaryDragPolicy(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.060f);
            fixture.Time(10f);
            Assert.That(fixture.Eligible(click, drag), Is.False);
            fixture.Time(10.060f);
            Assert.That(drag.CanHandleTouch(), Is.True, "fresh Drag remains a real accepted candidate");
            Assert.That(fixture.Eligible(click, drag), Is.True, "old unconditional 30ms gate must be replaced");
            SetProperty(drag, nameof(Note.Type), NoteType.DragHead);
            Assert.That(fixture.Eligible(click, drag), Is.False, "ordinary Drag policy is unchanged");
        }
    }

    [Test]
    public void ArmedDragStillProtectsEvenThoughItIsAbsentFromTouchableList()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(4, 3.29670f);
            var click = fixture.Click(6, 3.62637f);
            SetProperty(drag, nameof(Note.IsArmed), true);
            fixture.Time(3.288872f);
            CollectionAssert.DoesNotContain(fixture.Input.TouchableDragNotes, drag);
            Assert.That(drag.CanHandleTouch(), Is.False);
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Time(3.62637f);
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [Test]
    public void SameFrameClearSurvivesPoolReuseAndPauseButResetsOnRetry()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(46, 13.84614f);
            var click = fixture.Click(49, 14.50548f);
            fixture.Time(13.850166f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            // Reproduce Collect resetting Model and reusing the same object at another landing.
            fixture.Game.SpawnedNotes.Remove(46);
            SetProperty(drag, nameof(Note.Model), null);
            drag.transform.position = new Vector3(100, 100, 0);
            fixture.Input.OnGamePaused(fixture.Game);
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Input.DisableInput();
            fixture.Input.EnableInput();
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [Test]
    public void MissAndRewindDoNotLeaveClearedGuards()
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.6f);
            fixture.Time(10.01f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Miss, 0, 0));
            fixture.Game.SpawnedNotes.Remove(1);
            Assert.That(fixture.Eligible(click, null), Is.True);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Time(9.99f);
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    private static DropDragGuard Guard(float time) =>
        new DropDragGuard(1, time, time, 0, 1f, Vector2.zero);

    private static bool Block(DropDragGuard guard, float clickTime, float now) =>
        DropNoteIsolation.BlocksSelect(guard, clickTime, 2f, Vector2.zero, now, 0);

    internal static void SetProperty(object instance, string name, object value)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property == null) continue;
            property.SetValue(instance, value);
            return;
        }
        throw new System.MissingMemberException(name);
    }

    internal sealed class InputFixture : System.IDisposable
    {
        public readonly Game Game;
        public readonly InputController Input;
        private readonly List<GameObject> objects = new List<GameObject>();

        public InputFixture(GameMode mode)
        {
            var go = Object("DropGuardInputTest", false);
            Game = go.AddComponent<Game>();
            var chart = (Chart) FormatterServices.GetUninitializedObject(typeof(Chart));
            chart.Model = new ChartModel();
            chart.Model.page_list.Add(new ChartModel.Page {start_time = 0, end_time = 30});
            SetProperty(Game, nameof(Game.Chart), chart);
            SetProperty(Game, nameof(Game.IsLoaded), true);
            // Grading needs only Mode; the constructor requires fully initialized level metadata.
            var state = (GameState) FormatterServices.GetUninitializedObject(typeof(GameState));
            state.IsPlaying = true;
            typeof(GameState).GetField("<Mode>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(state, mode);
            SetProperty(Game, nameof(Game.State), state);
            Game.ObjectPool = new ObjectPool(Game);
            Input = go.AddComponent<InputController>();
            Input.game = Game;
            Invoke("Awake");
            Input.EnableInput();
        }

        public DropDragGuardTestDrag Drag(int id, float time) => Make<DropDragGuardTestDrag>(id, time, NoteType.DropDrag);
        public DropDragGuardTestClick Click(int id, float time) => Make<DropDragGuardTestClick>(id, time, NoteType.DropClick);

        private T Make<T>(int id, float time, NoteType type) where T : Note
        {
            var go = Object(type.ToString(), true);
            go.AddComponent<CircleCollider2D>().radius = 2f;
            var note = go.AddComponent<T>();
            SetProperty(note, nameof(Note.Game), Game);
            SetProperty(note, nameof(Note.Type), type);
            SetProperty(note, nameof(Note.Model), new ChartModel.Note {id = id, start_time = time, page_index = 0});
            SetProperty(note, nameof(Note.Page), Game.Chart.Model.page_list[0]);
            note.MissThreshold = type.GetDefaultMissThreshold();
            note.Renderer = new NoteRenderer(note);
            Game.SpawnedNotes[id] = note;
            return note;
        }

        public void Time(float time)
        {
            SetProperty(Game, nameof(Game.Time), time);
            Input.OnGameUpdate(Game);
        }

        public bool Eligible(Note click, Note acceptedDrag)
        {
            Invoke("BuildDropGuardsForDown");
            return (bool) Invoke("IsEligibleSelectAfterDrag", click, acceptedDrag, Vector2.zero);
        }

        private object Invoke(string name, params object[] args) =>
            typeof(InputController).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Input, args);

        private GameObject Object(string name, bool active)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            objects.Add(go);
            return go;
        }

        public void Dispose()
        {
            Input.DisableInput();
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
        }
    }
}

// Minimal renderers avoid loading gameplay art; inherited admission and grading stay real.
public class DropDragGuardTestDrag : DropDragNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public new void OnDestroy() { }
}

public class DropDragGuardTestClick : DropClickNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public new void OnDestroy() { }
}
