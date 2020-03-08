public class StageScoreCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_STAGE_SCORE".Get();

    public int StageIndex { get; }
    public double RequiredScore { get; }

    public StageScoreCriterion(int stageIndex, double requiredScore)
    {
        StageIndex = stageIndex;
        RequiredScore = requiredScore;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null || !state.Stages[StageIndex].IsCompleted) return CriterionState.Undetermined;
        return state.Stages[StageIndex].Score >= RequiredScore ? CriterionState.Passed : CriterionState.Failed;
    }
}