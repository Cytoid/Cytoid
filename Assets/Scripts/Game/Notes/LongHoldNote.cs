public class LongHoldNote : HoldNote
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicLongHoldNoteRenderer(this)
            : new ClassicLongHoldNoteRenderer(this);
    }
}