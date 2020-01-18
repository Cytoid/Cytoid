using UnityEngine;
using UnityEngine.UI;

public class LevelCoverArtistText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    private void Awake()
    {
        Context.OnSelectedLevelChanged.AddListener(Load);
    }

    public void OnScreenBecameActive()
    {
        Load(Context.SelectedLevel);
    }

    public void Load(Level level)
    {
        text.text = level?.Meta.illustrator ?? "Unknown";
    }
}