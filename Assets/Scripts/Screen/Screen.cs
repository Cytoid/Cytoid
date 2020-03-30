using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup))]
public abstract class Screen : MonoBehaviour, ScreenListener, ScreenPostActiveListener, ScreenChangeListener, ScreenEnterCompletedListener, ScreenLeaveCompletedListener
{
    public Canvas Canvas { get; set; }
    public RectTransform RectTransform { get; set; }
    public CanvasGroup CanvasGroup { get; set; }
    
    private ScreenState state = ScreenState.Destroyed;

    private bool rebuiltLayoutGroups;

    public ScreenState State
    {
        get => state;
        set
        {
            var originalValue = state;
            state = value;
            switch (state)
            {
                case ScreenState.Destroyed:
                    if (originalValue != ScreenState.Destroyed)
                    {
                        OnScreenDestroyed();
                    }

                    break;
                case ScreenState.Active:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            OnScreenInitialized();
                            OnScreenBecameActive();
                            OnScreenPostActive();
                            break;
                        case ScreenState.Inactive:
                            OnScreenBecameActive();
                            OnScreenPostActive();
                            break;
                    }

                    break;
                case ScreenState.Inactive:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            OnScreenInitialized();
                            break;
                        case ScreenState.Active:
                            OnScreenBecameInactive();
                            break;
                    }

                    break;
            }
        }
    }
    
    [HideInInspector] public UnityEvent onScreenInitialized = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenBecameActive = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenPostActive = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenEnterCompleted = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenUpdate = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenBecameInactive = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenLeaveCompleted = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenDestroyed = new UnityEvent();

    protected virtual async void Awake()
    {
        Canvas = GetComponent<Canvas>();
        CanvasGroup = GetComponent<CanvasGroup>();
        RectTransform = GetComponent<RectTransform>();
        Context.OnLanguageChanged.AddListener(() => rebuiltLayoutGroups = false);
        if (Context.ScreenManager == null) await UniTask.WaitUntil(() => Context.ScreenManager != null);
        Context.ScreenManager.AddHandler(this);
    }

    private void OnDestroy()
    {
        Context.ScreenManager.RemoveHandler(this);
    }

    protected void OnEnable()
    {
        UseChildrenListeners();
    }

    public void RegisterEvents(object obj)
    {
        AddListener<ScreenInitializedListener>(obj, onScreenInitialized, it => it.OnScreenInitialized);
        AddListener<ScreenBecameActiveListener>(obj, onScreenBecameActive, it => it.OnScreenBecameActive);
        AddListener<ScreenPostActiveListener>(obj, onScreenPostActive, it => it.OnScreenPostActive);
        AddListener<ScreenEnterCompletedListener>(obj, onScreenEnterCompleted, it => it.OnScreenEnterCompleted);
        AddListener<ScreenUpdateListener>(obj, onScreenUpdate, it => it.OnScreenUpdate);
        AddListener<ScreenBecameInactiveListener>(obj, onScreenBecameInactive, it => it.OnScreenBecameInactive);
        AddListener<ScreenLeaveCompletedListener>(obj, onScreenLeaveCompleted, it => it.OnScreenLeaveCompleted);
        AddListener<ScreenDestroyedListener>(obj, onScreenDestroyed, it => it.OnScreenDestroyed);
    }

    public void UnregisterEvents(object obj)
    {
        RemoveListener<ScreenInitializedListener>(obj, onScreenInitialized, it => it.OnScreenInitialized);
        RemoveListener<ScreenBecameActiveListener>(obj, onScreenBecameActive, it => it.OnScreenBecameActive);
        RemoveListener<ScreenPostActiveListener>(obj, onScreenPostActive, it => it.OnScreenPostActive);
        RemoveListener<ScreenEnterCompletedListener>(obj, onScreenEnterCompleted, it => it.OnScreenEnterCompleted);
        RemoveListener<ScreenUpdateListener>(obj, onScreenUpdate, it => it.OnScreenUpdate);
        RemoveListener<ScreenBecameInactiveListener>(obj, onScreenBecameInactive, it => it.OnScreenBecameInactive);
        RemoveListener<ScreenLeaveCompletedListener>(obj, onScreenLeaveCompleted, it => it.OnScreenLeaveCompleted);
        RemoveListener<ScreenDestroyedListener>(obj, onScreenDestroyed, it => it.OnScreenDestroyed);
    }

    public void UseChildrenListeners()
    {
        AddChildrenListener<ScreenInitializedListener>(onScreenInitialized, it => it.OnScreenInitialized);
        AddChildrenListener<ScreenBecameActiveListener>(onScreenBecameActive, it => it.OnScreenBecameActive);
        AddChildrenListener<ScreenPostActiveListener>(onScreenPostActive, it => it.OnScreenPostActive);
        AddChildrenListener<ScreenEnterCompletedListener>(onScreenEnterCompleted, it => it.OnScreenEnterCompleted);
        AddChildrenListener<ScreenUpdateListener>(onScreenUpdate, it => it.OnScreenUpdate);
        AddChildrenListener<ScreenBecameInactiveListener>(onScreenBecameInactive, it => it.OnScreenBecameInactive);
        AddChildrenListener<ScreenLeaveCompletedListener>(onScreenLeaveCompleted, it => it.OnScreenLeaveCompleted);
        AddChildrenListener<ScreenDestroyedListener>(onScreenDestroyed, it => it.OnScreenDestroyed);
    }
    
    private void AddListener<T>(object obj, UnityEvent unityEvent, Func<T, UnityAction> use)
    {
        if (obj is T it)
        {
            unityEvent.RemoveListener(use(it));
            unityEvent.AddListener(use(it));
        }
    }
    
    private void RemoveListener<T>(object obj, UnityEvent unityEvent, Func<T, UnityAction> use)
    {
        if (obj is T it)
        {
            unityEvent.RemoveListener(use(it));
        }
    }

    private void AddChildrenListener<T>(UnityEvent unityEvent, Func<T, UnityAction> use)
    {
        GetComponentsInChildren<T>(true).Where(it => (object) it != (object) this).ToList().ForEach(it =>
        {
            unityEvent.RemoveListener(use(it));
            unityEvent.AddListener(use(it));
        });
    }
    
    private void Update()
    {
        if (state == ScreenState.Active)
        {
            OnScreenUpdate();
        }
    }

    public abstract string GetId();

    public virtual void OnScreenInitialized()
    {
        // Unfortunately, due to how Unity implements their shitty & buggy layout group system,
        // we need to rebuild every layout group two times in order to have correct rect values.
        for (var i = 1; i <= 2; i++)
        {
            foreach (var layoutGroup in gameObject.GetComponentsInChildren<LayoutGroup>())
            {
                layoutGroup.transform.RebuildLayout();
            }
        }
        onScreenInitialized.Invoke();
    }

    public virtual void OnScreenBecameActive()
    {
        CanvasGroup.blocksRaycasts = true;
        onScreenBecameActive.Invoke();
    }

    public virtual void OnScreenUpdate()
    {
        onScreenUpdate.Invoke();
    }

    public virtual void OnScreenBecameInactive()
    {
        CanvasGroup.blocksRaycasts = false;
        onScreenBecameInactive.Invoke();
    }

    public virtual void OnScreenDestroyed()
    {
        Context.ScreenManager.createdScreens.Remove(this);
        onScreenDestroyed.Invoke();
    }

    public virtual void OnScreenPostActive()
    {
        onScreenPostActive.Invoke();
        if (!rebuiltLayoutGroups)
        {
            rebuiltLayoutGroups = true;
            GetComponentsInChildren<LayoutGroup>().ForEach(it => LayoutFixer.Fix(it.transform));
        }
    }

    public virtual void OnScreenChangeStarted(Screen from, Screen to)
    {
    }

    public virtual void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this)
        {
            OnScreenLeaveCompleted();
        } 
        else if (to == this)
        {
            OnScreenEnterCompleted();
        }
    }

    public virtual void OnScreenEnterCompleted()
    {
        onScreenEnterCompleted.Invoke();
    }

    public virtual void OnScreenLeaveCompleted()
    {
        onScreenLeaveCompleted.Invoke();
    }
}

public enum ScreenState
{
    Destroyed,
    Active,
    Inactive
}

#if UNITY_EDITOR

[CustomEditor(typeof(Screen), true)]
public class ScreenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var component = (Screen) target;
        
        if (GUILayout.Button("Set inactive"))
        {
            component.State = ScreenState.Inactive;
        }

        if (GUILayout.Button("Set active"))
        {
            component.State = ScreenState.Active;
        }
        
        if (GUILayout.Button("Destroy"))
        {
            component.State = ScreenState.Destroyed;
        }
    }

}

#endif

public static class ScreenExtensions
{
    public static Screen GetScreenParent(this GameObject gameObject)
    {
        var transform = gameObject.transform;
        while (transform != null)
        {
            var screen = transform.GetComponent<Screen>();
            if (screen != null) return screen;
            transform = transform.parent;
        }
        return null;
    }

    public static Screen GetScreenParent(this MonoBehaviour monoBehaviour)
    {
        return monoBehaviour.gameObject.GetScreenParent();
    }
    
    public static T GetScreenParent<T>(this MonoBehaviour monoBehaviour) where T : Screen
    {
        return (T) monoBehaviour.gameObject.GetScreenParent();
    }
}