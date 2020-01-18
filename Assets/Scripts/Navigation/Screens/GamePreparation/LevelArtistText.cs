using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelArtistText : MonoBehaviour, ScreenBecameActiveListener
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
        text.text = level?.Meta.artist ?? "Unknown";
    }
}