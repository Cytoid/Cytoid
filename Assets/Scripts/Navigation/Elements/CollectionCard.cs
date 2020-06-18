using System;
using System.Threading;
using DG.Tweening;
using MoreMountains.NiceVibrations;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CollectionCard : InteractableMonoBehavior
{
    public static bool DoNotLoadCover = false;
    
    public Image cover;
    public Text title;
    public Text slogan;

    public bool isStatic;

    public string titleOverride;
    public string sloganOverride;
    
    private CollectionMeta collection;
    private bool loadedCover;
    private Vector2 pressPosition;

    public void ScrollCellContent(object levelObject)
    {
        SetModel((CollectionMeta) levelObject);
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
        title.text = "";
        slogan.text = "";
        cover.sprite = null;
        cover.DOKill();
        cover.SetAlpha(0);
    }

    private void OnDestroy()
    {
        coverToken?.Cancel();
    }

    public void SetModel(CollectionMeta collection)
    {
        this.collection = collection;

        title.text = collection.title;
        slogan.text = collection.slogan;
        
        LoadCover();
    }

    private CancellationTokenSource coverToken;

    public async void LoadCover()
    {
        if (loadedCover && cover.sprite != null && cover.sprite.texture != null)
        {
            // If sprite was loaded and the texture is not destroyed
            return;
        }
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
                var path = collection.cover.ThumbnailUrl.WithImageCdn().WithSizeParam(
                    Context.CollectionThumbnailWidth, Context.CollectionThumbnailHeight);
                sprite = await Context.AssetMemory.LoadAsset<Sprite>(path, AssetTag.CollectionCoverThumbnail,
                    coverToken.Token, true,
                    new SpriteAssetOptions(new[] {Context.CollectionThumbnailWidth, Context.CollectionThumbnailHeight}));
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
        if (collection != null)
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

            Context.ScreenManager.ChangeScreen(CollectionDetailsScreen.Id, ScreenTransition.In, 0.4f,
                transitionFocus: GetComponent<RectTransform>().GetScreenSpaceCenter(),
                payload: new CollectionDetailsScreen.Payload
                    {
                        CollectionId = collection.id,
                        TitleOverride = !titleOverride.IsNullOrEmptyTrimmed() ? titleOverride : null, 
                        SloganOverride = !sloganOverride.IsNullOrEmptyTrimmed() ? sloganOverride : null, 
                    });
        }
    }

}