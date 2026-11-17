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

    /// <summary>The note's own approach speed in 1080-normalized screen heights per
    /// second — the slope of ComputeScreenSpaceOffsetY in timeDiff. 0 for a malformed
    /// chart (durationTick &lt;= 0).</summary>
    private static double ApproachSpeedScreensPerSecond(Note note, double durationTick)
    {
        if (durationTick <= 0 || !double.IsFinite(durationTick)) return 0;
        return 8_000_000d / durationTick * (note.Chart.time_base / ReferenceTimeBase) / 1080d;
    }

    /// <summary>Real seconds the exit ride lasts at the note's own approach speed: the
    /// fade duration capped by the travel distance (travel / speed). Port of the `ride`
    /// term of exitClock in cytoid-design packages/player/src/state.ts — with the Cylheim
    /// approach speed standing in for the design player's span/fadein, since that is the
    /// speed this engine actually renders. The result always fits inside the drop miss
    /// window (0.15s &lt; 0.2s), so a drop is fully faded before its MISS is judged.</summary>
    public static double ExitRideSeconds(Note note)
    {
        var page = note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;
        var speed = ApproachSpeedScreensPerSecond(note, durationTick);
        return speed > 0 && double.IsFinite(speed)
            ? System.Math.Min(SkinTimingTokens.Note.ExitDrop.Duration,
                SkinTimingTokens.Note.ExitDropTravel / speed)
            : SkinTimingTokens.Note.ExitDrop.Duration;
    }

    /// <summary>Exit-clock compression for the ride: multiplying real seconds past the
    /// beat by this converts them onto the exit slot's clock (ExitTime). 1 while the
    /// travel cap is not binding (slow falls ride the full fade duration); &gt; 1 when a
    /// fast fall compresses the fade so it completes exactly at the travel cap.</summary>
    public static double ExitClockScale(Note note)
    {
        var ride = ExitRideSeconds(note);
        return ride > 1e-9 ? SkinTimingTokens.Note.ExitDrop.Duration / ride : 1;
    }

    /// <summary>Classic behavior — the offset stops at the line (timeDiff &lt;= 0 → zero):
    /// the capsule freezes on the scanline until it is judged.</summary>
    public static Vector3 ComputeLocalOffset(Note note) => ComputeOffset(note, ridePastLine: false);

    /// <summary>Flat-skin behavior — the exit ride (cytoid-design drop exit): an unjudged
    /// drop keeps its approach speed past the line and fades on the same clock, capped at
    /// <see cref="SkinTimingTokens.Note.ExitDropTravel"/> past it. When the cap binds, the
    /// exit clock compresses (<see cref="ExitClockScale"/>) so the fade completes exactly
    /// at the cap — the position clamps there too, so the note is never cut off mid-fade.
    /// Only the Flat renderers ride: classic sprites have no fade to ride with.</summary>
    public static Vector3 ComputeRideLocalOffset(Note note) => ComputeOffset(note, ridePastLine: true);

    private static Vector3 ComputeOffset(Note note, bool ridePastLine)
    {
        var page = note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;
        if (durationTick <= 0 || !double.IsFinite(durationTick)) return Vector3.zero;

        double timeDiffSeconds = note.Model.start_time - note.Game.Time;
        if (!double.IsFinite(timeDiffSeconds)) return Vector3.zero;

        if (ridePastLine)
        {
            // Ride past the line at approach speed, clamped at the travel distance.
            // A degenerate speed (malformed page) freezes at the line.
            var speed = ApproachSpeedScreensPerSecond(note, durationTick);
            if (speed <= 0 || !double.IsFinite(speed))
                timeDiffSeconds = System.Math.Max(timeDiffSeconds, 0);
            else
                timeDiffSeconds = System.Math.Max(timeDiffSeconds,
                    -SkinTimingTokens.Note.ExitDropTravel / speed);
        }
        else if (timeDiffSeconds <= 0)
        {
            return Vector3.zero;
        }

        // Cylheim's screen space is Y down (Up=+1, Down=-1); Unity world space is Y up,
        // so the ported offset is negated: Down falls from above (+Y), Up rises from
        // below (-Y). Extending timeDiff past zero continues the same straight line
        // through the landing point — the ride needs no extra direction math.
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
