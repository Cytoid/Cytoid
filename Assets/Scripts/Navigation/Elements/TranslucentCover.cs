using System;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class TranslucentCover : SingletonMonoBehavior<TranslucentCover>
{
    private Image image;
    private static CancellationTokenSource asyncToken;

    protected override void Awake()
    {
        base.Awake();
        image = GetComponentInChildren<Image>();
    }

    public static void LightMode()
    {
        Instance.image.color = Color.white.WithAlpha(Instance.image.color.a);
    }

    public static void DarkMode()
    {
        Instance.image.color = Color.black.WithAlpha(Instance.image.color.a);
    }

    public static void SetSprite(Sprite sprite)
    {
        Instance.image.Apply(it =>
        {
            it.sprite = sprite;
            it.FitSpriteAspectRatio();
        });
        
        MainTranslucentImage.Instance.WillUpdateTranslucentImage();
    }

    public static void Show(float toAlpha = 1, float fadeDuration = 0.4f)
    {
        asyncToken?.Cancel();
        Instance.image.Apply(async it =>
        {
            it.enabled = true;
            it.DOKill();
            if (fadeDuration > 0)
            {
                it.DOFade(toAlpha, fadeDuration);
                asyncToken = new CancellationTokenSource();
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), cancellationToken: asyncToken.Token);
                }
                catch
                {
                    return;
                }
            }
            else
            {
                it.SetAlpha(toAlpha);
            }
            MainTranslucentImage.Instance.WillUpdateTranslucentImage();
        });
    }

    public static void Hide(float fadeDuration = 0.4f, bool clearSprite = true  )
    {
        asyncToken?.Cancel();
        Instance.image.Apply(async it =>
        {
            it.DOKill();
            if (fadeDuration > 0)
            {
                it.DOFade(0, fadeDuration);
                asyncToken = new CancellationTokenSource();
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), cancellationToken: asyncToken.Token);
                }
                catch
                {
                    return;
                }
            }
            else
            {
                it.SetAlpha(0);
            }

            if (clearSprite) it.sprite = null;
            MainTranslucentImage.Instance.WillUpdateTranslucentImage();

            it.enabled = false;
        });
    }
    
}