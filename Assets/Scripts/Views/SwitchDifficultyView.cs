using QuickEngine.Common;
using UnityEngine;
using UnityEngine.UI;

public class SwitchDifficultyView : DisplayDifficultyView
{
    private Level level;

    protected override void Awake()
    {
        base.Awake();
        gameObject.SetActive(false);
    }

    public void OnLevelLoaded()
    {
        gameObject.SetActive(true);
        level = LevelSelectionController.Instance.LoadedLevel;
        chartType = CytoidApplication.CurrentChartType;
        var hasType = false;
        foreach (var chart in level.charts)
        {
            if (chart.type == chartType)
            {
                SwitchDifficulty(chartType);
                hasType = true;
            }
        }

        if (!hasType)
        {
            chartType = level.charts[0].type;
            SwitchDifficulty(chartType);
        }
    }

    public void SwitchDifficulty(string type)
    {
        SetDifficulty(type, level.GetDifficulty(type));
        CytoidApplication.CurrentChartType = type;
        LevelSelectionController.Instance.UpdateBestText();
    }

    public void SwitchDifficulty()
    {
        var index = level.charts.IndexOf(level.charts.Find(chart => chart.type == chartType));
        if (index == level.charts.Count - 1) index = 0;
        else index++;
        SwitchDifficulty(level.charts[index].type);
    }
}