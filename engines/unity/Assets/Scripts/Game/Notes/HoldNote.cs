using System;
using System.Collections.Generic;
using UnityEngine;

public class HoldNote : Note
{
    public float HoldingStartTime { get; protected set; } = float.MaxValue;
    public float HeldDuration  { get; protected set; }
    public float HoldProgress { get; protected set; }
    public List<int> HoldingFingers { get; } = new List<int>(2);

    private bool playedHitSoundAtBegin;
    private long cy1BoundAt;
    private long? cy1PausedAt;

    private long Cy1Close => Cy1Judgment.HoldClose(Cy1Start, Cy1End);
    internal bool Cy1ColliderActiveForHold =>
        Cy1Now < (IsHolding || cy1PausedAt.HasValue || IsAutoEnabled() ? Cy1End : Cy1Close);

    internal bool CanBindFinger =>
        !UsesCy1Judgment || (!IsAutoEnabled() && !cy1PausedAt.HasValue &&
            (IsHolding ? Cy1Now < Cy1End : Cy1Judgment.CanStartHold(Cy1Now, Cy1Start, Cy1End)));
    
    public bool IsHolding => HoldingFingers.Count > 0;

    protected override NoteRenderer CreateRenderer()
    {
        if (Game.Config.UseFlatNoteDesign) return new FlatHoldNoteRenderer(this);
        return Game.Config.UseClassicStyle
            ? (NoteRenderer) new ClassicHoldNoteRenderer(this)
            : throw new NotSupportedException();
    }

    public override void Collect()
    {
        if (IsCollected) return;
        
        HoldingStartTime = float.MaxValue;
        HeldDuration = default;
        HoldProgress = default;
        HoldingFingers.Clear();
        playedHitSoundAtBegin = false;
        cy1BoundAt = default;
        cy1PausedAt = null;
        base.Collect();
    }

    protected override void OnGameUpdate(Game _)
    {
        base.OnGameUpdate(_);
        if (IsHolding)
        {
            if (UsesCy1Judgment)
            {
                HeldDuration = Cy1HeldTime(Cy1Now) / 1_000_000f;
            }
            else if (Game.Time >= Model.start_time + JudgmentOffset)
            {
                HeldDuration = Game.Time - Mathf.Max(Model.start_time + JudgmentOffset, HoldingStartTime);
            }
            else
            {
                HeldDuration = 0;
            }
            HoldProgress = Model.Duration > 1e-4f
                ? (Game.Time - (Model.start_time + JudgmentOffset)) / Model.Duration
                : (Game.Time >= Model.start_time + JudgmentOffset ? 1f : 0f);
            
            if (!playedHitSoundAtBegin && HoldProgress >= 0 && Context.Player.Settings.HoldHitSoundTiming.Let(it => it == HoldHitSoundTiming.Begin || it == HoldHitSoundTiming.Both))
            {
                playedHitSoundAtBegin = true;
                PlayHitSound();
            }

            // Already completed?
            if (UsesCy1Judgment)
            {
                if (Cy1Now >= Cy1End) FinishCy1Hold(Cy1End);
            }
            else if (Game.Time >= Model.end_time + JudgmentOffset)
            {
                HoldingFingers.Clear();
                if (Game.Time > Model.start_time + JudgmentOffset && Game.State.IsPlaying)
                {
                    Clear(IsAutoEnabled() ? NoteGrade.Perfect : CalculateGrade());
                }
            }
        }
        else
        {
            HoldProgress = 0;
        }
    }

    public override bool ShouldMiss()
    {
        if (UsesCy1Judgment)
            return !IsHolding && !cy1PausedAt.HasValue && Cy1Now >= Cy1Close;
        return !IsHolding && base.ShouldMiss();
    }
    
    public override bool OnTouch(Vector2 screenPos)
    {
        // Hold start is owned by InputController (FingerDown / Update → UpdateFinger).
        // false keeps TryClear off the Down path; binding consumes the event there.
        return false;
    }

    public void UpdateFinger(int finger, bool isHolding)
    {
        if (UsesCy1Judgment)
        {
            if (!Game.State.IsPlaying || IsCleared) return;
            if (isHolding)
            {
                if (HoldingFingers.Contains(finger)) return;
                if (!IsHolding)
                {
                    HoldingStartTime = Game.Time;
                    cy1BoundAt = Cy1Now;
                }
                HoldingFingers.Add(finger);
            }
            else if (HoldingFingers.Remove(finger) && !IsHolding)
            {
                ReleaseCy1Hold(Cy1Now);
            }
            return;
        }

        var previouslyHolding = IsHolding;
        
        if (isHolding)
        {
            HoldingFingers.Add(finger);
            if (!previouslyHolding)
            {
                HoldingStartTime = Game.Time;
            }
        }
        else
        {
            HoldingFingers.Remove(finger);
        }

        if (HoldingFingers.Count == 0 && Game.Time > Model.start_time + JudgmentOffset)
        {
            if (Game.Time > Model.start_time + JudgmentOffset && Game.State.IsPlaying)
            {
                Clear(IsAutoEnabled() ? NoteGrade.Perfect : CalculateGrade());
            }
        }
    }

    internal void SuspendCy1Hold()
    {
        if (UsesCy1Judgment && IsHolding) cy1PausedAt = Cy1Now;
    }

    internal void ResumeCy1Hold()
    {
        if (!cy1PausedAt.HasValue) return;
        var pausedAt = cy1PausedAt.Value;
        cy1PausedAt = null;
        if (!IsHolding) ReleaseCy1Hold(pausedAt);
    }

    private void ReleaseCy1Hold(long time)
    {
        if (time >= Cy1Start)
        {
            FinishCy1Hold(time);
        }
        else
        {
            HoldingStartTime = float.MaxValue;
            HeldDuration = 0;
        }
    }

    private long Cy1HeldTime(long time) => Cy1Judgment.HeldTime(Cy1Start, Cy1End, cy1BoundAt, time);

    private void FinishCy1Hold(long time)
    {
        var held = Cy1HeldTime(time);
        HeldDuration = held / 1_000_000f;
        HoldingFingers.Clear();
        Clear(IsAutoEnabled() ? NoteGrade.Perfect : Cy1Judgment.Hold(Cy1End - Cy1Start, held));
    }

    public override NoteGrade CalculateGrade()
    {
        if (UsesCy1Judgment) return Cy1Judgment.Hold(Cy1End - Cy1Start, Cy1HeldTime(Cy1Now));
        var grade = NoteGrade.Miss;
        var rankedGrade = NoteGrade.Miss;
        // print($"HeldDuration: {HeldDuration}, ModelDuration: {Model.Duration}, HoldingStartTime: {HoldingStartTime}, ModelStartTime: {Model.start_time}");
        if (HeldDuration > Model.Duration - 0.05f) grade = NoteGrade.Perfect;
        else if (HeldDuration > Model.Duration * 0.7f) grade = NoteGrade.Great;
        else if (HeldDuration > Model.Duration * 0.5f) grade = NoteGrade.Good;
        else if (HeldDuration > Model.Duration * 0.3f) grade = NoteGrade.Bad;

        if (Game.State.Mode != GameMode.Practice)
        {
            if (HoldingStartTime != float.MaxValue && Mathf.Max(HoldingStartTime, Model.start_time + JudgmentOffset) > Model.start_time + JudgmentOffset)
            {
                var lateBy = HoldingStartTime - (Model.start_time + JudgmentOffset);
                if (lateBy < 0.200f) rankedGrade = NoteGrade.Bad;
                if (lateBy < 0.150f) rankedGrade = NoteGrade.Good;
                if (lateBy < 0.070f) rankedGrade = NoteGrade.Great;
                if (lateBy <= 0.040f) rankedGrade = NoteGrade.Perfect;
                if (rankedGrade == NoteGrade.Great) GreatGradeWeight = 1.0f - (lateBy - 0.040f) / (0.070f - 0.040f);
            }
            else
            {
                rankedGrade = grade;
                if (rankedGrade == NoteGrade.Great) GreatGradeWeight = 1.0f - (HeldDuration - Model.Duration * 0.70f) /
                                       (Model.Duration - 0.050f - Model.Duration * 0.70f);
            }
        }

        if (Game.State.Mode != GameMode.Practice && rankedGrade < grade)
            return rankedGrade; // Return the "worse" ranking (Note miss < bad < good < great < perfect)
        return grade;
    }

    public override bool IsAutoEnabled()
    {
        return base.IsAutoEnabled() || Game.State.Mods.Contains(Mod.AutoHold);
    }
}