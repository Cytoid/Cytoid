using UnityEngine;

// DropDrag uses the same 30ms subsequent-note gate as DragHead/Child (DragCoHit).
// Landing colliders sit on the scanline while sprites fall in, so a later Select can
// overlap this tap even when DropDrag's own smaller hitbox missed. The spatial test
// uses the later note's radius around DropDrag's landing; the time test is
// InputController.DragCoHitWindowSeconds.
internal static class DropNoteIsolation
{
    public static float WorldHitboxRadius(Note note)
    {
        if (note == null) return 0f;
        var collider = note.Renderer != null ? note.Renderer.GetCollider() : null;
        if (collider == null) return 0f;
        var scale = Mathf.Abs(note.transform.lossyScale.x);
        if (scale <= 0f || !float.IsFinite(scale)) return 0f;
        var radius = collider.radius * scale;
        return float.IsFinite(radius) ? radius : 0f;
    }

    /// <summary>
    /// True when <paramref name="later"/> is more than
    /// <see cref="InputController.DragCoHitWindowSeconds"/> after this DropDrag and the
    /// tap still sits on the DropDrag landing (later note's radius).
    /// </summary>
    public static bool OccludesSelect(Note dropDrag, Note later, Vector2 tap)
    {
        if (dropDrag == null || later == null) return false;
        if (dropDrag.Type != NoteType.DropDrag) return false;

        var gap = (later.Model.start_time + later.JudgmentOffset)
                  - (dropDrag.Model.start_time + dropDrag.JudgmentOffset);
        if (gap <= InputController.DragCoHitWindowSeconds) return false;

        var laterRadius = WorldHitboxRadius(later);
        if (laterRadius <= 0f || !float.IsFinite(laterRadius)) return false;

        Vector2 landing = dropDrag.transform.position;
        return (tap - landing).sqrMagnitude <= laterRadius * laterRadius;
    }
}
