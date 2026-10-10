using UnityEngine;

/// <summary>Value snapshot: remains valid after the note is returned to its pool.</summary>
public readonly struct DropDragGuard
{
    public readonly int NoteId;
    public readonly float RawStartTime;
    public readonly float EffectiveTime;
    public readonly int PageIndex;
    public readonly float PageDuration;
    public readonly Vector2 Landing;

    public DropDragGuard(int noteId, float rawStartTime, float effectiveTime,
        int pageIndex, float pageDuration, Vector2 landing)
    {
        NoteId = noteId;
        RawStartTime = rawStartTime;
        EffectiveTime = effectiveTime;
        PageIndex = pageIndex;
        PageDuration = pageDuration;
        Landing = landing;
    }
}
