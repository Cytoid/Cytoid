using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class TierState
{
    public Tier Tier { get; }

    public List<Criterion> Criteria => Tier.Meta.Criteria;

    public int CurrentStageIndex { get; set; } = -1;

    public GameState[] Stages { get; set; }

    public GameState CurrentStage => Stages[CurrentStageIndex];

    public bool IsCompleted => Stages.All(it => it != null && it.IsCompleted);
    
    public bool IsFailed { get; set; }
    
    [AvailableOnComplete]
    public double Completion { get; set; } /* 0, 1~2 */
    
    public double AverageAccuracy => Stages.Sum(it => it.Accuracy) / Stages.Length;
    
    public int Combo { get; set; }
    
    public int MaxCombo { get; set; }
    
    public double Health { get; set; }

    [AvailableOnComplete]
    public Dictionary<NoteGrade, int> GradeCounts => OnCompleteGuard(gradeCounts);

    [AvailableOnComplete]
    public int EarlyCount => OnCompleteGuard(Stages.Sum(it => it.EarlyCount));
    
    [AvailableOnComplete]
    public int LateCount => OnCompleteGuard(Stages.Sum(it => it.LateCount));
    
    [AvailableOnComplete]
    public double AverageTimingError => OnCompleteGuard(Stages.Sum(state => state.Judgements.Values.Sum(it => it.Error)) / Stages.Sum(it => it.Judgements.Count));

    [AvailableOnComplete]
    public double StandardTimingError
    {
        get
        {
            OnCompleteGuard();
            var difference = 0.0;
            Stages.ForEach(state => state.Judgements.Values.ForEach(it => difference += Math.Pow(AverageTimingError - it.Error, 2)));
            return Math.Sqrt(difference / Stages.Sum(it => it.Judgements.Count));
        }
    }

    private Dictionary<NoteGrade, int> gradeCounts = new Dictionary<NoteGrade, int>();

    public TierState(Tier tier)
    {
        Tier = tier;
        Stages = new GameState[tier.Meta.stages.Count];
    }

    public void OnComplete()
    {
        if (Criteria.TrueForAll(it => it.Judge(this) == CriterionState.Passed))
        {
            var threshold = Tier.Meta.thresholdAccuracy;
            var c = (AverageAccuracy - threshold) / (1 - threshold);
            if (c < 0) c = 0;
            if (c > 1) c = 1;
            Completion = c + 1;
        }
        else
        {
            Completion = 0;
        }

        foreach (NoteGrade grade in Enum.GetValues(typeof(NoteGrade)))
        {
            if (grade == NoteGrade.None) continue;
            gradeCounts[grade] = Stages.Sum(it => it.GradeCounts[grade]);
        }
    }

    public void OnStageComplete()
    {
        if (Criteria.Any(it => it.Judge(this) == CriterionState.Failed))
        {
            IsFailed = true;
        }
        
        // Refill health by 30% of lost health
        var maxHealth = Tier.Meta.maxHealth;
        Health += 0.3 * (maxHealth - Health);
        if (Health > maxHealth) Health = maxHealth;
        
        if (CurrentStageIndex == Stages.Length - 1)
        {
            OnComplete();
        }
    }
    
    private void OnCompleteGuard()
    {
        if (!IsCompleted) throw new InvalidOperationException();
    }
    
    private T OnCompleteGuard<T>(T target)
    {
        if (!IsCompleted) throw new InvalidOperationException();
        return target;
    }
}