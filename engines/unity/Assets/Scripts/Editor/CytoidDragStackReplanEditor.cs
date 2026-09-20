using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-side acceptance harness for <see cref="Game.RebuildDragStacks"/>, which has
/// no gameplay caller yet: while a game is running in Editor Play Mode, re-runs
/// drag-stack planning against the current storyboard note-controller set (trigger
/// spawn/destroy included) and migrates live hosts, registrations, and drag lines.
/// </summary>
public static class CytoidDragStackReplanEditor
{
    [MenuItem("Cytoid/Debug/Rebuild Drag Stacks (running game)")]
    public static void RebuildDragStacks()
    {
        var game = Object.FindFirstObjectByType<Game>();
        if (game == null)
        {
            Debug.LogWarning("Rebuild Drag Stacks: no running Game found (enter Play Mode first).");
            return;
        }

        var before = game.Chart.NoteIdToDragStackId.Count;
        game.RebuildDragStacks();
        var after = game.Chart.NoteIdToDragStackId.Count;
        Debug.Log($"Rebuild Drag Stacks: stacked note entries {before} -> {after}");
    }
}
