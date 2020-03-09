public class BadMissCountCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_BAD_MISS_COUNT".Get(MaxCount);

    public int MaxCount { get; }

    public BadMissCountCriterion(int maxCount)
    {
        MaxCount = maxCount;
    }

    public override CriterionState Judge(TierState state)
    {
        var count = state.GradeCounts[NoteGrade.Bad] + state.GradeCounts[NoteGrade.Miss];
        if (count > MaxCount) return CriterionState.Failed;
        return state.IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}