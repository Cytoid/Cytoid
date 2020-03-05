using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class SpinnerOverlay : SingletonMonoBehavior<SpinnerOverlay>
{
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponentInChildren] public SpinnerElement spinnerElement;
    public Text message;
    
    private void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        spinnerElement.IsSpinning = true;
        message.text = "";
    }

    public static async void Show(Action onFullyShown = null)
    {
        Instance.Apply(it =>
        {
            it.message.text = "";
            it.canvasGroup.blocksRaycasts = true;
            it.canvasGroup.interactable = true;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(false);
        });
        if (onFullyShown != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            onFullyShown();
        }
    }

    public static async void Hide(Action onFullyHidden = null)
    {
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = false;
            it.canvasGroup.interactable = false;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(true);
        });
        if (onFullyHidden != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
            onFullyHidden();
        }
    }
}