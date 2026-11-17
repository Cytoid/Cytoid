// Skin-only flat click head: enter/exit/press visuals draw from the compiled
// skin's slots in FlatNoteRendererBase (DrawSkinHead). This subclass remains as
// the kind/family binding point for ClickNote renderer selection.
public class FlatClickNoteRenderer : FlatNoteRendererBase
{
    public FlatClickNoteRenderer(ClickNote note) : base(note)
    {
    }
}
