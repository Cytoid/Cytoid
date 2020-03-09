using UnityEngine;

public class AverageAccuracyCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_AVG_ACCURACY".Get((Mathf.Floor((float) (RequiredAverageAccuracy * 100 * 100)) / 100).ToString("0.00"));

    public double RequiredAverageAccuracy { get; }

    public AverageAccuracyCriterion(double requiredAverageAccuracy)
    {
        RequiredAverageAccuracy = requiredAverageAccuracy;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (!state.IsCompleted) return CriterionState.Undetermined;
        return state.AverageAccuracy >= RequiredAverageAccuracy ? CriterionState.Passed : CriterionState.Failed;
    }
}