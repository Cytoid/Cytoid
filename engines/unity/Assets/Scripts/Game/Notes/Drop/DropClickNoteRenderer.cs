using UnityEngine;
using Object = UnityEngine.Object;

// Drop note renderer for DropClick. Inherits ClassicClickNoteRenderer (which is currently a
// pass-through to ClassicNoteRenderer) and adds:
//   1. A Core child SpriteRenderer (added to the prefab in T7) that sits above Fill, always white.
//   2. The drop "falling" Y offset ported faithfully from Cylheim, applied ONLY to visual
//      children (Ring / Fill / Core / NoteId). Note.transform itself is left at the landing
//      scanline point so the CircleCollider2D — and therefore hit detection — stays anchored
//      where the player should tap, regardless of the falling animation.
// Drop notes do NOT use the approach scale/fill animations — they are full-size from intro_time
// and reach the scanline via the Y offset alone, so UpdateTransformScale / UpdateFillScale are
// overridden to set fixed values.
public class DropClickNoteRenderer : ClassicClickNoteRenderer
{
    protected SpriteRenderer Core;

    // Base localPosition of each visual child, snapshotted ONCE in the constructor when the
    // Note GameObject is freshly created from its prefab. Capturing the pristine prefab
    // positions is pool-reuse safe: a Drop note that is cleared mid-fall leaves residual
    // offsets on its children, and snapshotting at every OnNoteLoaded would bake those in.
    // Each frame we write localPosition = baseLocal + localOffset so the offset never
    // compounds across frames.
    private Vector3 ringBaseLocal;
    private Vector3 fillBaseLocal;
    private Vector3 coreBaseLocal;
    private Vector3 noteIdBaseLocal;
    private bool hasCore;
    private bool hasNoteId;

    public DropClickNoteRenderer(ClickNote clickNote) : base(clickNote)
    {
        // The Core child is added to the prefab in T7; null-check until then.
        var coreTransform = Note.transform.Find("NoteCore");
        if (coreTransform != null) Core = coreTransform.GetComponent<SpriteRenderer>();

        // Snapshot base localPosition once. base() has already instantiated Ring, Fill, and
        // (when DisplayNoteId) NoteId, so all visual children are available here.
        ringBaseLocal = Ring.transform.localPosition;
        fillBaseLocal = Fill.transform.localPosition;
        hasCore = Core != null;
        if (hasCore) coreBaseLocal = Core.transform.localPosition;
        hasNoteId = DisplayNoteId && NoteId != null;
        if (hasNoteId) noteIdBaseLocal = NoteId.transform.localPosition;
    }

    public override void OnNoteLoaded()
    {
        base.OnNoteLoaded();
        // Drop note Ring sprites are solid bars (unlike Classic note rings which are outlines).
        // Render Fill on top of Ring so the colored Fill is visible.
        Fill.sortingOrder = Ring.sortingOrder + 1;
        if (Core != null) Core.sortingOrder = Fill.sortingOrder + 1;
    }

    public override void OnCollect()
    {
        base.OnCollect();
        // Restore prefab-local positions so a Note GameObject pulled from the pool starts the
        // next cycle from a clean visual state. ApplyDropOffset would correct this on the
        // first frame anyway, but this keeps the lifecycle explicit and prevents any brief
        // pre-first-frame visibility of stale offsets.
        Ring.transform.localPosition = ringBaseLocal;
        Fill.transform.localPosition = fillBaseLocal;
        if (hasCore) Core.transform.localPosition = coreBaseLocal;
        if (hasNoteId) NoteId.transform.localPosition = noteIdBaseLocal;
    }

    protected override void Render()
    {
        base.Render();
        ApplyDropOffset();
    }

    // Faithful port of Cylheim pixi-playback-note-layer.ts:524-540 getPlaybackDropSpriteOffsetY,
    // then applied only to visual children so the collider stays at the landing scanline point.
    // Verified: durationTick=500, timeDiff=0.1s, height=540 -> 800px; height=1080 -> 1600px.
    private void ApplyDropOffset()
    {
        var page = Note.Page;
        double durationTick = (page.end_tick - page.start_tick) * 5.0;

        Vector3 localOffset = Vector3.zero;
        // Cylheim guard: skip when chart is malformed. timeDiff<=0 means "at or past landing";
        // localOffset stays zero and children snap back to base localPosition.
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
                // Translate to Unity world units via visible camera height (2 * orthographicSize):
                float screenHeightWorld = 2f * Note.Game.camera.orthographicSize;
                float offsetYWorld = dirSign * (8_000_000f / (float) durationTick) * timeDiffSeconds
                                     / 1080f * screenHeightWorld;
                // Translate the world-space vertical offset into Note's local space so the
                // visual amplitude is correct under any combination of rotation AND scale.
                // InverseTransformVector accounts for both. Guard against singular (zero) or
                // non-finite scale to avoid NaN from the matrix inversion — a zero-scale note
                // is invisible anyway, so the offset is irrelevant.
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
        if (hasCore) ApplyChildOffset(Core.transform, coreBaseLocal, localOffset);
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

    protected override void UpdateComponentStates()
    {
        base.UpdateComponentStates();
        if (Core == null) return;
        if (!Note.IsCleared && Game.Time >= Note.Model.intro_time &&
            Game.Time <= Note.Model.end_time + Note.MissThreshold)
        {
            Core.enabled = !Game.State.Mods.Contains(Mod.HideNotes);
        }
        else
        {
            Core.enabled = false;
        }
    }

    protected override void UpdateComponentOpacity()
    {
        base.UpdateComponentOpacity();
        // Core is always pure white — only alpha is modulated, no tint applied.
        if (Core != null) Core.color = Color.white.WithAlpha(EasedOpacity);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Core != null) Object.Destroy(Core);
    }
}
