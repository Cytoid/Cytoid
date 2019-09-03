using UnityEngine;
using UnityEngine.UI;

public class AccuracyText : MonoBehaviour
{
    [GetComponent] public Text text;
    public Game game;

    protected void Awake()
    {
        text.text = "100.00%";
    }

    protected void LateUpdate()
    {
        if (game.IsLoaded && game.State.IsStarted && game.State.ClearCount > 0)
        {
            text.text = game.State.Accuracy.ToString("0.00") + "%";
        }
    }
}