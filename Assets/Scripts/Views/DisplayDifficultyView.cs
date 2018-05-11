using QuickEngine.Common;
using UnityEngine;
using UnityEngine.UI;

public class DisplayDifficultyView : SingletonMonoBehavior<DisplayDifficultyView>
{

    public Sprite easy;
    public Sprite hard;
    public Sprite extreme;
    public Text levelText;
    public Text typeText;

    protected Image image;

    protected string chartType = ChartType.Easy;
    protected int chartLevel = 1;
    
    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        image = GetComponentInChildren<Image>();
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
                typeText.text = "Easy";
                break;
            case ChartType.Hard:
                sprite = hard;
                typeText.text = "Hard";
                break;
            default:
                sprite = extreme;
                typeText.text = "EX";
                break;
        }
        image.overrideSprite = sprite;
        levelText.text = "LV." + level;
    }
    
}
