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
    
    [MenuItem("Cytoid/Generate AssetBundle Catalog")]
    static void GenerateCatalog()
    {
        var json = new JObject();
        var dir = $"AssetBundles/{PlatformName}";
        AssetBundle.UnloadAllAssetBundles(true);
        var manifestAb = AssetBundle.LoadFromFile(dir + $"/{PlatformName}");
        var rootManifest = (AssetBundleManifest) manifestAb.LoadAsset("AssetBundleManifest");
        foreach (var name in rootManifest.GetAllAssetBundles())
        {
            /*Debug.Log(name);
            var ab = AssetBundle.LoadFromFile(dir + $"/{name}");
            foreach (var asset in ab.GetAllAssetNames())
            {
                Debug.Log($"\t{asset}");
            }*/
            var hash = rootManifest.GetAssetBundleHash(name);
            var jObj = new JObject {["hash"] = hash.ToString()};
            json[name] = jObj;
        }
        manifestAb.Unload(false);
        File.WriteAllText(dir + "/catalog.json", json.ToString());
        File.Copy(dir + "/catalog.json", "Assets/StreamingAssets/catalog.json", true);
        Debug.Log($"Catalog generated for {PlatformName} and copied to StreamingAssets.");

        foreach (var bundle in BundleManager.BuiltInBundles)
        {
            File.Copy(dir + "/" + bundle, "Assets/StreamingAssets/Bundles/" + bundle, true);
        }
        Debug.Log("Copied built-in bundles to StreamingAssets.");
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