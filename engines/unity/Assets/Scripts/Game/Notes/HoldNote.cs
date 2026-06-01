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
    
    public bool IsHolding => HoldingFingers.Count > 0;

    protected override NoteRenderer CreateRenderer()
    {
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
        base.Collect();
    }

    protected override void OnGameUpdate(Game _)
    {
        base.OnGameUpdate(_);
        if (IsHolding)
        {
            if (Game.Time >= Model.start_time + JudgmentOffset)
            {
                HeldDuration = Game.Time - Mathf.Max(Model.start_time + JudgmentOffset, HoldingStartTime);
            }
            else
            {
                HeldDuration = 0;
            }
            HoldProgress = (Game.Time - (Model.start_time + JudgmentOffset)) / Model.Duration;
            
            if (!playedHitSoundAtBegin && HoldProgress >= 0 && Context.Player.Settings.HoldHitSoundTiming.Let(it => it == HoldHitSoundTiming.Begin || it == HoldHitSoundTiming.Both))
            {
                playedHitSoundAtBegin = true;
                PlayHitSound();
            }

            // Already completed?
            if (Game.Time >= Model.end_time + JudgmentOffset)
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
        return !IsHolding && base.ShouldMiss();
    }
    
    public override void OnTouch(Vector2 screenPos)
    {
        // Do nothing
    }

    public void UpdateFinger(int finger, bool isHolding)
    {
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

    public override NoteGrade CalculateGrade()
    {
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