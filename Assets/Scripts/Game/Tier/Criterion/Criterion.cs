using System;

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

    public static Criterion Parse(CriterionMeta meta)
    {
        switch (meta.name)
        {
            case "averageAccuracy":
                return new AverageAccuracyCriterion((double) meta.args.SelectToken("requiredAverageAccuracy"));
            case "badMissCount":
                return new BadMissCountCriterion((int) meta.args.SelectToken("maxCount"));
            case "fullCombo":
                return new FullComboCriterion();
            case "goodCount":
                return new GoodCountCriterion((int) meta.args.SelectToken("maxCount"));
            case "greatCount":
                return new GreatCountCriterion((int) meta.args.SelectToken("maxCount"));
            case "healthPercentage":
                return new HealthPercentageCriterion((float) meta.args.SelectToken("requiredPercentage"));
            case "stageAccuracy":
                return new StageAccuracyCriterion((int) meta.args.SelectToken("stageIndex"), (double) meta.args.SelectToken("requiredAccuracy"));
            case "stageBadMissCount":
                return new StageBadMissCountCriterion((int) meta.args.SelectToken("stageIndex"), (int) meta.args.SelectToken("maxCount"));
            case "stageCombo":
                return new StageComboCriterion((int) meta.args.SelectToken("stageIndex"), (int) meta.args.SelectToken("requiredCombo"));
            case "stageFullCombo":
                return new StageFullComboCriterion((int) meta.args.SelectToken("stageIndex"));
            case "stageGoodCount":
                return new StageGoodCountCriterion((int) meta.args.SelectToken("stageIndex"), (int) meta.args.SelectToken("maxCount"));
            case "stageGreatCount":
                return new StageGreatCountCriterion((int) meta.args.SelectToken("stageIndex"), (int) meta.args.SelectToken("maxCount"));
            case "stageScore":
                return new StageScoreCriterion((int) meta.args.SelectToken("stageIndex"), (double) meta.args.SelectToken("requiredScore"));
            case "totalCombo":
                return new TotalComboCriterion((int) meta.args.SelectToken("requiredTotalCombo"));
            case "totalScore":
                return new TotalScoreCriterion((double) meta.args.SelectToken("requiredTotalScore"));
            default:
                throw new ArgumentOutOfRangeException(meta.name);
        }
    }
    
}

public enum CriterionState
{
    Passed, Failed, Undetermined
}