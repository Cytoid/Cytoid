using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LevelSelectionScreen : Screen
{
    public const string Id = "LevelSelection";
    
    public LoopVerticalScrollRect scrollRect;
    
    public override string GetId() => Id;
    
    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();

        RefillLevels(LevelSort.AddedDate, false);
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        
        Destroy(scrollRect);
        Context.SpriteCache.Clear();
    }

    public void RefillLevels(LevelSort sort, bool asc, string query = null)
    {
        var levels = new List<Level>(Context.LevelManager.LoadedLevels);

        if (query != null)
        {
            query = query.Trim();
            var keywords = query.Split(' ');
            var filteredLevels = new List<Level>(levels);
            foreach (var level in levels)
            {
                foreach (var keyword in keywords)
                {
                    if (level.Meta.title.Contains(keyword)) continue;
                    if (level.Meta.title_localized.Contains(keyword)) continue;
                    if (level.Meta.artist.Contains(keyword)) continue;
                    if (level.Meta.artist_localized.Contains(keyword)) continue;
                    if (string.Equals(level.Meta.charter, keyword, StringComparison.CurrentCultureIgnoreCase)) continue;
                    if (string.Equals(level.Meta.storyboarder, keyword, StringComparison.CurrentCultureIgnoreCase)) continue;
                    filteredLevels.Remove(level);
                }
            }
            levels = filteredLevels;
        }

        switch (sort)
        {
            case LevelSort.Alphabetical:
                levels.Sort((a, b) =>
                    string.Compare(a.Meta.title, b.Meta.title, StringComparison.Ordinal) * (asc ? 1 : -1));
                break;
            case LevelSort.Difficulty:
                levels.Sort((a, b) =>
                    (asc ? a.Meta.GetEasiestDifficulty() : a.Meta.GetHardestDifficulty())
                    .CompareTo(asc ? b.Meta.GetEasiestDifficulty() : b.Meta.GetHardestDifficulty())
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

}

public enum LevelSort
{
    Alphabetical,
    Difficulty,
    AddedDate,
    PlayedDate
}