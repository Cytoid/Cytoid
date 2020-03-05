using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;

public class OpaqueOverlay : SingletonMonoBehavior<OpaqueOverlay>
{
    [GetComponent] public CanvasGroup canvasGroup;
    
    private void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
    
    public static async void Show(float duration = 0.8f, Action onFullyShown = null)
    {
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = true;
            it.canvasGroup.interactable = true;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(1, duration).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(false);
        });
        if (onFullyShown != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            onFullyShown();
        }
    }

    public static async void Hide(float duration = 0.8f, Action onFullyHidden = null)
    {
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = false;
            it.canvasGroup.interactable = false;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(0, duration).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(true);
        });
        if (onFullyHidden != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            onFullyHidden();
        }
    }
}