using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class CommunityHomeScreen : Screen, ScreenChangeListener
{
    public const string Id = "CommunityHome";
    private const int LatestLevelsSize = 6;

    private static float savedScrollPosition;
    private static Content savedContent;

    public ScrollRect scrollRect;
    public Transform latestLevelsHolder;
    public InteractableMonoBehavior latestLevelsButton;
    
    public InputField searchInputField;

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        searchInputField.onEndEdit.AddListener(SearchLevels);
        latestLevelsButton.onPointerClick.AddListener(_ => BrowseLatestLevels());
        
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
        var content = new Content();
        RestClient.GetArray<OnlineLevel>(new RequestHelper
        {
            Uri = $"{Context.ApiBaseUrl}/levels?sort=creation_date&order=desc&limit={LatestLevelsSize}",
            EnableDebug = true
        }).Then(entries =>
        {
            content.OnlineLevels = entries.ToList();
            savedContent = content;
            OnContentLoaded(savedContent);
        }).Catch(Debug.LogError);
    }

    public void OnContentLoaded(Content content)
    {
        var levelCards = latestLevelsHolder.GetComponentsInChildren<LevelCard>();
        for (var i = 0; i < LatestLevelsSize; i++)
        {
            levelCards[i].SetModel(content.OnlineLevels[i].ToLevel());
        }
    }

    public void BrowseLatestLevels()
    {
        CommunityLevelSelectionScreen.SavedContent = new CommunityLevelSelectionScreen.Content
        {
            Sort = "creation_date",
            Order = "desc",
            Category = "all",
            Time = "all",
            Query = "",
            OnlineLevels = null // Signal reload
        };
        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In, 0.4f,
            transitionFocus: ((RectTransform) latestLevelsButton.transform).GetScreenSpaceCenter());
    }

    public void SearchLevels(string query)
    {
        CommunityLevelSelectionScreen.SavedContent = new CommunityLevelSelectionScreen.Content
        {
            Sort = "creation_date",
            Order = "desc",
            Category = "all",
            Time = "all",
            Query = query,
            OnlineLevels = null // Signal reload
        };
        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In);
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
        if (from.GetId() == MainMenuScreen.Id && to.GetId() == Id)
        {
            // Clear search query
            searchInputField.SetTextWithoutNotify("");
            savedContent = null;
            savedScrollPosition = default;
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from.GetId() == Id && to.GetId() == MainMenuScreen.Id)
        {
            // Clear community cache
            // TODO
            Context.SpriteCache.DisposeTagged("RemoteLevelCoverThumbnail");
            GetComponentsInChildren<LevelCard>().ForEach(it => it.cover.SetAlpha(0));
        }
    }

    public class Content
    {
        public List<OnlineLevel> OnlineLevels;
    }
}