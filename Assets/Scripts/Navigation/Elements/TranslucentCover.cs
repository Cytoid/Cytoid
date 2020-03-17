using System;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class TranslucentCover : SingletonMonoBehavior<TranslucentCover>
{
    private Image image;
    private static CancellationTokenSource hideToken;

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
    }

    public static void Show(float toAlpha = 1, float fadeDuration = 0.8f)
    {
        hideToken?.Cancel();
        Instance.image.Apply(it =>
        {
            it.enabled = true;
            it.DOKill();
            it.DOFade(toAlpha, fadeDuration);
        });
    }

    public static void Hide(float fadeDuration = 0.8f)
    {
        Instance.image.Apply(async it =>
        {
            it.DOKill();
            it.DOFade(0, fadeDuration);
            hideToken = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(fadeDuration), cancellationToken: hideToken.Token);
            }
            catch
            {
                return;
            }

            it.enabled = false;
        });
    }
    
}