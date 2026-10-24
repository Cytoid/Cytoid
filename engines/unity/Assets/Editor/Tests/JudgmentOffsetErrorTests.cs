using NUnit.Framework;

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
}
