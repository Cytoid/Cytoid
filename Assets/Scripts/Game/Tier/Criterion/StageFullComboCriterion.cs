public class StageFullComboCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_STAGE_FULL_COMBO".Get(StageName(StageIndex));

    public int StageIndex { get; }

    public StageFullComboCriterion(int stageIndex)
    {
        StageIndex = stageIndex;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null) return CriterionState.Undetermined;
        var count = state.Stages[StageIndex].GradeCounts[NoteGrade.Bad] + state.Stages[StageIndex].GradeCounts[NoteGrade.Miss];
        if (count > 0) return CriterionState.Failed;
        return state.Stages[StageIndex].IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}