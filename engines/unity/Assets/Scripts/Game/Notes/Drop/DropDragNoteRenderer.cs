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

    // Faithful port of Cylheim pixi-playback-note-layer.ts:524-540 getPlaybackDropSpriteOffsetY,
    // applied only to visual children so the collider stays at the landing scanline point.
    // Verified: durationTick=500, timeDiff=0.1s, height=540 -> 800px; height=1080 -> 1600px.
    private void ApplyDropOffset()
    {
        var page = Note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;

        Vector3 localOffset = Vector3.zero;
        if (durationTick > 0 && double.IsFinite(durationTick))
        {
            // Cylheim uses screen coords (Y down): Up=+1, Down=-1.
            // Unity uses world coords (Y up): flip the sign so Down falls from above (+Y),
            // Up rises from below (-Y).
            float dirSign = Note.Model.NoteDirection == 1 ? -1f : 1f; // Up -> -1, Down -> +1
            float timeDiffSeconds = (float) (Note.Model.start_time - Note.Game.Time);

            if (timeDiffSeconds > 0f)
            {
                // Cylheim formula in screen-heights (resolution-independent):
                //   offsetY_screen_heights = dir * (8_000_000 / durationTick) * timeDiff / 1080
                float screenHeightWorld = 2f * Note.Game.camera.orthographicSize;
                float offsetYWorld = dirSign * (8_000_000f / (float) durationTick) * timeDiffSeconds
                                     / 1080f * screenHeightWorld;
                // Translate the world-space vertical offset into Note's local space so the
                // visual amplitude is correct under any combination of rotation AND scale.
                // Guard against singular (zero) or non-finite scale to avoid NaN — see
                // DropClickNoteRenderer.ApplyDropOffset for full rationale.
                Vector3 s = Note.transform.localScale;
                if (s.x != 0f && s.y != 0f && s.z != 0f
                    && float.IsFinite(s.x) && float.IsFinite(s.y) && float.IsFinite(s.z))
                {
                    localOffset = Note.transform.InverseTransformVector(
                        new Vector3(0f, offsetYWorld, 0f));
                }
            }
        }

        ApplyChildOffset(Ring.transform, ringBaseLocal, localOffset);
        ApplyChildOffset(Fill.transform, fillBaseLocal, localOffset);
        if (hasNoteId) ApplyChildOffset(NoteId.transform, noteIdBaseLocal, localOffset);
    }

    private static void ApplyChildOffset(Transform t, Vector3 baseLocal, Vector3 offset)
    {
        t.localPosition = baseLocal + offset;
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
