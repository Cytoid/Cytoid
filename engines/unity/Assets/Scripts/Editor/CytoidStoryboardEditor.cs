using Cytoid.Storyboard;
using Cytoid.Storyboard.PostProcess;
using UnityEditor;
using UnityEngine;

public static class CytoidStoryboardEditor
{
    [MenuItem("Cytoid/Log Storyboard Effects Backend", false, 12)]
    public static void LogBackend()
    {
        // Use the same build-safe lookup the runtime path uses, so the menu
        // reports what exported plugins actually resolve.
        var bootstrapType = VendorStoryboardInstall.ResolveBootstrapType();
        var complete = VendorStoryboardInstall.IsComplete();
        var onDisk = VendorStoryboardInstall.FilesPresentOnDisk();
        Debug.Log($"[Cytoid] Vendor install complete: {complete} ({VendorStoryboardInstall.StoryboardFiltersRelative})");
        Debug.Log($"[Cytoid] Vendor files on disk: {onDisk}");
        Debug.Log($"[Cytoid] Vendor bootstrap type loaded: {bootstrapType != null} ({bootstrapType?.Assembly.GetName().Name ?? "not found"})");
        Debug.Log($"[Cytoid] Active backend: {(StoryboardEffects.Current != null ? StoryboardEffects.Current.GetType().Name : "(none yet — enter Play mode)")}");
    }
}
