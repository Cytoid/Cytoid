public class StageBadMissCountCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_BAD_MISS_COUNT".Get();

    public int StageIndex { get; }
    public int MaxCount { get; }

    public StageBadMissCountCriterion(int stageIndex, int maxCount)
    {
        StageIndex = stageIndex;
        MaxCount = maxCount;
    }

    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null) return CriterionState.Undetermined;
        var count = state.Stages[StageIndex].GradeCounts[NoteGrade.Bad] + state.Stages[StageIndex].GradeCounts[NoteGrade.Miss];
        if (count > MaxCount) return CriterionState.Failed;
        return state.Stages[StageIndex].IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}