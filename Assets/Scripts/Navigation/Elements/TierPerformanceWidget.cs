using System;
using UnityEngine;
using UnityEngine.UI;

public class TierPerformanceWidget : ContainerEntry<TierRankingEntry>
{
    public Text completion;
    public GradientMeshEffect completionGradient;
    public Text accuracy;

    public TierRankingEntry Model { get; protected set; }
    
    public override void SetModel(TierRankingEntry entry)
    {
        Model = entry;
        completion.text =  $"{(Mathf.FloorToInt((float) (entry.completion * 100 * 100)) / 100f):0.00}%";
        completionGradient.SetGradient(ScoreGrades.FromTierCompletion(entry.completion).GetGradient());
        accuracy.text = (Math.Floor(entry.averageAccuracy * 100 * 100) / 100).ToString("0.00") + "%";
        LayoutFixer.Fix(transform as RectTransform);
    }

    public override TierRankingEntry GetModel() => Model;

}