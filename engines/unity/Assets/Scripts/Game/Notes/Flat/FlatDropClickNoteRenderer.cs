using UnityEngine;

// Flat drop click: the capsule visuals draw from the compiled skin's slots in
// FlatNoteRendererBase. Past the line the exit ride takes over: the head keeps
// its approach speed past the scanline (DropNoteOffset.ComputeRideLocalOffset,
// capped at the travel distance) while the exit slot fades it on the compressed
// ride clock (ExitClockScale) — the fade IS the miss, there is no miss clip. The
// enter slot is a loop (the shipped skin freezes it on the first frame with
// period 0, so the capsule is still a still image that falls into the scanline),
// mirroring DropClickNoteRenderer; the fall offset moves only the FlatNoteMesh
// child so the collider stays anchored at the landing scanline.
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

    protected override double ExitClockScale => DropNoteOffset.ExitClockScale(Note);

    protected override void Render()
    {
        base.Render();
        var localOffset = DropNoteOffset.ComputeRideLocalOffset(Note);
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
