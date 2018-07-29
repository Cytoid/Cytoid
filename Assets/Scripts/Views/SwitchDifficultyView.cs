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
        EventKit.Subscribe("level loaded", OnLevelLoaded);
        EventKit.Subscribe<string>("meta reloaded", OnLevelMetaReloaded);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventKit.Unsubscribe("level loaded", OnLevelLoaded);
        EventKit.Unsubscribe<string>("meta reloaded", OnLevelMetaReloaded);
    }

    public void OnLevelMetaReloaded(string levelId)
    {
        OnLevelLoaded();
    }

    private void OnLevelLoaded()
    {
        gameObject.SetActive(true);
        level = LevelSelectionController.Instance.LoadedLevel;
        chartType = CytoidApplication.CurrentChartType;
        var hasType = false;
        foreach (var chart in level.charts)
        {
            if (chart.type == chartType)
            {
                SwitchDifficulty(chartType, true);
                hasType = true;
            }
        }

        if (!hasType)
        {
            chartType = level.charts[0].type;
            SwitchDifficulty(chartType, true);
        }
    }

    public void SwitchDifficulty(string type, bool newLevel)
    {
        if (CytoidApplication.CurrentChartType == type && !newLevel) return;
        SetDifficulty(level, level.charts.Find(it => it.type == type));
        CytoidApplication.CurrentChartType = type;
        LevelSelectionController.Instance.UpdateBestText();

        if (PlayerPrefsExt.GetBool("ranked"))
        {
            EventKit.Broadcast("reload rankings");
        }
    }

    public void SwitchDifficulty()
    {
        var index = level.charts.IndexOf(level.charts.Find(chart => chart.type == chartType));
        if (index == level.charts.Count - 1) index = 0;
        else index++;
        SwitchDifficulty(level.charts[index].type, false);
    }
}