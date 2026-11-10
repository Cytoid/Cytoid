using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using InputFixture = DropDragGuardTests.InputFixture;

public class HoldResumeTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void CountdownRebindPreservesProgressAndRankedGrade(bool longHold)
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            fixture.PrepareJudgments(GameMode.Standard);
            Context.Player.Settings.HoldHitSoundTiming = HoldHitSoundTiming.End;
            var hold = longHold
                ? fixture.Make<LongHoldNote>(1, 1f, NoteType.LongHold)
                : fixture.Make<HoldNote>(1, 1f, NoteType.Hold);
            hold.Model.end_time = 3f;
            fixture.Time(1.05f);
            fixture.Down(0);
            fixture.Time(2f);
            Tick(fixture, hold);
            Assert.That(hold.HeldDuration, Is.EqualTo(0.95f).Within(0.0001f));

            Pause(fixture);
            Assert.That(hold.IsHolding, Is.False);
            Countdown(fixture);
            fixture.Down(1);
            Assert.That(hold.HoldingFingers, Is.EqualTo(new[] {1}));
            Assert.That(hold.HoldingStartTime, Is.EqualTo(1.05f));
            Assert.That(hold.HeldDuration, Is.EqualTo(0.95f).Within(0.0001f));

            // A second pause while still holding must keep the same timing.
            Resume(fixture);
            Pause(fixture);
            Countdown(fixture);
            fixture.Update(1);
            Resume(fixture);
            fixture.Time(2.99f);
            Tick(fixture, hold);
            Assert.That(hold.HeldDuration, Is.EqualTo(1.94f).Within(0.0001f));
            Assert.That(hold.CalculateGrade(), Is.EqualTo(NoteGrade.Great));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LeavingTheHoldDuringCountdownKeepsItUnheldAtResume(bool lift)
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            var hold = fixture.Make<HoldNote>(1, 1f, NoteType.Hold);
            hold.Model.end_time = 3f;
            fixture.Time(1f);
            fixture.Down(0);
            fixture.Time(2f);
            Pause(fixture);
            Countdown(fixture);
            fixture.Update(1);
            Assert.That(hold.IsHolding, Is.True);

            if (lift) fixture.Up(1, Vector3.zero);
            else fixture.Update(1, new Vector3(4f, 0f, 0f));
            Assert.That(hold.IsCleared, Is.False);
            Assert.That(hold.IsHolding, Is.False);
            Assert.That(fixture.Input.HoldingNotes, Is.Empty);
            Resume(fixture);
            Assert.That(hold.ShouldMiss(), Is.True);
        }
    }

    [Test]
    public void CountdownOnlyBindsHoldsThatWereHeldAtPause()
    {
        using (var fixture = new InputFixture(GameMode.Standard))
        {
            var held = fixture.Make<HoldNote>(1, 1f, NoteType.Hold);
            var untouched = fixture.Make<HoldNote>(2, 1f, NoteType.Hold);
            untouched.transform.position = new Vector3(5f, 0f, 0f);
            fixture.Time(1f);
            fixture.Down(0);
            Pause(fixture);
            fixture.Down(1);
            Assert.That(held.IsHolding, Is.False, "Pause menu does not accept Hold input.");

            Countdown(fixture);
            fixture.Down(2, untouched.transform.position);
            fixture.Update(2, untouched.transform.position);
            Assert.That(untouched.IsHolding, Is.False);
            Assert.That(fixture.Input.HoldingNotes, Is.Empty);
        }
    }

    private static void Pause(InputFixture fixture)
    {
        fixture.Game.State.IsPlaying = false;
        fixture.Game.onGamePaused.Invoke(fixture.Game);
    }

    private static void Countdown(InputFixture fixture) =>
        DropDragGuardTests.SetProperty(fixture.Game, nameof(Game.UnpauseCountdown), 3f);

    private static void Resume(InputFixture fixture)
    {
        DropDragGuardTests.SetProperty(fixture.Game, nameof(Game.UnpauseCountdown), 0f);
        fixture.Game.State.IsPlaying = true;
        fixture.Game.onGameUnpaused.Invoke(fixture.Game);
    }

    private static void Tick(InputFixture fixture, HoldNote hold) =>
        typeof(HoldNote).GetMethod("OnGameUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(hold, new object[] {fixture.Game});
}
