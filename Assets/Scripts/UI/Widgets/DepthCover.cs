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

    private TweenerCore<Color, Color, ColorOptions> colorTweener;
    
    public void OnScreenInitialized() => Expression.Empty();

    public void OnScreenBecameActive()
    {
        mask.color = Color.black;
        image.color = Color.black;
    }

    public void OnCoverLoaded(Texture2D coverTexture)
    {
        if (image.sprite != null)
        {
            Destroy(image.sprite);
            image.sprite = null;
        }

        var sprite = coverTexture.CreateSprite();
        image.sprite = sprite;
        image.color = Color.white;
        image.GetComponent<AspectRatioFitter>().aspectRatio =
            coverTexture.width * 1.0f / coverTexture.height;
        mask.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
        transform.localScale = Vector3.one;
        transform.DOScale(1.05f, 3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        colorTweener = DOTween.To(() => image.color, color => image.color = color, Color.white, 3)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
    }

    public void OnScreenUpdate() => Expression.Empty();

    public void OnScreenBecameInactive()
    {
        mask.color = Color.black;
        transform.DOKill();
        transform.DOScale(1f, 0.4f).SetEase(Ease.InOutFlash);
        image.DOKill();
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