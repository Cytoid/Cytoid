using System.Linq.Expressions;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

public class DepthCover : MonoBehaviour, ScreenEventListener
{
    [GetComponent] public Image image;

    private TweenerCore<Color, Color, ColorOptions> colorTweener;
    
    public void OnScreenInitialized() => Expression.Empty();

    public void OnScreenBecomeActive()
    {
        transform.localScale = Vector3.one;
        transform.DOScale(1.05f, 3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        image.color = new Color(1f, 1f, 1f, 0.8f);
        colorTweener = DOTween.To(() => image.color, color => image.color = color,
            Color.white, 3).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
    }

    public void OnScreenUpdate() => Expression.Empty();

    public void OnScreenBecomeInactive()
    {
        transform.DOKill();
        transform.DOScale(1f, 0.4f).SetEase(Ease.InOutFlash);
        colorTweener?.Kill();
        DOTween.To(() => image.color, color => image.color = color,
            Color.white, 0.4f).SetEase(Ease.InOutFlash);
    }

    public void OnScreenDestroyed()
    {
        transform.DOKill();
    }
}