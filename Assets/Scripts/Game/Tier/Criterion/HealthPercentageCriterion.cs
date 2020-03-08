public class HealthPercentageCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_HEALTH_PERCENTAGE".Get();

    public int StageIndex { get; }
    public float RequiredPercentage { get; }
    
    public HealthPercentageCriterion(int stageIndex, float requiredPercentage)
    {
        StageIndex = stageIndex;
        RequiredPercentage = requiredPercentage;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null || !state.Stages[StageIndex].IsCompleted) return CriterionState.Undetermined;
        return state.Stages[StageIndex].Health / state.Stages[StageIndex].MaxHealth >= RequiredPercentage ? CriterionState.Passed : CriterionState.Failed;
    }
}