using UnityEngine;
using UnityEngine.UI;

public class GameTimeText : MonoBehaviour
{
    public Text text;
    public Game game;

    private void OnValidate()
    {
        this.AutoFill(ref text);
    }

    private void Awake()
    {
        this.AutoFill(ref text);
    }

    private void Update()
    {
        text.text = $"Time: {game.Time:F3}";
    }
}