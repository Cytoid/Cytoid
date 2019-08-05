using UnityEngine;
using UnityEngine.UI;

public class LevelLocalizedTitleText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        text.text = Context.ActiveLevel?.Meta.title_localized ?? "Unknown";
    }
}