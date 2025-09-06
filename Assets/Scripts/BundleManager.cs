using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class BundleManager
{
    public BundleCatalog Catalog { get; private set; }

    public Dictionary<string, Entry> LoadedBundles { get; } = new Dictionary<string, Entry>();

    private static string BuiltInCatalogPath => BuiltInBundlesBasePath + "catalog.json";
    private static string BuiltInBundlesBasePath {
        get
        {
#if UNITY_EDITOR && UNITY_ANDROID
            return "file://" + Application.streamingAssetsPath + "/Android/Bundles/";
#elif UNITY_ANDROID
            return Application.streamingAssetsPath + "/Android/Bundles/";
#elif UNITY_IOS
            return "file://" + Application.streamingAssetsPath + "/iOS/Bundles/";
#else
            throw new InvalidOperationException();
#endif
        }
    }
    private static string CachedCatalogPath => Application.temporaryCachePath + "/cached_catalog.json";

    public async UniTask Initialize()
    {
        // Get built-in catalog first
        BundleCatalog builtInCatalog;
        using (var request = UnityWebRequest.Get(BuiltInCatalogPath))
        {
            Debug.Log($"[BundleManager] Loading built-in catalog from {BuiltInCatalogPath}");
            await request.SendWebRequest();
            var text = Encoding.UTF8.GetString(request.downloadHandler.data);
            builtInCatalog = new BundleCatalog(JObject.Parse(text));
        }
        
        // Then the cached catalog
        if (File.Exists(CachedCatalogPath))
        {
            Debug.Log($"[BundleManager] Reading cached catalog from {CachedCatalogPath}");
            using (var request = UnityWebRequest.Get("file://" + CachedCatalogPath))
            {
                var valid = true;
                try
                {
                    await request.SendWebRequest();
                    if (request.isHttpError || request.isNetworkError)
                    {
                        throw new Exception(request.error);
                    }
                    var text = Encoding.UTF8.GetString(request.downloadHandler.data);
                    Catalog = new BundleCatalog(JObject.Parse(text));
                    foreach (var bundleName in builtInCatalog.GetEntryNames())
                    {
                        if (!Catalog.ContainsEntry(bundleName))
                        {
                            valid = false;
                            break;
                        }

                        var cachedVersion = Catalog.GetEntry(bundleName).version;
                        var builtInVersion = builtInCatalog.GetEntry(bundleName).version;
                        if (builtInVersion > cachedVersion)
                        {
                            Debug.Log($"[BundleManager] Bumping {bundleName} from {cachedVersion} to {builtInVersion}");
                            Catalog.SetEntry(bundleName, Catalog.GetEntry(bundleName).JsonDeepCopy().Also(it => it.version = builtInVersion));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    valid = false;
                }

                if (!valid)
                {
                    Debug.Log($"[BundleManager] Invalid cached catalog! Using built-in catalog");
                    Catalog = builtInCatalog;
                }
            }
        }
        else
        {
            Catalog = builtInCatalog;
        }
        
        var cachePaths = new List<string>();
        Caching.GetAllCachePaths(cachePaths);
        cachePaths.ForEach(it => Debug.Log($"[BundleManager] Cache path: {it}"));
        
        // Always cache built in bundles
        foreach (var bundle in builtInCatalog.GetEntryNames())
        {
            if (IsCached(bundle) && IsUpToDate(bundle))
            {
                Debug.Log($"[BundleManager] Built-in bundle {bundle} is cached and up-to-date (version {Catalog.GetEntry(bundle).version})");
                continue;
            }
            await LoadBundle(bundle, true, false);
            Release(bundle);
        }
    }
    
    public async UniTask<BundleCatalog> GetRemoteCatalog()
    {
        var url = Context.BundleRemoteFullUrl + "catalog.json";
        Debug.Log($"[BundleManager] Requested catalog from {url}");
        var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", $"CytoidClient/{Context.VersionIdentifier}");
        request.timeout = 10;
        using (request)
        {
            try
            {
                await request.SendWebRequest();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }

            var text = Encoding.UTF8.GetString(request.downloadHandler.data);
            return new BundleCatalog(JObject.Parse(text));
        }
    }

    public async UniTask<bool> DownloadAndSaveCatalog()
    {
        var remoteCatalog = await GetRemoteCatalog();
        if (remoteCatalog == null) return false;
        Catalog = remoteCatalog;
        File.WriteAllText(CachedCatalogPath.Replace("file://", ""), JsonConvert.SerializeObject(remoteCatalog.JObject));
        return true;
    }

    public bool IsCached(string bundleId)
    {
        if (!Catalog.ContainsEntry(bundleId)) return false;
        var list = new List<Hash128>();
        Caching.GetCachedVersions(bundleId, list);
        return list.Count > 0;
    }
    
    public bool IsUpToDate(string bundleId)
    {
        if (!Catalog.ContainsEntry(bundleId)) return false;
        var list = new List<Hash128>();
        Caching.GetCachedVersions(bundleId, list);
        Debug.Log($"Checking if {bundleId} is up to date...");
        list.ForEach(it => Debug.Log($"    Version {it}"));
        return Caching.IsVersionCached(bundleId, (int) Catalog.GetEntry(bundleId).version);
    }

    public async UniTask<AssetBundle> LoadCachedBundle(string bundleId)
    {
        if (LoadedBundles.ContainsKey(bundleId))
        {
            LoadedBundles[bundleId].RefCount++;
            return LoadedBundles[bundleId].AssetBundle;
        }
        Debug.Log($"[BundleManager] Requested cached bundle {bundleId}");
        Debug.Log($"[BundleManager] Version: {Catalog.GetEntry(bundleId).version}");
        if (!IsCached(bundleId)) return null;
        UnityWebRequest request;
        if (IsUpToDate(bundleId))
        {
            Debug.Log($"[BundleManager] Cached bundle matches version");
            // Use latest version
            request = UnityWebRequestAssetBundle.GetAssetBundle(bundleId, Catalog.GetEntry(bundleId).version, 0U);
        }
        else
        {
            // Use any existing version
            var list = new List<Hash128>();
            Caching.GetCachedVersions(bundleId, list);
            Debug.Log($"[BundleManager] Cached bundle does not match version. Using {list.Last()}...");
            request = UnityWebRequestAssetBundle.GetAssetBundle(bundleId, list.Last());
        }
        using (request)
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return null;
            }

            var ab = DownloadHandlerAssetBundle.GetContent(request);
            LoadedBundles[bundleId] = new Entry {Id = bundleId, AssetBundle = ab, RefCount = 1};
            return ab;
        }
    }

    public async UniTask<AssetBundle> LoadBundle(
        string bundleId,
        bool loadFromStreamingAssets,
        bool showDialog,
        bool allowAbort = true,
        bool instantiate = true,
        Action onDownloadSucceeded = default,
        Action onDownloadAborted = default,
        Action onDownloadFailed = default,
        Action onLocallyResolved = default
    )
    {
        if (bundleId == null) throw new ArgumentNullException(nameof(bundleId));
        if (!loadFromStreamingAssets)
        {
            Debug.Log($"[BundleManager] Requested remote bundle {bundleId}");
        }
        else
        {
            Debug.Log($"[BundleManager] Requested StreamingAssets bundle {bundleId}");
        }
        
        if (!Catalog.ContainsEntry(bundleId))
        {
            Debug.LogError($"[BundleManager] {bundleId} does not exist in the catalog!");
            return null;
        }
        Debug.Log($"[BundleManager] Version: {Catalog.GetEntry(bundleId).version}");

        if (onLocallyResolved == default) onLocallyResolved = () => { };
        if (onDownloadSucceeded == default) onDownloadSucceeded = () => { };
        if (onDownloadAborted == default) onDownloadAborted = () => { };
        if (onDownloadFailed == default) onDownloadFailed = () => { };

        if (IsCached(bundleId))
        {
            Debug.Log($"[BundleManager] Bundle is cached");
            
            if (IsUpToDate(bundleId)) {
                Debug.Log($"[BundleManager] Cached bundle matches version");
                onLocallyResolved();

                if (!instantiate) return null;
                return await LoadCachedBundle(bundleId);
            }
        }

        var downloadUrl = loadFromStreamingAssets ? BuiltInBundlesBasePath + bundleId : Context.BundleRemoteFullUrl + bundleId;
        Debug.Log($"[BundleManager] URL: {downloadUrl}");
        
        // Check download size
        var totalSize = 0ul;
        if (!loadFromStreamingAssets)
        {
            if (!Application.isEditor || !Context.Instance.editorUseLocalAssetBundles)
            {
                try
                {
                    using (var headRequest = UnityWebRequest.Head(downloadUrl))
                    {
                        headRequest.SetRequestHeader("User-Agent", $"CytoidClient/{Context.VersionIdentifier}");
                        await headRequest.SendWebRequest();

                        if (headRequest.isNetworkError || headRequest.isHttpError)
                        {
                            Debug.LogError(headRequest.error);
                            onDownloadFailed();
                            return null;
                        }

                        totalSize = ulong.Parse(headRequest.GetResponseHeader("Content-Length"));
                        Debug.Log($"[BundleManager] Download size: {totalSize.ToHumanReadableFileSize()}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    onDownloadFailed();
                    return null;
                }
            }
            else
            {
                totalSize = 99999999;
            }
        }

        Dialog dialog = null;
        var request = UnityWebRequestAssetBundle.GetAssetBundle(downloadUrl, Catalog.GetEntry(bundleId).version, 0U);
        request.SetRequestHeader("User-Agent", $"CytoidClient/{Context.VersionIdentifier}");
        var aborted = false;
        if (showDialog)
        {
            dialog = Dialog.Instantiate();
            dialog.Message = "DIALOG_DOWNLOADING".Get();
            dialog.UseProgress = true;
            dialog.UsePositiveButton = false;
            dialog.UseNegativeButton = allowAbort;
            dialog.onUpdate.AddListener(it =>
            {
                if (aborted) return;
                var downloadedSize = request.downloadedBytes;
                it.Progress = totalSize == 0 ? 0 : downloadedSize * 1.0f / totalSize;
                it.Message = "DIALOG_DOWNLOADING_X_Y".Get(downloadedSize.ToHumanReadableFileSize(),
                    totalSize.ToHumanReadableFileSize());
            });
            if (allowAbort)
            {
                dialog.OnNegativeButtonClicked = it =>
                {
                    dialog.Close();
                    aborted = true;
                };
            }

            dialog.Open();
        }

        using (request)
        {
            try
            {
                request.SendWebRequest();

                while (!request.isDone)
                {
                    if (aborted) break;
                    await UniTask.Yield();
                }

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                onDownloadFailed();
                return null;
            }

            if (aborted)
            {
                request.Abort();
                onDownloadAborted();
                return null;
            }

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                onDownloadFailed();
                return null;
            }
            
            if (showDialog) dialog.Close();

            onDownloadSucceeded();

            if (!instantiate) return null;
            
            if (LoadedBundles.ContainsKey(bundleId))
            {
                Release(bundleId);
            }

            var ab = ((DownloadHandlerAssetBundle) request.downloadHandler).assetBundle;
            ab.GetAllAssetNames().ForEach(Debug.Log);
            LoadedBundles[bundleId] = new Entry {Id = bundleId, AssetBundle = ab, RefCount = 1};
            return ab;
        }
    }

    public void Release(string bundleId, bool force = false)
    {
        if (bundleId == null) throw new ArgumentNullException();
        if (!LoadedBundles.ContainsKey(bundleId)) return;
        var entry = LoadedBundles[bundleId];
        entry.RefCount--;
        if (entry.RefCount <= 0 || force)
        {
            if (entry.RefCount < 0)
            {
                Debug.LogError("RefCount < 0!");
            }
            entry.AssetBundle.Unload(true);
            LoadedBundles.Remove(bundleId);
        }
    }

    public void ReleaseAll()
    {
        new List<string>(LoadedBundles.Keys).ForEach(it => Release(it, true));
    }

    public class Entry
    {
        public string Id;
        public AssetBundle AssetBundle;
        public int RefCount;
    }
    
}

public class BundleCatalog
{

    public JObject JObject { get; }

    public BundleCatalog(JObject jObject)
    {
        JObject = jObject;
    }

    public Entry GetEntry(string id)
    {
        return JObject[id] == null ? null : JObject[id].ToObject<Entry>();
    }

    public void SetEntry(string id, Entry entry)
    {
        JObject[id] = JObject.FromObject(entry);
    }

    public List<string> GetEntryNames()
    {
        return JObject.Properties().Select(p => p.Name).ToList();
    }

    public bool ContainsEntry(string id)
    {
        return GetEntry(id) != null;
    }

    [Serializable]
    public class Entry
    {
        public uint version;
    }
    
}
