using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Proyecto26;
using RSG;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using IPromise = RSG.IPromise;

public class CommunityHomeScreen : Screen
{
    
    private static Layout DefaultLayout => new Layout
    {
        Sections = new List<Layout.Section>
        {
            new Layout.CollectionSection
            {
                CollectionIds = new List<string>
                {
                    "5e23eb6c017d2e2198d2f7cb",
                    "5e23eb6c017d2e2198d2f7cd",
                    "5e23eb6c017d2e2198d2f7d6"
                },
                CollectionTitleKeys = new List<string>
                {
                    "COMMUNITY_HOME_GETTING_STARTED",
                    "COMMUNITY_HOME_POWERED_BY_STORYBOARD",
                    "COMMUNITY_HOME_OCTOPUS_TOURNAMENT"
                },
                CollectionSloganKeys = new List<string>
                {
                    "COMMUNITY_HOME_GETTING_STARTED_DESC",
                    "COMMUNITY_HOME_POWERED_BY_STORYBOARD_DESC",
                    "COMMUNITY_HOME_OCTOPUS_TOURNAMENT_DESC"
                }
            },
            new Layout.LevelSection
            {
                TitleKey = "COMMUNITY_HOME_NEW_QUALIFIED",
                Query = new OnlineLevelQuery
                {
                    sort = "modification_date",
                    order = "desc",
                    category = "qualified"
                }
            },
            new Layout.LevelSection
            {
                TitleKey = "COMMUNITY_HOME_NEW_UPLOADS",
                Query = new OnlineLevelQuery
                {
                    sort = "creation_date",
                    order = "desc",
                    category = "all"
                }
            },
            new Layout.LevelSection
            {
                TitleKey = "COMMUNITY_HOME_TRENDING_THIS_MONTH",
                Query = new OnlineLevelQuery
                {
                    sort = "rating",
                    order = "desc",
                    category = "all",
                    time = "month"
                }
            },
            new Layout.LevelSection
            {
                TitleKey = "COMMUNITY_HOME_BEST_OF_CYTOID",
                Query = new OnlineLevelQuery
                {
                    sort = "creation_date",
                    order = "desc",
                    category = "featured"
                }
            }
        }
    };

    public GameObject levelSectionPrefab;
    public GameObject levelCardPrefab;
    public GameObject collectionSectionPrefab;
    public GameObject collectionCardPrefab;
    
    public ScrollRect scrollRect;
    public CanvasGroup contentHolder;
    public Transform sectionHolder;
    public InputField searchInputField;
    public InputField ownerInputField;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        searchInputField.onEndEdit.AddListener(_ => SearchLevels());
        ownerInputField.onEndEdit.AddListener(_ => SearchLevels());
    }

    public override void OnScreenBecameActive()
    {
        contentHolder.alpha = 0;
        
        base.OnScreenBecameActive();
    }
    
    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        if (LoadedPayload != null) LoadedPayload.ScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    protected override async void LoadPayload(ScreenLoadPromise promise)
    {
        if (Context.Player.ShouldOneShot("Agree Copyright Policy"))
        {
            Context.Player.ClearOneShot("Agree Copyright Policy");
            if (!await TermsOverlay.Show("COPYRIGHT_POLICY".Get()))
            {
                promise.Reject();
                Context.ScreenManager.ChangeScreen(Context.ScreenManager.PopAndPeekHistory(), ScreenTransition.Out,
                    addTargetScreenToHistory: false);
                return;
            }
            Context.Player.ShouldOneShot("Agree Copyright Policy");
        }
        
        SpinnerOverlay.Show();

        var promises = new List<IPromise>();
        foreach (var section in IntentPayload.Layout.Sections)
        {
            switch (section)
            {
                case Layout.LevelSection levelSection:
                    promises.Add(RestClient.GetArray<OnlineLevel>(new RequestHelper
                    {
                        Uri = levelSection.Query.BuildUri(levelSection.PreviewSize),
                        Headers = Context.OnlinePlayer.GetRequestHeaders(),
                        EnableDebug = true
                    }).Then(data =>
                    {
                        levelSection.Levels = data.ToList();
                    }));
                    break;
                case Layout.CollectionSection collectionSection:
                {
                    var collectionPromises = new List<RSG.IPromise<CollectionMeta>>();
                    foreach (var collectionId in collectionSection.CollectionIds) {
                    
                        collectionPromises.Add( RestClient.Get<CollectionMeta>(new RequestHelper {
                            Uri = $"{Context.ApiUrl}/collections/{collectionId}",
                            Headers = Context.OnlinePlayer.GetRequestHeaders(),
                            EnableDebug = true
                        }));
                    }
                    promises.Add(Promise<CollectionMeta>.All(collectionPromises).Then(data =>
                    {
                        collectionSection.Collections = data.ToList()
                            .Zip(collectionSection.CollectionIds, (meta, id) => meta.Also(it => it.id = id))
                            .Zip(collectionSection.CollectionTitleKeys, (meta, title) => meta.Also(it => it.title = title.Get()))
                            .Zip(collectionSection.CollectionSloganKeys, (meta, slogan) => meta.Also(it => it.slogan = slogan.Get()))
                            .ToList();
                    }));
                    break;
                }
            }
        }

        Promise.All(promises)
            .Then(() =>
            {
                promise.Resolve(IntentPayload);
            })
            .Catch(error =>
            {
                Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                Debug.LogError(error);
                promise.Reject();
            })
            .Finally(() => SpinnerOverlay.Hide());
    }

    protected override void Render()
    {
        foreach (Transform child in sectionHolder.transform) Destroy(child.gameObject);

        foreach (var section in LoadedPayload.Layout.Sections)
        {
            switch (section)
            {
                case Layout.LevelSection levelSection:
                {
                    var sectionGameObject = Instantiate(levelSectionPrefab, sectionHolder.transform);
                    var sectionBehavior = sectionGameObject.GetComponent<LevelSection>();
                    sectionBehavior.titleText.text = levelSection.TitleKey.Get();
                    foreach (var onlineLevel in levelSection.Levels)
                    {
                        var levelCardGameObject = Instantiate(levelCardPrefab, sectionBehavior.levelCardHolder.transform);
                        var levelCard = levelCardGameObject.GetComponent<LevelCard>();
                        levelCard.SetModel(new LevelView{Level = onlineLevel.ToLevel(LevelType.User), DisplayOwner = true});
                    }
                    sectionBehavior.viewMoreButton.GetComponentInChildren<Text>().text =
                        "COMMUNITY_HOME_VIEW_ALL".Get();
                    sectionBehavior.viewMoreButton.onPointerClick.AddListener(_ =>
                    {
                        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In, 0.4f,
                            transitionFocus: ((RectTransform) sectionBehavior.viewMoreButton.transform).GetScreenSpaceCenter(),
                            payload: new CommunityLevelSelectionScreen.Payload {Query = levelSection.Query.JsonDeepCopy()});
                    });
                    break;
                }
                case Layout.CollectionSection collectionSection:
                {
                    var sectionGameObject = Instantiate(collectionSectionPrefab, sectionHolder.transform);
                    var sectionBehavior = sectionGameObject.GetComponent<CollectionSection>();
                    foreach (var collection in collectionSection.Collections)
                    {
                        var collectionGameObject = Instantiate(collectionCardPrefab, sectionBehavior.collectionCardHolder.transform);
                        var collectionCard = collectionGameObject.GetComponent<CollectionCard>();
                        collectionCard.SetModel(collection);
                        collectionCard.titleOverride = collection.title;
                        collectionCard.sloganOverride = collection.slogan;
                    }
                    break;
                }
            }
        }

        LayoutFixer.Fix(contentHolder.transform);
        base.Render();
    }

    protected override async void OnRendered()
    {
        base.OnRendered();
        
        await UniTask.DelayFrame(3); // Scroll position not set fix
        if (LoadedPayload.ScrollPosition > -1) scrollRect.verticalNormalizedPosition = LoadedPayload.ScrollPosition;
        
        contentHolder.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
    }

    public void SearchLevels()
    {
        var search = searchInputField.text;
        var owner = ownerInputField.text.ToLower();
        if (search.IsNullOrEmptyTrimmed() && owner.IsNullOrEmptyTrimmed()) return;
        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In,
            payload: new CommunityLevelSelectionScreen.Payload
            {
                Query = new OnlineLevelQuery
                {
                    sort = search != null ? "relevance" : "creation_date",
                    order = "desc",
                    category = "all",
                    time = "all",
                    search = search,
                    owner = owner,
                }
            });
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this && to is MainMenuScreen)
        {
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalLevelCoverThumbnail);
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.RemoteLevelCoverThumbnail);
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.CollectionCoverThumbnail);
            
            searchInputField.SetTextWithoutNotify("");
            ownerInputField.SetTextWithoutNotify("");
            LoadedPayload = null;
            
            foreach (Transform child in sectionHolder) Destroy(child.gameObject);
        }
    }

    public class Layout
    {
        public List<Section> Sections;

        public class Section
        {
        }

        public class LevelSection : Section
        {
            public string TitleKey;
            public int PreviewSize = 6;
            public OnlineLevelQuery Query;
            public List<OnlineLevel> Levels;
        }

        public class CollectionSection : Section
        {
            public List<string> CollectionIds;
            public List<CollectionMeta> Collections;
            public List<string> CollectionTitleKeys { get; set; }
            public List<string> CollectionSloganKeys { get; set; }
        }
        
    }

    public class Payload : ScreenPayload
    {
        public Layout Layout;
        public float ScrollPosition = -1;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }

    public override ScreenPayload GetDefaultPayload() => new Payload {Layout = DefaultLayout};

    public const string Id = "CommunityHome";

    public override string GetId() => Id;
}