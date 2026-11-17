// Skin-only flat flick head: the diamond/chevron visuals draw from the compiled
// skin's slots in FlatNoteRendererBase (DrawSkinHead). This subclass keeps only
// the flick-specific clear-FX rotation binding.
public class FlatFlickNoteRenderer : FlatNoteRendererBase
{
    public FlatFlickNoteRenderer(FlickNote note) : base(note)
    {
    }

    protected FlickNote FlickNote => (FlickNote) Note;

    // FlatClearFxPool swaps the note's world rotation for these FX-frame degrees when
    // set (0 = right, 180 = left), so the streaks point along the actual swipe instead
    // of the note frame. The skin's burst graphics are 4-fold symmetric, so only the
    // streak chevrons carry the flip. 0 (no directional swipe, auto) keeps the note
    // rotation.
    protected override float? FlickClearRotationDegrees =>
        FlickNote.LastSwipeDirection == 0 ? null : FlickNote.LastSwipeDirection > 0 ? 0f : 180f;
}
