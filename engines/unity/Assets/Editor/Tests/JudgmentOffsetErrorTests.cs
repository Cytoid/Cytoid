using NUnit.Framework;
using UnityEngine;

public class JudgmentOffsetErrorTests
{
    [TestCase(GameMode.Standard, 0.1f, -0.05f)]
    [TestCase(GameMode.Standard, -0.1f, 0.05f)]
    [TestCase(GameMode.Practice, 0.1f, -0.10f)]
    [TestCase(GameMode.Practice, -0.1f, 0.10f)]
    public void ClearRecordsEffectiveErrorInStateAndEvent(GameMode mode, float offset, float error)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(mode))
        {
            var note = fixture.Click(1, 0f);
            fixture.PrepareJudgments(mode);
            DropDragGuardTests.SetProperty(note, nameof(Note.JudgmentOffset), offset);
            fixture.Time(offset + error);
            JudgeData observed = null;
            fixture.Game.onNoteJudged.AddListener((game, judged, data) => observed = data);

            var grade = note.CalculateGrade();
            note.Clear(grade);

            Assert.That(fixture.Game.State.Judgements[1].Error, Is.EqualTo(error).Within(1e-6));
            Assert.That(observed, Is.Not.Null);
            Assert.That(observed.Error, Is.EqualTo(error).Within(1e-6));
            Assert.That(observed.Grade, Is.EqualTo(grade));

            fixture.Game.State.IsCompleted = true;
            fixture.Game.State.OnComplete();
            if (grade != NoteGrade.Perfect && grade != NoteGrade.Miss)
            {
                Assert.That(fixture.Game.State.EarlyCount, Is.EqualTo(error < 0 ? 1 : 0));
                Assert.That(fixture.Game.State.LateCount, Is.EqualTo(error > 0 ? 1 : 0));
            }
        }
    }

    [Test]
    public void ZeroOffsetKeepsExistingError()
    {
        using (var fixture = new DropDragGuardTests.InputFixture(GameMode.Standard))
        {
            var note = fixture.Click(1, 0f);
            fixture.PrepareJudgments(GameMode.Standard);
            fixture.Time(0.05f);

            note.Clear(note.CalculateGrade());

            Assert.That(fixture.Game.State.Judgements[1].Error, Is.EqualTo(0.05f).Within(1e-6));
        }
    }

    [TestCase(0.1f)]
    [TestCase(-0.1f)]
    public void AutoplayKeepsRawChartTimingError(float offset)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(GameMode.Standard))
        {
            var model = fixture.Model(1, 1f, NoteType.DropClick);
            fixture.PrepareJudgments(GameMode.Standard);
            fixture.Game.State.Mods.Add(Mod.Auto);
            var go = new GameObject("AutoplayOffsetTest");
            go.AddComponent<CircleCollider2D>();
            var note = go.AddComponent<JudgmentOffsetTestAutoClick>();
            try
            {
                note.Initialize(fixture.Game);
                note.SetData(model.id);
                fixture.Game.SpawnedNotes[model.id] = note;
                DropDragGuardTests.SetProperty(note, nameof(Note.JudgmentOffset), offset);
                JudgeData observed = null;
                fixture.Game.onNoteJudged.AddListener((game, judged, data) => observed = data);
                fixture.Time(1.001f);

                note.TickStackFollowerJudgment();

                Assert.That(fixture.Game.State.Judgements[1].Grade, Is.EqualTo(NoteGrade.Perfect));
                Assert.That(fixture.Game.State.Judgements[1].Error, Is.EqualTo(0.001f).Within(1e-6));
                Assert.That(observed, Is.Not.Null);
                Assert.That(observed.Error, Is.EqualTo(0.001f).Within(1e-6));
            }
            finally
            {
                note.Collect();
                Object.DestroyImmediate(go);
            }
        }
    }

    [TestCase(-0.1f, false)]
    [TestCase(0.1f, false)]
    [TestCase(-0.1f, true)]
    [TestCase(0.1f, true)]
    public void TouchedAutoDragUsesEffectiveTiming(float offset, bool release)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(GameMode.Standard))
        {
            var model = fixture.Model(1, 1f, NoteType.DragChild);
            fixture.PrepareJudgments(GameMode.Standard);
            fixture.Game.State.Mods.Add(Mod.AutoDrag);
            var go = new GameObject("TouchedAutoDragOffsetTest");
            go.AddComponent<CircleCollider2D>();
            var note = go.AddComponent<JudgmentOffsetTestAutoDrag>();
            try
            {
                note.Initialize(fixture.Game);
                note.SetData(model.id);
                fixture.Game.SpawnedNotes[model.id] = note;
                DropDragGuardTests.SetProperty(note, nameof(Note.JudgmentOffset), offset);
                JudgeData observed = null;
                fixture.Game.onNoteJudged.AddListener((game, judged, data) => observed = data);
                fixture.Time(0.8f);
                Assert.That(note.OnTouchDeferred(Vector2.zero), Is.True);
                Assert.That(note.IsArmed, Is.True);
                fixture.Time(1f + offset);

                if (release) note.SettleArmed();
                else note.TickStackFollowerJudgment();

                Assert.That(fixture.Game.State.Judgements[1].Grade, Is.EqualTo(NoteGrade.Perfect));
                Assert.That(fixture.Game.State.Judgements[1].Error, Is.EqualTo(0).Within(1e-6));
                Assert.That(observed, Is.Not.Null);
                Assert.That(observed.Error, Is.EqualTo(0).Within(1e-6));
            }
            finally
            {
                note.Collect();
                Object.DestroyImmediate(go);
            }
        }
    }
}

public class JudgmentOffsetTestAutoClick : DropClickNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}

public class JudgmentOffsetTestAutoDrag : DragChildNote
{
    protected override NoteRenderer CreateRenderer() => new NoteRenderer(this);
    public override void PlayHitSound() { }
    public new void OnDestroy() { }
}
