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

    public ToggleRadioGroupPreferenceElement sortByRadioGroup;
    public ToggleRadioGroupPreferenceElement sortOrderRadioGroup;
    public InputField searchInputField;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        categorySelect.onSelect.AddListener((index, canvasGroup) => RefillLevels());

        var lp = Context.LocalPlayer;
        sortByRadioGroup.SetContent("SORT BY", null, () => lp.LocalLevelsSortBy,
            it => lp.LocalLevelsSortBy = it, new []
            {
                ("Added date", LevelSort.AddedDate.ToString()),
                ("Played date", LevelSort.PlayedDate.ToString()),
                ("Difficulty", LevelSort.Difficulty.ToString()),
                ("Title", LevelSort.Title.ToString())
            });
        sortByRadioGroup.radioGroup.onSelect.AddListener(value => RefillLevels());
        sortOrderRadioGroup.SetContent("SORT BY", null, () => lp.LocalLevelsSortInAscendingOrder,
            it => lp.LocalLevelsSortInAscendingOrder = it, new []
            {
                ("Ascending", true),
                ("Descending", false)
            });
        sortOrderRadioGroup.radioGroup.onSelect.AddListener(value => RefillLevels());
        searchInputField.onEndEdit.AddListener(value => RefillLevels());
        
        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            RefillLevels(true);
        });
        
        Context.ScreenManager.AddHandler(this);
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();
        RefillLevels();
        if (savedScrollPosition > 0)
        {
            scrollRect.verticalNormalizedPosition = savedScrollPosition;
        }

        if (SavedContent != null)
        {
            print("not null");
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

        Context.ScreenManager.RemoveHandler(this);
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
            scrollRect.verticalNormalizedPosition = savedScrollPosition;
        }
    }

    public void RefillLevels(LevelSort sort, bool asc, string query = "", List<Func<Level, bool>> filters = null)
    {
        var levels = new List<Level>(Context.LevelManager.LoadedLocalLevels.Values);

        if (!string.IsNullOrEmpty(query))
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

    public void OnScreenChangeStarted(Screen from, Screen to)
    {
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

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from == this)
        {
            Context.SpriteCache.DisposeTagged("LocalLevelCoverThumbnail");
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