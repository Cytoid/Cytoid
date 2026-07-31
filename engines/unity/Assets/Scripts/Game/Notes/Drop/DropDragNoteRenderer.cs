using UnityEngine;

// Drop note renderer for DropDrag. Inherits ClassicNoteRenderer directly (DropDrag does not
// extend ClickNote, and DropDrag has no Core layer). Behaves like DropClickNoteRenderer minus
// the Core handling: drop notes ignore approach scale/fill animations and reach the scanline
// via the Y offset alone.
public class DropDragNoteRenderer : ClassicNoteRenderer
{
    public DropDragNoteRenderer(Note note) : base(note)
    {
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        // Drop note Ring sprites are solid bars (unlike Classic note rings which are outlines).
        // Render Fill on top of Ring so the colored Fill is visible.
        Fill.sortingOrder = Ring.sortingOrder + 1;
    }

    protected override void Render()
    {
        base.Render();
        ApplyDropOffset();
    }

    // Faithful port of Cylheim pixi-playback-note-layer.ts:524-540 getPlaybackDropSpriteOffsetY.
    // Verified: durationTick=500, timeDiff=0.1s, height=540 -> 800px; height=1080 -> 1600px.
    private void ApplyDropOffset()
    {
        var page = Note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;
        if (durationTick <= 0 || !double.IsFinite(durationTick)) return; // Cylheim guard

        // Cylheim uses screen coords (Y down): Up=+1, Down=-1.
        // Unity uses world coords (Y up): flip the sign so Down falls from above (+Y), Up rises from below (-Y).
        float dirSign = Note.Model.NoteDirection == 1 ? -1f : 1f; // Up -> -1, Down -> +1
        float timeDiffSeconds = (float) (Note.Model.start_time - Note.Game.Time);

        // At or past landing: no offset, note rests at scanline.
        if (timeDiffSeconds <= 0f) return;

        // Cylheim formula in screen-heights (resolution-independent):
        //   offsetY_screen_heights = dir * (8_000_000 / durationTick) * timeDiff / 1080
        // Translate to Unity world units via visible camera height (2 * orthographicSize):
        float screenHeightWorld = 2f * Note.Game.camera.orthographicSize;
        float offsetYWorld = dirSign * (8_000_000f / (float) durationTick) * timeDiffSeconds / 1080f *
                             screenHeightWorld;

        var pos = Note.transform.localPosition;
        pos.y += offsetYWorld;
        Note.transform.localPosition = pos;
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
