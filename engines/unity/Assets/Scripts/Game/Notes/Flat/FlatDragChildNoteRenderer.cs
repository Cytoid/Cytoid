// Flat drag child (drag.ts childDraw semantics live in the skin now): the skin's
// enter/exit slots draw the bead. This subclass keeps the click-drag family
// palette binding and the judged-child chain visibility window.
public class FlatDragChildNoteRenderer : FlatNoteRendererBase
{
    public FlatDragChildNoteRenderer(DragChildNote note) : base(note)
    {
    }

    protected override FlatFamily Family =>
        Note.Type == NoteType.CDragChild ? FlatFamily.ClickDrag : FlatFamily.Drag;

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (HeadVisible || !ClearedSuccessfully || Note.IsDragStackFollower ||
            Game.State.Mods.Contains(Mod.HideNotes) || Game.Time > Note.Model.start_time)
            return;
        // A judged child stays visible until the head passes it (start_time) so the
        // chain reads as lit — Classic ClassicDragChildNoteRenderer semantics.
        HeadVisible = true;
        HeadMesh.SetVisible(true);
    }
}
