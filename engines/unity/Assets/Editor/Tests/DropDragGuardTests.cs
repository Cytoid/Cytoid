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
    public void MissDoesNotCaptureAGuardAndExplicitResetClearsSuccessfulGuards()
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
            fixture.Input.ResetDropGuards();
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [TestCase(0.005f)]
    [TestCase(0.020f)]
    public void BackwardClockCorrectionKeepsAnUnexpiredClearedGuard(float correction)
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.6f);
            fixture.Time(10.03f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            fixture.Game.SpawnedNotes.Remove(1);
            Assert.That(fixture.Eligible(click, null), Is.False);

            fixture.Time(10.03f - correction);
            Assert.That(fixture.Eligible(click, null), Is.False, "DSP correction is not a new play");
            fixture.Time(10.201f);
            Assert.That(fixture.Eligible(click, null), Is.True, "normal expiry still applies");
        }
    }

    [Test]
    public void NewPlayClearsGuardsThroughTheGameStartedEvent()
    {
        using (var fixture = new InputFixture(GameMode.Practice))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.3f);
            fixture.Time(10.01f);
            fixture.Game.onNoteJudged.Invoke(fixture.Game, drag, new JudgeData(NoteGrade.Perfect, 0, 0));
            fixture.Game.SpawnedNotes.Remove(1);
            Assert.That(fixture.Eligible(click, null), Is.False);
            fixture.Game.onGameStarted.Invoke(fixture.Game);
            Assert.That(fixture.Eligible(click, null), Is.True);
        }
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void TwoRealDownsInOneFrameCannotJudgeALaterClickAfterDragPoolReuse(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var drag = fixture.Drag(1, 10f);
            var click = fixture.Click(2, 10.33f);
            var reusedModel = fixture.Model(3, 20f, NoteType.DropDrag);
            fixture.PrepareJudgments(mode);
            fixture.Time(10.01f);
            var frame = Time.frameCount;

            fixture.Down(0);
            Assert.That(fixture.Game.State.IsJudged(1), Is.True, "first Down must really settle the Drag");
            Assert.That(fixture.Game.State.IsJudged(2), Is.False, "guard must be built before Select selection");
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(1));

            drag.Collect();
            Assert.That(drag.Model, Is.Null, "real Collect resets the pooled model");
            var reused = fixture.Game.ObjectPool.SpawnNote(reusedModel);
            Assert.That(reused, Is.SameAs(drag), "the actual pool reuses the old object");
            reused.transform.position = new Vector3(100, 100, 0);

            // Do not refresh the per-frame touchable lists or yield between Downs.
            fixture.Down(1);
            Assert.That(Time.frameCount, Is.EqualTo(frame));
            Assert.That(fixture.Game.State.IsJudged(2), Is.False, "cleared snapshot must survive pool reuse");
            Assert.That(click.IsCleared, Is.False);
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(1));

            fixture.Time(10.33f);
            fixture.Down(2);
            Assert.That(fixture.Game.State.IsJudged(2), Is.True, "the later real Click remains available");
            Assert.That(fixture.Game.State.GetJudgement(2).Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    // GameTouchInput dispatches FingerUpdate only from Moved/Stationary phases -- never on
    // the press frame. These tests lock that contract at the settle level using the timings
    // of a recorded replay over two consecutive DropDrag landings (20ms apart, beyond
    // DragStackBatchGapSeconds, so the pair can never co-batch): one Down settles exactly
    // one note; the second settles on the next stationary contact update with the same
    // grade (late pair), or arms and resolves at its own trigger point (early pair). A
    // port over an event layer that co-dispatches Down+Update on the press frame (e.g.
    // LeanTouch UpdateEvents) regresses both and settles two notes from one tap.
    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void NextStationaryUpdateSettlesSecondConsecutiveDropDragWithSameGrade(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var first = fixture.Drag(56, 5.056f);
            var second = fixture.Drag(58, 5.076f); // 20ms apart: never co-batched
            first.transform.position = Vector3.zero;
            second.transform.position = Vector3.zero;
            fixture.PrepareJudgments(mode);

            fixture.Time(5.251f); // 195ms / 175ms late: both inside the late Perfect window
            fixture.Down(0);
            Assert.That(fixture.Game.State.IsJudged(56), Is.True, "the Down settles the first drag");
            Assert.That(fixture.Game.State.GetJudgement(56).Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(fixture.Game.State.IsJudged(58), Is.False,
                "a Down must not settle the second consecutive drag");
            Assert.That(second.IsArmed, Is.False, "and must not arm it either");

            fixture.Time(5.259f); // next stationary contact frame (120Hz)
            fixture.Update(0);
            Assert.That(fixture.Game.State.IsJudged(58), Is.True,
                "the first stationary update settles the second drag");
            Assert.That(fixture.Game.State.GetJudgement(58).Grade, Is.EqualTo(NoteGrade.Perfect),
                "same grade a press-frame judgment would have given");
        }
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void EarlySecondConsecutiveDropDragArmsOnContactUpdateAndResolvesAtTriggerPoint(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var first = fixture.Drag(44, 4.125f);
            var second = fixture.Drag(47, 4.287f);
            first.transform.position = Vector3.zero;
            second.transform.position = Vector3.zero;
            fixture.PrepareJudgments(mode);

            fixture.Time(4.284f); // 159ms late / 2.7ms early
            fixture.Down(0);
            Assert.That(fixture.Game.State.GetJudgement(44).Grade, Is.EqualTo(NoteGrade.Perfect));
            Assert.That(fixture.Game.State.IsJudged(47), Is.False,
                "a Down must not settle the still-early second drag");

            fixture.Time(4.285f); // contact while still before the trigger point
            fixture.Update(0);
            Assert.That(second.IsArmed, Is.True, "contact before start arms the note");
            Assert.That(fixture.Game.State.IsJudged(47), Is.False);

            second.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(47), Is.False,
                "armed note resolves only at its trigger point");
            fixture.Time(4.292f);
            second.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(47), Is.True);
            Assert.That(fixture.Game.State.GetJudgement(47).Grade, Is.EqualTo(NoteGrade.Perfect));
        }
    }

    // Cyanine record 21347999 (C2v3-restored, engine timings): tapping note 14
    // (DropClick 5.934) armed the overlapping note 15 (DropDrag 6.264) because the
    // Select won that Down; the next tap at 6.2641 then made the still-early note 17
    // (6.429) the first colliding drag and the representative slot judged it Perfect
    // 164ms before its trigger point while 15 correctly resolved at its own trigger
    // -- two simultaneous pops, one early. Tapping 15 directly (record 21348005)
    // behaves as intended: the past-start 15 legally takes the immediate slot and 17
    // arms via contact updates. All drag types share the settle pipeline, so the gate
    // is type-agnostic: an early representative arms instead of judging at tap time.
    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void EarlyDragRepresentativeArmsInsteadOfJudgingAtTapTime(GameMode mode)
    {
        using (var fixture = new InputFixture(mode))
        {
            var armed = fixture.Drag(15, 10.0f);
            var early = fixture.Drag(17, 10.164f); // 164ms after the in-front note
            armed.transform.position = Vector3.zero;
            early.transform.position = Vector3.zero;
            SetProperty(armed, nameof(Note.IsArmed), true); // the "14" tap armed it
            fixture.PrepareJudgments(mode);

            fixture.Time(10.0f);
            fixture.Down(0);
            Assert.That(fixture.Game.State.IsJudged(17), Is.False,
                "an early representative must not judge at tap time");
            Assert.That(early.IsArmed, Is.True, "it arms like any other early contact");
            Assert.That(fixture.Game.State.IsJudged(15), Is.False,
                "the already-armed in-front note is not consumed by this Down");

            early.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(17), Is.False,
                "armed note resolves only at its trigger point");
            fixture.Time(10.17f);
            early.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.GetJudgement(17).Grade, Is.EqualTo(NoteGrade.Perfect));
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
        private LocalPlayerSettings previousSettings;
        private bool settingsOverridden;

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
            Game.inputController = Input;
            Game.effectController = go.AddComponent<EffectController>();
            Game.effectController.game = Game;
            Game.camera = Object("DropGuardCamera", true).AddComponent<Camera>();
            Game.camera.orthographic = true;
            Game.camera.transform.position = new Vector3(0, 0, -10);
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
            SetProperty(note, nameof(Note.Model), Model(id, time, type));
            SetProperty(note, nameof(Note.Page), Game.Chart.Model.page_list[0]);
            note.MissThreshold = type.GetDefaultMissThreshold();
            note.Renderer = new NoteRenderer(note);
            Game.SpawnedNotes[id] = note;
            return note;
        }

        public ChartModel.Note Model(int id, float time, NoteType type)
        {
            var model = new ChartModel.Note {id = id, type = (int) type, start_time = time, end_time = time, page_index = 0};
            Game.Chart.Model.note_list.Add(model);
            Game.Chart.Model.note_map[id] = model;
            return model;
        }

        public void PrepareJudgments(GameMode mode)
        {
            // Real pool spawning reads the player's judgment offset; restore it after the test.
            previousSettings = Context.Player.Settings;
            settingsOverridden = true;
            SetProperty(Context.Player, nameof(Player.Settings), new LocalPlayerSettings());
            SetProperty(Game, nameof(Game.Level), Level.FromExternal(new LevelMeta()));
            SetProperty(Game, nameof(Game.Difficulty), Difficulty.Extreme);
            var state = new GameState(Game, mode, new HashSet<Mod>()) {IsPlaying = true};
            SetProperty(Game, nameof(Game.State), state);
        }

        public void Down(int finger)
        {
            Physics2D.SyncTransforms();
            Invoke("OnFingerDown", new GameFinger
            {
                Index = finger,
                ScreenPosition = Game.camera.WorldToScreenPoint(Vector3.zero)
            });
        }

        /// <summary>
        /// Raises the real OnFingerUpdate chain for a finger resting at world zero, as
        /// GameTouchInput does on every Moved/Stationary frame after the press frame.
        /// </summary>
        public void Update(int finger)
        {
            Physics2D.SyncTransforms();
            Invoke("OnFingerUpdate", new GameFinger
            {
                Index = finger,
                ScreenPosition = Game.camera.WorldToScreenPoint(Vector3.zero)
            });
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
            // Drain pending Clear -> AwaitAndCollect callbacks safely even after pool reuse.
            foreach (var go in objects)
            {
                var note = go.GetComponent<Note>();
                if (note != null && note.Model != null) note.Collect();
            }
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
            if (settingsOverridden) SetProperty(Context.Player, nameof(Player.Settings), previousSettings);
        }
    }
}

// Minimal renderers avoid loading gameplay art; inherited admission and grading stay real.
public class DropDragGuardTestDrag : DropDragNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}

public class DropDragGuardTestClick : DropClickNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override bool IsAutoEnabled() => false;
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}
