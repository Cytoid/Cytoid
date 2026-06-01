using System.IO;
using UnityEngine;

namespace Cytoid.Storyboard.PostProcess
{
    /// <summary>
    /// Detects optional paid packages under Assets/Vendor/ (not in public git).
    /// </summary>
    public static class VendorStoryboardInstall
    {
        public const string VendorRootRelative = "Assets/Vendor";
        public const string StoryboardFiltersRelative = "Assets/Vendor/StoryboardFilters";

        public static string StoryboardFiltersAbsolute =>
            Path.Combine(Application.dataPath, "Vendor", "StoryboardFilters");

        public static bool IsComplete()
        {
            var root = StoryboardFiltersAbsolute;
            if (!Directory.Exists(root))
                return false;

            return File.Exists(Path.Combine(root, "VendorStoryboardEffectsBootstrap.cs"))
                   && Directory.Exists(Path.Combine(root, "Camera Filter Pack"))
                   && Directory.Exists(Path.Combine(root, "Sleek Render"));
        }
    }
}
