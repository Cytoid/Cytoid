using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CharacterDisplay : MonoBehaviour, ScreenBecameActiveListener, ScreenLeaveCompletedListener
{

    [GetComponent] public CanvasGroup canvasGroup;
    public TransitionElement transitionElement;
    public bool loadOnScreenBecameActive = true;
    public bool loadActiveCharacter = true;
    public bool enterTransitionElementOnEnter = true;

    public bool IsLoaded { get; private set; }

    private GameObject loadedTachie;
    private RectTransform rectTransform;

    public async void Load(string assetId)
    {
        Unload();
        
        print("CharacterDisplay: Loaded " + assetId);
        loadedTachie = await Context.RemoteResourceManager.LoadResource(assetId);
        IsLoaded = true;
        loadedTachie.transform.SetParent(transform);
        loadedTachie.transform.localPosition = Vector3.zero;
        loadedTachie.transform.localScale = Vector3.one;
        Enter();
    }

    public void Enter()
    {
        if (transitionElement != null && enterTransitionElementOnEnter)
        {
            transitionElement.Leave(false, true);
            transitionElement.Enter();
        }
        loadedTachie.GetComponent<AnimatedCharacter>()?.OnEnter();
    }

    public void Unload()
    {
        if (loadedTachie != null)
        {
            IsLoaded = false;
            print("CharacterDisplay: Unloaded");
            Destroy(loadedTachie.gameObject);
            Context.RemoteResourceManager.Release(loadedTachie);
            loadedTachie = null;
            if (rectTransform != null)
            {
                Destroy(rectTransform.gameObject);
                rectTransform = null;
            }
        }
    }

    private void Awake()
    {
        if (transitionElement != null && enterTransitionElementOnEnter) transitionElement.enterOnScreenBecomeActive = false;
    }

    public void OnScreenBecameActive()
    {
        if (loadOnScreenBecameActive && loadActiveCharacter)
        {
            Load(CharacterAsset.GetTachieAssetId(Context.CharacterManager.ActiveCharacterAssetId));
        }
    }

    public void OnScreenLeaveCompleted()
    {
        Unload();
    }
    
}

#if UNITY_EDITOR
[CustomEditor(typeof(CharacterDisplay))]
public class CharacterDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Enter"))
            {
                ((CharacterDisplay) target).Enter();
            }
            EditorUtility.SetDirty(target);
        }
    }
}
#endif