using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuCardButton : NavigationElement
{
    public RectTransform parent;

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (Context.ScreenManager.IsScreenCreated(targetScreenId))
        {
            transitionFocus = parent.GetScreenSpaceCenter();
            base.OnPointerClick(eventData);
        }
        else
        {
            var dialog = Dialog.Instantiate();
            dialog.UseNegativeButton = false;
            dialog.UsePositiveButton = true;
            dialog.Message = "Coming soon!";
            dialog.Open();
        }
    }
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        parent.DOScale(0.95f, 0.2f).SetEase(Ease.OutCubic);
    }
        
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        parent.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }
}