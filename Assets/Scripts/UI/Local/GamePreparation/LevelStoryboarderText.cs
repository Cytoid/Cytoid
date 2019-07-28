using UnityEngine;
using UnityEngine.UI;

public class LevelStoryboarderText : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public Text text;
    public void OnScreenBecameActive()
    {
        if (Context.activeLevel?.meta.storyboarder == null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            transform.parent.gameObject.SetActive(true);
            text.text = Context.activeLevel?.meta.storyboarder;
        }
    }
}