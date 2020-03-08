public class GreatCountCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_GREAT_COUNT".Get();

    public int MaxCount { get; }

    public GreatCountCriterion(int maxCount)
    {
        MaxCount = maxCount;
    }

    public override CriterionState Judge(TierState state)
    {
        var count = state.GradeCounts[NoteGrade.Great];
        if (count > MaxCount) return CriterionState.Failed;
        return state.IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}