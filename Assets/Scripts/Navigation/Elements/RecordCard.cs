using System;
using System.Threading;
using DG.Tweening;
using MoreMountains.NiceVibrations;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecordCard : InteractableMonoBehavior
{
    public static bool DoNotLoadCover = false;
    
    public Image cover;
    public Text title;
    public Text date;
    public RectTransform ownerRoot;
    public Avatar ownerAvatar;
    public Text ownerName;
    public DifficultyBall difficultyBall;
    public PerformanceWidget performanceWidget;

    public bool isStatic;

    private RecordView recordView;
    private OnlineRecord record => recordView?.Record;
    private bool loadedCover;
    private Vector2 pressPosition;

    public void ScrollCellContent(object levelObject)
    {
        SetModel((RecordView) levelObject);
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
        Unload();
    }

    public void Unload()
    {
        cover.sprite = null;
        cover.DOKill();
        cover.SetAlpha(0);
    }

    private void OnDestroy()
    {
        coverToken?.Cancel();
    }

    public void SetModel(RecordView view)
    {
        recordView = view;

        title.text = record.chart.level.Title;
        date.text = record.date.ToLocalTime().Date.Humanize();
        difficultyBall.SetModel(Difficulty.Parse(record.chart.type), record.chart.difficulty);
        performanceWidget.SetModel(new LevelRecord.Performance{Score = record.score, Accuracy = record.accuracy});

        if (ownerRoot != null)
        {
            if (recordView.DisplayOwner && recordView.Record.owner != null)
            {
                ownerRoot.gameObject.SetActive(true);
                ownerAvatar.action = AvatarAction.ViewLevels;
                ownerAvatar.SetModel(recordView.Record.owner);
                ownerName.text = recordView.Record.owner.Uid;
            }
            else
            {
                ownerRoot.gameObject.SetActive(false);
                ownerAvatar.Dispose();
                ownerName.text = "";
            }
        }

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
        
        if (DoNotLoadCover)
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
            try
            {
                const int width = 576;
                const int height = 216;
                var path = record.chart.level.Cover.ThumbnailUrl.WithImageCdn().WithSizeParam(width, height);
                sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.RecordCoverThumbnail,
                    coverToken.Token, true,
                    new SpriteAssetOptions(new[] {width, height}));
            }
            catch
            {
                return;
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
                    cover.DOFade(0.15f, 0.2f);
                    cover.FitSpriteAspectRatio();
                    loadedCover = true;
                }
            }
        }

        coverToken = null;
    }

    public override async void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (isStatic) return;
        
        pressPosition = eventData.position;
        if (loadedCover) cover.DOFade(0.45f, 0.2f).SetEase(Ease.OutCubic);
        // cover.rectTransform.DOScale(1.02f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (isStatic)
        {
            base.OnPointerUp(eventData);
            return;
        }
        
        var d = Vector2.Distance(pressPosition, eventData.position);
        if (d > 0.005f * Context.ReferenceWidth)
        {
            IsPointerDown = false;
        }

        base.OnPointerUp(eventData);
        if (loadedCover) cover.DOFade(0.15f, 0.2f).SetEase(Ease.OutCubic);
        // cover.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (isStatic) return;
        if (record != null)
        {
            if (Context.ScreenManager.IsChangingScreen)
            {
                return;
            }
            
            if (Context.IsOffline())
            {
                throw new InvalidOperationException();
            }

            Context.AudioManager.Get("Navigate2").Play();
            Context.Haptic(HapticTypes.MediumImpact, true);

            /*
            CollectionDetailsScreen.LoadedContent = new CollectionDetailsScreen.Content {Id = collection.id, TitleOverride = titleOverride, SloganOverride = sloganOverride};
            Context.ScreenManager.ChangeScreen(CollectionDetailsScreen.Id, ScreenTransition.In, 0.4f,
                transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter());*/
        }
    }

}

public class RecordView
{
    public OnlineRecord Record;
    public bool DisplayOwner;
}