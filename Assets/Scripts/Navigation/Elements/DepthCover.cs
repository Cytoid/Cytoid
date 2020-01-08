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
        if (sprite != null)
        {
            if (image.sprite != null)
            {
                Destroy(image.sprite);
                image.sprite = null;
            }

            image.sprite = sprite;
            image.FitSpriteAspectRatio();
        }
        image.color = Color.white;
        mask.color = Color.black;
        mask.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
        transform.localScale = Vector3.one;
        transform.DOScale(1.05f, 3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        colorTweener = DOTween.To(() => image.color, color => image.color = color, Color.white, 3)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
    }

    public void OnScreenUpdate() => Expression.Empty();

    public void OnScreenBecameInactive()
    {
        mask.DOKill();
        mask.DOFade(0, 0.4f).SetEase(Ease.OutCubic);
        transform.DOKill();
        transform.DOScale(1f, 0.4f).SetEase(Ease.InOutFlash);
        colorTweener?.Kill();
        DOTween.To(() => image.color, color => image.color = color,
            Color.white, 0.4f).SetEase(Ease.InOutFlash);
    }

    public void OnScreenDestroyed()
    {
        transform.DOKill();
        colorTweener?.Kill();
    }
}