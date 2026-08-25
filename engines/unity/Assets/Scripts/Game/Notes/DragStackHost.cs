using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One visual/collision host for co-located drag children (size ≥ 2).
/// Primary note owns Ring/Fill/Collider and Update listeners; followers are judgment-only
/// and sync transforms from the primary each tick.
/// </summary>
public class DragStackHost
{
    public int StackId { get; }
    public Note Primary { get; private set; }

    private readonly List<Note> members = new List<Note>();

    public IReadOnlyList<Note> Members => members;

    public DragStackHost(int stackId)
    {
        StackId = stackId;
    }

    public void Add(Note note)
    {
        if (note == null || members.Contains(note)) return;

        var insertAt = members.Count;
        for (var i = 0; i < members.Count; i++)
        {
            if (note.Model.id < members[i].Model.id)
            {
                insertAt = i;
                break;
            }
        }

        members.Insert(insertAt, note);
        note.DragStack = this;

        if (Primary == null)
        {
            SetPrimary(note, promote: false);
            return;
        }

        // Keep the lowest id as the stable host, independent of spawn order.
        if (note.Model.id < Primary.Model.id)
        {
            var previous = Primary;
            SetPrimary(note, promote: false);
            previous.BecomeDragStackFollower();
            return;
        }

        note.BecomeDragStackFollower();
    }

    public void OnMemberCollected(Note note)
    {
        if (note == null) return;
        members.Remove(note);
        if (note.DragStack == this) note.DragStack = null;

        if (Primary == note)
        {
            Primary = null;
            Note next = null;
            for (var i = 0; i < members.Count; i++)
            {
                var candidate = members[i];
                if (candidate == null || candidate.IsCollected || candidate.IsCleared) continue;
                next = candidate;
                break;
            }

            if (next != null) SetPrimary(next, promote: true);
        }
    }

    public bool IsPrimary(Note note) => Primary == note;

    public void TickFollowers()
    {
        if (Primary == null || members.Count == 0) return;

        var primaryPos = Primary.transform.localPosition;
        var primaryRot = Primary.transform.localEulerAngles;

        for (var i = 0; i < members.Count; i++)
        {
            var note = members[i];
            if (note == null || note == Primary || note.IsCollected) continue;

            note.transform.localPosition = primaryPos;
            note.transform.localEulerAngles = primaryRot;

            if (!note.IsCleared)
            {
                note.TickStackFollowerJudgment();
            }
        }
    }

    private void SetPrimary(Note note, bool promote)
    {
        Primary = note;
        if (promote) note.PromoteToDragStackPrimary();
        else note.IsDragStackFollower = false;
    }
}
