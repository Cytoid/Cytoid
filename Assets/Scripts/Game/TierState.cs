using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TierState
{
    public Tier Tier { get; }

    public GameState[] Stages { get; set; }
    
    public double Completion { get; private set; }
    
    public double AverageAccuracy => Stages.Sum(it => it.Accuracy) / Stages.Length;
    
    public int Combo { get; private set; }
    
    public int MaxCombo { get; private set; }
    
    public Dictionary<NoteGrade, int> GradeCounts => new Dictionary<NoteGrade, int>
    {
        {NoteGrade.Perfect, Stages.Sum(state => state.Judgements.Count(it => it.Value.Grade == NoteGrade.Perfect))},
        {NoteGrade.Great, Stages.Sum(state => state.Judgements.Count(it => it.Value.Grade == NoteGrade.Great))},
        {NoteGrade.Good, Stages.Sum(state => state.Judgements.Count(it => it.Value.Grade == NoteGrade.Good))},
        {NoteGrade.Bad, Stages.Sum(state => state.Judgements.Count(it => it.Value.Grade == NoteGrade.Bad))},
        {NoteGrade.Miss, Stages.Sum(state => state.Judgements.Count(it => it.Value.Grade == NoteGrade.Miss))}
    };

    public int EarlyCount => Stages.Sum(it => it.EarlyCount);
    
    public int LateCount => Stages.Sum(it => it.LateCount);
    
    public double AverageTimingError => Stages.Sum(state => state.Judgements.Values.Sum(it => it.Error)) / Stages.Sum(it => it.Judgements.Count);

    public double StandardTimingError
    {
        get
        {
            var difference = 0.0;
            Stages.ForEach(state => state.Judgements.Values.ForEach(it => difference += Math.Pow(AverageTimingError - it.Error, 2)));
            return Math.Sqrt(difference / Stages.Sum(it => it.Judgements.Count));
        }
    }

    public TierState(Tier tier)
    {
        Tier = tier;
    }

    public void UpdateCompletion()
    {
        Completion = 100;
    }
}