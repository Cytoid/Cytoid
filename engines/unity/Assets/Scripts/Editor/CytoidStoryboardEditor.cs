using System;
using System.Linq;
using Cytoid.Storyboard;
using Cytoid.Storyboard.PostProcess;
using UnityEditor;
using UnityEngine;

public static class CytoidStoryboardEditor
{
    const string BootstrapTypeName = "Cytoid.Storyboard.Vendor.VendorStoryboardEffectsBootstrap";

    [MenuItem("Cytoid/Log Storyboard Effects Backend", false, 12)]
    public static void LogBackend()
    {
        // Mirror StoryboardVendorEffectsLoader's build-safe lookup so this menu
        // reports the same resolution the runtime path uses in exported plugins.
        var bootstrapType = Type.GetType(BootstrapTypeName)
                            ?? AppDomain.CurrentDomain.GetAssemblies()
                                .Select(a => a.GetType(BootstrapTypeName))
                                .FirstOrDefault(t => t != null);
        var complete = VendorStoryboardInstall.IsComplete();
        var onDisk = VendorStoryboardInstall.FilesPresentOnDisk();
        Debug.Log($"[Cytoid] Vendor install complete: {complete} ({VendorStoryboardInstall.StoryboardFiltersRelative})");
        Debug.Log($"[Cytoid] Vendor files on disk: {onDisk}");
        Debug.Log($"[Cytoid] Vendor bootstrap type loaded: {bootstrapType != null} ({bootstrapType?.Assembly.GetName().Name ?? "not found"})");
        Debug.Log($"[Cytoid] Active backend: {(StoryboardEffects.Current != null ? StoryboardEffects.Current.GetType().Name : "(none yet — enter Play mode)")}");
    }
}
