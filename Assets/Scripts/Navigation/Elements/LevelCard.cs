using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using MoreMountains.NiceVibrations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelCard : InteractableMonoBehavior
{
    public Image cover;
    public Text artist;
    public Text title;
    public Text titleLocalized;
    public RectTransform ownerRoot;
    public Avatar ownerAvatar;
    public Text ownerName;

    public bool isStatic;
    public List<DifficultyBall> difficultyBalls = new List<DifficultyBall>();

    public CanvasGroup actionOverlay;
    public Image actionIcon;
    public GradientMeshEffect actionGradient;

    private LevelView levelView;
    public Level Level
    {
        get => levelView?.Level;
        set
        {
            if (levelView == null) throw new InvalidOperationException();
            levelView.Level = value;
        }
    }
    private bool loadedCover;
    private CancellationTokenSource actionToken;
    private Vector2 pressPosition;

    private Screen screenParent;
    private bool addedScreenListeners;
    private LevelCardState currentState = LevelCardState.Normal;

    private void AddScreenListeners()
    {
        screenParent = this.GetScreenParent();
        if (screenParent != null && !addedScreenListeners)
        {
            addedScreenListeners = true;
            screenParent.onScreenRendered.AddListener(OnScreenRendered);
            screenParent.onScreenBecameInactive.AddListener(OnScreenBecameInactive);
            screenParent.onScreenUpdate.AddListener(OnScreenUpdate);
        }
    }
    
    private void RemoveScreenListeners()
    {
        if (addedScreenListeners)
        {
            addedScreenListeners = false;
            screenParent.onScreenRendered.RemoveListener(OnScreenRendered);
            screenParent.onScreenBecameInactive.RemoveListener(OnScreenBecameInactive);
            screenParent.onScreenUpdate.RemoveListener(OnScreenUpdate);
        }
        screenParent = null;
    }
    
    public void ScrollCellContent(object levelObject)
    {
        AddScreenListeners();
        SetModel((LevelView) levelObject);
    }

    public void ScrollCellReturn()
    {
        loadedCover = false;
        if (coverToken != null && !coverToken.IsCancellationRequested)
        {
            coverToken.Cancel();
            coverToken = null;
        }

        cover.sprite = null;
        cover.DOKill();
        cover.SetAlpha(0);
        currentState = LevelCardState.Normal;
        actionOverlay.gameObject.SetActive(false);
        actionStateAnimation?.Kill();

        RemoveScreenListeners();
    }

    private void Awake()
    {
        Unload();
        AddScreenListeners();
    }

    public void Unload()
    {
        artist.text = "";
        title.text = "";
        titleLocalized.text = "";
        for (var index = 0; index < 3; index++) difficultyBalls[index].gameObject.SetActive(false);
        ownerRoot.gameObject.SetActive(false);
        ownerAvatar.Dispose();
        cover.sprite = null;
        cover.DOKill();
        cover.SetAlpha(0);
    }

    private void OnDestroy()
    {
        actionToken?.Cancel();
        coverToken?.Cancel();
        if (addedScreenListeners)
        {
            addedScreenListeners = false;
            screenParent.onScreenRendered.RemoveListener(OnScreenRendered);
            screenParent.onScreenBecameInactive.RemoveListener(OnScreenBecameInactive);
        }
    }

    private void OnScreenRendered()
    {
        // Texture could be collected, or we didn't load at all, so let's try to load again
        LoadCover();
    }

    private void OnScreenUpdate()
    {
        SetActionState(true);
    }

    private void OnScreenBecameInactive()
    {
        actionToken?.Cancel();
        coverToken?.Cancel();
    }

    public void SetModel(Level level)
    {
        SetModel(new LevelView {Level = level});
    }

    public void SetModel(LevelView view)
    {
        levelView = view;

        artist.text = Level.Meta.artist;
        title.text = Level.Meta.title;
        titleLocalized.text = Level.Meta.title_localized;
        titleLocalized.gameObject.SetActive(!Level.Meta.title_localized.IsNullOrEmptyTrimmed());

        for (var index = 0; index < 3; index++)
        {
            if (index <= Level.Meta.charts.Count - 1)
            {
                var chart = Level.Meta.charts[index];
                difficultyBalls[index].gameObject.SetActive(true);
                difficultyBalls[index].SetModel(Difficulty.Parse(chart.type), chart.difficulty);
            }
            else
            {
                difficultyBalls[index].gameObject.SetActive(false);
            }
        }
        
        if (levelView.DisplayOwner && Level.OnlineLevel?.Owner != null && Level.OnlineLevel?.Owner.Uid != Context.OfficialAccountId)
        {
            ownerRoot.gameObject.SetActive(true);
            ownerAvatar.action = AvatarAction.OpenProfile;
            ownerAvatar.SetModel(Level.OnlineLevel.Owner);
            ownerName.text = Level.OnlineLevel.Owner.Uid;
        }
        else
        {
            ownerRoot.gameObject.SetActive(false);
            ownerAvatar.Dispose();
            ownerName.text = "";
        }

        SetActionState(false);

        LayoutFixer.Fix(transform, count: 2);

        loadedCover = false;
        LoadCover();
    }

    private CancellationTokenSource coverToken;

    public async void LoadCover()
    { 
        if (loadedCover && cover.sprite != null && cover.sprite.texture != null) return;
        
        cover.DOKill();
        cover.SetAlpha(0);

        if (coverToken != null)
        {
            if (!coverToken.IsCancellationRequested) coverToken.Cancel();
            coverToken = null;
        }
        
        /*if (DoNotLoadCover)
        {
            coverToken = new CancellationTokenSource();
            try
            {
                await UniTask.WaitUntil(() => !DoNotLoadCover, cancellationToken: coverToken.Token);
            }
            catch
            {
                return;
            }
        }*/

        if (!((RectTransform) transform).IsVisible())
        {
            coverToken = new CancellationTokenSource();
            try
            {
                await UniTask.WaitUntil(() => this == null || transform == null || ((RectTransform) transform).IsVisible(), cancellationToken: coverToken.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (this == null || transform == null) return;
        }
        
        coverToken = new CancellationTokenSource();
        try
        {
            await UniTask.DelayFrame(0, cancellationToken: coverToken.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        coverToken = new CancellationTokenSource();
        Sprite sprite = null;
        try
        {
            // Debug.Log($"LevelCard {GetHashCode()}: Loading " + level.Id);
            // DebugGUI.Log($"LevelCard {GetHashCode()}: Loading " + level.Id);
            var width = 576;
            var height = 360;
            if (Context.Player.Settings.GraphicsQuality <= GraphicsQuality.Medium)
            {
                if (Context.Player.Settings.GraphicsQuality <= GraphicsQuality.Low)
                {
                    width = 288;
                    height = 180;
                }
                else
                {
                    width = 432;
                    height = 270;
                }
            }
            
            // It's possible that this level now has a local version
            if (!Level.IsLocal && Level.OnlineLevel.HasLocal(LevelType.User))
            {
                Level = Level.OnlineLevel.ToLevel(LevelType.User);
            }

            if (Level.IsLocal)
            {
                var path = "file://" + Level.Path + LevelManager.CoverThumbnailFilename;
                try
                {
                    sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.LocalLevelCoverThumbnail,
                        coverToken.Token, options: new SpriteAssetOptions(new[] {width, height}));
                }
                catch
                {
                    return;
                }
            }
            else
            {
                try
                {
                    var path = Level.OnlineLevel.Cover.ThumbnailUrl;
                    sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.RemoteLevelCoverThumbnail,
                        coverToken.Token,
                        new SpriteAssetOptions(new[] {width, height}));
                }
                catch
                {
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            if (sprite != null)
            {
                // Should be impossible
                Destroy(sprite.texture);
                Destroy(sprite);
            }

            return;
        }
        
        if (this == null || transform == null) return;

        if (sprite != null)
        {
            lock (sprite)
            {
                if (sprite != null)
                {
                    if (cover == null) return;
                    cover.sprite = sprite;
                    cover.DOFade(0.7f, 0.2f);
                    cover.FitSpriteAspectRatio();
                    loadedCover = true;
                }
            }
        }

        coverToken = null;
    }

    private bool ignoreNextPointerUp;

    public override async void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (isStatic) return;
        
        pressPosition = eventData.position;
        if (loadedCover) cover.DOFade(1.0f, 0.2f).SetEase(Ease.OutCubic);
        // cover.rectTransform.DOScale(1.02f, 0.2f).SetEase(Ease.OutCubic);
        actionToken?.Cancel();
        actionToken = new CancellationTokenSource();
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.8f), cancellationToken: actionToken.Token);
        }
        catch
        {
            // ignored
            return;
        }

        if (transform == null) return; // Transform destroyed?
        ignoreNextPointerUp = true;
        OnPointerUp(eventData);
        OnLongPress();
        actionToken = null;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (isStatic)
        {
            base.OnPointerUp(eventData);
            return;
        }
        
        var d = Vector2.Distance(pressPosition, eventData.position);
        if (d > 0.005f * Context.ReferenceWidth || ignoreNextPointerUp)
        {
            ignoreNextPointerUp = false;
            IsPointerDown = false;
        }

        actionToken?.Cancel();
        base.OnPointerUp(eventData);
        if (loadedCover) cover.DOFade(0.7f, 0.2f).SetEase(Ease.OutCubic);
        // cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (isStatic) return;
        if (Level != null)
        {
            if (Context.ScreenManager.IsChangingScreen)
            {
                return;
            }

            var openLevel = false;
            var screen = Context.ScreenManager.ActiveScreen;
            if (screen is LevelCardEventHandler handler)
            {
                if (handler.OnLevelCardPressed(levelView))
                {
                    openLevel = true;
                }
            }
            else
            {
                openLevel = true;
            }

            if (openLevel)
            {
                Context.AudioManager.Get("Navigate2").Play();
                Context.Haptic(HapticTypes.MediumImpact, true);

                Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In, 0.4f,
                    transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter(),
                    payload: new GamePreparationScreen.Payload {Level = levelView.Level});
            }
        }
    }

    private Sequence actionStateAnimation;

    private void SetActionState(bool animate)
    {
        if (!(screenParent is LevelBatchSelection batchSelection)) return;
        
        var toState = LevelCardState.Normal;
        if (batchSelection.IsBatchSelectingLevels)
        {
            switch (batchSelection.LevelBatchAction)
            {
                case LevelBatchAction.Delete:
                    if (batchSelection.BatchSelectedLevels.ContainsKey(levelView.Level.Id))
                    {
                        toState = LevelCardState.WillDelete;
                    }

                    break;
                case LevelBatchAction.Download:
                    if (Context.LevelManager.LoadedLocalLevels.ContainsKey(levelView.Level.Id))
                    {
                        toState = LevelCardState.Downloaded;
                    }
                    else if (batchSelection.BatchSelectedLevels.ContainsKey(levelView.Level.Id))
                    {
                        toState = LevelCardState.WillDownload;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (toState != currentState)
        {
            currentState = toState;

            switch (currentState)
            {
                case LevelCardState.WillDelete:
                    actionIcon.sprite = NavigationUiElementProvider.Instance.levelActionOverlayDeleteIcon;
                    actionGradient.SetGradient(NavigationUiElementProvider.Instance.levelActionOverlayDeleteGradient.GetGradient());
                    break;
                case LevelCardState.WillDownload:
                    actionIcon.sprite = NavigationUiElementProvider.Instance.levelActionOverlayDownloadIcon;
                    actionGradient.SetGradient(NavigationUiElementProvider.Instance.levelActionOverlayDownloadGradient.GetGradient());
                    break;
                case LevelCardState.Downloaded:
                    actionIcon.sprite = NavigationUiElementProvider.Instance.levelActionOverlayDownloadedIcon;
                    actionGradient.SetGradient(NavigationUiElementProvider.Instance.levelActionOverlayDownloadedGradient.GetGradient());
                    break;
            }
            
            if (currentState != LevelCardState.Normal)
            {
                actionOverlay.alpha = 0;
                actionOverlay.gameObject.SetActive(true);
                if (animate)
                {
                    actionOverlay.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
                    if (currentState == LevelCardState.WillDelete || currentState == LevelCardState.WillDownload)
                    {
                        actionStateAnimation?.Kill();
                        actionStateAnimation = DOTween.Sequence()
                            .Append(actionIcon.rectTransform.DOScale(0.95f, 0.1f))
                            .Append(actionIcon.rectTransform.DOScale(1f, 0.1f));
                    }
                }
                else
                {
                    actionOverlay.alpha = 1;
                    actionIcon.rectTransform.SetLocalScale(1);
                }
            }
            else
            {
                if (animate)
                {
                    actionOverlay.DOFade(0, 0.2f).SetEase(Ease.OutCubic);
                }
                else
                {
                    actionOverlay.gameObject.SetActive(false);
                }
            }
        }
    }

    public void OnLongPress()
    {
        var screen = Context.ScreenManager.ActiveScreen;
        if (screen is LevelCardEventHandler handler)
        {
            handler.OnLevelCardLongPressed(levelView);
        }
    }
    
    private enum LevelCardState
    {
        Normal, WillDelete, WillDownload, Downloaded
    }
}

public class LevelView
{
    public Level Level;
    public bool DisplayOwner;
}

public interface LevelCardEventHandler
{
    bool OnLevelCardPressed(LevelView view);

    void OnLevelCardLongPressed(LevelView view);
}