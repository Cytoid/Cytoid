using UnityEngine;
using UnityEngine.UI;

public class PerformanceWidget : ContainerEntry<RankingEntry>
{
    public Text grade;
    public GradientMeshEffect gradeGradient;
    public Text score;
    public Text accuracy;

    public override void SetModel(RankingEntry entry)
    {
        var scoreGrade = ScoreGrades.From(entry.score);
        grade.text = scoreGrade.ToString();
        gradeGradient.SetGradient(scoreGrade.GetGradient());
        score.text = entry.score.ToString();
        accuracy.text = (entry.accuracy * 100f).ToString("N2") + "%";
    }
}