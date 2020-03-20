using System.Collections.Generic;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using UnityEngine;

public class MainTranslucentImage : SingletonMonoBehavior<MainTranslucentImage>, ScreenChangeListener
{
    public Camera uiCamera;
    public float baseScale = 1.0f;
    public static ParallaxElement ParallaxElement => ParallaxHolder.Instance.Target;
    [GetComponent] public TranslucentImage translucentImage;
    public bool hiddenOnStart = true;

    private void Start()
    {
        Context.ScreenManager.AddHandler(this);
        if (hiddenOnStart) translucentImage.SetAlpha(0);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Context.ScreenManager.RemoveHandler(this);
    }

    private static readonly List<string> OverlayScreenIds = new List<string> {SignInScreen.Id, ProfileScreen.Id};

    public void Initialize()
    {
        translucentImage.color = "#1e2129".ToColor();
        translucentImage.DOKill();
        translucentImage.DOFade(1, 1f).SetEase(Ease.OutCubic);
        ParallaxElement.gameObject.SetActive(true);
        ParallaxElement.Enabled = false;
        ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 0.9f, 0.4f).SetEase(Ease.OutCubic);
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from is GamePreparationScreen || from is TierSelectionScreen) {
            TranslucentImageSource.Disabled = false;
            uiCamera.gameObject.SetActive(true);
            ParallaxElement.gameObject.SetActive(true);
        }
        if (to is ResultScreen || to is TierBreakScreen || to is TierResultScreen)
        {
            TranslucentImageSource.Disabled = false;
            ParallaxElement.gameObject.SetActive(false);
            translucentImage.color = Color.black;
        }
        else if (to is MainMenuScreen)
        {
            ParallaxElement.Enabled = true;
            translucentImage.color = Color.black;
            translucentImage.DOKill();
            translucentImage.DOFade(0, 0.4f).SetEase(Ease.OutCubic);
            ParallaxElement.gameObject.SetActive(true);
            ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 1f, 0.4f).SetEase(Ease.OutCubic);
        }
        else if (!OverlayScreenIds.Contains(to.GetId())) {
            translucentImage.color = Color.black;
            translucentImage.DOKill();
            translucentImage.DOFade(1, 0.4f);
            ParallaxElement.gameObject.SetActive(true);
            ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 1.1f, 0.4f).SetEase(Ease.OutCubic);
        }
        else
        {
            translucentImage.color = Color.black;
            translucentImage.DOKill();
            translucentImage.DOFade(1, 0.4f);
            ParallaxElement.gameObject.SetActive(true);
            ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 0.98f, 0.4f).SetEase(Ease.OutCubic);
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (to is GamePreparationScreen || to is TierSelectionScreen)
        {
            TranslucentImageSource.Disabled = true;
            uiCamera.gameObject.SetActive(false);
            ParallaxElement.gameObject.SetActive(false);
        }
    }
}