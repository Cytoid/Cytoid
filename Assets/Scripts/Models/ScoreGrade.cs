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
    public static ScoreGrade From(float score)
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

    public static Color Color(this ScoreGrade grade)
    {
        Color color = UnityEngine.Color.white;
        switch (grade)
        {
            case ScoreGrade.MAX:
                ColorUtility.TryParseHtmlString("#ffc107", out color);
                break;
            case ScoreGrade.SSS:
                ColorUtility.TryParseHtmlString("#007bff", out color);
                break;
            case ScoreGrade.SS:
                ColorUtility.TryParseHtmlString("#2E9EF5", out color);
                break;
            case ScoreGrade.S:
                ColorUtility.TryParseHtmlString("#5BC0EB", out color);
                break;
            case ScoreGrade.AA:
                ColorUtility.TryParseHtmlString("#FDE74C", out color);
                break;
            case ScoreGrade.A:
                ColorUtility.TryParseHtmlString("#FDE74C", out color);
                break;
            case ScoreGrade.B:
                ColorUtility.TryParseHtmlString("#9BC53D", out color);
                break;
            case ScoreGrade.C:
                ColorUtility.TryParseHtmlString("#9BC53D", out color);
                break;
            case ScoreGrade.D:
                ColorUtility.TryParseHtmlString("#E55934", out color);
                break;
            case ScoreGrade.F:
                ColorUtility.TryParseHtmlString("#E55934", out color);
                break;
        }
        return color;
    }
}