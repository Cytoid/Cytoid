public class LongHoldNote : HoldNote
{
    protected override NoteRenderer CreateRenderer()
    {
        return new LongHoldNoteRenderer(this);
    }
}