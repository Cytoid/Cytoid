using UnityEngine;

// Drop sprites fall toward fixed landing colliders. Protect that landing while a
// Down is temporally closer to the DropDrag; let Select win after the midpoint.
// The same rule applies to fresh, armed and successfully cleared DropDrags.
public static class DropNoteIsolation
{
    public const float LateWindowSeconds = 0.200f;

    public static DropDragGuard Snapshot(Note note) =>
        new DropDragGuard(note.Model.id, note.Model.start_time,
            note.Model.start_time + note.JudgmentOffset, note.Model.page_index,
            note.Page.Duration, note.transform.position);

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
    /// Co-hits within 30ms stay eligible. A later Select is protected only while
    /// the Down is no later than the midpoint and lies near the DropDrag landing.
    /// Raw admission limits match DropDragNote; expiry uses its 200ms Perfect late
    /// window, not the note's 300ms automatic Miss threshold.
    /// </summary>
    public static bool BlocksSelect(in DropDragGuard guard, float selectTime,
        float selectRadius, Vector2 tap, float now, int currentPageId)
    {
        if (!float.IsFinite(guard.RawStartTime) || !float.IsFinite(guard.EffectiveTime) ||
            !float.IsFinite(guard.PageDuration) || !float.IsFinite(selectTime) ||
            !float.IsFinite(now) || !float.IsFinite(selectRadius) || selectRadius <= 0f ||
            !float.IsFinite(tap.x) || !float.IsFinite(tap.y) ||
            !float.IsFinite(guard.Landing.x) || !float.IsFinite(guard.Landing.y)) return false;
        if (selectTime <= guard.EffectiveTime + InputController.DragCoHitWindowSeconds) return false;
        if (guard.RawStartTime - now > 0.310f) return false;
        if (guard.PageIndex > currentPageId &&
            guard.RawStartTime - now > guard.PageDuration * 0.5f) return false;
        if (now >= guard.EffectiveTime + LateWindowSeconds) return false;
        var midpoint = guard.EffectiveTime + (selectTime - guard.EffectiveTime) * 0.5f;
        if (now > midpoint) return false;
        return (tap - guard.Landing).sqrMagnitude <= selectRadius * selectRadius;
    }
}
