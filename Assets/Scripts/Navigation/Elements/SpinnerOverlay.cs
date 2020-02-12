using System;
using DG.Tweening;
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

    public static void Show()
    {
        Instance.Apply(it =>
        {
            it.message.text = "";
            it.canvasGroup.blocksRaycasts = true;
            it.canvasGroup.interactable = true;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
        });
    }

    public static void Hide()
    {
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = false;
            it.canvasGroup.interactable = false;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
        });
    }
}