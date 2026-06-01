using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class OpaqueOverlay : SingletonMonoBehavior<OpaqueOverlay>
{
    private const int SortingOrder = 181;

    public Canvas canvas;
    public CanvasGroup canvasGroup;

    private void OnValidate()
    {
        this.AutoFill(ref canvas);
        this.AutoFill(ref canvasGroup);
    }

    private void Awake()
    {
        this.AutoFill(ref canvas);
        this.AutoFill(ref canvasGroup);
    }

    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public static async void Show(float duration = 0.8f, Action onFullyShown = null)
    {
        Instance.Apply(it =>
        {
            it.canvas.enabled = true;
            it.canvas.overrideSorting = true;
            it.canvas.sortingOrder = SortingOrder;
            it.canvasGroup.enabled = true;
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
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        Instance.canvas.enabled = false;
        Instance.canvasGroup.enabled = false;
        onFullyHidden?.Invoke();
    }
}
