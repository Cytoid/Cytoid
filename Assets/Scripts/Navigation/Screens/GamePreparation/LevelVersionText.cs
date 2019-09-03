using UnityEngine;
using UnityEngine.UI;

public class LevelVersionText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        text.text = Context.SelectedLevel?.Meta.version.ToString() ?? "Unknown";
    }
}