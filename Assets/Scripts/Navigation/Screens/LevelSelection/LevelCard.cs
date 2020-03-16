using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelCard : InteractableMonoBehavior
{
    public Image cover;
    public Text artist;
    public Text title;
    public Text titleLocalized;

    public List<DifficultyBall> difficultyBalls = new List<DifficultyBall>();

    private Level level;
    private bool loadedCover;
    private CancellationTokenSource actionToken;
    private Vector2 pressPosition;

    public void ScrollCellContent(object levelObject)
    {
        SetModel((Level) levelObject);
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
    }

    private void Awake()
    {
        artist.text = "";
        title.text = "";
        titleLocalized.text = "";
        for (var index = 0; index < 3; index++) difficultyBalls[index].gameObject.SetActive(false);
    }

    public void SetModel(Level level)
    {
        this.level = level;
        
        artist.text = level.Meta.artist;
        title.text = level.Meta.title;
        titleLocalized.text = level.Meta.title_localized;
        titleLocalized.gameObject.SetActive(!level.Meta.title_localized.IsNullOrEmptyTrimmed());

        for (var index = 0; index < 3; index++)
        {
            if (index <= level.Meta.charts.Count - 1)
            {
                var chart = level.Meta.charts[index];
                difficultyBalls[index].gameObject.SetActive(true);
                difficultyBalls[index].SetModel(Difficulty.Parse(chart.type), chart.difficulty);
            }
            else
            {
                difficultyBalls[index].gameObject.SetActive(false);
            }
        }

        LayoutFixer.Fix(transform);

        LoadCover();
    }

    private CancellationTokenSource coverToken;

    public async void LoadCover()
    {
        loadedCover = false;
        cover.DOKill();
        cover.SetAlpha(0);
        if (coverToken != null)
        {
            if (!coverToken.IsCancellationRequested) coverToken.Cancel();
            coverToken = null;
        }
        
        if (!((RectTransform) transform).IsVisible())
        {
            coverToken = new CancellationTokenSource();
            try
            {
                await UniTask.WaitUntil(() => ((RectTransform) transform).IsVisible(),
                    cancellationToken: coverToken.Token);
                await UniTask.DelayFrame(0);
            }
            catch
            {
                return;
            }
        }
        
        coverToken = new CancellationTokenSource();
        Sprite sprite = null;
        try
        {
            if (level.IsLocal)
            {
                var path = "file://" + level.Path + LevelManager.CoverThumbnailFilename;
                sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.LocalCoverThumbnail,
                    coverToken.Token);
            }
            else
            {
                var path = level.Meta.background.path.WithImageCdn().WithSizeParam(
                    Context.ThumbnailWidth, Context.ThumbnailHeight);
                sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.OnlineCoverThumbnail,
                    coverToken.Token, true, new SpriteAssetOptions(new []{ Context.ThumbnailWidth, Context.ThumbnailHeight }));
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

        if (sprite != null)
        {
            lock (sprite)
            {
                if (sprite != null)
                {
                    if (cover == null) return;
                    cover.sprite = sprite;
                    cover.DOFade(0.5f, 0.2f);
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
        Context.Vibrate();
        actionToken = null;
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        var d = Vector2.Distance(pressPosition, eventData.position);
        if (d > 0.005f * Context.ReferenceWidth || ignoreNextPointerUp)
        {
            ignoreNextPointerUp = false;
            IsPointerDown = false;
        }
        actionToken?.Cancel();
        base.OnPointerUp(eventData);
        if (loadedCover) cover.DOFade(0.5f, 0.2f).SetEase(Ease.OutCubic);
        // cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (level != null)
        {
            Context.SelectedLevel = level;
            Context.AudioManager.Get("Navigate2").Play();
            Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In, 0.4f,
                transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter());
        }
    }

    public void OnAction()
    {
        if (!level.IsLocal) return;
        if (Context.ScreenManager.ActiveScreenId != LevelSelectionScreen.Id &&
            Context.ScreenManager.ActiveScreenId != CommunityLevelSelectionScreen.Id) return;
        
        var dialog = Dialog.Instantiate();
        dialog.Message = "DIALOG_CONFIRM_DELETE".Get(level.Meta.title);
        dialog.UsePositiveButton = true;
        dialog.UseNegativeButton = true;
        dialog.OnPositiveButtonClicked = _ =>
        {
            Context.LevelManager.DeleteLocalLevel(level.Id);
            Toast.Next(Toast.Status.Success, "TOAST_SUCCESSFULLY_DELETED_LEVEL".Get());
            dialog.Close();
        };
        dialog.Open();
    }

}