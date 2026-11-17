using System;

public class ClickNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        // Flat is exclusive under its setting: Game.Initialize aborts the load
        // when the skin is unavailable, so selection never defers to classic.
        if (Game.Config.UseFlatNoteDesign) return new FlatClickNoteRenderer(this);
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicClickNoteRenderer(this)
            : throw new NotSupportedException();
    }
}