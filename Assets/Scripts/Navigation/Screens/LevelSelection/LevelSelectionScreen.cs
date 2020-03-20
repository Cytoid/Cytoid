using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen, ScreenChangeListener
{
    private static float savedScrollPosition = -1;
    public static Content SavedContent;
    
    public const string Id = "LevelSelection";

    public LoopVerticalScrollRect scrollRect;

    public LabelSelect categorySelect;

    [GetComponentInChildren] public ActionTabs actionTabs;
    public ToggleRadioGroupPreferenceElement sortByRadioGroup;
    public ToggleRadioGroupPreferenceElement sortOrderRadioGroup;
    public InputField searchInputField;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        categorySelect.onSelect.AddListener((index, canvasGroup) => RefillLevels());
        
        void InstantiateOptions() {
            var lp = Context.LocalPlayer;
            sortByRadioGroup.SetContent("LEVEL_SELECT_SORT_BY".Get(), null, () => lp.LocalLevelsSortBy,
                it => lp.LocalLevelsSortBy = it, new []
                {
                    ("LEVEL_SELECT_SORT_BY_ADDED_DATE".Get(), LevelSort.AddedDate.ToString()),
                    ("LEVEL_SELECT_SORT_BY_PLAYED_DATE".Get(), LevelSort.PlayedDate.ToString()),
                    ("LEVEL_SELECT_SORT_BY_DIFFICULTY".Get(), LevelSort.Difficulty.ToString()),
                    ("LEVEL_SELECT_SORT_BY_TITLE".Get(), LevelSort.Title.ToString())
                });
            sortByRadioGroup.radioGroup.onSelect.AddListener(value => RefillLevels());
            sortOrderRadioGroup.SetContent("LEVEL_SELECT_SORT_ORDER".Get(), null, () => lp.LocalLevelsSortInAscendingOrder,
                it => lp.LocalLevelsSortInAscendingOrder = it, new []
                {
                    ("LEVEL_SELECT_SORT_ORDER_ASC".Get(), true),
                    ("LEVEL_SELECT_SORT_ORDER_DESC".Get(), false)
                });
            sortOrderRadioGroup.radioGroup.onSelect.AddListener(value => RefillLevels());
            searchInputField.onEndEdit.AddListener(value =>
            {
                actionTabs.Close();
                if (!value.IsNullOrEmptyTrimmed()) RefillLevels();
            });
        }
        
        InstantiateOptions();
        Context.OnLanguageChanged.AddListener(InstantiateOptions);

        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            RefillLevels(true);
        });
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        RefillLevels();
        if (savedScrollPosition > 0)
        {
            scrollRect.SetVerticalNormalizedPositionFix(savedScrollPosition);
        }

        if (SavedContent != null)
        {
            categorySelect.Select(SavedContent.CategoryIndex);
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
    }

    public void RefillLevels(bool saveScrollPosition = false)
    {
        print("Refilling levels");

        if (saveScrollPosition)
        {
            savedScrollPosition = scrollRect.verticalNormalizedPosition;
        }

        // Sort with selected method
        Enum.TryParse(sortByRadioGroup.radioGroup.Value, out LevelSort sort);
        // Sort with selected order
        var asc = bool.Parse(sortOrderRadioGroup.radioGroup.Value);
        // Category?
        var category = categorySelect.SelectedIndex;
        var filters = new List<Func<Level, bool>>();
        switch (category)
        {
            case 1:
                filters.Add(level => level.Id.StartsWith("io.cytoid"));
                break;
            case 2:
                filters.Add(level => !level.Id.StartsWith("io.cytoid"));
                break;
        }
        var query = searchInputField.text.Trim();

        RefillLevels(sort, asc, query, filters);

        if (saveScrollPosition)
        {
            scrollRect.SetVerticalNormalizedPositionFix(savedScrollPosition);
        }
    }

    public void RefillLevels(LevelSort sort, bool asc, string query = "", List<Func<Level, bool>> filters = null)
    {
        var levels = new List<Level>(Context.LevelManager.LoadedLocalLevels.Values.Where(it => it.Type == LevelType.Community));

        if (!query.IsNullOrEmptyTrimmed())
        {
            query = query.Trim();
            var keywords = query.Split(' ');
            var filteredLevels = new List<Level>(levels);
            foreach (var level in levels)
            {
                var meta = level.Meta;
                foreach (var keyword in keywords.Select(it => it.ToLower()))
                {
                    if (meta.title != null && meta.title.ToLower().Contains(keyword)) continue;
                    if (meta.title_localized != null && meta.title_localized.ToLower().Contains(keyword)) continue;
                    if (meta.artist != null && meta.artist.ToLower().Contains(keyword)) continue;
                    if (meta.artist_localized != null && meta.artist_localized.ToLower().Contains(keyword)) continue;
                    if (meta.charter != null && meta.charter.ToLower().Contains(keyword)) continue;
                    if (meta.storyboarder != null && meta.storyboarder.ToLower().Contains(keyword)) continue;
                    filteredLevels.Remove(level);
                }
            }

            levels = filteredLevels;
        }

        if (filters != null)
        {
            var filteredLevels = new List<Level>();
            foreach (var level in levels)
            {
                var fail = false;
                foreach (var predicate in filters)
                {
                    if (predicate(level)) continue;
                    fail = true;
                    break;
                }

                if (fail) continue;
                filteredLevels.Add(level);
            }

            levels = filteredLevels;
        }

        switch (sort)
        {
            case LevelSort.Title:
                levels.Sort((a, b) =>
                    string.Compare(a.Meta.title.LowerCaseFirstChar(), b.Meta.title.LowerCaseFirstChar(), StringComparison.Ordinal) * (asc ? 1 : -1));
                break;
            case LevelSort.Difficulty:
                levels.Sort((a, b) =>
                    (asc ? a.Meta.GetEasiestDifficultyLevel() : b.Meta.GetHardestDifficultyLevel())
                    .CompareTo(asc ? b.Meta.GetEasiestDifficultyLevel() : a.Meta.GetHardestDifficultyLevel())
                );
                break;
            case LevelSort.AddedDate:
                levels.Sort((a, b) => a.AddedDate.CompareTo(b.AddedDate) * (asc ? 1 : -1));
                break;
            case LevelSort.PlayedDate:
                levels.Sort((a, b) => a.PlayedDate.CompareTo(b.PlayedDate) * (asc ? 1 : -1));
                break;
        }

        scrollRect.totalCount = levels.Count;
        scrollRect.objectsToFill = levels.ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
    }

    public override void OnScreenChangeStarted(Screen from, Screen to)
    {
        base.OnScreenChangeStarted(from, to);
        if (from is MainMenuScreen && to == this)
        {
            // Clear search query
            searchInputField.SetTextWithoutNotify("");
        }
        if (from == this)
        {
            SavedContent = new Content
            {
                CategoryIndex = categorySelect.SelectedIndex,
                Sort = (LevelSort) Enum.Parse(typeof(LevelSort), sortByRadioGroup.radioGroup.Value),
                IsAscendingOrder = bool.Parse(sortOrderRadioGroup.radioGroup.Value)
            };
        }
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalCoverThumbnail);
            scrollRect.ClearCells();
            if (to is MainMenuScreen)
            {
                savedScrollPosition = default;
            }
        }
    }

    public class Content
    {
        public int CategoryIndex;
        public LevelSort Sort;
        public bool IsAscendingOrder;
    }

}

public enum LevelSort
{
    Title,
    Difficulty,
    AddedDate,
    PlayedDate
}