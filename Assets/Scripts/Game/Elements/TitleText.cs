using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public Game game;
    
    protected void Awake()
    {
        if (game != null)
        {
            game.onGameReadyToLoad.AddListener(_ =>
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