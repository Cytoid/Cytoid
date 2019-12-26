using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen, RadioGroupChangeListener, ScreenChangeListener
{
    public const string Id = "LevelSelection";
    
    public LoopVerticalScrollRect scrollRect;

    public RadioGroup sortByRadioGroup;
    public RadioGroup sortOrderRadioGroup;
    public InputField searchInputField;

    private UniTask willSearchTask;
    private string query;
    
    public override string GetId() => Id;

    protected override void Awake()
    {
        base.Awake();
    }

    public override async void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        
        sortByRadioGroup.AddHandler(this);
        sortOrderRadioGroup.AddHandler(this);
        searchInputField.onValueChanged.AddListener(WillSearch);
        
        await Context.LevelManager.LoadAllFromDataPath();

        RefillLevels();
        
        Context.ScreenManager.AddHandler(this);
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        
        Destroy(scrollRect);

        sortByRadioGroup.RemoveHandler(this);
        sortOrderRadioGroup.RemoveHandler(this);
        
        Context.ScreenManager.RemoveHandler(this);
    }
    
    public void OnRadioGroupChange(RadioGroup radioGroup, string value)
    {
        RefillLevels();
    }

    public async void WillSearch(string query)
    {
        if (willSearchTask.Status == AwaiterStatus.Pending) willSearchTask.Forget();
        this.query = query;
        willSearchTask = UniTask.Delay(TimeSpan.FromSeconds(0.5));
        await willSearchTask;
        
        RefillLevels();
    }
    
    public void RefillLevels()
    {
        // Sort with selected method
        Enum.TryParse(sortByRadioGroup.Value, out LevelSort sort);
        // Sort with selected order
        var asc = bool.Parse(sortOrderRadioGroup.Value);

        RefillLevels(sort, asc, query);
    }

    public void RefillLevels(LevelSort sort, bool asc, string query = "")
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
            query = null;
            searchInputField.SetTextWithoutNotify("");
            
            RefillLevels();
        }
    }

    public void OnScreenChangeFinished(Screen from, Screen to)
    {
        if (from.GetId() == Id && to.GetId() == MainMenuScreen.Id)
        {
            // Clear level cover sprite cache
            Context.SpriteCache.ClearTagged("LevelCover");
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