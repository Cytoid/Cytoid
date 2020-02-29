public class ClickNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClickNoteClassicRenderer(this)
            : new ClickNoteDefaultRenderer(this);
    }
}