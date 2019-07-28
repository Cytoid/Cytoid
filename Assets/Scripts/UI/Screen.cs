using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
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
                        foreach (var handler in screenDestroyedListeners) handler.OnScreenDestroyed();
                    }

                    break;
                case ScreenState.Active:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            foreach (var handler in screenInitializedListeners) handler.OnScreenInitialized();
                            foreach (var handler in screenBecameActiveListeners) handler.OnScreenBecameActive();
                            break;
                        case ScreenState.Inactive:
                            foreach (var handler in screenBecameActiveListeners) handler.OnScreenBecameActive();
                            break;
                    }

                    break;
                case ScreenState.Inactive:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            foreach (var handler in screenInitializedListeners) handler.OnScreenInitialized();
                            break;
                        case ScreenState.Active:
                            foreach (var handler in screenBecameInactiveListeners) handler.OnScreenBecameInactive();
                            break;
                    }

                    break;
            }
        }
    }

    protected HashSet<ScreenInitializedListener> screenInitializedListeners = new HashSet<ScreenInitializedListener>();
    protected HashSet<ScreenBecameActiveListener> screenBecameActiveListeners = new HashSet<ScreenBecameActiveListener>();
    protected HashSet<ScreenUpdateListener> screenUpdateListeners = new HashSet<ScreenUpdateListener>();
    protected HashSet<ScreenBecameInactiveListener> screenBecameInactiveListeners = new HashSet<ScreenBecameInactiveListener>();
    protected HashSet<ScreenDestroyedListener> screenDestroyedListeners = new HashSet<ScreenDestroyedListener>();

    private void Start()
    {
        screenInitializedListeners = new HashSet<ScreenInitializedListener>(GetComponentsInChildren<ScreenInitializedListener>().ToList());
        screenBecameActiveListeners = new HashSet<ScreenBecameActiveListener>(GetComponentsInChildren<ScreenBecameActiveListener>().ToList());
        screenUpdateListeners = new HashSet<ScreenUpdateListener>(GetComponentsInChildren<ScreenUpdateListener>().ToList());
        screenBecameInactiveListeners = new HashSet<ScreenBecameInactiveListener>(GetComponentsInChildren<ScreenBecameInactiveListener>().ToList());
        screenDestroyedListeners = new HashSet<ScreenDestroyedListener>(GetComponentsInChildren<ScreenDestroyedListener>().ToList());
    }

    private void Update()
    {
        if (state == ScreenState.Active)
        {
            foreach (var handler in screenUpdateListeners) handler.OnScreenUpdate();
        }
    }

    public void AddHandler(ScreenListener listener)
    {
        AddHandler((ScreenInitializedListener) listener);
        AddHandler((ScreenBecameActiveListener) listener);
        AddHandler((ScreenUpdateListener) listener);
        AddHandler((ScreenBecameActiveListener) listener);
        AddHandler((ScreenDestroyedListener) listener);
    }

    public void RemoveHandler(ScreenListener listener)
    {
        RemoveHandler((ScreenInitializedListener) listener);
        RemoveHandler((ScreenBecameActiveListener) listener);
        RemoveHandler((ScreenUpdateListener) listener);
        RemoveHandler((ScreenBecameActiveListener) listener);
        RemoveHandler((ScreenDestroyedListener) listener);
    }
    
    public void AddHandler(ScreenInitializedListener listener)
    {
        screenInitializedListeners.Add(listener);
    }

    public void RemoveHandler(ScreenInitializedListener listener)
    {
        screenInitializedListeners.Remove(listener);
    }
    
    public void AddHandler(ScreenBecameActiveListener listener)
    {
        screenBecameActiveListeners.Add(listener);
    }

    public void RemoveHandler(ScreenBecameActiveListener listener)
    {
        screenBecameActiveListeners.Remove(listener);
    }
    
    public void AddHandler(ScreenUpdateListener listener)
    {
        screenUpdateListeners.Add(listener);
    }

    public void RemoveHandler(ScreenUpdateListener listener)
    {
        screenUpdateListeners.Remove(listener);
    }
    
    public void AddHandler(ScreenBecameInactiveListener listener)
    {
        screenBecameInactiveListeners.Add(listener);
    }

    public void RemoveHandler(ScreenBecameInactiveListener listener)
    {
        screenBecameInactiveListeners.Remove(listener);
    }
    
    public void AddHandler(ScreenDestroyedListener listener)
    {
        screenDestroyedListeners.Add(listener);
    }

    public void RemoveHandler(ScreenDestroyedListener listener)
    {
        screenDestroyedListeners.Remove(listener);
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
    }

    public virtual void OnScreenBecameActive() => Expression.Empty();

    public virtual void OnScreenUpdate() => Expression.Empty();

    public virtual void OnScreenBecameInactive() => Expression.Empty();

    public virtual void OnScreenDestroyed() => Expression.Empty();
}

public enum ScreenState
{
    Destroyed,
    Active,
    Inactive
}

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

    public static Screen GetOwingScreen(this MonoBehaviour monoBehaviour)
    {
        return monoBehaviour.gameObject.GetOwningScreen();
    }
}