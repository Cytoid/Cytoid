public class DropClickNote : ClickNote
{
    // Practice aligns with the drop chart's 400ms early / 200ms late window.
    // Ranked keeps the normal stricter grades, including its 40ms highest grade.
    public override NoteGrade CalculateGrade()
    {
        if (ShouldMiss()) return NoteGrade.Miss;
        var grade = base.CalculateGrade();
        return Game.State.Mode == GameMode.Practice && grade == NoteGrade.Bad
            ? NoteGrade.None
            : grade;
    }

    public override bool ShouldMiss() =>
        Game.Time >= Model.start_time + JudgmentOffset + MissThreshold;

    protected override NoteRenderer CreateRenderer()
    {
        return new DropClickNoteRenderer(this);
    }
}
