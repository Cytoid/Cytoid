using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TitleText : MonoBehaviour
{
    [GetComponent] public Text text;
    public Game game;
    protected void Awake()
    {
        game.onGameLoaded.AddListener(_ => text.text = game.Level.Meta.title);
    }
    
}