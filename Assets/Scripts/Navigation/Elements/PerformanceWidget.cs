using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class PerformanceWidget : ContainerEntry<RankingEntry>
{
    public Text grade;
    public GradientMeshEffect gradeGradient;
    public Text score;
    public Text accuracy;

    public RankingEntry Model { get; protected set; }
    
    public override void SetModel(RankingEntry entry)
    {
        Model = entry;
        var scoreGrade = ScoreGrades.From(entry.score);
        grade.text = scoreGrade.ToString();
        gradeGradient.SetGradient(scoreGrade.GetGradient());
        score.text = entry.score.ToString("D6");
        accuracy.text = (entry.accuracy * 100f).ToString("N2") + "%";
        LayoutFixer.Fix(transform as RectTransform);
    }

    public override RankingEntry GetModel() => Model;

    public void SetModel(LocalPlayer.Performance performance)
    {
        SetModel(new RankingEntry { score = (int) performance.Score, accuracy = performance.Accuracy / 100.0f });
    }
    
}