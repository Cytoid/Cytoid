using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public abstract class Screen : MonoBehaviour, ScreenEventListener
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
                        foreach (var handler in handlers) handler.OnScreenDestroyed();
                    }

                    break;
                case ScreenState.Active:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            foreach (var handler in handlers) handler.OnScreenInitialized();
                            foreach (var handler in handlers) handler.OnScreenBecomeActive();
                            break;
                        case ScreenState.Inactive:
                            foreach (var handler in handlers) handler.OnScreenBecomeActive();
                            break;
                    }

                    break;
                case ScreenState.Inactive:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            foreach (var handler in handlers) handler.OnScreenInitialized();
                            break;
                        case ScreenState.Active:
                            foreach (var handler in handlers) handler.OnScreenBecomeInactive();
                            break;
                    }

                    break;
            }
        }
    }

    protected HashSet<ScreenEventListener> handlers = new HashSet<ScreenEventListener>();

    private void Start()
    {
        handlers = new HashSet<ScreenEventListener>(GetComponentsInChildren<ScreenEventListener>().ToList()); // Including self
    }

    private void Update()
    {
        if (state == ScreenState.Active)
        {
            foreach (var handler in handlers) handler.OnScreenUpdate();
        }
    }

    public void AddHandler(ScreenEventListener listener)
    {
        handlers.Add(listener);
    }

    public void RemoveHandler(ScreenEventListener listener)
    {
        handlers.Remove(listener);
    }

    public abstract string GetId();

    public virtual void OnScreenInitialized()
    {
        foreach (var layoutGroup in gameObject.GetComponentsInChildren<LayoutGroup>())
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            foreach (var transitionElement in layoutGroup.GetComponentsInChildren<TransitionElement>())
            {
                transitionElement.UseCurrentStateAsDefault();
            }
        }
    }

    public virtual void OnScreenBecomeActive() => Expression.Empty();

    public virtual void OnScreenUpdate() => Expression.Empty();

    public virtual void OnScreenBecomeInactive() => Expression.Empty();

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