using System.Linq.Expressions;
using UnityEngine.EventSystems;

public class NavigationElement : InteractableMonoBehavior
{
    public string targetScreenId;
    public ScreenTransition transition;
    public float duration;
    public float transitionDelay;
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Context.screenManager.ChangeScreen(targetScreenId, transition, duration, transitionDelay, null, targetScreen => OnScreenChanged(targetScreen));
    }

    protected virtual void OnScreenChanged(Screen screen) => Expression.Empty();
    
}