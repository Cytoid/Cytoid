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

    protected string chartType = Level.Easy;
    protected int chartLevel = 1;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        image = GetComponentInChildren<Image>();
    }

    public void SetDifficulty(Level.ChartSection section)
    {
        chartType = section.type;
        chartLevel = section.difficulty;
        Sprite sprite;
        switch (section.type)
        {
            case Level.Easy:
                sprite = easy;
                typeText.text = "Easy";
                break;
            case Level.Hard:
                sprite = hard;
                typeText.text = "Hard";
                break;
            default:
                sprite = extreme;
                typeText.text = "EX";
                break;
        }

        if (section.name != null)
        {
            typeText.text = section.name;
        }

        image.overrideSprite = sprite;
        levelText.text = "LV." + section.difficulty;
    }
}