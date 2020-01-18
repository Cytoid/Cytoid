using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelStoryboarderText : MonoBehaviour, ScreenBecameActiveListener
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
        if (level?.Meta.storyboarder == null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            transform.parent.gameObject.SetActive(true);
            text.text = level.Meta.storyboarder;
        }
    }
}