using UnityEngine;
using UnityEngine.UI;

public class GameTimeText : MonoBehaviour
{
    [GetComponent] public Text text;
    public Game game;

    private void Update()
    {
        text.text = $"Time: {game.Time:F3}";
    }
}