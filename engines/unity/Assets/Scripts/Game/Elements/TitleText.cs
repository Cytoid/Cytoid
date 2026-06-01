using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleText : MonoBehaviour, ScreenBecameActiveListener
{
    public Text text;
    public Game game;

    private void OnValidate()
    {
        this.AutoFill(ref text);
    }

    protected void Awake()
    {
        this.AutoFill(ref text);
        text.text = "";
        if (game != null)
        {
            game.onGameLoaded.AddListener(_ =>
            {
                text.text = game.Level.Meta.title;
            });
        }
    }

    public void OnScreenBecameActive()
    {
        if (Context.SelectedLevel != null)
        {
            text.text = Context.SelectedLevel.Meta.title;
        }
    }
}