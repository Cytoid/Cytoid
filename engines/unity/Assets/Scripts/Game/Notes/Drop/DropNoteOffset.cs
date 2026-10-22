using UnityEngine;

// Shared drop-offset math for DropClickNoteRenderer and DropDragNoteRenderer.
// Port of Cylheim pixi-playback-note-layer.ts getPlaybackDropSpriteOffsetY, updated to
// Cylheim commit ca424f81 ("fix: compensate drop speed for chart time base"), then
// translated into Note local space so the on-screen amplitude stays correct under any
// combination of rotation and scale.
// Verified at time_base 480: durationTick=500, timeDiff=0.1s, height=540 -> 800px; height=1080 -> 1600px.
public static class DropNoteOffset
{
    // The drop speed constant is calibrated for charts at time_base 480. A chart stored
    // at another time base rescales every tick by timeBase/480, so the raw
    // ticks-per-second speed ratio scales with the inverse; multiplying it back keeps
    // equivalent charts falling at the same speed (Cylheim ca424f81).
    private const double ReferenceTimeBase = 480;

    // The ported Cylheim formula, resolution-independent: returns the offset in
    // 1080-normalized screen heights (multiply by the visible height in the target
    // space), Y down, with direction +1 when the note falls upward (Up) and -1
    // downward (Down).
    public static double ComputeScreenSpaceOffsetY(
        double durationTick, double timeBase, double timeDiffSeconds, int direction)
    {
        return direction * (8_000_000d / durationTick)
               * (timeBase / ReferenceTimeBase) * timeDiffSeconds / 1080d;
    }

    // Per-frame visual-only offset to apply to each child of a Drop note so the note appears
    // to fall from above (Down direction) or below (Up direction) into the scanline. Note.transform
    // itself is left untouched so the CircleCollider2D stays anchored at the landing point.
    //
    // Returns Vector3.zero when the offset should be skipped: malformed chart (durationTick<=0),
    // already landed (timeDiff<=0), non-finite inputs (NaN/Infinity in timeDiff or scale), or
    // singular local scale (note is invisible anyway).
    public static Vector3 ComputeLocalOffset(Note note)
    {
        var page = note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;
        if (durationTick <= 0 || !double.IsFinite(durationTick)) return Vector3.zero;

        float timeDiffSeconds = (float) (note.Model.start_time - note.Game.Time);
        if (timeDiffSeconds <= 0f || !float.IsFinite(timeDiffSeconds)) return Vector3.zero;

        // Cylheim's screen space is Y down (Up=+1, Down=-1); Unity world space is Y up,
        // so the ported offset is negated: Down falls from above (+Y), Up rises from
        // below (-Y).
        int direction = note.Model.NoteDirection == 1 ? 1 : -1;

        // Translate to Unity world units via visible camera height (2 * orthographicSize):
        float screenHeightWorld = 2f * note.Game.camera.orthographicSize;
        float offsetYWorld = -(float) ComputeScreenSpaceOffsetY(
            durationTick, note.Chart.time_base, timeDiffSeconds, direction) * screenHeightWorld;

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
