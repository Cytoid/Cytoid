using System.Collections.Generic;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using UniRx.Async;
using UnityEngine;

/**
 * TODO: Yeah one day I will refactor this.
 */
public class MainTranslucentImage : SingletonMonoBehavior<MainTranslucentImage>, ScreenChangeListener
{
    public Camera uiCamera;
    public float baseScale = 1.0f;
    public static ParallaxElement ParallaxElement => ParallaxHolder.Instance.Target;
    [GetComponent] public TranslucentImage translucentImage;
    public bool hiddenOnStart = true;

    public static bool Static = false;

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
    private static readonly List<string> BlockedScreenIds = new List<string> {GamePreparationScreen.Id, TierSelectionScreen.Id};

    public void Initialize()
    {
        translucentImage.color = Color.black;
        translucentImage.DOKill();
        translucentImage.DOFade(1, 2f).SetEase(Ease.OutCubic);
        ParallaxElement.gameObject.SetActive(true);
        ParallaxElement.Enabled = false;
        ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 0.9f, 0.4f).SetEase(Ease.OutCubic);
        uiCamera.gameObject.SetActive(true);
        WillUpdateTranslucentImage();
    }

    public void WillUpdateTranslucentImage()
    {
        TranslucentImageSource.WillUpdate = true;
    }
    
    private void Update()
    {
        if (Context.ScreenManager == null || Context.ScreenManager.ActiveScreenId == null) return;
        var activeScreenId = Context.ScreenManager.ActiveScreenId;
        if (!Static && !BlockedScreenIds.Contains(activeScreenId) && activeScreenId != MainMenuScreen.Id)
        {
            TranslucentImageSource.WillUpdate = true;
        }
    }

    public async void OnScreenChangeStarted(Screen from, Screen to)
    {
        WillUpdateTranslucentImage();
        if (from != null && BlockedScreenIds.Contains(from.GetId())) {
            uiCamera.gameObject.SetActive(true);
            ParallaxElement.gameObject.SetActive(true);
        }
        if (to is ResultScreen || to is TierBreakScreen || to is TierResultScreen)
        {
            uiCamera.gameObject.SetActive(true);
            ParallaxElement.gameObject.SetActive(false);
            translucentImage.color = Color.black;
        }
        else if (to is MainMenuScreen)
        {
            uiCamera.gameObject.SetActive(true);
            ParallaxElement.Enabled = true;
            translucentImage.color = Color.black;
            translucentImage.DOKill();
            translucentImage.DOFade(0, 0.4f).SetEase(Ease.OutCubic);
            ParallaxElement.gameObject.SetActive(true);
            ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 1f, 0.4f).SetEase(Ease.OutCubic);
        }
        else {
            translucentImage.color = Color.black;
            translucentImage.DOKill();
            translucentImage.DOFade(1, 0.4f);
            if (Static)
            {
                uiCamera.gameObject.SetActive(true);
                ParallaxElement.gameObject.SetActive(true);
                await UniTask.DelayFrame(0);
                uiCamera.gameObject.SetActive(false);
                ParallaxElement.gameObject.SetActive(false);
            }
            else
            {
                ParallaxElement.gameObject.SetActive(true);
                ParallaxElement.GetComponent<RectTransform>().DOScale(baseScale * (OverlayScreenIds.Contains(to.GetId()) ? 0.98f : 1.1f), 0.4f).SetEase(Ease.OutCubic);
            }
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (to != null && BlockedScreenIds.Contains(to.GetId()))
        {
            uiCamera.gameObject.SetActive(false);
            ParallaxElement.gameObject.SetActive(false);
        }
    }
}