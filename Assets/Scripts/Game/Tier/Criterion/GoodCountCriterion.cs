public class GoodCountCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_GOOD_COUNT".Get();

    public int MaxCount { get; }

    public GoodCountCriterion(int maxCount)
    {
        MaxCount = maxCount;
    }

    public override CriterionState Judge(TierState state)
    {
        var count = state.GradeCounts[NoteGrade.Good];
        if (count > MaxCount) return CriterionState.Failed;
        return state.IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}