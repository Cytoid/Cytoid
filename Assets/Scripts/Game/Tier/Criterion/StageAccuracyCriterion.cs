using UnityEngine;

public class StageAccuracyCriterion : Criterion
{
    public override string Description => "TIER_CRITERIA_STAGE_ACCURACY".Get((Mathf.Floor((float) (RequiredAccuracy * 100 * 100)) / 100).ToString("0.00"), StageName(StageIndex));

    public int StageIndex { get; }
    public double RequiredAccuracy { get; }

    public StageAccuracyCriterion(int stageIndex, double requiredAccuracy)
    {
        StageIndex = stageIndex;
        RequiredAccuracy = requiredAccuracy;
    }
    
    public override CriterionState Judge(TierState state)
    {
        if (state.Stages[StageIndex] == null || !state.Stages[StageIndex].IsCompleted) return CriterionState.Undetermined;
        return state.Stages[StageIndex].Accuracy >= RequiredAccuracy ? CriterionState.Passed : CriterionState.Failed;
    }
}