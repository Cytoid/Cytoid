using System.Linq;
using DG.Tweening;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class CollectionDetailsScreen : Screen
{
    public TransitionElement icons;
    
    public LoopVerticalScrollRect scrollRect;
    public RectTransform scrollRectPaddingReference;

    public Image coverImage;
    public Text titleText;
    public Text sloganText;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        coverImage.sprite = null;
        titleText.text = "";
        sloganText.text = "";
    }

    public override void OnScreenEnterCompleted()
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
        if (LoadedPayload != null) LoadedPayload.ScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    protected override void Render()
    {
        var collection = LoadedPayload.Collection;
        titleText.text = LoadedPayload.TitleOverride ?? collection.title;
        sloganText.text = LoadedPayload.SloganOverride ?? collection.slogan;
        sloganText.transform.parent.RebuildLayout();
        scrollRect.totalCount = collection.levels.Count;
        scrollRect.objectsToFill = collection.levels.Select(it => new LevelView{ Level = it.ToLevel(LoadedPayload.Type), DisplayOwner = true}).ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
        if (LoadedPayload.ScrollPosition > 0)
        {
            scrollRect.SetVerticalNormalizedPositionFix(LoadedPayload.ScrollPosition);
        }

        base.Render();
    }
    
    protected override void LoadPayload(ScreenLoadPromise promise)
    {
        coverImage.color = Color.black;

        if (IntentPayload.Collection != null)
        {
            promise.Resolve(IntentPayload);
            return;
        }
        
        SpinnerOverlay.Show();
        RestClient.Get<CollectionMeta>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/collections/{IntentPayload.CollectionId}",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            })
            .Then(meta =>
            {
                IntentPayload.Collection = meta;

                promise.Resolve(IntentPayload);
            })
            .CatchRequestError(error =>
            {
                Debug.LogError(error);
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());

                promise.Reject();
            })
            .Finally(() => SpinnerOverlay.Hide());
    }

    protected override void OnRendered()
    {
        base.OnRendered();

        scrollRect.GetComponent<TransitionElement>()
            .Let(it =>
            {
                it.Leave(false, true);
                it.Enter();
            });

        icons.Leave(false, true);
        if (LoadedPayload.Collection.owner.Uid != Context.OfficialAccountId)
        {
            icons.Enter();
        }
        
        if (coverImage.sprite == null || coverImage.sprite.texture == null)
        {
            AddTask(async token =>
            {
                Sprite sprite;
                try
                {
                    sprite = await Context.AssetMemory.LoadAsset<Sprite>(LoadedPayload.Collection.cover.CoverUrl,
                        AssetTag.CollectionCover, cancellationToken: token);
                }
                catch
                {
                    return;
                }

                if (sprite != null)
                {
                    coverImage.sprite = sprite;
                    coverImage.FitSpriteAspectRatio();
                    coverImage.DOColor(new Color(0.2f, 0.2f, 0.2f, 1), 0.4f);
                }
            });
        }
        else
        {
            coverImage.DOColor(new Color(0.2f, 0.2f, 0.2f, 1), 0.4f);
        }
    }

    public class Payload : ScreenPayload
    {
        public string CollectionId;
        public CollectionMeta Collection;
        public string TitleOverride;
        public string SloganOverride;
        public LevelType Type = LevelType.User;
        
        public float ScrollPosition;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }
    
    public const string Id = "CollectionDetails";
    public override string GetId() => Id;
    
}
