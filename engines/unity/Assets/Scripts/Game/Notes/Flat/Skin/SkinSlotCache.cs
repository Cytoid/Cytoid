// Per-renderer slot cache for the hold renderer family. PURE C#: no
// UnityEngine references.

/// <summary>CompiledSkin.Slot() interpolates a key string on every call, and the
/// hold path re-queries its slots every frame — press/loopUnder in the driver
/// check and again in the draw, progress and body in their own per-frame passes —
/// so the hold hot path allocated even after warm-up. This cache resolves the
/// slots once per compiled-skin instance behind a reference check, the same
/// contract as the enter-slot cache on FlatNoteRendererBase: skin swaps compile
/// a new instance, so the reference change re-resolves exactly when needed.</summary>
public sealed class SkinSlotCache
{
    private CompiledSkin skin;

    public CompiledSkinSlot Press, LoopUnder, Progress, Body;

    /// <summary>Call every frame with the runtime's current skin; a steady-state
    /// reference check allocates nothing. A null skin clears the slots (the
    /// callers' existing null gates then fall back).</summary>
    public void Resolve(CompiledSkin current, NoteKind kind)
    {
        if (ReferenceEquals(skin, current)) return;
        skin = current;
        Press = current?.Slot(kind, "press");
        LoopUnder = current?.Slot(kind, "loopUnder");
        Progress = current?.Slot(kind, "progress");
        Body = current?.Slot(kind, "body");
    }
}
