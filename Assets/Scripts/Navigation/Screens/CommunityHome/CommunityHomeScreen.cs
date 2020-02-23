using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Proyecto26;
using RSG;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class CommunityHomeScreen : Screen, ScreenChangeListener
{
    public const string Id = "CommunityHome";

    private static readonly Layout defaultLayout = new Layout
    {
        Sections = new List<Layout.Section>
        {
            new Layout.Section
            {
                TitleKey = "COMMUNITY_HOME_NEW_UPLOADS",
                Query = new OnlineLevelQuery
                {
                    sort = "creation_date",
                    order = "desc",
                    category = "all"
                }
            },
            new Layout.Section
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
            new Layout.Section
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

    private static float savedScrollPosition;
    private static Content savedContent;

    public GameObject sectionPrefab;
    public GameObject levelCardPrefab;

    public ScrollRect scrollRect;
    public CanvasGroup contentHolder;
    public Transform sectionHolder;
    public InputField searchInputField;

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        searchInputField.onEndEdit.AddListener(SearchLevels);
        
        Context.ScreenManager.AddHandler(this);
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Context.ScreenManager.RemoveHandler(this);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        contentHolder.alpha = 0;
        if (savedContent != null)
        {
            OnContentLoaded(savedContent);
            scrollRect.verticalNormalizedPosition = savedScrollPosition;
        }
        else
        {
            LoadContent();
        }
    }
    
    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameActive();
        savedScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public void LoadContent()
    {
        SpinnerOverlay.Show();
        
        var content = new Content();
        var promises = new List<RSG.IPromise<OnlineLevel[]>>();
        foreach (var section in defaultLayout.Sections)
        {
            promises.Add( RestClient.GetArray<OnlineLevel>(new RequestHelper
            {
                Uri = section.Query.BuildUri(section.PreviewSize),
                EnableDebug = true
            }));
        }

        Promise<OnlineLevel[]>.All(promises)
            .Then(payload =>
            {
                content.SectionOnlineLevels = payload.Select(it => it.ToList()).ToList();
                content.Layout = defaultLayout;
                savedContent = content;
                OnContentLoaded(savedContent);
            })
            .Catch(error =>
            {
                Toast.Next(Toast.Status.Failure, "TOAST_CHECK_NETWORK_CONNECTION".Get());
                Debug.LogError(error);
            })
            .Finally(() => SpinnerOverlay.Hide());
    }

    public async void OnContentLoaded(Content content)
    {
        foreach (Transform child in sectionHolder.transform) Destroy(child.gameObject);

        for (var index = 0; index < content.SectionOnlineLevels.Count; index++)
        {
            var onlineLevels = content.SectionOnlineLevels[index];
            var section = content.Layout.Sections[index];
            var sectionGameObject = Instantiate(sectionPrefab, sectionHolder.transform);
            var sectionBehavior = sectionGameObject.GetComponent<CommunityHomeSection>();
            sectionBehavior.titleText.text = section.TitleKey.Get();
            foreach (var onlineLevel in onlineLevels)
            {
                var levelCardGameObject = Instantiate(levelCardPrefab, sectionBehavior.levelCardHolder.transform);
                var levelCard = levelCardGameObject.GetComponent<LevelCard>();
                levelCard.SetModel(onlineLevel.ToLevel());
            }
            sectionBehavior.viewMoreButton.onPointerClick.AddListener(_ =>
            {
                CommunityLevelSelectionScreen.SavedContent = new CommunityLevelSelectionScreen.Content
                {
                    Query = section.Query.JsonDeepCopy(),
                    OnlineLevels = null // Signal reload
                };
                Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In, 0.4f,
                    transitionFocus: ((RectTransform) sectionBehavior.viewMoreButton.transform).GetScreenSpaceCenter());
            });
        }

        LayoutFixer.Fix(contentHolder.transform);
        await UniTask.DelayFrame(0);
        contentHolder.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
    }

    public void SearchLevels(string query)
    {
        CommunityLevelSelectionScreen.SavedContent = new CommunityLevelSelectionScreen.Content
        {
            Query = new OnlineLevelQuery
            {
                sort = "creation_date",
                order = "desc",
                category = "all",
                time = "all",
                search = query
            },
            OnlineLevels = null // Signal reload
        };
        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In);
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this && to.GetId() == MainMenuScreen.Id)
        {
            // Clear community cache
            Context.SpriteCache.DisposeTagged("RemoteLevelCoverThumbnail");
            
            searchInputField.SetTextWithoutNotify("");
            savedContent = null;
            savedScrollPosition = default;
            
            foreach (Transform child in sectionHolder) Destroy(child.gameObject);
        }
    }

    public class Layout
    {
        public List<Section> Sections;

        public class Section
        {
            public string TitleKey;
            public int PreviewSize = 6;
            public OnlineLevelQuery Query;
        }
    }

    public class Content
    {
        public Layout Layout;
        public List<List<OnlineLevel>> SectionOnlineLevels;
    }
}