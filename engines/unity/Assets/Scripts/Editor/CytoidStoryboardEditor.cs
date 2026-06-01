using Cytoid.Storyboard;
using Cytoid.Storyboard.PostProcess;
using UnityEditor;
using UnityEngine;

public static class CytoidStoryboardEditor
{
    [MenuItem("Cytoid/Log Storyboard Effects Backend", false, 12)]
    public static void LogBackend()
    {
        var complete = VendorStoryboardInstall.IsComplete();
        Debug.Log($"[Cytoid] Vendor install complete: {complete} ({VendorStoryboardInstall.StoryboardFiltersRelative})");
        Debug.Log($"[Cytoid] Vendor bootstrap type loaded: {System.Type.GetType("Cytoid.Storyboard.Vendor.VendorStoryboardEffectsBootstrap") != null}");
        Debug.Log($"[Cytoid] Active backend: {(StoryboardEffects.Current != null ? StoryboardEffects.Current.GetType().Name : "(none yet — enter Play mode)")}");
    }
}
