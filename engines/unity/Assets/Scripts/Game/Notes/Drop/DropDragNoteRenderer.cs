using UnityEngine;

// Drop note renderer for DropDrag. Inherits ClassicNoteRenderer directly (DropDrag does not
// extend ClickNote, and DropDrag has no Core layer). Behaves like DropClickNoteRenderer minus
// the Core handling: drop notes ignore approach scale/fill animations and reach the scanline
// via the Y offset alone, applied only to visual children so the CircleCollider2D stays
// anchored at the landing scanline point.
public class DropDragNoteRenderer : ClassicNoteRenderer
{
    // Base localPosition of each visual child, snapshotted ONCE in the constructor when the
    // Note GameObject is freshly created from its prefab. Pool-reuse safe — see DropClickNoteRenderer.
    private Vector3 ringBaseLocal;
    private Vector3 fillBaseLocal;
    private Vector3 noteIdBaseLocal;
    private bool hasNoteId;

    public DropDragNoteRenderer(Note note) : base(note)
    {
        // Snapshot base localPosition once. base() has already instantiated Ring, Fill, and
        // (when DisplayNoteId) NoteId, so all visual children are available here.
        ringBaseLocal = Ring.transform.localPosition;
        fillBaseLocal = Fill.transform.localPosition;
        hasNoteId = DisplayNoteId && NoteId != null;
        if (hasNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        // Drop note Ring sprites are solid bars (unlike Classic note rings which are outlines).
        // Render Fill on top of Ring so the colored Fill is visible.
        Fill.sortingOrder = Ring.sortingOrder + 1;
    }

    public override void OnCollect()
    {
        base.OnCollect();
        // Restore prefab-local positions so a Note GameObject pulled from the pool starts the
        // next cycle from a clean visual state.
        Ring.transform.localPosition = ringBaseLocal;
        Fill.transform.localPosition = fillBaseLocal;
        if (hasNoteId) NoteId.transform.localPosition = noteIdBaseLocal;
    }

    protected override void Render()
    {
        base.Render();
        ApplyDropOffset();
    }

    // Drives the falling animation by offsetting only the visual children — see
    // DropNoteOffset.ComputeLocalOffset for the math and rationale.
    private void ApplyDropOffset()
    {
        Vector3 localOffset = DropNoteOffset.ComputeLocalOffset(Note);
        DropNoteOffset.ApplyToChild(Ring.transform, ringBaseLocal, localOffset);
        DropNoteOffset.ApplyToChild(Fill.transform, fillBaseLocal, localOffset);
        if (hasNoteId) DropNoteOffset.ApplyToChild(NoteId.transform, noteIdBaseLocal, localOffset);
    }

    // Drop notes ignore the approach-scale animation: always full BaseTransformSize.
    protected override void UpdateTransformScale()
    {
        var scale = BaseTransformSize * Note.Model.Override.SizeMultiplier;
        Note.transform.localScale = new Vector3(scale, scale, Note.transform.localScale.z);
    }

    // Drop notes ignore the fill-grow animation: always full (1,1).
    protected override void UpdateFillScale()
    {
        var z = Fill.transform.localScale.z;
        Fill.transform.localScale = new Vector3(1, 1, z);
    }
}
