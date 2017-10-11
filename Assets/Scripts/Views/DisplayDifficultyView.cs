using QuickEngine.Common;
using UnityEngine;
using UnityEngine.UI;

public class DisplayDifficultyView : SingletonMonoBehavior<DisplayDifficultyView>
{

    public Sprite easy;
    public Sprite hard;
    public Sprite extreme;

    protected Image image;
    protected Text text;

    protected string chartType = ChartType.Easy;
    protected int chartLevel = 1;
    
    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        image = GetComponentInChildren<Image>();
        text = GetComponentInChildren<Text>();
    }

    public void SetDifficulty(string type, int level)
    {
        chartType = type;
        chartLevel = level;
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
        text.text = level.ToString();
    }
    
}
