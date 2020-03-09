public class StageComboCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_STAGE_COMBO".Get(RequiredCombo, StageName(StageIndex));

    public int StageIndex { get; }
    public double RequiredCombo { get; }

    public StageComboCriterion(int stageIndex, double requiredCombo)
    {
        StageIndex = stageIndex;
        RequiredCombo = requiredCombo;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null || !state.Stages[StageIndex].IsCompleted) return CriterionState.Undetermined;
        return state.Stages[StageIndex].MaxCombo >= RequiredCombo ? CriterionState.Passed : CriterionState.Failed;
    }
}