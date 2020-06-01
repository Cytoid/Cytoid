using System;
using System.Linq;
using DG.Tweening;
using Proyecto26;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CollectionDetailsScreen : Screen
{
    public static Content LoadedContent;
    private static float lastScrollPosition = -1;

    public const string Id = "CollectionDetails";

    public TransitionElement icons;
    
    public LoopVerticalScrollRect scrollRect;
    public RectTransform scrollRectPaddingReference;

    public Image coverImage;
    public Text titleText;
    public Text sloganText;

    [HideInInspector] public UnityEvent onContentLoaded = new UnityEvent();

    private DateTimeOffset loadToken;
    
    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        coverImage.sprite = null;
        titleText.text = "";
        sloganText.text = "";
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (LoadedContent != null)
        {
            if (LoadedContent.Collection != null)
            {
                OnContentLoaded(LoadedContent);
            }
            else
            {
                LoadContent();
            }
        }
        else
        {
            LoadContent();
        }
    }

    public override async void OnScreenEnterCompleted()
    {
        base.OnScreenEnterCompleted();
        var canvasRectTransform = Canvas.GetComponent<RectTransform>();
        var canvasScreenRect = canvasRectTransform.GetScreenSpaceRect();

        scrollRect.contentLayoutGroup.padding.top = (int) ((canvasScreenRect.height -
                                                            scrollRectPaddingReference.GetScreenSpaceRect().min.y) *
                canvasRectTransform.rect.height / canvasScreenRect.height) +
            48 - 156;
        scrollRect.transform.RebuildLayout();
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        lastScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    public void OnContentLoaded(Content content)
    {
        var collection = content.Collection;
        titleText.text = content.TitleOverride ?? collection.title;
        sloganText.text = content.SloganOverride ?? collection.slogan;
        sloganText.transform.parent.RebuildLayout();
        scrollRect.totalCount = collection.levels.Count;
        scrollRect.objectsToFill = collection.levels.Select(it => new LevelView{ Level = it.ToLevel(content.Type), DisplayOwner = true}).ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
        if (lastScrollPosition > 0)
        {
            scrollRect.SetVerticalNormalizedPositionFix(lastScrollPosition);
        }
        scrollRect.GetComponent<TransitionElement>()
            .Let(it =>
            {
                it.Leave(false, true);
                it.Enter();
            });

        if (content.Collection.owner.Uid != Context.OfficialAccountId)
        {
            icons.Enter();
        }
        
        if (coverImage.sprite == null)
        {
            var token = loadToken = DateTimeOffset.Now;

            async void LoadCover()
            {
                var sprite = await Context.AssetMemory.LoadAsset<Sprite>(content.Collection.cover.CoverUrl,
                    AssetTag.CollectionCover, allowFileCache: true);
                if (token != loadToken) return;

                if (sprite != null)
                {
                    coverImage.sprite = sprite;
                    coverImage.FitSpriteAspectRatio();
                    coverImage.DOColor(new Color(0.2f, 0.2f, 0.2f, 1), 0.4f);
                }
            }

            LoadCover();
        }
        
        onContentLoaded.Invoke();
    }

    public void LoadContent()
    {
        coverImage.color = Color.black;
        
        SpinnerOverlay.Show();

        RestClient.Get<CollectionMeta>(new RequestHelper {
            Uri = $"{Context.ApiUrl}/collections/{LoadedContent.Id}",
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        })
            .Then(meta =>
            {
                SpinnerOverlay.Hide();

                LoadedContent.Collection = meta;
                OnContentLoaded(LoadedContent);
            })
            .CatchRequestError(error =>
            {
                Debug.LogError(error);
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                SpinnerOverlay.Hide();
            });
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            scrollRect.ClearCells();
            if (!(to is ProfileScreen || to is GamePreparationScreen))
            {
                coverImage.sprite = null;
                LoadedContent = null;
                lastScrollPosition = default;
                loadToken = DateTimeOffset.MinValue;
                Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.CollectionCover);
            }
        }
    }

    public class Content
    {
        public string Id;
        public string TitleOverride;
        public string SloganOverride;
        public LevelType Type = LevelType.Community;
        public CollectionMeta Collection;
    }
    
}
