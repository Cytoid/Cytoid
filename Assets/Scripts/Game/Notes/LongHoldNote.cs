public class LongHoldNote : HoldNote
{
    public override NoteGrade CalculateGrade()
    {
        return base.CalculateGrade();
    }

    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicLongHoldNoteRenderer(this)
            : new ClassicLongHoldNoteRenderer(this);
    }
}