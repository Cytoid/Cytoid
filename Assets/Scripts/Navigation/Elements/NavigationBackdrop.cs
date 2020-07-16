using System;
using System.Collections.Generic;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class NavigationBackdrop : SingletonMonoBehavior<NavigationBackdrop>, ScreenChangeListener
{
    public Camera renderCamera;
    public Canvas renderCanvas;
    public RectTransform renderRectTransform;
    public TranslucentImage translucentImage;
    public TranslucentImageSource translucentImageSource;
    public Image translucentRawImageBackground;
    public RawImage translucentRawImage;
    public Image backdropOverlay;

    public bool ShouldParallaxActive { get; set; } = true;
    public bool IsVisible { get => isVisible; set => SetVisible(value); }
    public bool IsBlurred { get => isBlurred; set => SetBlurred(value); }
    public float Brightness
    {
        get => brightness;
        set
        {
            brightness = value;
            backdropOverlay.color = backdropOverlay.color.WithAlpha(1 - value);
        }
    }
    public float Scale
    {
        get => scale;
        set
        {
            SetScale(value);
        }
    }
    public float TransitionTime { get; set; } = 0.4f;

    public bool HighQuality
    {
        get => highQuality;
        set
        {
            highQuality = value;
            if (isBlurred)
            {
                SetBlurred(false, true);
                SetBlurred(true, true);
            }
        }
    }

    private bool isVisible;
    private bool isBlurred;
    private float brightness = 1;
    private bool highQuality;
    private float scale = 1;
    private float overlayOpacity;

    protected override async void Awake()
    {
        base.Awake();
        Brightness = 0;
        await UniTask.WaitUntil(() => Context.ScreenManager != null);
        Context.ScreenManager.AddHandler(this);
        SetVisible(false);
        translucentImage.enabled = false;
        translucentRawImageBackground.enabled = false;
        translucentRawImage.enabled = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Context.ScreenManager.RemoveHandler(this);
    }

    public void UpdateBlur()
    {
        if (!ShouldParallaxActive && !TranslucentCover.IsActive()) return;
        translucentImageSource.enabled = true;
        TranslucentImageSource.WillUpdate = true;
        if (!HighQuality)
        {
            translucentRawImage.texture = translucentImageSource.blurredScreen;
        }
    }

    private void Update()
    {
        if (Context.Player?.Settings == null) return;
        if (Context.Player.Settings.GraphicsQuality == GraphicsQuality.Ultra)
        {
            UpdateBlur();
        }
    }

    private DateTimeOffset blurToken;

    private async void SetBlurred(bool blurred, bool immediate = false)
    {
        if (isBlurred == blurred) return;
        Debug.Log($"[NavigationBackdrop] Blurred: {blurred}");
        isBlurred = blurred;
        var token = blurToken = DateTimeOffset.UtcNow;
        var target = HighQuality ? (Graphic) translucentImage : translucentRawImage;
        var disabled = HighQuality ? (Graphic) translucentRawImage : translucentImage;
        disabled.enabled = false;
        if (disabled == translucentRawImage) translucentRawImageBackground.enabled = false;
        if (!blurred)
        {
            ShouldParallaxActive = translucentImageSource.enabled = !TranslucentCover.IsActive();
            renderCanvas.enabled = true;
            target.DOKill();
            if (!immediate)
            {
                target.DOFade(0, TransitionTime);
            }
            else
            {
                target.color = target.color.WithAlpha(0);
            }
            if (target == translucentRawImage)
            {
                translucentRawImageBackground.DOKill();
                if (!immediate)
                {
                    translucentRawImageBackground.DOFade(0, TransitionTime);
                }
                else
                {
                    translucentRawImageBackground.SetAlpha(0);
                }
            }
            if (!immediate)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(TransitionTime));
                if (token != blurToken) return;
            }
            target.enabled = false;
            if (target == translucentRawImage) translucentRawImageBackground.enabled = false;
        }
        else
        {
            UpdateBlur();
            target.enabled = true;
            target.DOKill();
            if (!immediate)
            {
                target.DOFade(1, TransitionTime);
            }
            else
            {
                target.SetAlpha(1);
            }

            if (target == translucentRawImage)
            {
                translucentRawImageBackground.enabled = true;
                translucentRawImageBackground.DOKill();
                if (!immediate)
                {
                    translucentRawImageBackground.DOFade(1, TransitionTime);
                }
                else
                {
                    translucentRawImageBackground.SetAlpha(1);
                }
                if (!immediate)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(TransitionTime));
                    if (token != blurToken) return;
                }
                if (!immediate) ShouldParallaxActive = translucentImageSource.enabled = false; // Dirty hack
            }
        }
    }

    private void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        Debug.Log($"[NavigationBackdrop] Visible: {visible}");
        isVisible = visible;
        renderCanvas.enabled = visible;
        renderCamera.enabled = visible;
        ParallaxHolder.Instance.Target.gameObject.SetActive(visible);
    }

    private void SetScale(float value, float duration = 0.4f)
    {
        if (scale == value) return;
        scale = value;
        if (duration == 0)
        {
            renderRectTransform.localScale = new Vector3(value, value, 1);
        }
        else
        {
            renderRectTransform.DOScale(scale, duration).OnUpdate(UpdateBlur);
        }
    }

    public void FadeBrightness(float value, float duration = 0.4f, Action onComplete = default, Action onUpdate = default)
    {
        if (onComplete == default) onComplete = () => { };
        if (onUpdate == default) onUpdate = () => { };
        backdropOverlay.DOKill();
        backdropOverlay.DOFade(1 - value, duration).SetEase(Ease.OutCubic).OnUpdate(() => onUpdate()).OnComplete(() => onComplete());
    }
    
    private static readonly HashSet<string> BlockedScreenIds = new HashSet<string>
    {
        CollectionDetailsScreen.Id,
        EventSelectionScreen.Id,
        GamePreparationScreen.Id,
        TierSelectionScreen.Id,
    };
    private static readonly HashSet<string> ClearScreenIds = new HashSet<string>
    {
        MainMenuScreen.Id
    };

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from == null) return;
        if (BlockedScreenIds.Contains(from.GetId()) && !BlockedScreenIds.Contains(to.GetId()))
        {
            SetVisible(true);
        }
        if (!ClearScreenIds.Contains(from.GetId()) && ClearScreenIds.Contains(to.GetId()))
        {
            SetBlurred(false);
        }
        else if (ClearScreenIds.Contains(from.GetId()) && !ClearScreenIds.Contains(to.GetId()))
        {
            SetBlurred(true);
        }
        if (from is InitializationScreen && to is MainMenuScreen)
        {
            FadeBrightness(1f, 0.2f);
        }
        if (from is MainMenuScreen && !(to is MainMenuScreen))
        {
            SetScale(1.1f);
        }
        if (!(from is MainMenuScreen) && to is MainMenuScreen)
        {
            SetScale(1f);
        }
        if (from is CharacterSelectionScreen && !(to is CharacterSelectionScreen))
        {
            ShouldParallaxActive = translucentImageSource.enabled = false;
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (to == null) return;
        if (BlockedScreenIds.Contains(to.GetId()))
        {
            SetVisible(false);
        }
        
        if (to is InitializationScreen)
        {
            ShouldParallaxActive = true;
            SetVisible(true);
            SetBlurred(true);
            SetScale(0.98f, 0);
            FadeBrightness(0.5f, 2);
        }

        if (to is ResultScreen || to is TierBreakScreen || to is TierResultScreen)
        {
            ShouldParallaxActive = true;
            SetVisible(true);
            SetBlurred(true);
        }

        if (to is CharacterSelectionScreen)
        {
            ShouldParallaxActive = translucentImageSource.enabled = true;
        }
    }
}