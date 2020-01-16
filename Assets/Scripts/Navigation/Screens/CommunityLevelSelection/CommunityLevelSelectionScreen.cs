using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using UniRx.Async;
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

    public RadioGroup sortRadioGroup;
    public RadioGroup orderRadioGroup;
    public RadioGroup categoryRadioGroup;
    public RadioGroup timeRadioGroup;
    public InputField searchInputField;

    private bool canLoadMore;

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        sortRadioGroup.onSelect.AddListener(value => LoadContent());
        orderRadioGroup.onSelect.AddListener(value => LoadContent());
        categoryRadioGroup.onSelect.AddListener(value => LoadContent());
        timeRadioGroup.onSelect.AddListener(value => LoadContent());
        searchInputField.onEndEdit.AddListener(value => LoadContent());

        titleText.text = "Browse";

        Context.ScreenManager.AddHandler(this);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (SavedContent != null)
        {
            sortRadioGroup.Select(SavedContent.Query.sort, false);
            orderRadioGroup.Select(SavedContent.Query.order, false);
            categoryRadioGroup.Select(SavedContent.Query.category, false);
            timeRadioGroup.Select(SavedContent.Query.time, false);
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
            sort = sortRadioGroup.Value,
            order = orderRadioGroup.Value,
            category = categoryRadioGroup.Value,
            time = timeRadioGroup.Value,
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
                sort = sortRadioGroup.Value,
                order = orderRadioGroup.Value,
                category = categoryRadioGroup.Value,
                time = timeRadioGroup.Value,
                search = searchInputField.text
            }
        };
        RestClient.GetArray<OnlineLevel>(new RequestHelper
        {
            Uri = uri,
            EnableDebug = true
        }).Then(entries =>
        {
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
        if (from.GetId() == Id)
        {
            scrollRect.ClearCells();
            if (to.GetId() == CommunityHomeScreen.Id)
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