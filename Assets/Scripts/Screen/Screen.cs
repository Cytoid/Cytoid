using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public abstract class Screen : MonoBehaviour, ScreenListener, ScreenPostActiveListener
{
    public RectTransform RectTransform { get; set; }
    public CanvasGroup CanvasGroup { get; set; }
    
    private ScreenState state = ScreenState.Destroyed;

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
    [HideInInspector] public UnityEvent onScreenUpdate = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenBecameInactive = new UnityEvent();
    [HideInInspector] public UnityEvent onScreenDestroyed = new UnityEvent();

    protected virtual void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
        RectTransform = GetComponent<RectTransform>();
    }

    protected void OnEnable()
    {
        UseChildrenListeners();
    }

    public void UseChildrenListeners()
    {
        UseChildrenListener<ScreenInitializedListener>(onScreenInitialized, it => it.OnScreenInitialized);
        UseChildrenListener<ScreenBecameActiveListener>(onScreenBecameActive, it => it.OnScreenBecameActive);
        UseChildrenListener<ScreenPostActiveListener>(onScreenPostActive, it => it.OnScreenPostActive);
        UseChildrenListener<ScreenUpdateListener>(onScreenUpdate, it => it.OnScreenUpdate);
        UseChildrenListener<ScreenBecameInactiveListener>(onScreenBecameInactive, it => it.OnScreenBecameInactive);
        UseChildrenListener<ScreenDestroyedListener>(onScreenDestroyed, it => it.OnScreenDestroyed);
    }

    private void UseChildrenListener<T>(UnityEvent unityEvent, Func<T, UnityAction> use)
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
        onScreenDestroyed.Invoke();
    }

    public virtual void OnScreenPostActive()
    {
        onScreenPostActive.Invoke();
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