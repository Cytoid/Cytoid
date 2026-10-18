public class LongHoldNote : HoldNote
{
    protected override NoteRenderer CreateRenderer()
    {
        if (Game.Config.UseFlatNoteDesign) return new FlatLongHoldNoteRenderer(this);
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicLongHoldNoteRenderer(this)
            : new ClassicLongHoldNoteRenderer(this);
    }
}