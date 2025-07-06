using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommunityLevelSelectionScreen : Screen, LevelCardEventHandler, LevelBatchSelection
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
    
    public TransitionElement batchActionBar;
    public Text batchActionBarMessage;
    public InteractableMonoBehavior batchActionCancelButton;
    public InteractableMonoBehavior batchActionDownloadButton;

    private readonly LevelBatchSelectionDownloadHandler levelBatchSelectionHandler = new LevelBatchSelectionDownloadHandler();
    
    public bool IsBatchSelectingLevels => levelBatchSelectionHandler.IsBatchSelectingLevels;
    public Dictionary<string, Level> BatchSelectedLevels => levelBatchSelectionHandler.BatchSelectedLevels;
    public LevelBatchAction LevelBatchAction => levelBatchSelectionHandler.LevelBatchAction;
    public bool OnLevelCardPressed(LevelView view) => levelBatchSelectionHandler.OnLevelCardPressed(view);
    public void OnLevelCardLongPressed(LevelView view) => levelBatchSelectionHandler.OnLevelCardLongPressed(view);

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        searchInputField.onEndEdit.AddListener(value =>
        {
            actionTabs.Close();
            if (LoadedPayload.Query.search.IsNullOrEmptyTrimmed() && !value.IsNullOrEmptyTrimmed())
            {
                LoadedPayload.Query.sort = "relevance"; // Reset to relevance
            }
            LoadedPayload.Query.search = value.Trim();
            LoadLevels(true);
        });
        ownerInputField.onEndEdit.AddListener(value =>
        {
            actionTabs.Close();
            LoadedPayload.Query.owner = value.Trim().ToLower();
            LoadLevels(true);
        });

        SetupOptions();
        Context.OnLanguageChanged.AddListener(SetupOptions);
        
        levelBatchSelectionHandler.OnEnterBatchSelection.AddListener(() =>
        {
            batchActionBar.transform.RebuildLayout();
            batchActionBar.Enter();
        });
        levelBatchSelectionHandler.OnLeaveBatchSelection.AddListener(() =>
        {
            batchActionBar.Leave();
        });
        levelBatchSelectionHandler.batchActionBarMessage = batchActionBarMessage;
        batchActionCancelButton.onPointerClick.AddListener(_ => levelBatchSelectionHandler.LeaveBatchSelection());
        batchActionDownloadButton.onPointerClick.AddListener(_ => levelBatchSelectionHandler.DownloadBatchSelection());
    }

    public override void OnScreenBecameActive()
    {
        nextLoadMore = DateTimeOffset.MaxValue;
        if (LoadedPayload != null && LoadedPayload.CanLoadMore)
        {
            nextLoadMore = DateTimeOffset.Now + TimeSpan.FromSeconds(1);
        }
        base.OnScreenBecameActive();
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

    private void SetupOptions()
    {
        sortRadioGroup.SetContent(null, null,
            () => "creation_date", it =>
            {
                LoadedPayload.Query.sort = it;
                LoadLevels(true);
            },
            new []
            {
                ("COMMUNITY_SELECT_SORT_BY_RELEVANCE".Get(), "relevance"),
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
                LoadLevels(true);
            },
            new[]
            {
                ("COMMUNITY_SELECT_SORT_ORDER_ASC".Get(), "asc"),
                ("COMMUNITY_SELECT_SORT_ORDER_DESC".Get(), "desc")
            });
        categoryRadioGroup.SetContent(null, null,
            () => "all", it => {
                LoadedPayload.Query.category = it;
                LoadLevels(true);
            },
            new[]
            {
                ("COMMUNITY_SELECT_CATEGORY_ALL".Get(), "all"),
                ("COMMUNITY_SELECT_CATEGORY_FEATURED".Get(), "featured")
            });
        timeRadioGroup.SetContent(null, null,
            () => "all", it => {
                LoadedPayload.Query.time = it;
                LoadLevels(true);
            },
            new[]
            {
                ("COMMUNITY_SELECT_TIME_ANY_TIME".Get(), "all"),
                ("COMMUNITY_SELECT_TIME_PAST_WEEK".Get(), "week"),
                ("COMMUNITY_SELECT_TIME_PAST_MONTH".Get(), "month"),
                ("COMMUNITY_SELECT_TIME_PAST_6_MONTHS".Get(), "halfyear"),
                ("COMMUNITY_SELECT_TIME_PAST_YEAR".Get(), "year")
            });
        // sortRadioGroup.transform.parent.RebuildLayout();
    }

    private async UniTask UpdateComponents()
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

        sortRadioGroup.radioGroup.GetComponent<VerticalLayoutGroup>().enabled = true;
        sortRadioGroup.radioGroup.GetComponent<ContentSizeFitter>().enabled = true;
        sortRadioGroup.GetComponent<VerticalLayoutGroup>().enabled = true;
        sortRadioGroup.GetComponent<ContentSizeFitter>().enabled = true;
        sortRadioGroup.radioGroup.transform.GetChild(0).gameObject.SetActive(!LoadedPayload.Query.search.IsNullOrEmptyTrimmed());
        sortRadioGroup.transform.parent.RebuildLayout();

        await UniTask.DelayFrame(0);
        
        sortRadioGroup.radioGroup.GetComponent<VerticalLayoutGroup>().enabled = false;
        sortRadioGroup.radioGroup.GetComponent<ContentSizeFitter>().enabled = false;
        sortRadioGroup.GetComponent<VerticalLayoutGroup>().enabled = false;
        sortRadioGroup.GetComponent<ContentSizeFitter>().enabled = false;
    }

    protected override void Render()
    {
        UpdateComponents();
        
        scrollRect.ClearCells();
        RenderLevels();
        
        base.Render();
    }

    protected override void OnRendered()
    {
        base.OnRendered();

        if (!LoadedPayload.IsLastPageLoaded)
        {
            LoadLevels(false);
        }
        else
        {
            if (LoadedPayload.ScrollPosition > -1) scrollRect.verticalNormalizedPosition = LoadedPayload.ScrollPosition;
        }
    }

    public void LoadLevels(bool reset)
    {
        if (reset)
        {
            levelBatchSelectionHandler.LeaveBatchSelection();
        }
        
        const int pageSize = 12;
        
        SpinnerOverlay.Show();

        var query = LoadedPayload.Query;
        var uri = query.BuildUri(pageSize, LoadedPayload.LastPage);

        if (reset)
        {
            UpdateComponents();
            LoadedPayload.Levels.Clear();
            LoadedPayload.LastPage = 0;
            LoadedPayload.IsLastPageLoaded = false;
            LoadedPayload.CanLoadMore = false;
            LoadedPayload.ScrollPosition = 0;
        }
        
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

            if (LoadedPayload.Levels.Count == 0)
            {
                Toast.Next(Toast.Status.Failure, "TOAST_NO_RESULTS_FOUND".Get());
            }
            
            if (reset)
            {
                scrollRect.ClearCells();
            }
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
        LoadLevels(false);
    }

    private void RenderLevels()
    {
        var append = scrollRect.totalCount > 0;
        scrollRect.totalCount = LoadedPayload.Levels.Count;
        scrollRect.objectsToFill =
            LoadedPayload.Levels.Select(it => new LevelView{Level = it.ToLevel(LevelType.User), DisplayOwner = true}).Cast<object>().ToArray();
        if (append) scrollRect.RefreshCells();
        else scrollRect.RefillCells();

        nextLoadMore = DateTimeOffset.Now + TimeSpan.FromSeconds(1);
    }

    private DateTimeOffset nextLoadMore = DateTimeOffset.MaxValue;
    
    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();
        if (LoadedPayload != null && LoadedPayload.CanLoadMore 
                                  && scrollRect.content.anchoredPosition.y - scrollRect.content.sizeDelta.y > 128 
                                  && DateTimeOffset.Now >= nextLoadMore)
        {
            nextLoadMore = DateTimeOffset.Now + TimeSpan.FromSeconds(1);
            LoadMoreLevels();
        }
    }

    public override void OnScreenChangeStarted(Screen @from, Screen to)
    {
        base.OnScreenChangeStarted(@from, to);
        if (from == this)
        {
            levelBatchSelectionHandler.LeaveBatchSelection();
        }
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this && to is CommunityHomeScreen)
        {
            scrollRect.ClearCells();
            LoadedPayload = null;
        }
    }

    public class Payload : ScreenPayload
    {
        public OnlineLevelQuery Query;
        public List<OnlineLevel> Levels = new List<OnlineLevel>();
        public int LastPage = 0;
        public bool IsLastPageLoaded;
        public bool CanLoadMore = true;
        public float ScrollPosition = -1;
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
            sort = "creation_date",
            order = "desc",
            category = "all",
            time = "all",
            search = "",
            owner = ""
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
