public class ClickNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return new ClickNoteRenderer(this);
    }
}