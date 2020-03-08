using System.Linq;

public abstract class Criterion
{
    public virtual string Description { get; }

    public abstract CriterionState Judge(TierState state);

}

public enum CriterionState
{
    Passed, Failed, Undetermined
}