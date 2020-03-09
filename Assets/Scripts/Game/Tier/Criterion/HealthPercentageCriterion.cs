using System.Linq;
using UnityEngine;

public class HealthPercentageCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_HEALTH_PERCENTAGE".Get((Mathf.Floor(RequiredPercentage * 100 * 100) / 100).ToString("0.00"));

    public float RequiredPercentage { get; }
    
    public HealthPercentageCriterion(float requiredPercentage)
    {
        RequiredPercentage = requiredPercentage;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (!state.IsCompleted) return CriterionState.Undetermined;
        return state.Stages.Last().Health / state.Stages.Last().MaxHealth >= RequiredPercentage ? CriterionState.Passed : CriterionState.Failed;
    }
}