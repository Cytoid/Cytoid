using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Cytoid.Storyboard.PostProcess
{
    /// <summary>
    /// Detects the optional paid packages under Assets/Vendor/ (not in public git).
    /// </summary>
    public static class VendorStoryboardInstall
    {
        public const string VendorRootRelative = "Assets/Vendor";
        public const string StoryboardFiltersRelative = "Assets/Vendor/StoryboardFilters";

        // Full name of the bootstrap type that only exists when the Vendor package
        // is compiled into the assembly. Presence is decided at compile time, so
        // this is a build-safe gate (filesystem checks fail in built players where
        // Assets/ does not exist and source files are gone).
        const string BootstrapTypeName = "Cytoid.Storyboard.Vendor.VendorStoryboardEffectsBootstrap";

        public static string StoryboardFiltersAbsolute =>
            Path.Combine(Application.dataPath, "Vendor", "StoryboardFilters");

        /// <summary>
        /// True when the Vendor storyboard package is compiled in. Works in both
        /// Editor and built players (IL2CPP/AOT).
        /// </summary>
        public static bool IsComplete()
        {
            return ResolveBootstrapType() != null;
        }

        /// <summary>
        /// Editor-only filesystem inspection used by the diagnostic menu to show
        /// humans where the Vendor package lives on disk. Not used as a runtime gate.
        /// </summary>
        public static bool FilesPresentOnDisk()
        {
            var root = StoryboardFiltersAbsolute;
            if (!Directory.Exists(root))
                return false;

            return File.Exists(Path.Combine(root, "VendorStoryboardEffectsBootstrap.cs"))
                   && Directory.Exists(Path.Combine(root, "Camera Filter Pack"))
                   && Directory.Exists(Path.Combine(root, "Sleek Render"));
        }

        /// <summary>
        /// Resolves the vendor bootstrap type via a build-safe two-step lookup:
        /// <see cref="Type.GetType(string)"/> first, then an AppDomain assembly
        /// scan. <see cref="Type.GetType(string)"/> with a bare name only searches
        /// the calling assembly and mscorlib, which fails for cross-assembly
        /// lookups in built players (IL2CPP/AOT). Used by IsComplete(), the
        /// runtime loader, and the editor diagnostic menu (the latter lives in
        /// Assembly-CSharp-Editor, so this must be public, not internal).
        /// </summary>
        public static Type ResolveBootstrapType()
        {
            return Type.GetType(BootstrapTypeName)
                   ?? AppDomain.CurrentDomain.GetAssemblies()
                       .Select(a => a.GetType(BootstrapTypeName))
                       .FirstOrDefault(t => t != null);
        }
    }
}
