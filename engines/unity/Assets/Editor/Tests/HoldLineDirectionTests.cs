using NUnit.Framework;

public class HoldLineDirectionTests
{
    [Test]
    public void WithoutOverride_FlipFollowsChronologicalTravel()
    {
        Assert.That(Flip(Note(), Page(scan: 1, a: 1)), Is.False);
        Assert.That(Flip(Note(), Page(scan: -1, a: 1)), Is.True);
        // Negative a reverses the page; travel flips with it (#212 behavior preserved).
        Assert.That(Flip(Note(), Page(scan: 1, a: -1)), Is.True);
        Assert.That(Flip(Note(), Page(scan: -1, a: -1)), Is.False);
    }

    [Test]
    public void OverrideMinusOne_FlipsAgainstUpwardTravel()
    {
        // Audited repro: page travels up (scan 1, a 1), controller sets hold_direction -1.
        var note = Note();
        note.Override.HoldDirection = -1;

        Assert.That(Flip(note, Page(scan: 1, a: 1)), Is.True);
    }

    [Test]
    public void OverrideOne_IsAbsoluteNoFlipOnDownwardTravel()
    {
        var note = Note();
        note.Override.HoldDirection = 1;

        Assert.That(Flip(note, Page(scan: -1, a: 1)), Is.False);
    }

    [Test]
    public void OverrideZero_DoesNotFlip()
    {
        var note = Note();
        note.Override.HoldDirection = 0;

        Assert.That(Flip(note, Page(scan: -1, a: 1)), Is.False);
    }

    static bool Flip(ChartModel.Note note, ChartModel.Page page)
    {
        return ClassicHoldNoteRenderer.ComputeLineFlipY(note, page);
    }

    static ChartModel.Note Note()
    {
        return new ChartModel.Note {id = 1, type = (int) NoteType.Hold};
    }

    static ChartModel.Page Page(int scan, float a)
    {
        var page = new ChartModel.Page {scan_line_direction = scan};
        page.position_arg_a = a;
        return page;
    }
}
