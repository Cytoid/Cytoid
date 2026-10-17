using System;

public class ClickNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        if (Game.Config.UseFlatNoteDesign) return new FlatClickNoteRenderer(this);
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicClickNoteRenderer(this)
            : throw new NotSupportedException();
    }
}