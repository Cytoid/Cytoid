using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TierState
{
    public Tier Tier { get; }

    public GameState[] Stages { get; set; }

    public bool IsCompleted => Stages.All(it => it != null && it.IsCompleted);
    
    public bool IsFailed { get; set; }
    
    [AvailableOnComplete]
    public double Completion { get; private set; }
    
    public double AverageAccuracy => Stages.Sum(it => it.Accuracy) / Stages.Length;
    
    public int Combo { get; private set; }
    
    public int MaxCombo { get; private set; }

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
        Stages = new GameState[3];
    }

    public void OnComplete()
    {
        Completion = 100;

        foreach (NoteGrade grade in Enum.GetValues(typeof(NoteGrade)))
        {
            gradeCounts[grade] = Stages.Sum(it => it.GradeCounts[grade]);
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