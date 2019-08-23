using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public abstract class Screen : MonoBehaviour, ScreenListener
{
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
                            break;
                        case ScreenState.Inactive:
                            OnScreenBecameActive();
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

    public UnityEvent onScreenInitialized = new UnityEvent();
    public UnityEvent onScreenBecameActive = new UnityEvent();
    public UnityEvent onScreenUpdate = new UnityEvent();
    public UnityEvent onScreenBecameInactive = new UnityEvent();
    public UnityEvent onScreenDestroyed = new UnityEvent();

    protected virtual void Awake()
    {
        GetComponentsInChildren<ScreenInitializedListener>().Where(it => it != this).ToList().ForEach(it => onScreenInitialized.AddListener(it.OnScreenInitialized));
        GetComponentsInChildren<ScreenBecameActiveListener>().Where(it => it != this).ToList().ForEach(it => onScreenBecameActive.AddListener(it.OnScreenBecameActive));
        GetComponentsInChildren<ScreenUpdateListener>().Where(it => it != this).ToList().ForEach(it => onScreenUpdate.AddListener(it.OnScreenUpdate));
        GetComponentsInChildren<ScreenBecameInactiveListener>().Where(it => it != this).ToList().ForEach(it => onScreenBecameInactive.AddListener(it.OnScreenBecameInactive));
        GetComponentsInChildren<ScreenDestroyedListener>().Where(it => it != this).ToList().ForEach(it => onScreenDestroyed.AddListener(it.OnScreenDestroyed));
    }

    private void Update()
    {
        if (state == ScreenState.Active)
        {
            onScreenUpdate.Invoke();
        }
    }

    public abstract string GetId();

    public virtual void OnScreenInitialized()
    {
        foreach (var layoutGroup in gameObject.GetComponentsInChildren<LayoutGroup>())
        {
            layoutGroup.transform.RebuildLayout();
            foreach (var transitionElement in layoutGroup.GetComponentsInChildren<TransitionElement>())
            {
                transitionElement.UseCurrentStateAsDefault();
            }
        }
        onScreenInitialized.Invoke();
    }

    public virtual void OnScreenBecameActive()
    {
        onScreenBecameActive.Invoke();
    }

    public virtual void OnScreenUpdate()
    {
        onScreenUpdate.Invoke();
    }

    public virtual void OnScreenBecameInactive()
    {
        onScreenBecameInactive.Invoke();
    }

    public virtual void OnScreenDestroyed()
    {
        onScreenDestroyed.Invoke();
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
    public static Screen GetOwningScreen(this GameObject gameObject)
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

    public static Screen GetOwningScreen(this MonoBehaviour monoBehaviour)
    {
        return monoBehaviour.gameObject.GetOwningScreen();
    }
}