using System.Linq;

public class TotalScoreCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_TOTAL_SCORE".Get();

    public double RequiredTotalScore { get; }

    public TotalScoreCriterion(double requiredTotalScore)
    {
        RequiredTotalScore = requiredTotalScore;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (!state.IsCompleted) return CriterionState.Undetermined;
        return state.Stages.Sum(it => it.Score) >= RequiredTotalScore ? CriterionState.Passed : CriterionState.Failed;
    }
}