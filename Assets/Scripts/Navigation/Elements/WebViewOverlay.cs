using System;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WebViewOverlay : SingletonMonoBehavior<WebViewOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    [GetComponentInChildren] public SpinnerElement spinnerElement;

    public InteractableMonoBehavior closeButton;
    public Action onFullyHidden = null;

    private DateTime loadToken;

    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.WebViewOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        spinnerElement.IsSpinning = false;
        closeButton.onPointerClick.AddListener(_ => Hide());
    }

    private WebViewObject webView => WebViewHolder.Instance.webView;

    public async void Open(string url)
    {
        webView.SetVisibility(true);
        webView.SetURLPattern("Never!", "Never!", "^((?!artifacts\\.cytoid\\.io|w\\.soundcloud|services\\.cytoid\\.io|artifacts\\.cytoid\\.cn|api\\.cytoid\\.cn|EmbeddedPlayer|embed|outchain|player\\.bilibili|blackboard\\/html5).)*$");
        webView.LoadURL(url);
        spinnerElement.IsSpinning = true;
        var token = loadToken = new DateTime();
        void OnLoaded()
        {
            if (loadToken != token) return;
            spinnerElement.IsSpinning = false;
        }
        WebViewHolder.Instance.OnWebViewLoaded.AddListener(loadedUrl =>
        {
            if (loadedUrl != url) return;
            OnLoaded();
        });
        if (Application.isEditor)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            OnLoaded();
        }
    }

    public void Close()
    {
        // webView.EvaluateJS(@"FadeOut()");
    }
    
    public static async void Show(string url, float duration = 0.4f, Action onFullyShown = null, Action onFullyHidden = null)
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
            it.Open(url);
        });
        if (onFullyShown != null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            onFullyShown();
        }
        Instance.onFullyHidden = onFullyHidden;
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
        Instance.webView.LoadURL("about:blank");
        Instance.webView.LoadURL("about:blank");
        Instance.webView.SetVisibility(false); // TODO: Maybe fix this... or maybe not ¯\_(ツ)_/¯
        await UniTask.Delay(TimeSpan.FromSeconds(duration)); 
        Instance.canvas.enabled = false;
        Instance.canvasGroup.enabled = false;
        if (onFullyHidden == null) onFullyHidden = Instance.onFullyHidden;
        onFullyHidden?.Invoke();
    }
}