using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

/**
 * Logic:
 * On startup, cache the latest catalog -> id to download url and hash
 * Whenever a resource is required:
 *  - when strict mode off (i.e. game start): Just load from cache or streaming assets
 *  - when strict mode on: Just load from cache or streaming assets; if hash does not match or asset does not exist, download from url and cache; if fail, crash
 */
public class AssetBundleBuilder : OdinEditorWindow
{
    public static readonly List<string> BuiltInBundles = new List<string>
    {
        "character_sayaka",
        "character_sayaka_tachie",
        "character_kaede",
        "character_kaede_tachie"
    };
    
    [MenuItem("Cytoid/AssetBundle Builder")]
    private static void OpenWindow()
    {
        GetWindow<AssetBundleBuilder>().Show();
    }

    [TableList] public readonly List<Row> assetBundles = new List<Row>();

    [ValueDropdown(nameof(SupportedBuildTargets))]
    public BuildTarget buildTarget;

    private static IEnumerable<BuildTarget> SupportedBuildTargets = new List<BuildTarget>
    {
        BuildTarget.Android,
        BuildTarget.iOS,
        BuildTarget.StandaloneWindows64,
        BuildTarget.StandaloneOSX
    };

    [Button]
    public void Reload()
    {
        buildTarget = EditorUserBuildSettings.activeBuildTarget;
        assetBundles.Clear();

        var versions = GetVersionJson();
        foreach (var (bundleName, assetPath) in AssetDatabase.GetAllAssetBundleNames()
            .Zip(AssetDatabase.GetAllAssetPaths()))
        {
            var children = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            Assert.AreEqual(children.Length, 1);
            var row = new Row
            {
                Name = bundleName,
                Path = children[0],
                Version = versions[bundleName]?.Value<int>() ?? 1
            };
            versions[bundleName] = row.Version;
            assetBundles.Add(row);
        }

        SaveVersionJson();
    }

    private JObject GetVersionJson()
    {
        if (!File.Exists("AssetBundles/versions.json"))
        {
            File.WriteAllText("AssetBundles/versions.json", "{}");
        }

        return JObject.Parse(File.ReadAllText("AssetBundles/versions.json"));
    }

    private void SaveVersionJson()
    {
        var versions = new JObject();
        foreach (var row in assetBundles)
        {
            versions[row.Name] = row.Version;
        }

        File.WriteAllText("AssetBundles/versions.json", versions.ToString());
    }

    public class Row
    {
        [TableColumnWidth(40)] public bool Selected;

        [TableColumnWidth(160)] [ReadOnly] public string Name;

        [ReadOnly] public string Path;

        [TableColumnWidth(40)] public int Version;
    }

    [Button]
    public void SelectAll()
    {
        assetBundles.ForEach(it => it.Selected = true);
    }

    [Button]
    public void BuildSelectedAssetBundles()
    {
        UnloadAllAssetBundles();

        var assetBundleBuilds = new List<AssetBundleBuild>();
        var processedBundles = new HashSet<string>();

        // Get asset bundle names from selection
        foreach (var row in assetBundles.Where(it => it.Selected))
        {
            var assetPath = row.Path;
            var importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
            {
                continue;
            }

            // Get asset bundle name & variant
            var assetBundleName = importer.assetBundleName;
            var assetBundleVariant = importer.assetBundleVariant;
            var assetBundleFullName = string.IsNullOrEmpty(assetBundleVariant)
                ? assetBundleName
                : assetBundleName + "." + assetBundleVariant;

            // Only process assetBundleFullName once. No need to add it again.
            if (processedBundles.Contains(assetBundleFullName))
            {
                continue;
            }

            processedBundles.Add(assetBundleFullName);

            var build = new AssetBundleBuild
            {
                assetBundleName = assetBundleName,
                assetBundleVariant = assetBundleVariant,
                assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleFullName)
            };

            assetBundleBuilds.Add(build);
        }

        // Outside Assets folder
        Directory.CreateDirectory($"AssetBundles/{FolderName}");

        BuildPipeline.BuildAssetBundles($"AssetBundles/{FolderName}", assetBundleBuilds.ToArray(),
            BuildAssetBundleOptions.StrictMode, buildTarget);

        SaveVersionJson();
        Debug.Log($"Completed building {assetBundleBuilds.Count} bundles for target {buildTarget}");
    }

    [Button]
    public void ClearLocalCacheForSelectedAssetBundles()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor) throw new NotImplementedException();
        foreach (var row in assetBundles.Where(it => it.Selected))
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                $@"..\LocalLow\Unity\{Application.companyName}_{Application.productName}\{row.Name}");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Debug.Log($"Deleted {path}");
            }
        }
    }

    [Button]
    public void PublishCatalog()
    {
        // Generate catalog
        var catalog = new JObject();
        foreach (var row in assetBundles)
        {
            var ab = new JObject {["version"] = row.Version};
            catalog[row.Name] = ab;
        }

        File.WriteAllText($"AssetBundles/{FolderName}/catalog.json", catalog.ToString());
        Debug.Log($"Catalog generated for {buildTarget}");

        // Generate retail catalog
        var retailCatalog = new JObject();
        foreach (var bundleName in BuiltInBundles)
        {
            if (catalog[bundleName] == null)
            {
                Debug.LogError($"Requested to built-in bundle {bundleName} which does not exist");
                continue;
            }

            retailCatalog[bundleName] = catalog[bundleName];
        }

        Directory.CreateDirectory($"Assets/StreamingAssets/{FolderName}/Bundles/");
        File.WriteAllText($"Assets/StreamingAssets/{FolderName}/Bundles/catalog.json", retailCatalog.ToString());
        Debug.Log($"Stripped catalog generated for {buildTarget} and moved to StreamingAssets");

        foreach (var bundle in BuiltInBundles)
        {
            File.Copy($"AssetBundles/{FolderName}/" + bundle, $"Assets/StreamingAssets/{FolderName}/Bundles/" + bundle,
                true);
        }

        Debug.Log($"Completed publishing for target {buildTarget}");
    }

    [Button]
    public void DeployAllAssetBundles()
    {
        Process.Start($"{Environment.CurrentDirectory}/AssetBundles/{buildTarget}.bat");
    }

    [Button]
    public void UnloadAllAssetBundles()
    {
        AssetBundle.UnloadAllAssetBundles(true);
    }

    private string FolderName
    {
        get
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.StandaloneOSX:
                    return "MacOS";
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
