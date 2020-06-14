using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommunityLevelSelectionScreen : Screen
{
    public LoopVerticalScrollRect scrollRect;

    public Text titleText;

    [GetComponentInChildren] public ActionTabs actionTabs;
    public ToggleRadioGroupPreferenceElement sortRadioGroup;
    public ToggleRadioGroupPreferenceElement orderRadioGroup;
    public ToggleRadioGroupPreferenceElement categoryRadioGroup;
    public ToggleRadioGroupPreferenceElement timeRadioGroup;
    public InputField searchInputField;
    public InputField ownerInputField;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        searchInputField.onEndEdit.AddListener(value =>
        {
            actionTabs.Close();
            LoadedPayload.Query.search = value.Trim();
            LoadLevels();
        });
        ownerInputField.onEndEdit.AddListener(value =>
        {
            actionTabs.Close();
            LoadedPayload.Query.owner = value.Trim();
            LoadLevels();
        });

        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            // Reload active content
            if (LoadedPayload.Levels.Count > 0)
            {
                LoadedPayload.ScrollPosition = scrollRect.verticalNormalizedPosition;
                scrollRect.ClearCells();
                RenderLevels();
                scrollRect.SetVerticalNormalizedPositionFix(LoadedPayload.ScrollPosition);
            }
        });
        SetupOptions();
        Context.OnLanguageChanged.AddListener(SetupOptions);
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        LoadedPayload.ScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    private void SetupOptions()
    {
        sortRadioGroup.SetContent(null, null,
            () => "creation_date", it =>
            {
                LoadedPayload.Query.sort = it;
                LoadLevels();
            },
            new []
            {
                ("COMMUNITY_SELECT_SORT_BY_UPLOADED_DATE".Get(), "creation_date"),
                ("COMMUNITY_SELECT_SORT_BY_MODIFIED_DATE".Get(), "modification_date"),
                ("COMMUNITY_SELECT_SORT_BY_RATING".Get(), "rating"),
                ("COMMUNITY_SELECT_SORT_BY_DOWNLOADS".Get(), "downloads"),
                ("COMMUNITY_SELECT_SORT_BY_DIFFICULTY".Get(), "difficulty"),
                ("COMMUNITY_SELECT_SORT_BY_DURATION".Get(), "duration")
            });
        orderRadioGroup.SetContent(null, null,
            () => "desc", it => {
                LoadedPayload.Query.order = it;
                LoadLevels();
            },
            new[]
            {
                ("COMMUNITY_SELECT_SORT_ORDER_ASC".Get(), "asc"),
                ("COMMUNITY_SELECT_SORT_ORDER_DESC".Get(), "desc")
            });
        categoryRadioGroup.SetContent(null, null,
            () => "category", it => {
                LoadedPayload.Query.category = it;
                LoadLevels();
            },
            new[]
            {
                ("COMMUNITY_SELECT_CATEGORY_ALL".Get(), "all"),
                ("COMMUNITY_SELECT_CATEGORY_FEATURED".Get(), "featured")
            });
        timeRadioGroup.SetContent(null, null,
            () => "all", it => {
                LoadedPayload.Query.time = it;
                LoadLevels();
            },
            new[]
            {
                ("COMMUNITY_SELECT_TIME_ANY_TIME".Get(), "all"),
                ("COMMUNITY_SELECT_TIME_PAST_WEEK".Get(), "week"),
                ("COMMUNITY_SELECT_TIME_PAST_MONTH".Get(), "month"),
                ("COMMUNITY_SELECT_TIME_PAST_6_MONTHS".Get(), "halfyear"),
                ("COMMUNITY_SELECT_TIME_PAST_YEAR".Get(), "year")
            });
        sortRadioGroup.transform.parent.RebuildLayout();
    }

    protected override void Render()
    {
        sortRadioGroup.radioGroup.Select(LoadedPayload.Query.sort, false);
        orderRadioGroup.radioGroup.Select(LoadedPayload.Query.order, false);
        categoryRadioGroup.radioGroup.Select(LoadedPayload.Query.category, false);
        timeRadioGroup.radioGroup.Select(LoadedPayload.Query.time, false);
        searchInputField.SetTextWithoutNotify(LoadedPayload.Query.search);
        ownerInputField.SetTextWithoutNotify(LoadedPayload.Query.owner);
        if (LoadedPayload.Query.search.IsNullOrEmptyTrimmed() && LoadedPayload.Query.owner.IsNullOrEmptyTrimmed())
        {
            titleText.text = "COMMUNITY_SELECT_BROWSE".Get();
        }
        else
        {
            var text = "";
            if (!LoadedPayload.Query.search.IsNullOrEmptyTrimmed())
            {
                text += "COMMUNITY_SELECT_SEARCH_QUERY".Get(LoadedPayload.Query.search.Trim());
            }
            if (!LoadedPayload.Query.owner.IsNullOrEmptyTrimmed())
            {
                if (text != "") text += " & ";
                text += "COMMUNITY_SELECT_SEARCH_UPLOADER".Get(LoadedPayload.Query.owner.Trim());
            }
            titleText.text = text;
        }
        
        scrollRect.ClearCells();
        RenderLevels();
        
        base.Render();
    }

    protected override void OnRendered()
    {
        base.OnRendered();

        if (!LoadedPayload.IsLastPageLoaded)
        {
            LoadLevels();
        }
        else
        {
            if (LoadedPayload.ScrollPosition > 0) scrollRect.verticalNormalizedPosition = LoadedPayload.ScrollPosition;
        }
    }

    public void LoadLevels()
    {
        const int pageSize = 12;
        
        SpinnerOverlay.Show();

        var query = LoadedPayload.Query;
        var uri = query.BuildUri(pageSize, LoadedPayload.LastPage);

        RestClient.GetArray<OnlineLevel>(new RequestHelper
        {
            Uri = uri,
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(entries =>
        {
            if (entries == null) throw new Exception("Entries returned null");

            LoadedPayload.Levels.AddRange(entries.ToList());
            LoadedPayload.IsLastPageLoaded = true;
            LoadedPayload.CanLoadMore = entries.Length == pageSize;

            RenderLevels();
        }).Catch(error =>
        {
            Dialog.PromptGoBack("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
            Debug.LogError(error);
        }).Finally(() =>
        {
            SpinnerOverlay.Hide();
        });
    }
    
    public void LoadMoreLevels()
    {
        scrollRect.OnEndDrag(new PointerEventData(EventSystem.current).Also(it => it.button = PointerEventData.InputButton.Left));
        LoadedPayload.LastPage++;
        LoadedPayload.IsLastPageLoaded = false;
        LoadedPayload.CanLoadMore = false;
        LoadLevels();
    }

    private void RenderLevels()
    {
        var append = scrollRect.totalCount > 0;
        scrollRect.totalCount = LoadedPayload.Levels.Count;
        scrollRect.objectsToFill =
            LoadedPayload.Levels.Select(it => new LevelView{Level = it.ToLevel(LevelType.Community), DisplayOwner = true}).Cast<object>().ToArray();
        if (append) scrollRect.RefreshCells();
        else scrollRect.RefillCells();
    }

    private DateTimeOffset lastLoadMore = DateTimeOffset.MinValue;
    
    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();
        if (LoadedPayload != null && LoadedPayload.CanLoadMore 
                                  && scrollRect.content.anchoredPosition.y - scrollRect.content.sizeDelta.y > 128 
                                  && DateTimeOffset.Now - lastLoadMore > TimeSpan.FromSeconds(1))
        {
            lastLoadMore = DateTimeOffset.Now;
            LoadMoreLevels();
        }
    }

    public class Payload : ScreenPayload
    {
        public OnlineLevelQuery Query;
        public List<OnlineLevel> Levels = new List<OnlineLevel>();
        public int LastPage = 0;
        public bool IsLastPageLoaded;
        public bool CanLoadMore = true;
        public float ScrollPosition;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }

    public override ScreenPayload GetDefaultPayload() => new Payload
    {
        Query = new OnlineLevelQuery
        {
            sort = sortRadioGroup.radioGroup.Value,
            order = orderRadioGroup.radioGroup.Value,
            category = categoryRadioGroup.radioGroup.Value,
            time = timeRadioGroup.radioGroup.Value,
            search = searchInputField.text,
            owner = ownerInputField.text
        }
    };
    
    public const string Id = "CommunityLevelSelection";
    public override string GetId() => Id;
}

#if UNITY_EDITOR

[CustomEditor(typeof(CommunityLevelSelectionScreen))]
public class CommunityLevelSelectionScreenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Load more"))
        {
            ((CommunityLevelSelectionScreen) target).LoadMoreLevels();
        }
    }
}

#endif