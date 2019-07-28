using UnityEngine;
using UnityEngine.UI;

public class LevelVersionText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        text.text = Context.activeLevel?.meta.version.ToString() ?? "Unknown";
    }
}