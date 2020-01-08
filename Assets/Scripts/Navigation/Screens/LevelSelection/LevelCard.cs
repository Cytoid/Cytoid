using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelCard : InteractableMonoBehavior, IPointerClickHandler
{
    public static int i = 1;
    public int j = 0;

    public Image cover;
    public Text artist;
    public Text title;
    public Text titleLocalized;

    public List<DifficultyBall> difficultyBalls = new List<DifficultyBall>();
    
    public override bool IsPointerEntered => false;

    private Level level;

    public void ScrollCellContent(object levelObject)
    {
        SetModel((Level) levelObject);
    }

    private void Awake()
    {
        j = i++;
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

        transform.RebuildLayout();

        LoadCover();
    }

    private CancellationTokenSource cancelToken = new CancellationTokenSource();

    public async void LoadCover()
    {
        if (cancelToken != null)
        {
            if (!cancelToken.IsCancellationRequested) cancelToken.Cancel();
        }
        if (!((RectTransform) transform).IsVisible())
        {
            cancelToken = new CancellationTokenSource();
            cancelToken.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                await UniTask.WaitUntil(() => ((RectTransform) transform).IsVisible(),
                    cancellationToken: cancelToken.Token);
            }
            catch
            {
                return;
            }
        }
        
        cover.SetAlpha(0);
        Sprite sprite;
        if (level.IsLocal)
        {
            var path = "file://" + level.Path + ".thumbnail";
            sprite = await Context.SpriteCache.CacheSprite(path, "LocalLevelCoverThumbnail");
        }
        else
        {
            var path = level.Meta.background.path.WithImageCdn().WithSizeParam(576, 360);
            sprite = await Context.SpriteCache.CacheSprite(path, "RemoteLevelCoverThumbnail");
        }
        
        cover.sprite = sprite;
        cover.DOFade(0.5f, 0.2f);
        cover.FitSpriteAspectRatio();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        cover.DOFade(1.0f, 0.2f).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.02f, 0.2f).SetEase(Ease.OutCubic);
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        cover.DOFade(0.5f, 0.2f).SetEase(Ease.OutCubic);
        cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        Context.SelectedLevel = level;
        Context.ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.In, 0.4f,
            transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter());
    }

}