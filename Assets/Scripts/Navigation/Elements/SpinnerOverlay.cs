using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class SpinnerOverlay : SingletonMonoBehavior<SpinnerOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponentInChildren] public SpinnerElement spinnerElement;
    public Text message;
    
    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.SpinnerOverlay;
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
            it.canvas.enabled = true;
            it.canvas.overrideSorting = true;
            it.canvas.sortingOrder = NavigationSortingOrder.SpinnerOverlay;
            it.canvasGroup.enabled = true;
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
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        Instance.canvas.enabled = false;
        Instance.canvasGroup.enabled = false;
        if (onFullyHidden != null)
        {
            onFullyHidden();
        }
    }
    
    public static void OnLevelInstallProgress(string fileName, int current, int total)
    {
        Instance.message.text = total > 1
            ? "INIT_UNPACKING_X_Y".Get(fileName, current, total)
            : "INIT_UNPACKING_X".Get(fileName);
    }
    
    public static void OnLevelLoadProgress(string levelId, int current, int total)
    {
        Instance.message.text = "INIT_LOADING_X_Y".Get(levelId, current, total);
    }
}