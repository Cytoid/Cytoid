using System;
using UnityEngine;
using UnityEngine.UI;

public class StatusText : MonoBehaviour
{
    [GetComponent] public Text text;

    public Game game;

    private void Awake()
    {
        text.text = "";
        game.onGameLoaded.AddListener(_ => Load());
    }

    public void Load()
    {
        if (game.Config.IsCalibration)
        {
            text.text = "GAME_CALIBRATION_MODE".Get();
            text.color = "#728CE4".ToColor().WithAlpha(0.7f);
        }
        else if (!game.State.IsRanked)
        {
            text.text = "GAME_PRACTICE_MODE".Get();
            text.color = "#F953C6".ToColor().WithAlpha(0.7f);
        }
        else
        {
            text.text = "";
        }
    }
}