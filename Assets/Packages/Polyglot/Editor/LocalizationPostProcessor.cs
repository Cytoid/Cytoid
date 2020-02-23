
#if UNITY_5
using JetBrains.Annotations;
#endif
using UnityEditor;

namespace Polyglot
{
    public class LocalizationPostProcessor : AssetPostprocessor
    {
        #if UNITY_5
        [UsedImplicitly]
        #endif
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int index = 0; index < importedAssets.Length; index++)
            {
                var str = importedAssets[index];
                if (str.EndsWith(".csv") && str.Contains("Localization"))
                {
                    LocalizationImporter.Refresh();
                }
            }
        }
    }
}
