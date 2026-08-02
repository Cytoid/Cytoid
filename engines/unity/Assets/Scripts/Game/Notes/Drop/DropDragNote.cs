using UnityEngine;

// SYNC-WARNING: DropDrag judgment is a verbatim copy of DragHeadNote's CanHandleTouch, CalculateGrade,
// IsAutoEnabled, and PlayHitSound (DragHeadNote.cs:165-252). If you change judgment in either
// file, update the other. DragHead chain logic (OnGameLateUpdate, Collect, FromNoteModel/ToNoteModel)
// is INTENTIONALLY NOT inherited — DropDrag is standalone per design decision.
// NOTE: DragHead's cross-page check is redundant for drop notes (5×pageDuration >> Page.Duration/2
// always, so the 0.31s check dominates) — safe to copy verbatim, do not remove.
public class DropDragNote : Note
{
    protected override NoteRenderer CreateRenderer()
    {
        return new DropDragNoteRenderer(this);
    }

    public override bool CanHandleTouch(Vector2 screenPos)
    {
        if (!base.CanHandleTouch(screenPos)) return false;
        // Do not handle touch event if touched too ahead of scanner
        if (Model.start_time - Game.Time > 0.31f) return false;
        // Do not handle touch event if in a later page, unless the timing is close (half a screen) TODO: Fix inaccurate algorithm
        if (Model.page_index > Game.Chart.CurrentPageId &&
            Model.start_time - Game.Time > Page.Duration / 2f) return false;
        return true;
    }

    public override NoteGrade CalculateGrade()
    {
        var grade = NoteGrade.Miss;
        var timeUntilStart = TimeUntilStart + JudgmentOffset;
        if (timeUntilStart >= 0)
        {
            grade = NoteGrade.None;
            if (timeUntilStart < 0.500f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        else
        {
            var timePassed = -timeUntilStart;
            if (timePassed < 0.200f)
            {
                grade = NoteGrade.Perfect;
            }
        }
        return grade;
    }

    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoDrag);
    }

    public override void PlayHitSound()
    {
        if (Context.AudioManager.IsSfxLoaded("HitSound"))
        {
            Context.AudioManager.GetSfx("HitSound").Play();
        }
        Context.Haptic(HapticTypes.Selection, false);
    }
}
