public class StageGoodCountCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_GOOD_COUNT".Get();

    public int StageIndex { get; }
    public int MaxCount { get; }

    public StageGoodCountCriterion(int stageIndex, int maxCount)
    {
        StageIndex = stageIndex;
        MaxCount = maxCount;
    }

    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null) return CriterionState.Undetermined;
        var count = state.Stages[StageIndex].GradeCounts[NoteGrade.Good];
        if (count > MaxCount) return CriterionState.Failed;
        return state.Stages[StageIndex].IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}