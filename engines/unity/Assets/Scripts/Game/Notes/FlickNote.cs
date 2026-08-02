using System;
using UnityEngine;

public enum FlickFingerResult
{
    /// <summary>Keep the finger→Flick binding.</summary>
    Pending,
    /// <summary>Note cleared; caller must unbind.</summary>
    Cleared,
    /// <summary>Finger released/canceled; caller must unbind regardless of clear.</summary>
    Released,
}

public class FlickNote : Note
{
    public bool IsFlicking { get; set; }
    public float FlickingStartTime { get; set; }
    public Vector2 FlickingStartPosition { get; set; }

    private float age;

    protected override NoteRenderer CreateRenderer()
    {
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicFlickNoteRenderer(this)
            : throw new NotSupportedException();
    }

    public override bool OnTouch(Vector2 screenPos)
    {
        // This method should never be invoked!
        throw new InvalidOperationException();
    }

    public void StartFlicking(Vector2 screenPos)
    {
        if (!Game.State.IsPlaying || IsFlicking) return;
        IsFlicking = true;
        FlickingStartTime = Game.Time;
        FlickingStartPosition = screenPos;
    }

    /// <summary>
    /// Clears flicking session state so another FingerDown can reserve this note.
    /// Does not clear the note judgment.
    /// </summary>
    public void StopFlicking()
    {
        IsFlicking = default;
        FlickingStartTime = default;
        FlickingStartPosition = default;
    }

    /// <summary>
    /// Movement while reserved. Threshold crossed + failed <see cref="Note.TryClear"/> stays
    /// <see cref="FlickFingerResult.Pending"/> and resets the swipe origin.
    /// </summary>
    public FlickFingerResult UpdateFingerPosition(Vector2 screenPos)
    {
        if (!Game.State.IsPlaying) return FlickFingerResult.Pending;
        if (IsCleared) return FlickFingerResult.Cleared;

        var swipeVector = screenPos - FlickingStartPosition;
        // TODO: Consider rotation
        if (Math.Abs(swipeVector.x) < Game.camera.orthographicSize * 0.01f)
            return FlickFingerResult.Pending;

        if (TryClear())
            return FlickFingerResult.Cleared;

        // Failed early/out-of-window attempt: keep binding; require a new threshold cross.
        FlickingStartPosition = screenPos;
        FlickingStartTime = Game.Time;
        return FlickFingerResult.Pending;
    }

    /// <summary>
    /// FingerUp / cancel: attempt clear only if release displacement meets the Flick
    /// threshold, then always release ownership (no stationary Up clear).
    /// </summary>
    public FlickFingerResult ReleaseFinger(Vector2 screenPos)
    {
        if (!IsCleared && Game.State.IsPlaying)
        {
            var swipeVector = screenPos - FlickingStartPosition;
            // TODO: Consider rotation
            if (Math.Abs(swipeVector.x) >= Game.camera.orthographicSize * 0.01f)
                TryClear();
        }

        if (!IsCleared)
            StopFlicking();

        return FlickFingerResult.Released;
    }

    public override void Collect()
    {
        if (IsCollected) return;

        StopFlicking();
        age = default;
        base.Collect();
    }

    public override NoteGrade CalculateGrade()
    {
        if (ShouldMiss()) return NoteGrade.Miss;

        var grade = NoteGrade.None;
        var timeUntil = TimeUntilStart + JudgmentOffset;

        if (Game.State.Mode == GameMode.Practice)
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.800f) grade = NoteGrade.Great;
                if (timeUntil <= 0.200f) grade = NoteGrade.Perfect;
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.300f) grade = NoteGrade.Great;
                if (timePassed <= 0.100f) grade = NoteGrade.Perfect;
            }
        }
        else
        {
            if (timeUntil >= 0)
            {
                if (timeUntil < 0.150f) grade = NoteGrade.Great; // 0.400
                if (timeUntil <= 0.060f) grade = NoteGrade.Perfect; // 0.120
            }
            else
            {
                var timePassed = -timeUntil;
                if (timePassed < 0.150f) grade = NoteGrade.Great;
                if (timePassed <= 0.060f) grade = NoteGrade.Perfect;
            }
        }
        return grade;
    }

    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoFlick);
    }

}
