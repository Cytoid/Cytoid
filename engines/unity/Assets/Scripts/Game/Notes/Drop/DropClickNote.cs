public class DropClickNote : ClickNote
{
    protected override NoteRenderer CreateRenderer()
    {
        return new DropClickNoteRenderer(this);
    }
}
