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
    public bool scaleOnClick;
    public float scaleToOnClick = 0.9f;
    public bool pulseOnClick;
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (pulseOnClick) GetComponent<PulseElement>()?.Pulse();
        Context.ScreenManager.ChangeScreen(navigateToLastScreen ? Context.ScreenManager.GetLastScreenId() : targetScreenId, transition, duration, currentScreenDelay, newScreenDelay, transitionFocus, OnScreenChanged);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (scaleOnClick) transform.DOScale(scaleToOnClick, 0.2f).SetEase(Ease.OutCubic);
    }
        
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (scaleOnClick) transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

    protected virtual void OnScreenChanged(Screen screen) => Expression.Empty();
    
}