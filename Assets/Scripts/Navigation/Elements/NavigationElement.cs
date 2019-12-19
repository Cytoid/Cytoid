using System.Linq.Expressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class NavigationElement : InteractableMonoBehavior
{
    public bool navigateToLastScreen;
    public string targetScreenId;
    public ScreenTransition transition;
    public float duration;
    public float currentScreenDelay;
    public float newScreenDelay;
    public Vector2 transitionFocus;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Context.ScreenManager.ChangeScreen(navigateToLastScreen ? Context.ScreenManager.GetLastScreenId() : targetScreenId, transition, duration, currentScreenDelay, newScreenDelay, transitionFocus, OnScreenChanged);
    }
    
    protected virtual void OnScreenChanged(Screen screen) => Expression.Empty();
    
}