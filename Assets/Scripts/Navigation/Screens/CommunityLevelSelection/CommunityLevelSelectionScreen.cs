using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CommunityLevelSelectionScreen : Screen, ScreenChangeListener
{
    private static float savedScrollPosition = -1;
    public static Content SavedContent;

    public const string Id = "CommunityLevelSelection";

    public LoopVerticalScrollRect scrollRect;

    public Text titleText;

    public ToggleRadioGroupPreferenceElement sortRadioGroup;
    public ToggleRadioGroupPreferenceElement orderRadioGroup;
    public ToggleRadioGroupPreferenceElement categoryRadioGroup;
    public ToggleRadioGroupPreferenceElement timeRadioGroup;
    public InputField searchInputField;

    private bool canLoadMore;

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        sortRadioGroup.SetContent(null, null,
            () => "creation_date", it => LoadContent(),
            new []
            {
                ("Uploaded date", "creation_date"),
                ("Modified date", "modified_date"),
                ("Rating", "rating"),
                ("Downloads", "downloads"),
                ("Difficulty", "difficulty"),
                ("Duration", "duration")
            });
        orderRadioGroup.SetContent(null, null,
            () => "desc", it => LoadContent(),
            new []
            {
                ("Ascending", "asc"),
                ("Descending", "desc")
            });
        categoryRadioGroup.SetContent(null, null,
            () => "category", it => LoadContent(),
            new []
            {
                ("All", "all"),
                ("Featured", "featured")
            });
        timeRadioGroup.SetContent(null, null,
            () => "all", it => LoadContent(),
            new []
            {
                ("Any time", "all"),
                ("Past week", "week"),
                ("Past month", "month"),
                ("Past 6 months", "halfyear"),
                ("Past year", "year")
            });
        searchInputField.onEndEdit.AddListener(value => LoadContent());

        titleText.text = "Browse";

        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            // Reload active content
            if (SavedContent.OnlineLevels != null)
            {
                savedScrollPosition = scrollRect.verticalNormalizedPosition;
                OnContentLoaded(SavedContent);
                scrollRect.verticalNormalizedPosition = savedScrollPosition;
            }
        });
        Context.ScreenManager.AddHandler(this);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (SavedContent != null)
        {
            sortRadioGroup.radioGroup.Select(SavedContent.Query.sort, false);
            orderRadioGroup.radioGroup.Select(SavedContent.Query.order, false);
            categoryRadioGroup.radioGroup.Select(SavedContent.Query.category, false);
            timeRadioGroup.radioGroup.Select(SavedContent.Query.time, false);
            searchInputField.SetTextWithoutNotify(SavedContent.Query.search);
            if (SavedContent.OnlineLevels != null)
            {
                OnContentLoaded(SavedContent);
                if (savedScrollPosition > 0)
                {
                    scrollRect.verticalNormalizedPosition = savedScrollPosition;
                }
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

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        canLoadMore = false;
        savedScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
        Context.ScreenManager.RemoveHandler(this);
    }

    public void LoadContent()
    {
        LoadContent(new OnlineLevelQuery
        {
            sort = sortRadioGroup.radioGroup.Value,
            order = orderRadioGroup.radioGroup.Value,
            category = categoryRadioGroup.radioGroup.Value,
            time = timeRadioGroup.radioGroup.Value,
            search = searchInputField.text
        });
    }

    public void LoadMoreContent()
    {
        if (!canLoadMore || SavedContent == null) return;
        canLoadMore = false;
        scrollRect.OnEndDrag(new PointerEventData(EventSystem.current).Also(it => it.button = PointerEventData.InputButton.Left));
        LoadContent(SavedContent.Query, SavedContent.PageLoaded + 1, true);
    }

    public void LoadContent(OnlineLevelQuery query, int page = 0, bool append = false)
    {
        SpinnerOverlay.Show();

        var uri = query.BuildUri(page: page);

        var content = new Content
        {
            Query = new OnlineLevelQuery
            {
                sort = sortRadioGroup.radioGroup.Value,
                order = orderRadioGroup.radioGroup.Value,
                category = categoryRadioGroup.radioGroup.Value,
                time = timeRadioGroup.radioGroup.Value,
                search = searchInputField.text
            }
        };
        RestClient.GetArray<OnlineLevel>(new RequestHelper
        {
            Uri = uri,
            EnableDebug = true
        }).Then(entries =>
        {
            if (entries == null) throw new Exception("Entries returned null");
            if (append)
            {
                content.OnlineLevels = SavedContent.OnlineLevels;
                content.OnlineLevels.AddRange(entries.ToList());
            }
            else
            {
                content.OnlineLevels = entries.ToList();
            }
            content.PageLoaded = page;
            SavedContent = content;
            OnContentLoaded(SavedContent, append);
        }).Catch(error =>
        {
            Toast.Next(Toast.Status.Failure, "Please check your network connection.");
            Debug.LogError(error);
        }).Finally(() =>
        {
            SpinnerOverlay.Hide();
            if (string.IsNullOrWhiteSpace(content.Query.search))
            {
                titleText.text = "Browse";
            }
            else
            {
                titleText.text = "Search: " + content.Query.search.Trim();
            }
        });
    }

    public void OnContentLoaded(Content content, bool append = false)
    {
        if (!append) scrollRect.ClearCells();
        scrollRect.totalCount = content.OnlineLevels.Count;
        scrollRect.objectsToFill = content.OnlineLevels.Select(it => it.ToLevel()).Cast<object>().ToArray();
        if (append) scrollRect.RefreshCells();
        else scrollRect.RefillCells();
        if (content.OnlineLevels.Count <= 9 && content.PageLoaded == 0)
        {
            // Impossible to have more levels
            canLoadMore = false;
        }
        else
        {
            Run.After(1f, () => canLoadMore = true);
        }
    }
    
    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();
        if (canLoadMore)
        {
            if (scrollRect.content.anchoredPosition.y - scrollRect.content.sizeDelta.y > 128)
            {
                LoadMoreContent();
            }
        }
    }

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this)
        {
            scrollRect.ClearCells();
            if (to is CommunityHomeScreen)
            {
                SavedContent = null;
                savedScrollPosition = default;
            }
        }
    }

    public class Content
    {
        public OnlineLevelQuery Query;
        public int PageLoaded = -1;
        public List<OnlineLevel> OnlineLevels; // Can be null
    }
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
            ((CommunityLevelSelectionScreen) target).LoadMoreContent();
        }
    }
}

#endif