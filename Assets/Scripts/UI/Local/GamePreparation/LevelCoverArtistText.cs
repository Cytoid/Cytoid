using UnityEngine;
using UnityEngine.UI;

public class LevelCoverArtistText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        text.text = Context.activeLevel?.meta.illustrator ?? "Unknown";
    }
}