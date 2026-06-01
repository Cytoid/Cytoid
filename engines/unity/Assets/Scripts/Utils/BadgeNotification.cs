using DG.Tweening;
using UnityEngine;

public class BadgeNotification : MonoBehaviour, ScreenBecameActiveListener
{
    public CanvasGroup canvasGroup;

    private void OnValidate()
    {
        this.AutoFill(ref canvasGroup);
    }

    private void Awake()
    {
        this.AutoFill(ref canvasGroup);
    }

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