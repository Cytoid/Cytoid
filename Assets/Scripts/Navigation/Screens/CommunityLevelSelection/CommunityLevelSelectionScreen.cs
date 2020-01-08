using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
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

    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        sortRadioGroup.onSelect.AddListener(value => LoadContent());
        orderRadioGroup.onSelect.AddListener(value => LoadContent());
        categoryRadioGroup.onSelect.AddListener(value => LoadContent());
        timeRadioGroup.onSelect.AddListener(value => LoadContent());
        searchInputField.onEndEdit.AddListener(LoadContent);

        Context.ScreenManager.AddHandler(this);
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        if (SavedContent != null)
        {
            sortRadioGroup.Select(SavedContent.Sort, false);
            orderRadioGroup.Select(SavedContent.Order, false);
            categoryRadioGroup.Select(SavedContent.Category, false);
            timeRadioGroup.Select(SavedContent.Time, false);
            searchInputField.SetTextWithoutNotify(SavedContent.Query);
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
        savedScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
        Context.ScreenManager.RemoveHandler(this);
    }

    public void LoadContent(string query = null)
    {
        LoadContent(sortRadioGroup.Value, orderRadioGroup.Value, categoryRadioGroup.Value, timeRadioGroup.Value,
            query ?? searchInputField.text);
    }

    public void LoadContent(string sort, string order, string category, string time, string query = "")
    {
        SpinnerOverlay.Show();

        var uri = $"{Context.ApiBaseUrl}/levels?sort={sort}&order={order}&search={query}&page=0";
        if (category == "featured")
        {
            uri += "&featured=true";
        }
        var content = new Content
        {
            Sort = sortRadioGroup.Value,
            Order = orderRadioGroup.Value,
            Category = categoryRadioGroup.Value,
            Time = timeRadioGroup.Value,
            Query = searchInputField.text
        };
        RestClient.GetArray<OnlineLevel>(new RequestHelper
        {
            Uri = uri,
            EnableDebug = true
        }).Then(entries =>
        {
            content.OnlineLevels = entries.ToList();
            SavedContent = content;
            OnContentLoaded(SavedContent);
        }).Catch(Debug.LogError).Finally(SpinnerOverlay.Hide);
    }

    public void OnContentLoaded(Content content)
    {
        scrollRect.totalCount = content.OnlineLevels.Count;
        scrollRect.objectsToFill = content.OnlineLevels.Select(it => it.ToLevel()).Cast<object>().ToArray();
        scrollRect.RefillCells();
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
        public string Sort;
        public string Order;
        public string Category;
        public string Time;
        public string Query;
        public List<OnlineLevel> OnlineLevels;
    }
}