using UnityEngine;
using UnityEngine.UI;

public class LevelTitleText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        text.text = Context.SelectedLevel?.Meta.title ?? "Unknown";
    }
}