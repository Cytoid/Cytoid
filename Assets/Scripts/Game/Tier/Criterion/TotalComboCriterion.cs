using System.Linq;

public class TotalComboCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_TOTAL_COMBO".Get();

    public double RequiredTotalCombo { get; }

    public TotalComboCriterion(double requiredTotalCombo)
    {
        RequiredTotalCombo = requiredTotalCombo;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (!state.IsCompleted) return CriterionState.Undetermined;
        return state.MaxCombo >= RequiredTotalCombo ? CriterionState.Passed : CriterionState.Failed;
    }
}