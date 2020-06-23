using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/**
 * Logic:
 * On startup, cache the latest catalog -> id to download url and hash
 * Whenever a resource is required:
 *  - when strict mode off (i.e. game start): Just load from cache or streaming assets
 *  - when strict mode on: Just load from cache or streaming assets; if hash does not match or asset does not exist, download from url and cache; if fail, crash
 */

public class CatalogGenerator
{

    [MenuItem("Cytoid/Unload All AssetBundles")]
    static void UnloadAllAssetBundles()
    {
        AssetBundle.UnloadAllAssetBundles(true);
    }
    
    [MenuItem("Cytoid/Build All AssetBundles")]
    static void GenerateCatalog()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        var platforms = new[] {("iOS", BuildTarget.iOS)};//new[] {("Android", BuildTarget.Android)};//, "iOS"};
        foreach (var (platformName, buildTarget) in platforms)
        {
            var dir = $"AssetBundles/{platformName}";
            BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.StrictMode, buildTarget);
            if (!File.Exists("AssetBundles/versions.json"))
            {
                File.WriteAllText("AssetBundles/versions.json", "{}");
            }

            var versions = JObject.Parse(File.ReadAllText("AssetBundles/versions.json"));
            
            // Post-process
            var json = new JObject();
            var manifestAb = AssetBundle.LoadFromFile(dir + $"/{platformName}");
            var rootManifest = (AssetBundleManifest) manifestAb.LoadAsset("AssetBundleManifest");
            foreach (var name in rootManifest.GetAllAssetBundles())
            {
                var ab = AssetBundle.LoadFromFile(dir + $"/{name}");
                foreach (var asset in ab.GetAllAssetNames())
                {
                    Debug.Log($"\t{asset}");
                }

                var version = versions[name]?.Value<int>() ?? 1;
                var jObj = new JObject {["version"] = version};
                json[name] = jObj;
            }
            File.WriteAllText("AssetBundles/versions.json", versions.ToString());

            manifestAb.Unload(false);
            Directory.CreateDirectory($"Assets/StreamingAssets/{platformName}/Bundles/");
            File.WriteAllText(dir + "/catalog.json", json.ToString());
            Debug.Log($"Catalog generated for {platformName}.");
            
            // Don't include non built-ins
            var stripped = new JObject();
            foreach (var bundle in BundleManager.BuiltInBundles)
            {
                stripped[bundle] = json[bundle];
            }

            File.WriteAllText($"Assets/StreamingAssets/{platformName}/Bundles/catalog.json", stripped.ToString());
            Debug.Log($"Stripped catalog generated for {platformName} and moved to StreamingAssets.");

            foreach (var bundle in BundleManager.BuiltInBundles)
            {
                File.Copy(dir + "/" + bundle, $"Assets/StreamingAssets/{platformName}/Bundles/" + bundle, true);
            }

            Debug.Log("Copied built-in bundles to StreamingAssets.");
            Debug.Log($"Completed building for target {platformName}");
        }
    }
    
    private static RuntimePlatform Platform 
    { 
        get
        {
#if UNITY_ANDROID
            return RuntimePlatform.Android;
#elif UNITY_IOS
                 return RuntimePlatform.IPhonePlayer;
#elif UNITY_STANDALONE_OSX
                 return RuntimePlatform.OSXPlayer;
#elif UNITY_STANDALONE_WIN
                 return RuntimePlatform.WindowsPlayer;
#endif
        }
    }
    
    private static string PlatformName 
    { 
        get
        {
            switch (Platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.OSXPlayer:
                    return "MacOS";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
}