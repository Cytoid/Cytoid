public class ClickNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicClickNoteRenderer(this)
            : new DefaultClickNoteRenderer(this);
    }
}