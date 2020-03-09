public class StageGreatCountCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_GREAT_COUNT".Get(MaxCount, StageName(StageIndex));

    public int StageIndex { get; }
    public int MaxCount { get; }

    public StageGreatCountCriterion(int stageIndex, int maxCount)
    {
        StageIndex = stageIndex;
        MaxCount = maxCount;
    }

    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null) return CriterionState.Undetermined;
        var count = state.Stages[StageIndex].GradeCounts[NoteGrade.Great];
        if (count > MaxCount) return CriterionState.Failed;
        return state.Stages[StageIndex].IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}