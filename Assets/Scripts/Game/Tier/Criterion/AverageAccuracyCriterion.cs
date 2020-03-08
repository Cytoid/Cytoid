public class AverageAccuracyCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_AVG_ACCURACY".Get();

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