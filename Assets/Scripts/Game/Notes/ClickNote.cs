using Random = UnityEngine.Random;

public class ClickNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return new ClickNoteClassicRenderer(this);
    }
}