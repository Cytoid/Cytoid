using System;
using UniRx.Async;
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

    private string loadedBundleId;
    private AssetBundle loadedAssetBundle;
    private GameObject loadedGameObject;
    private RectTransform rectTransform;
    private DateTime asyncLoadToken;
    private bool isLoading;

    public async UniTask Load(string tachieBundleId)
    {
        if (isLoading) await UniTask.WaitUntil(() => !isLoading);

        isLoading = true;
        Unload();
        
        print("CharacterDisplay: Loaded " + tachieBundleId);
        var token = asyncLoadToken = DateTime.Now;
        var ab = await Context.BundleManager.LoadBundle(tachieBundleId, false, false);
        if (token != asyncLoadToken) return;
        
        // Instantiate the GameObject
        var loader = ab.LoadAssetAsync<GameObject>("Tachie");
        await loader;
        if (token != asyncLoadToken) return;

        loadedBundleId = tachieBundleId;
        loadedAssetBundle = ab;
        loadedGameObject = Instantiate((GameObject) loader.asset);
        
        IsLoaded = true;
        var t = (RectTransform) loadedGameObject.transform;
        t.SetParent(transform);
        t.anchoredPosition = Vector3.zero;
        t.localScale = Vector3.one;
        Enter();

        isLoading = false;
    }

    public void Enter()
    {
        if (transitionElement != null && enterTransitionElementOnEnter)
        {
            transitionElement.Leave(false, true);
            transitionElement.Enter();
        }
        loadedGameObject.GetComponent<AnimatedCharacter>()?.OnEnter();
    }

    public void Unload()
    {
        if (loadedAssetBundle != null)
        {
            IsLoaded = false;
            print("CharacterDisplay: Unloaded");
            Destroy(loadedGameObject);
            Context.BundleManager.Release(loadedBundleId);
            loadedGameObject = null;
            loadedBundleId = null;
            loadedAssetBundle = null;
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
            Load(CharacterAsset.GetTachieBundleId(Context.CharacterManager.SelectedCharacterId));
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