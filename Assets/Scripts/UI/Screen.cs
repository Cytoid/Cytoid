using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public abstract class Screen : MonoBehaviour, ScreenHandler
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
                        handlers.ForEach(it => it.OnScreenDestroyed());
                    }

                    break;
                case ScreenState.Active:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            handlers.ForEach(it => it.OnScreenCreated());
                            break;
                        case ScreenState.Inactive:
                            handlers.ForEach(it => it.OnScreenBecomeActive());
                            break;
                    }
                    break;
                case ScreenState.Inactive:
                    switch (originalValue)
                    {
                        case ScreenState.Destroyed:
                            throw new ArgumentException("Destroyed screen cannot be paused");
                        case ScreenState.Active:
                            handlers.ForEach(it => it.OnScreenBecomeInactive());
                            break;
                    }

                    break;
            }
        }
    }

    protected List<ScreenHandler> handlers = new List<ScreenHandler>();

    private void Awake()
    {
        handlers = GetComponentsInChildren<ScreenHandler>().ToList(); // Including self
    }

    private void Update()
    {
        if (state == ScreenState.Active)
        {
            handlers.ForEach(it => it.OnScreenUpdate());
        }
    }

    public abstract string GetId();

    public virtual void OnScreenCreated() => Expression.Empty();
    
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