using UnityEngine;

// Flat drop click: the capsule visuals draw from the compiled skin's slots in
// FlatNoteRendererBase (DrawSkinHead — drops have no exit, the enter slot covers
// them). This subclass keeps the falling-mesh offset plumbing: Drop notes have no
// enter curve (their enter slot is a loop; the shipped skin freezes it on the
// first frame with period 0, so the capsule is still a still image that falls
// into the scanline), mirroring DropClickNoteRenderer; the fall offset moves only
// the FlatNoteMesh child so the collider stays anchored at the landing scanline.
public class FlatDropClickNoteRenderer : FlatNoteRendererBase
{
    // Base localPosition of the mesh child, snapshotted ONCE from the pristine child
    // (pool-reuse safety: see DropClickNoteRenderer).
    private Vector3 meshBaseLocal;
    private Vector3 noteIdBaseLocal;

    public FlatDropClickNoteRenderer(ClickNote note) : base(note)
    {
        meshBaseLocal = HeadMesh.transform.localPosition;
        if (DisplayNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    protected override void Render()
    {
        base.Render();
        var localOffset = DropNoteOffset.ComputeLocalOffset(Note);
        HeadMesh.transform.localPosition = meshBaseLocal + localOffset;
        // The ID rides the falling capsule like DropClickNoteRenderer.
        if (DisplayNoteId) DropNoteOffset.ApplyToChild(NoteId.transform, noteIdBaseLocal, localOffset);
    }

    public override void OnCollect()
    {
        base.OnCollect();
        HeadMesh.transform.localPosition = meshBaseLocal;
        if (DisplayNoteId) NoteId.transform.localPosition = noteIdBaseLocal;
    }
}
