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
    public static Content LoadedContent;
    private static float savedScrollPosition = -1;

    public const string Id = "CommunityLevelSelection";

    public LoopVerticalScrollRect scrollRect;

    public Text titleText;

    [GetComponentInChildren] public ActionTabs actionTabs;
    public ToggleRadioGroupPreferenceElement sortRadioGroup;
    public ToggleRadioGroupPreferenceElement orderRadioGroup;
    public ToggleRadioGroupPreferenceElement categoryRadioGroup;
    public ToggleRadioGroupPreferenceElement timeRadioGroup;
    public InputField searchInputField;
    public InputField ownerInputField;

    private bool canLoadMore;

    public override string GetId() => Id;

    private void InstantiateOptions(Content content)
    {
        var search = content?.Query != null && !content.Query.search.IsNullOrEmptyTrimmed();
        titleText.text = "COMMUNITY_SELECT_BROWSE".Get();
        sortRadioGroup.SetContent(null, null,
            () => "creation_date", it =>
            {
                LoadedContent.Query.sort = it;
                LoadContent();
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
                LoadedContent.Query.order = it;
                LoadContent();
            },
            new[]
            {
                ("COMMUNITY_SELECT_SORT_ORDER_ASC".Get(), "asc"),
                ("COMMUNITY_SELECT_SORT_ORDER_DESC".Get(), "desc")
            });
        categoryRadioGroup.SetContent(null, null,
            () => "category", it => {
                LoadedContent.Query.category = it;
                LoadContent();
            },
            new[]
            {
                ("COMMUNITY_SELECT_CATEGORY_ALL".Get(), "all"),
                ("COMMUNITY_SELECT_CATEGORY_FEATURED".Get(), "featured")
            });
        timeRadioGroup.SetContent(null, null,
            () => "all", it => {
                LoadedContent.Query.time = it;
                LoadContent();
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

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        InstantiateOptions(null);

        searchInputField.onEndEdit.AddListener(value =>
        {
            actionTabs.Close();
            LoadedContent.Query.search = value.Trim();
            LoadContent();
        });
        ownerInputField.onEndEdit.AddListener(value =>
        {
            actionTabs.Close();
            LoadedContent.Query.owner = value.Trim();
            LoadContent();
        });

        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            // Reload active content
            if (LoadedContent.OnlineLevels != null)
            {
                savedScrollPosition = scrollRect.verticalNormalizedPosition;
                OnContentLoaded();
                scrollRect.SetVerticalNormalizedPositionFix(savedScrollPosition);
            }
        });
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (LoadedContent != null)
        {
            if (LoadedContent.OnlineLevels != null)
            {
                LevelCard.DoNotLoadCover = true;
                OnContentLoaded();
                if (savedScrollPosition > 0)
                {
                    scrollRect.SetVerticalNormalizedPositionFix(savedScrollPosition);
                }

                await UniTask.DelayFrame(5);
                LevelCard.DoNotLoadCover = false;
            }
            else
            {
                LoadContent();
            }
        }
        else
        {
            LoadedContent = new Content
            {
                Query = new OnlineLevelQuery
                {
                    sort = sortRadioGroup.radioGroup.Value,
                    order = orderRadioGroup.radioGroup.Value,
                    category = categoryRadioGroup.radioGroup.Value,
                    time = timeRadioGroup.radioGroup.Value,
                    search = searchInputField.text,
                    owner = ownerInputField.text
                },
                OnlineLevels = null // Signal reload
            };
            LoadContent();
        }
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        LevelCard.DoNotLoadCover = false;
        
        canLoadMore = false;
        savedScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    public void LoadMoreContent()
    {
        if (!canLoadMore || LoadedContent == null) return;
        canLoadMore = false;
        scrollRect.OnEndDrag(
            new PointerEventData(EventSystem.current).Also(it => it.button = PointerEventData.InputButton.Left));
        LoadContent(LoadedContent.PageLoaded + 1, true);
    }

    public void LoadContent(int page = 0, bool append = false)
    {
        canLoadMore = false;
        UpdateViews();
        SpinnerOverlay.Show();

        var query = LoadedContent.Query;
        var uri = query.BuildUri(12, page);

        var content = new Content
        {
            Query = query
        };
        RestClient.GetArray<OnlineLevel>(new RequestHelper
        {
            Uri = uri,
            Headers = Context.OnlinePlayer.GetRequestHeaders(),
            EnableDebug = true
        }).Then(entries =>
        {
            if (entries == null) throw new Exception("Entries returned null");
            if (LoadedContent?.OnlineLevels == null)
            {
                append = false;
                Debug.LogWarning("LoadedContent is null but set append to true");
            }
            if (append)
            {
                content.OnlineLevels = LoadedContent.OnlineLevels;
                content.OnlineLevels.AddRange(entries.ToList());
            }
            else
            {
                content.OnlineLevels = entries.ToList();
            }

            content.PageLoaded = page;
            LoadedContent = content;

            OnContentLoaded(append);
        }).Catch(error =>
        {
            Toast.Next(Toast.Status.Failure, "TOAST_CHECK_NETWORK_CONNECTION".Get());
            Debug.LogError(error);
        }).Finally(() =>
        {
            SpinnerOverlay.Hide();
        });
    }

    public void OnContentLoaded(bool append = false)
    {
        UpdateViews();
        if (!append) scrollRect.ClearCells();
        scrollRect.totalCount = LoadedContent.OnlineLevels.Count;
        scrollRect.objectsToFill =
            LoadedContent.OnlineLevels.Select(it => new LevelView{Level = it.ToLevel(LevelType.Community), DisplayOwner = true}).Cast<object>().ToArray();
        if (append) scrollRect.RefreshCells();
        else scrollRect.RefillCells();
        if (LoadedContent.OnlineLevels.Count <= 9 && LoadedContent.PageLoaded == 0)
        {
            // Impossible to have more levels
            canLoadMore = false;
        }
        else
        {
            Run.After(1f, () => canLoadMore = true);
        }
    }

    private void UpdateViews()
    {
        sortRadioGroup.radioGroup.Select(LoadedContent.Query.sort, false);
        orderRadioGroup.radioGroup.Select(LoadedContent.Query.order, false);
        categoryRadioGroup.radioGroup.Select(LoadedContent.Query.category, false);
        timeRadioGroup.radioGroup.Select(LoadedContent.Query.time, false);
        searchInputField.SetTextWithoutNotify(LoadedContent.Query.search);
        ownerInputField.SetTextWithoutNotify(LoadedContent.Query.owner);
        if (LoadedContent.Query.search.IsNullOrEmptyTrimmed() && LoadedContent.Query.owner.IsNullOrEmptyTrimmed())
        {
            titleText.text = "COMMUNITY_SELECT_BROWSE".Get();
        }
        else
        {
            var text = "";
            if (!LoadedContent.Query.search.IsNullOrEmptyTrimmed())
            {
                text += "COMMUNITY_SELECT_SEARCH_QUERY".Get(LoadedContent.Query.search.Trim());
            }
            if (!LoadedContent.Query.owner.IsNullOrEmptyTrimmed())
            {
                if (text != "") text += " & ";
                text += "COMMUNITY_SELECT_SEARCH_UPLOADER".Get(LoadedContent.Query.owner.Trim());
            }
            titleText.text = text;
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

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            scrollRect.ClearCells();
            if (to is CommunityHomeScreen)
            {
                LoadedContent = null;
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