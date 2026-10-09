using NUnit.Framework;

public class DropClickWindowTests
{
    [TestCase(-0.800f, NoteGrade.None)]
    [TestCase(-0.600f, NoteGrade.None)]
    [TestCase(-0.400f, NoteGrade.None)]
    [TestCase(-0.399f, NoteGrade.Good)]
    [TestCase(-0.200f, NoteGrade.Good)]
    [TestCase(-0.199f, NoteGrade.Great)]
    [TestCase(-0.070f, NoteGrade.Great)]
    [TestCase(-0.069f, NoteGrade.Perfect)]
    [TestCase(0.069f, NoteGrade.Perfect)]
    [TestCase(0.070f, NoteGrade.Great)]
    [TestCase(0.149f, NoteGrade.Great)]
    [TestCase(0.150f, NoteGrade.Good)]
    [TestCase(0.199f, NoteGrade.Good)]
    [TestCase(0.200f, NoteGrade.Miss)]
    [TestCase(0.250f, NoteGrade.Miss)]
    public void PracticeMatchesReferenceWindow(float delta, NoteGrade expected)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(GameMode.Practice))
        {
            var click = fixture.Click(1, 0f);
            fixture.Time(delta);
            Assert.That(click.GetTouchGrade(), Is.EqualTo(expected));
            Assert.That(click.CalculateGrade(), Is.EqualTo(expected));
        }
    }

    [TestCase(-0.400f, NoteGrade.None)]
    [TestCase(-0.399f, NoteGrade.Bad)]
    [TestCase(-0.200f, NoteGrade.Bad)]
    [TestCase(-0.199f, NoteGrade.Good)]
    [TestCase(-0.070f, NoteGrade.Good)]
    [TestCase(-0.069f, NoteGrade.Great)]
    [TestCase(-0.040f, NoteGrade.Perfect)]
    [TestCase(0.040f, NoteGrade.Perfect)]
    [TestCase(0.041f, NoteGrade.Great)]
    [TestCase(0.070f, NoteGrade.Good)]
    [TestCase(0.150f, NoteGrade.Bad)]
    [TestCase(0.199f, NoteGrade.Bad)]
    [TestCase(0.200f, NoteGrade.Miss)]
    public void RankedKeepsStricterGradesAnd40msHighestWindow(float delta, NoteGrade expected)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(GameMode.Standard))
        {
            var click = fixture.Click(1, 0f);
            fixture.Time(delta);
            Assert.That(click.GetTouchGrade(), Is.EqualTo(expected));
        }
    }

    [TestCase(GameMode.Practice, -0.1f)]
    [TestCase(GameMode.Practice, 0.1f)]
    [TestCase(GameMode.Standard, -0.1f)]
    [TestCase(GameMode.Standard, 0.1f)]
    public void AutomaticMissAndGradesUseEffectiveTime(GameMode mode, float offset)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(mode))
        {
            var click = fixture.Click(1, 0f);
            DropDragGuardTests.SetProperty(click, nameof(Note.JudgmentOffset), offset);
            fixture.Time(offset + 0.199f);
            Assert.That(click.ShouldMiss(), Is.False);
            fixture.Time(offset + 0.201f);
            Assert.That(click.ShouldMiss(), Is.True);
            Assert.That(click.GetTouchGrade(), Is.EqualTo(NoteGrade.Miss));
        }
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void UntouchedDropClickExpiresAt200msWithoutChangingOrdinaryClick(GameMode mode)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(mode))
        {
            var click = fixture.Click(1, 0f);
            fixture.Time(0.200f);
            Assert.That(click.MissThreshold, Is.EqualTo(0.2f));
            Assert.That(click.ShouldMiss(), Is.True);
            Assert.That(NoteType.Click.GetDefaultMissThreshold(), Is.EqualTo(0.3f));
            Assert.That(NoteType.DropDrag.GetDefaultMissThreshold(), Is.EqualTo(0.3f));
        }
    }

    [TestCase(GameMode.Practice)]
    [TestCase(GameMode.Standard)]
    public void DropDragRetainsItsSameWindowInBothModes(GameMode mode)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(mode))
        {
            var drag = fixture.Drag(1, 0f);
            fixture.Time(-0.309f);
            Assert.That(drag.CanHandleTouch(), Is.True);
            Assert.That(drag.GetTouchGrade(), Is.EqualTo(NoteGrade.Perfect));
            fixture.Time(-0.311f);
            Assert.That(drag.CanHandleTouch(), Is.False);
            fixture.Time(0.199f);
            Assert.That(drag.GetTouchGrade(), Is.EqualTo(NoteGrade.Perfect));
            fixture.Time(0.200f);
            Assert.That(drag.GetTouchGrade(), Is.EqualTo(NoteGrade.Miss));
            Assert.That(drag.ShouldMiss(), Is.False);
            fixture.Time(0.301f);
            Assert.That(drag.ShouldMiss(), Is.True);
        }
    }
}
