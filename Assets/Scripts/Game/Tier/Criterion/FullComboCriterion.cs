public class FullComboCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_FULL_COMBO".Get();

    public override CriterionState Judge(TierState state)
    {
        var count = state.GradeCounts[NoteGrade.Bad] + state.GradeCounts[NoteGrade.Miss];
        if (count > 0) return CriterionState.Failed;
        return state.IsCompleted ? CriterionState.Passed : CriterionState.Undetermined;
    }
}