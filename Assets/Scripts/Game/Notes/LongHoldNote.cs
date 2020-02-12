public class LongHoldNote : HoldNote
{
    protected override NoteRenderer CreateRenderer()
    {
        return new LongHoldNoteClassicRenderer(this);
    }
}