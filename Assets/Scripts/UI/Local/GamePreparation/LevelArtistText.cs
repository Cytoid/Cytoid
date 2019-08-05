using UnityEngine;
using UnityEngine.UI;

public class LevelArtistText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        text.text = Context.ActiveLevel?.Meta.artist ?? "Unknown";
    }
}