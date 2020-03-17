using UnityEngine;

public class CharacterHolder : MonoBehaviour, ScreenBecameActiveListener, ScreenBecameInactiveListener
{

    [GetComponent] public TransitionElement transitionElement;
    public bool loadOnScreenBecameActive = true;
    
    private RectTransform rectTransform;

    public void Load()
    {
        Unload();
        
        var character = Context.CharacterManager.GetActiveCharacterAsset();
        rectTransform = Instantiate(character.tachiePrefab, transform).transform as RectTransform;
        transitionElement.Enter();
    }

    public void Unload()
    {
        if (rectTransform != null)
        {
            Destroy(rectTransform.gameObject);
            rectTransform = null;
        }
    }

    private void Awake()
    {
        transitionElement.enterOnScreenBecomeActive = false;
    }

    public void OnScreenBecameActive()
    {
        if (loadOnScreenBecameActive) Load();
    }

    public void OnScreenBecameInactive()
    {
        Unload();
    }
}