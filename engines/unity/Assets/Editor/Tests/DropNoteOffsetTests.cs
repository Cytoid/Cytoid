using NUnit.Framework;

public class DropNoteOffsetTests
{
    // The same music stored at time_base 960 doubles every tick, so durationTick doubles
    // and the raw speed ratio would halve; the time-base compensation must cancel that
    // exactly. Port of Cylheim ca424f81's equivalent-time-base tests.
    [Test]
    public void ScreenSpaceOffsetIsStableAcrossEquivalentTimeBases()
    {
        var baseOffset = DropNoteOffset.ComputeScreenSpaceOffsetY(500, 480, 0.1, 1);
        var scaledOffset = DropNoteOffset.ComputeScreenSpaceOffsetY(1000, 960, 0.1, 1);

        Assert.That(baseOffset, Is.EqualTo(1600d / 1080d).Within(1e-9),
            "pinned amplitude of the ported constant: 8_000_000/500 * 0.1 = 1600 screen pixels at 1080p");
        Assert.That(scaledOffset, Is.EqualTo(baseOffset).Within(1e-6),
            "equivalent charts must fall at the same speed");
        Assert.That(DropNoteOffset.ComputeScreenSpaceOffsetY(500, 480, 0.1, -1),
            Is.EqualTo(-baseOffset).Within(1e-6), "direction only flips the sign");
    }

    [TestCase(1, -14.814815f)] // Up rises from below: negative Y in Unity world space
    [TestCase(0, 14.814815f)] // Down falls from above: positive Y
    public void LocalOffsetKeepsEquivalentTimeBaseChartsAtEqualSpeed(int noteDirection, float expectedY)
    {
        using (var fixture = new DropDragGuardTests.InputFixture(GameMode.Practice))
        {
            var baseNote = fixture.Click(1, 2f);
            var scaledNote = fixture.Click(2, 2f);
            // durationTick=500 at time_base 480 vs durationTick=1000 at time_base 960:
            // same music, different tick scale.
            ApplyTimeBase(baseNote, 480, 100);
            ApplyTimeBase(scaledNote, 960, 200);
            baseNote.Model.NoteDirection = noteDirection;
            scaledNote.Model.NoteDirection = noteDirection;

            fixture.Time(1.9f); // both notes are 0.1s from landing

            var baseOffset = DropNoteOffset.ComputeLocalOffset(baseNote);
            var scaledOffset = DropNoteOffset.ComputeLocalOffset(scaledNote);

            // The fixture camera is orthographic with size 5 -> 10 world units per screen
            // height: 1600/1080 screen pixels * 10 = 14.814815 world units.
            Assert.That(baseOffset.y, Is.EqualTo(expectedY).Within(0.0001f));
            Assert.That(scaledOffset.y, Is.EqualTo(baseOffset.y).Within(0.0001f),
                "equivalent charts at another time base fall at the same speed");
        }
    }

    private static void ApplyTimeBase(Note note, double timeBase, int pageEndTick)
    {
        DropDragGuardTests.SetProperty(note, nameof(Note.Chart), new ChartModel {time_base = timeBase});
        DropDragGuardTests.SetProperty(note, nameof(Note.Page), new ChartModel.Page
        {
            start_tick = 0, end_tick = pageEndTick
        });
    }
}
