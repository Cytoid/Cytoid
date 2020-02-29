public class LongHoldNote : HoldNote
{
    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new LongHoldClassicNoteRenderer(this)
            : new LongHoldClassicNoteRenderer(this);
    }
}