using UnityEngine;

// Flat drop drag: the capsule visuals draw from the compiled skin's slots in
// FlatNoteRendererBase. Same exit ride (ComputeRideLocalOffset + ExitClockScale)
// and falling/size behavior as FlatDropClickNoteRenderer — SYNC-WARNING:
// the offset plumbing deliberately mirrors the Classic drop renderer pair
// (DropClickNoteRenderer / DropDragNoteRenderer), which also keep two parallel
// implementations instead of a shared base.
public class FlatDropDragNoteRenderer : FlatNoteRendererBase
{
    // Base localPosition of the mesh child, snapshotted ONCE from the pristine child
    // (pool-reuse safety: see DropDragNoteRenderer).
    private Vector3 meshBaseLocal;
    private Vector3 noteIdBaseLocal;

    public FlatDropDragNoteRenderer(Note note) : base(note)
    {
        meshBaseLocal = HeadMesh.transform.localPosition;
        if (DisplayNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    protected override double ExitClockScale => DropNoteOffset.ExitClockScale(Note);

    protected override void Render()
    {
        base.Render();
        var localOffset = DropNoteOffset.ComputeRideLocalOffset(Note);
        HeadMesh.transform.localPosition = meshBaseLocal + localOffset;
        // The ID rides the falling capsule like DropDragNoteRenderer.
        if (DisplayNoteId) DropNoteOffset.ApplyToChild(NoteId.transform, noteIdBaseLocal, localOffset);
    }

    public override void OnCollect()
    {
        base.OnCollect();
        HeadMesh.transform.localPosition = meshBaseLocal;
        if (DisplayNoteId) NoteId.transform.localPosition = noteIdBaseLocal;
    }
}
