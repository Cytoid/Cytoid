using UnityEngine;

public enum ScoreGrade
{
    MAX = 9,
    SSS = 8,
    SS = 7,
    S = 6,
    AA = 5,
    A = 4,
    B = 3,
    C = 2,
    D = 1,
    F = 0
}

public static class ScoreGrades
{
    public static ScoreGrade FromTierCompletion(double score)
    {
        var grade = ScoreGrade.F;
        if (score == 2)
        {
            grade = ScoreGrade.MAX;
        }
        else if (score >= 1.9)
        {
            grade = ScoreGrade.SSS;
        }
        else if (score >= 1.5)
        {
            grade = ScoreGrade.AA;
        }

        return grade;
    }
    public static ScoreGrade From(double score)
    {
        var grade = ScoreGrade.F;
        if (score == 1000000)
        {
            grade = ScoreGrade.MAX;
        }
        else if (score >= 999000)
        {
            grade = ScoreGrade.SSS;
        }
        else if (score >= 995000)
        {
            grade = ScoreGrade.SS;
        }
        else if (score >= 990000)
        {
            grade = ScoreGrade.S;
        }
        else if (score >= 950000)
        {
            grade = ScoreGrade.AA;
        }
        else if (score >= 900000)
        {
            grade = ScoreGrade.A;
        }
        else if (score >= 800000)
        {
            grade = ScoreGrade.B;
        }
        else if (score >= 700000)
        {
            grade = ScoreGrade.C;
        }
        else if (score >= 600000)
        {
            grade = ScoreGrade.D;
        }

        return grade;
    }

    public static ColorGradient GetGradient(this ScoreGrade grade)
    {
        switch (grade)
        {
            case ScoreGrade.MAX:
                return new ColorGradient("#0096FF".ToColor(), "#EC00C6".ToColor(), 135);
            case ScoreGrade.SSS:
            case ScoreGrade.SS:
            case ScoreGrade.S:
                return new ColorGradient("#FFC53D".ToColor(), "#FF5E07".ToColor(), -45);
            case ScoreGrade.AA:
            case ScoreGrade.A:
                return new ColorGradient("#FDEB70".ToColor(), "#E25FA6".ToColor(), -45);
            case ScoreGrade.B:
            case ScoreGrade.C:
                return new ColorGradient("#95C529".ToColor(), "#3DB1C5".ToColor(), -45);
            default:
                return new ColorGradient("#99A8D1".ToColor(), "#474E61".ToColor(), -45);
        }
    }
}