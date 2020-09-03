using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using MoreMountains.NiceVibrations;
using Cysharp.Threading.Tasks;
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

    public void ScrollCellContent(object levelObject)
    {
        screenParent = this.GetScreenParent();
        if (!addedScreenListeners)
        {
            addedScreenListeners = true;
            screenParent.onScreenRendered.AddListener(OnScreenRendered);
            screenParent.onScreenBecameInactive.AddListener(OnScreenBecameInactive);
        }
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

        if (addedScreenListeners)
        {
            addedScreenListeners = false;
            screenParent.onScreenRendered.RemoveListener(OnScreenRendered);
            screenParent.onScreenBecameInactive.RemoveListener(OnScreenBecameInactive);
        }
        screenParent = null;
    }

    private void Awake()
    {
        Unload();
        screenParent = this.GetScreenParent();
        if (screenParent != null && !addedScreenListeners)
        {
            addedScreenListeners = true;
            screenParent.onScreenRendered.AddListener(OnScreenRendered);
            screenParent.onScreenBecameInactive.AddListener(OnScreenBecameInactive);
        }
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
        OnAction();
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
            
            if (Context.IsOffline() && !Level.IsLocal)
            {
                Dialog.PromptAlert("DIALOG_OFFLINE_LEVEL_NOT_AVAILABLE".Get());
                return;
            }

            Context.AudioManager.Get("Navigate2").Play();
            Context.Haptic(HapticTypes.MediumImpact, true);

            Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In, 0.4f,
                transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter(),
                payload: new GamePreparationScreen.Payload {Level = Level});
        }
    }

    public void OnAction()
    {
        if (!Level.IsLocal || Level.Type == LevelType.BuiltIn) return;
        if (Context.ScreenManager.ActiveScreenId != LevelSelectionScreen.Id &&
            Context.ScreenManager.ActiveScreenId != CommunityLevelSelectionScreen.Id) return;

        Context.Haptic(HapticTypes.Warning, true);
        var dialog = Dialog.Instantiate();
        dialog.Message = "DIALOG_CONFIRM_DELETE".Get(Level.Meta.title);
        dialog.UsePositiveButton = true;
        dialog.UseNegativeButton = true;
        dialog.OnPositiveButtonClicked = _ =>
        {
            Context.LevelManager.DeleteLocalLevel(Level.Id);
            Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_DELETED_LEVEL".Get());
            dialog.Close();
        };
        dialog.Open();
    }
}

public class LevelView
{
    public Level Level;
    public bool DisplayOwner;
}