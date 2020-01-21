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
        titleLocalized.gameObject.SetActive(!string.IsNullOrEmpty(level.Meta.title_localized));

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

    private CancellationTokenSource cancelToken = new CancellationTokenSource();

    public async void LoadCover()
    {
        loadedCover = false;
        cover.DOKill();
        cover.SetAlpha(0);
        if (cancelToken != null)
        {
            if (!cancelToken.IsCancellationRequested) cancelToken.Cancel();
        }
        if (!((RectTransform) transform).IsVisible())
        {
            cancelToken = new CancellationTokenSource();
            try
            {
                await UniTask.WaitUntil(() => ((RectTransform) transform).IsVisible(),
                    cancellationToken: cancelToken.Token);
                await UniTask.DelayFrame(0);
            }
            catch
            {
                return;
            }
        }
        
        cancelToken = new CancellationTokenSource();
        Sprite sprite = null;
        try
        {
            if (level.IsLocal)
            {
                var path = "file://" + level.Path + ".thumbnail";
                sprite = await Context.SpriteCache.CacheSprite(path, "LocalLevelCoverThumbnail",
                    cancelToken.Token);
            }
            else
            {
                var path = level.Meta.background.path.WithImageCdn().WithSizeParam(576, 360);
                sprite = await Context.SpriteCache.CacheSprite(path, "RemoteLevelCoverThumbnail",
                    cancelToken.Token);
            }
        }
        catch
        {
            if (sprite != null)
            {
                // Should be impossible
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
                    cover.sprite = sprite;
                    cover.DOFade(0.5f, 0.2f);
                    cover.FitSpriteAspectRatio();
                    loadedCover = true;
                }
            }
        }
    }

    private bool ignoreNextPointerUp;
    
    public override async void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        pressPosition = eventData.position;
        if (loadedCover) cover.DOFade(1.0f, 0.2f).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.02f, 0.2f).SetEase(Ease.OutCubic);
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
        ignoreNextPointerUp = true;
        OnPointerUp(eventData);
        OnAction();
        Handheld.Vibrate();
        actionToken = null;
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        var d = Vector2.Distance(pressPosition, eventData.position);
        print(Context.ReferenceWidth * 0.005f + " <-> " + d);
        if (d > 0.005f * Context.ReferenceWidth || ignoreNextPointerUp)
        {
            ignoreNextPointerUp = false;
            IsPointerDown = false;
        }
        actionToken?.Cancel();
        base.OnPointerUp(eventData);
        if (loadedCover) cover.DOFade(0.5f, 0.2f).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
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
        dialog.Message = $"Delete \"{level.Meta.title}\"?\nYour best performance will not be deleted.";
        dialog.UsePositiveButton = true;
        dialog.UseNegativeButton = true;
        dialog.OnPositiveButtonClicked = _ =>
        {
            Context.LevelManager.DeleteLocalLevel(level.Id);
            dialog.Close();
        };
        dialog.Open();
    }

}