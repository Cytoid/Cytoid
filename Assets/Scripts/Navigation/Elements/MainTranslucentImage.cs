using System.Collections.Generic;
using System.Linq.Expressions;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using UnityEngine;

public class MainTranslucentImage : SingletonMonoBehavior<MainTranslucentImage>, ScreenChangeListener
{
    public float baseScale = 1.0f;
    public ParallaxElement parallaxElement;
    [GetComponent] public TranslucentImage translucentImage;
    public bool hiddenOnStart = true;

    private void Start()
    {
        Context.ScreenManager.AddHandler(this);
        if (hiddenOnStart) translucentImage.SetAlpha(0);
    }

    private static readonly List<string> OverlayScreenIds = new List<string> {SignInScreen.Id, ProfileScreen.Id};
    
    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (to.GetId() == ResultScreen.Id)
        {
            parallaxElement.gameObject.SetActive(false);
            translucentImage.color = Color.black;
        }
        else if (to.GetId() == MainMenuScreen.Id)
        {
            translucentImage.DOFade(0, 0.4f).SetEase(Ease.OutCubic);
            parallaxElement.gameObject.SetActive(true);
            parallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 1f, 0.4f).SetEase(Ease.OutCubic);
        } else if (!OverlayScreenIds.Contains(to.GetId())) {
            translucentImage.DOFade(1, 0.4f);
            parallaxElement.gameObject.SetActive(true);
            parallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 1.1f, 0.4f).SetEase(Ease.OutCubic);
        }
        else
        {
            translucentImage.DOFade(1, 0.4f);
            parallaxElement.gameObject.SetActive(true);
            parallaxElement.GetComponent<RectTransform>().DOScale(baseScale * 0.98f, 0.4f).SetEase(Ease.OutCubic);
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to) => Expression.Empty();
}