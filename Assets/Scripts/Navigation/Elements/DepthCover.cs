using System.Linq.Expressions;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

public class DepthCover : MonoBehaviour, ScreenListener
{
    [GetComponentInChildrenName] public Image mask;
    [GetComponentInChildrenName] public Image image;
    [GetComponent] public PulseElement pulseElement;
    public Color maxColor = Color.white;

    private TweenerCore<Color, Color, ColorOptions> colorTweener;
    
    public void OnScreenInitialized() => Expression.Empty();

    public void OnScreenBecameActive()
    {
        mask.color = Color.black;
        image.color = Color.black;
    }

    public void OnCoverLoaded(Sprite sprite)
    {
        transform.DOKill();
        mask.DOKill();
        if (sprite != null && sprite.texture != null)
        {
            image.sprite = sprite;
            image.FitSpriteAspectRatio();
        }
        image.color = maxColor;
        mask.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
        transform.localScale = Vector3.one;
        transform.DOScale(1.05f, 3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        colorTweener = DOTween.To(() => image.color, color => image.color = color, maxColor, 3)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
    }

    public void OnCoverUnloaded()
    {
        mask.DOKill();
        mask.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
        transform.DOKill();
        transform.DOScale(1f, 0.4f).SetEase(Ease.InOutFlash);
        colorTweener?.Kill();
    }

    public void OnScreenUpdate() => Expression.Empty();

    public void OnScreenBecameInactive()
    {
        mask.DOKill();
        mask.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
        transform.DOKill();
        transform.DOScale(1f, 0.4f).SetEase(Ease.InOutFlash);
        colorTweener?.Kill();
        DOTween.To(() => image.color, color => image.color = color,
            maxColor, 0.4f).SetEase(Ease.InOutFlash);
    }

    public void OnScreenDestroyed()
    {
        transform.DOKill();
        colorTweener?.Kill();
    }
}