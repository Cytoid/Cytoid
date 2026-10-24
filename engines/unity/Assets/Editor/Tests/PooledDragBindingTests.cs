using NUnit.Framework;
using UnityEngine;

public class PooledDragBindingTests
{
    [TestCase(GameMode.Standard)]
    [TestCase(GameMode.Practice)]
    public void OldFingerReleaseDoesNotSettleReusedStationaryRecording(GameMode mode)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(mode))
        {
            var first = fixture.DragChild(0, 10f);
            var nextModel = fixture.Model(1, 13f, NoteType.DragChild);
            nextModel.intro_time = 12f;
            fixture.PrepareJudgments(mode);

            fixture.Time(9.9f);
            fixture.Down(0, new Vector3(-6f, 0f, 0f));
            fixture.Update(0);
            Assert.That(first.IsArmed, Is.True);
            fixture.Update(0, new Vector3(6f, 0f, 0f));
            fixture.Time(10.01f);
            first.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(0), Is.True);
            first.Collect();

            fixture.Time(11.5f);
            var reused = fixture.Game.ObjectPool.SpawnNote(nextModel);
            Assert.That(reused, Is.SameAs(first));
            fixture.Down(1);
            Assert.That(reused.IsArmed, Is.False);
            fixture.Time(12.9f);
            fixture.Update(1);
            Assert.That(reused.IsArmed, Is.True);

            fixture.Up(0, new Vector3(6f, 0f, 0f));
            Assert.That(fixture.Game.State.IsJudged(1), Is.False);
            fixture.Time(13f);
            reused.TickStackFollowerJudgment();
            Assert.That(fixture.Game.State.IsJudged(1), Is.True);
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(2));
        }
    }

    [TestCase(GameMode.Standard)]
    [TestCase(GameMode.Practice)]
    public void CollectingAnotherNotePreservesLiveReleaseBinding(GameMode mode)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(mode))
        {
            var drag = fixture.DragChild(0, 10f);
            var other = fixture.Click(1, 20f);
            other.transform.position = new Vector3(20f, 0f, 0f);
            fixture.PrepareJudgments(mode);
            fixture.Time(9.9f);
            fixture.Down(0, new Vector3(-6f, 0f, 0f));
            fixture.Update(0);
            Assert.That(drag.IsArmed, Is.True);

            other.Collect();
            fixture.Up(0, Vector3.zero);

            Assert.That(fixture.Game.State.IsJudged(0), Is.True);
            Assert.That(fixture.Game.State.ClearCount, Is.EqualTo(1));
        }
    }
}
