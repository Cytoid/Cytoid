using UnityEngine;

// Shared drop-offset math for DropClickNoteRenderer and DropDragNoteRenderer.
// Faithful port of Cylheim pixi-playback-note-layer.ts:524-540 getPlaybackDropSpriteOffsetY,
// then translated into Note local space so the on-screen amplitude stays correct under any
// combination of rotation and scale.
// Verified: durationTick=500, timeDiff=0.1s, height=540 -> 800px; height=1080 -> 1600px.
internal static class DropNoteOffset
{
    // Per-frame visual-only offset to apply to each child of a Drop note so the note appears
    // to fall from above (Down direction) or below (Up direction) into the scanline. Note.transform
    // itself is left untouched so the CircleCollider2D stays anchored at the landing point.
    //
    // Returns Vector3.zero when the offset should be skipped: malformed chart (durationTick<=0),
    // already landed (timeDiff<=0), or singular/non-finite local scale (note is invisible anyway).
    public static Vector3 ComputeLocalOffset(Note note)
    {
        var page = note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;
        if (durationTick <= 0 || !double.IsFinite(durationTick)) return Vector3.zero;

        // Cylheim uses screen coords (Y down): Up=+1, Down=-1.
        // Unity uses world coords (Y up): flip the sign so Down falls from above (+Y),
        // Up rises from below (-Y).
        float dirSign = note.Model.NoteDirection == 1 ? -1f : 1f; // Up -> -1, Down -> +1
        float timeDiffSeconds = (float) (note.Model.start_time - note.Game.Time);
        if (timeDiffSeconds <= 0f) return Vector3.zero;

        // Cylheim formula in screen-heights (resolution-independent):
        //   offsetY_screen_heights = dir * (8_000_000 / durationTick) * timeDiff / 1080
        // Translate to Unity world units via visible camera height (2 * orthographicSize):
        float screenHeightWorld = 2f * note.Game.camera.orthographicSize;
        float offsetYWorld = dirSign * (8_000_000f / (float) durationTick) * timeDiffSeconds
                             / 1080f * screenHeightWorld;

        // Translate the world-space vertical offset into Note's local space so the visual
        // amplitude is correct under any combination of rotation AND scale. Guard against
        // singular (zero) or non-finite scale to avoid NaN from the matrix inversion — a
        // zero-scale note is invisible anyway, so the offset is irrelevant.
        Vector3 s = note.transform.localScale;
        if (s.x == 0f || s.y == 0f || s.z == 0f
            || !float.IsFinite(s.x) || !float.IsFinite(s.y) || !float.IsFinite(s.z))
        {
            return Vector3.zero;
        }

        return note.transform.InverseTransformVector(new Vector3(0f, offsetYWorld, 0f));
    }

    // Sets t.localPosition = baseLocal + offset. Kept as a named helper so the per-frame
    // iteration contract lives in one place across both Drop renderers.
    public static void ApplyToChild(Transform t, Vector3 baseLocal, Vector3 offset)
    {
        t.localPosition = baseLocal + offset;
    }
}
