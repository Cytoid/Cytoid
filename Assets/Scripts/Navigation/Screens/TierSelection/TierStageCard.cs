using System;
using System.Collections.Generic;
using System.Threading;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TierStageCard : InteractableMonoBehavior
{
    public Image cover;
    public Text artist;
    public Text title;
    public Text titleLocalized;

    public DifficultyBall difficultyBall;
    public GradientMeshEffect overlayGradient;
    
    private Level level;
    private bool loadedCover;
    private CancellationTokenSource actionToken;
    private Vector2 pressPosition;

    public void SetModel(Level level)
    {
        this.level = level;
        
        artist.text = level.Meta.artist;
        title.text = level.Meta.title;
        titleLocalized.text = level.Meta.title_localized;
        titleLocalized.gameObject.SetActive(!string.IsNullOrEmpty(level.Meta.title_localized));

        //difficultyball

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
                var path = "file://" + level.Path + LevelManager.CoverThumbnailFilename;
                sprite = await Context.SpriteCache.CacheSpriteInMemory(path, "LocalLevelCoverThumbnail",
                    cancelToken.Token);
            }
            else
            {
                var path = level.Meta.background.path.WithImageCdn().WithSizeParam(
                    Context.ThumbnailWidth, Context.ThumbnailHeight);
                sprite = await Context.SpriteCache.CacheSpriteInMemory(path, "RemoteLevelCoverThumbnail",
                    cancelToken.Token, new []{ Context.ThumbnailWidth, Context.ThumbnailHeight }, useFileCache: true);
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
                    if (cover == null) return;
                    cover.sprite = sprite;
                    cover.DOFade(0.5f, 0.2f);
                    cover.FitSpriteAspectRatio();
                    loadedCover = true;
                }
            }
        }
    }

}