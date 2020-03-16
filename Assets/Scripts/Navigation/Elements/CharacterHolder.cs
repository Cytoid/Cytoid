using System;
using UnityEngine;

public class CharacterHolder : MonoBehaviour, ScreenBecameActiveListener, ScreenBecameInactiveListener
{

    [GetComponent] public TransitionElement transitionElement;
    public bool loadOnScreenBecameActive = true;
    
    private RectTransform rectTransform;

    public async void Load()
    {
        Unload();
        if (Context.RemoteResourceManager.SelectedCharacterGameObject == null)
        {
            Context.RemoteResourceManager.SelectedCharacterGameObject =
                await Context.RemoteResourceManager.LoadResource(Context.RemoteResourceManager.SelectedCharacterAssetId);
        }
        
        if (Context.RemoteResourceManager.SelectedCharacterGameObject != null)
        {
            var character = Context.RemoteResourceManager.SelectedCharacterGameObject.GetComponent<CharacterAsset>();
            rectTransform = Instantiate(character.tachiePrefab, transform).transform as RectTransform;
            transitionElement.Enter();
        }
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