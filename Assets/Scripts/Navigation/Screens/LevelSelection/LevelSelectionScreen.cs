using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen
{

    public TransitionElement levelGrid;
    public LoopVerticalScrollRect scrollRect;

    public LabelSelect categorySelect;

    [GetComponentInChildren] public ActionTabs actionTabs;
    public ToggleRadioGroupPreferenceElement sortByRadioGroup;
    public ToggleRadioGroupPreferenceElement sortOrderRadioGroup;
    public InputField searchInputField;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        Context.Library.OnLibraryLoaded.AddListener(() =>
        {
            if (State == ScreenState.Active) RefillLevels();
        });
        
        void SetupOptions() {
            var lp = Context.Player;
            sortByRadioGroup.SetContent("LEVEL_SELECT_SORT_BY".Get(), null, () => lp.Settings.LocalLevelSort,
                it => lp.Settings.LocalLevelSort = it, new[]
                {
                    ("LEVEL_SELECT_SORT_BY_ADDED_DATE".Get(), LevelSort.AddedDate),
                    ("LEVEL_SELECT_SORT_BY_PLAYED_DATE".Get(), LevelSort.LastPlayedDate),
                    ("LEVEL_SELECT_SORT_BY_DIFFICULTY".Get(), LevelSort.Difficulty),
                    ("LEVEL_SELECT_SORT_BY_TITLE".Get(), LevelSort.Title),
                    ("LEVEL_SELECT_SORT_BY_PLAY_COUNT".Get(), LevelSort.PlayCount),
                }).SaveSettingsOnChange();
            sortByRadioGroup.radioGroup.onSelect.AddListener(value => RefillLevels());
            sortOrderRadioGroup.SetContent("LEVEL_SELECT_SORT_ORDER".Get(), null, () => lp.Settings.LocalLevelSortIsAscending,
                it => lp.Settings.LocalLevelSortIsAscending = it, new []
                {
                    ("LEVEL_SELECT_SORT_ORDER_ASC".Get(), true),
                    ("LEVEL_SELECT_SORT_ORDER_DESC".Get(), false)
                }).SaveSettingsOnChange();
            sortOrderRadioGroup.radioGroup.onSelect.AddListener(value => RefillLevels());
            searchInputField.onEndEdit.AddListener(value =>
            {
                actionTabs.Close();
                RefillLevels();
            });
        }
        
        SetupOptions();
        Context.OnLanguageChanged.AddListener(SetupOptions);

        Context.LevelManager.OnLevelDeleted.AddListener(_ =>
        {
            if (State != ScreenState.Active) return;
            RefillLevels(true);
        });
    }

    public override void OnScreenBecameInactive()
    {
        base.OnScreenBecameInactive();
        if (LoadedPayload != null)
        {
            LoadedPayload.ScrollPosition = scrollRect.verticalNormalizedPosition;
            LoadedPayload.CategoryIndex = categorySelect.SelectedIndex;
        }
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();

        Destroy(scrollRect);
    }

    protected override void Render()
    {
        if (LoadedPayload.ScrollPosition > 0) scrollRect.SetVerticalNormalizedPositionFix(LoadedPayload.ScrollPosition);
        categorySelect.Select(LoadedPayload.CategoryIndex);
        RefillLevels();
        categorySelect.onSelect.AddListener((index, canvasGroup) =>
        {
            RefillLevels();
        });
        base.Render();
    }

    public void RefillLevels(bool keepScrollPosition = false)
    {
        var scrollPosition = scrollRect.verticalNormalizedPosition;

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
                filters.Add(level => Context.Library.Levels.ContainsKey(level.Id));
                break;
            case 2:
                filters.Add(level => !Context.Library.Levels.ContainsKey(level.Id));
                break;
        }
        var query = searchInputField.text.Trim();

        RefillLevels(sort, asc, query, filters);
        if (scrollRect.totalCount == 0 && category == 0 && query.IsNullOrEmptyTrimmed() && Context.Player.ShouldOneShot("Tips: No Community Levels Yet"))
        {
            Dialog.PromptAlert("DIALOG_TIPS_NO_COMMUNITY_LEVELS_YET".Get());
        }

        if (keepScrollPosition)
        {
            scrollRect.SetVerticalNormalizedPositionFix(scrollPosition);
        }
    }

    public void RefillLevels(LevelSort sort, bool asc, string query = "", List<Func<Level, bool>> filters = null)
    {
        var dict = new Dictionary<string, Level>(Context.LevelManager.LoadedLocalLevels);
        foreach (var id in BuiltInData.TrainingModeLevelIds) dict.Remove(id);
        foreach (var (id, level) in Context.Library.Levels)
        {
            dict[id] = level.Level.ToLevel(LevelType.User);
        }

        var levels = dict.Values.ToList();
        
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
                    string.Compare(a.Meta.title, b.Meta.title, StringComparison.InvariantCulture) * (asc ? 1 : -1));
                break;
            case LevelSort.Difficulty:
                levels.Sort((a, b) =>
                    (asc ? a.Meta.GetEasiestDifficultyLevel() : b.Meta.GetHardestDifficultyLevel())
                    .CompareTo(asc ? b.Meta.GetEasiestDifficultyLevel() : a.Meta.GetHardestDifficultyLevel())
                );
                break;
            case LevelSort.AddedDate:
                levels.Sort((a, b) => a.Record.AddedDate.CompareTo(b.Record.AddedDate) * (asc ? 1 : -1));
                break;
            case LevelSort.LastPlayedDate:
                levels.Sort((a, b) => a.Record.LastPlayedDate.CompareTo(b.Record.LastPlayedDate) * (asc ? 1 : -1));
                break;
            case LevelSort.PlayCount:
                levels.Sort((a, b) => a.Record.PlayCounts.Values.Sum().CompareTo(b.Record.PlayCounts.Values.Sum()) * (asc ? 1 : -1));
                break;
        }

        scrollRect.totalCount = levels.Count;
        scrollRect.objectsToFill = levels.Select(it => new LevelView{Level = it}).ToArray().Cast<object>().ToArray();
        scrollRect.RefillCells();
    }

    public override void OnScreenChangeStarted(Screen from, Screen to)
    {
        base.OnScreenChangeStarted(from, to);
        if (to == this)
        {
            levelGrid.Enter();
            // Clear search query
            searchInputField.SetTextWithoutNotify("");
        }
    }

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (from == this)
        {
            if (to is GamePreparationScreen) {
                Context.AssetMemory.DisposeTaggedCacheAssets(AssetTag.LocalLevelCoverThumbnail);
            }
            if (to is MainMenuScreen)
            {
                levelGrid.Leave();
                scrollRect.ClearCells();
                LoadedPayload = null;
            }
        }
    }

    public class Payload : ScreenPayload
    {
        public int CategoryIndex;
        public float ScrollPosition;
    }
    
    public new Payload IntentPayload => (Payload) base.IntentPayload;
    public new Payload LoadedPayload
    {
        get => (Payload) base.LoadedPayload;
        set => base.LoadedPayload = value;
    }

    public override ScreenPayload GetDefaultPayload() => new Payload();
    
    public const string Id = "LevelSelection";
    public override string GetId() => Id;

}

public enum LevelSort
{
    Title,
    Difficulty,
    AddedDate,
    LastPlayedDate,
    PlayCount
}