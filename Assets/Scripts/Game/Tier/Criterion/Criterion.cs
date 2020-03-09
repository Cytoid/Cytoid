public abstract class Criterion
{
    public virtual string Description { get; }

    public abstract CriterionState Judge(TierState state);

    protected string StageName(int stageIndex)
    {
        switch (stageIndex)
        {
            case 0:
                return "TIER_STAGE_1ST".Get();
            case 1:
                return "TIER_STAGE_2ND".Get();
            case 2:
                return "TIER_STAGE_3RD".Get();
            default:
                return "Unknown";
        }
    }

}

public enum CriterionState
{
    Passed, Failed, Undetermined
}