using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.EventSystems;

public class NavigationElement : InteractableMonoBehavior
{
    public string targetScreenId;
    public ScreenTransition transition;
    public float duration;
    public float transitionDelay;
    public Vector2 transitionFocus;
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Context.ScreenManager.ChangeScreen(targetScreenId, transition, duration, transitionDelay, transitionFocus, targetScreen => OnScreenChanged(targetScreen));
    }

    protected virtual void OnScreenChanged(Screen screen) => Expression.Empty();
    
}