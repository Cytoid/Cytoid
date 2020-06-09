using System;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;

public class WebViewOverlay : SingletonMonoBehavior<WebViewOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;

    public InteractableMonoBehavior closeButton;

    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.WebViewOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        closeButton.onPointerClick.AddListener(_ => Hide());
    }

    private int s = 3;
    private WebViewObject webView => WebViewHolder.Instance.webView;

    public void Open(string url)
    {
        webView.SetVisibility(true);
        var k = "";
        for (var i = 0; i < s; i++) k += "/";
        s++;
        webView.LoadURL($"https://cytoid.github.io/test{k}test.html");
        webView.EvaluateJS("location.reload()");
    }

    public void Close()
    {
        webView.SetVisibility(false);
        webView.EvaluateJS(@"FadeOut()");
    }
    
    public static async void Show(float duration = 0.4f, Action onFullyShown = null)
    {
        Instance.Apply(it =>
        {
            it.canvas.enabled = true;
            it.canvas.overrideSorting = true;
            it.canvas.sortingOrder = NavigationSortingOrder.WebViewOverlay;
            it.canvasGroup.enabled = true;
            it.canvasGroup.blocksRaycasts = true;
            it.canvasGroup.interactable = true;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(1, duration).SetEase(Ease.OutCubic);
            Context.SetMajorCanvasBlockRaycasts(false);
            it.Open("");
        });
        if (onFullyShown != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            onFullyShown();
        }
    }

    public static async void Hide(float duration = 0.4f, Action onFullyHidden = null) {
        Instance.Apply(it =>
        {
            it.canvasGroup.blocksRaycasts = false;
            it.canvasGroup.interactable = false;
            it.canvasGroup.DOKill();
            it.canvasGroup.DOFade(0, duration).SetEase(Ease.OutCubic);
            it.Close();
            Context.SetMajorCanvasBlockRaycasts(true);
        });
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        Instance.canvas.enabled = false;
        Instance.canvasGroup.enabled = false;
        if (onFullyHidden != null)
        {
            Instance.webView.EvaluateJS("location.reload()"); // Stop any embedded audio/video
            Instance.webView.SetVisibility(false);
            onFullyHidden();
        }
    }
}