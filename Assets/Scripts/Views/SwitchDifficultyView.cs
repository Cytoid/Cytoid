using QuickEngine.Common;
using UnityEngine;
using UnityEngine.UI;

public class SwitchDifficultyView : MonoBehaviour
{

    public Sprite easy;
    public Sprite hard;
    public Sprite extreme;

    private Image image;
    private Text text;

    private Level level;
    private string selectedChartType = ChartType.Easy;
    
    protected void Awake()
    {
        image = GetComponentInChildren<Image>();
        text = GetComponentInChildren<Text>();
        gameObject.SetActive(false);
    }

    public void OnLevelLoaded()
    {
        gameObject.SetActive(true);
        level = LevelSelectionController.Instance.LoadedLevel;
        var hasType = false;
        foreach (var chart in level.charts)
        {
            if (chart.type == selectedChartType)
            {
                SwitchDifficulty(selectedChartType);
                hasType = true;
            }
        }
        if (!hasType)
        {
            selectedChartType = level.charts[0].type;
            SwitchDifficulty(selectedChartType);
        }
    }

    public void SwitchDifficulty(string type)
    {
        selectedChartType = type;
        CytoidApplication.CurrentChartType = selectedChartType;
        Sprite sprite;
        switch (type)
        {
            case ChartType.Easy:
                sprite = easy;
                break;
            case ChartType.Hard:
                sprite = hard;
                break;
            default:
                sprite = extreme;
                break;
        }
        image.overrideSprite = sprite;
        text.text = level.charts.Find(chart => chart.type == type).difficulty.ToString();
    }

    public void SwitchDifficulty()
    {
        var index = level.charts.IndexOf(level.charts.Find(chart => chart.type == selectedChartType));
        if (index == level.charts.Count - 1) index = 0;
        else index++;
        SwitchDifficulty(level.charts[index].type);
    }
    
}
