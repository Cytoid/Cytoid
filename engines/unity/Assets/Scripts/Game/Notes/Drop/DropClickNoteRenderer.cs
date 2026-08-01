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

    // Drives the falling animation by offsetting only the visual children — see
    // DropNoteOffset.ComputeLocalOffset for the math and rationale.
    private void ApplyDropOffset()
    {
        Vector3 localOffset = DropNoteOffset.ComputeLocalOffset(Note);
        DropNoteOffset.ApplyToChild(Ring.transform, ringBaseLocal, localOffset);
        DropNoteOffset.ApplyToChild(Fill.transform, fillBaseLocal, localOffset);
        if (hasCore) DropNoteOffset.ApplyToChild(Core.transform, coreBaseLocal, localOffset);
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
