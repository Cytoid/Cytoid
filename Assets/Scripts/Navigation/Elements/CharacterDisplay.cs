using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CharacterDisplay : MonoBehaviour, ScreenBecameActiveListener, ScreenLeaveCompletedListener, ScreenBecameInactiveListener
{

    [GetComponent] public CanvasGroup canvasGroup;
    public TransitionElement transitionElement;
    public InteractableMonoBehavior interactableMonoBehavior;
    public bool loadOnScreenBecameActive = true;
    public bool loadActiveCharacter = true;
    public bool enterTransitionElementOnEnter = true;
    public bool interactable;

    public bool IsLoaded { get; private set; }
    public Func<PointerEventData, UniTask> OnInteract { get; set; }

    private string loadedBundleId;
    private AssetBundle loadedAssetBundle;
    private GameObject loadedGameObject;
    private RectTransform rectTransform;
    private DateTime asyncLoadToken;
    private bool isLoading;

    public async UniTask Load(string tachieBundleId, bool silhouette = false)
    {
        if (isLoading) await UniTask.WaitUntil(() => !isLoading);

        isLoading = true;
        Unload();
        
        print("CharacterDisplay: Loaded " + tachieBundleId);
        var token = asyncLoadToken = DateTime.Now;
        var ab = await Context.BundleManager.LoadBundle(tachieBundleId, false, false);
        if (token != asyncLoadToken)
        {
            isLoading = false;
            return;
        }
        
        // Instantiate the GameObject
        var loader = ab.LoadAssetAsync<GameObject>("Tachie");
        await loader;
        if (token != asyncLoadToken)
        {
            isLoading = false;
            return;
        }

        loadedBundleId = tachieBundleId;
        loadedAssetBundle = ab;
        loadedGameObject = Instantiate((GameObject) loader.asset);
        
        IsLoaded = true;
        var t = (RectTransform) loadedGameObject.transform;
        t.SetParent(transform);
        t.anchoredPosition = Vector3.zero;
        t.localScale = Vector3.one;

        if (silhouette)
        {
            t.GetComponentsInChildren<Image>()
                .ForEach(it => it.color = Color.black.WithAlpha(it.color.a));
        }
        
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
        if (interactable)
        {
            var isInteracting = false;
            interactableMonoBehavior.onPointerClick.SetListener(async data =>
            {
                if (isInteracting || OnInteract == null) return;
                isInteracting = true;
                await OnInteract.Invoke(data);
                isInteracting = false;
            });
        }
    }

    public void Unload()
    {
        asyncLoadToken = DateTime.Now;
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
            if (interactable)
            {
                interactableMonoBehavior.onPointerClick.RemoveAllListeners();
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

    public void OnScreenBecameInactive()
    {
        if (interactable)
        {
            interactableMonoBehavior.onPointerClick.RemoveAllListeners();
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