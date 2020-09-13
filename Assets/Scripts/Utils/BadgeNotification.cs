using DG.Tweening;
using UnityEngine;

public class BadgeNotification : MonoBehaviour, ScreenBecameActiveListener
{
    [GetComponent] public CanvasGroup canvasGroup;
    
    public void OnScreenBecameActive()
    {
        canvasGroup.alpha = 0;
    }

    public void Show()
    {
        canvasGroup.DOFade(1, 0.4f);
    }

    public void Hide()
    {
        canvasGroup.DOFade(0, 0.4f);
    }
}