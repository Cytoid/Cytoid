using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen, ScreenChangeListener
{
    private static float savedScrollPosition = -1;
    
    public const string Id = "LevelSelection";

    public LoopVerticalScrollRect scrollRect;

    public LabelSelect categorySelect;

    public RadioGroup sortByRadioGroup;
    public RadioGroup sortOrderRadioGroup;
    public InputField searchInputField;

    public override string GetId() => Id;

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        categorySelect.onSelect.AddListener((index, canvasGroup) => RefillLevels());

        sortByRadioGroup.onSelect.AddListener(value => RefillLevels());
        sortOrderRadioGroup.onSelect.AddListener(value => RefillLevels());
        searchInputField.onEndEdit.AddListener(value => RefillLevels());

        await Context.LevelManager.LoadAllFromDataPath();

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

    public void RefillLevels()
    {
        print("Refilled levels");

        // Sort with selected method
        Enum.TryParse(sortByRadioGroup.Value, out LevelSort sort);
        // Sort with selected order
        var asc = bool.Parse(sortOrderRadioGroup.Value);
        // Category?
        var category = categorySelect.SelectedIndex;
        var filters = new List<Func<Level, bool>>();
        if (category == 1)
        {
            filters.Add(level => level.Meta.id.StartsWith("io.cytoid"));
        }
        else
        {
            filters.Add(level => !level.Meta.id.StartsWith("io.cytoid"));
        }
        var query = searchInputField.text.Trim();

        RefillLevels(sort, asc, query, filters);
    }

    public void RefillLevels(LevelSort sort, bool asc, string query = "", List<Func<Level, bool>> filters = null)
    {
        var levels = new List<Level>(Context.LevelManager.LoadedLevels);

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
                    string.Compare(a.Meta.title, b.Meta.title, StringComparison.Ordinal) * (asc ? 1 : -1));
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
        if (from.GetId() == MainMenuScreen.Id && to.GetId() == Id)
        {
            // Clear search query
            searchInputField.SetTextWithoutNotify("");
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from.GetId() == Id)
        {
            Context.SpriteCache.DisposeTagged("LocalLevelCoverThumbnail");
            scrollRect.ClearCells();
        }
    }

}

public enum LevelSort
{
    Title,
    Difficulty,
    AddedDate,
    PlayedDate
}