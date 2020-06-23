using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Priority_Queue;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

public class AssetMemory
{
    private static readonly Dictionary<AssetTag, int> TagLimits = new Dictionary<AssetTag, int>
    {
        {AssetTag.LocalLevelCoverThumbnail, 30},
        {AssetTag.RemoteLevelCoverThumbnail, 30},
        {AssetTag.RecordCoverThumbnail, 12},
        {AssetTag.PlayerAvatar, 1},
        {AssetTag.Avatar, 100},
        {AssetTag.GameCover, 1},
        {AssetTag.TierCover, 1},
        {AssetTag.PreviewMusic, 1}
    };

    private readonly Dictionary<AssetTag, SimplePriorityQueue<Entry>> taggedMemoryCache =
        new Dictionary<AssetTag, SimplePriorityQueue<Entry>>();

    private readonly Dictionary<string, Entry> memoryCache = new Dictionary<string, Entry>();
    private readonly HashSet<string> isLoading = new HashSet<string>();
    private int queuePriority;

    public Entry<T> GetCachedAssetEntry<T>(string path)
    {
        if (memoryCache.ContainsKey(path))
        {
            var entry = memoryCache[path];
            if (entry is Entry<T> typedEntry && typedEntry.Asset != null)
            {
                // Update priority
                typedEntry.Tags.ForEach(tag => taggedMemoryCache[tag].UpdatePriority(entry, queuePriority++));
                return typedEntry;
            }
        }

        return default;
    }

    public bool HasCachedAsset<T>(string path) => GetCachedAssetEntry<T>(path) != null;

    public static bool PrintDebugMessages = false;

    public async UniTask<T> LoadAsset<T>(string path, AssetTag tag, CancellationToken cancellationToken = default, 
        AssetOptions options = default, bool useFileCacheOnly = false) where T : Object
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new SimplePriorityQueue<Entry>();
        
        var suffix = "";
        if (typeof(T) == typeof(Sprite))
        {
            if (options != default)
            {
                if (options is SpriteAssetOptions spriteOptions)
                {
                    suffix = $".{spriteOptions.FitCropSize[0]}.{spriteOptions.FitCropSize[1]}";
                }
            }
        }

        string variantPath;
        if (!path.StartsWith("file://"))
        {
            variantPath = "file://" + GetCacheFilePath(path) + suffix;
        }
        else
        {
            variantPath = path + suffix;
        }
        var variantExists = File.Exists(variantPath.Substring("file://".Length));
        
        var cachedAsset = GetCachedAssetEntry<T>(variantPath);
        if (cachedAsset != null)
        {
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Returning cached asset {variantPath}");
            cachedAsset.Tags.Add(tag);
            var taggedMemory = taggedMemoryCache[tag];
            if (taggedMemory.Contains(cachedAsset))
            {
                taggedMemory.UpdatePriority(cachedAsset, queuePriority++);
            }
            else
            {
                taggedMemory.Enqueue(cachedAsset, queuePriority++);
            }
            return cachedAsset.Asset;
        }

        // Currently loading
        if (isLoading.Contains(variantPath))
        {
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Already loading {variantPath}. Waiting...");
            await UniTask.WaitUntil(() => !isLoading.Contains(variantPath), cancellationToken: cancellationToken);
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Wait {variantPath} complete.");
            
            return await LoadAsset<T>(path, tag, cancellationToken, options);
        }

        CheckIfExceedTagLimit(tag);

        if (PrintDebugMessages) Debug.Log($"AssetMemory: Started loading {variantPath}.");
        isLoading.Add(variantPath);

        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // Cache remote
        if (!path.StartsWith("file://"))
        {
            var cachePath = GetCacheFilePath(path);

            if (!File.Exists(cachePath))
            {
                if (!useFileCacheOnly)
                {
                    using (var request = UnityWebRequest.Get(path))
                    {
                        request.downloadHandler =
                            new DownloadHandlerFile(cachePath).Also(it => it.removeFileOnAbort = true);
                        await request.SendWebRequest();
                        if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                        {
                            isLoading.Remove(variantPath);
                            return default;
                        }

                        if (request.isNetworkError || request.isHttpError)
                        {
                            // TODO: Neo, fix your image CDN :)
                            if (request.responseCode != 422)
                            {
                                if (path.Contains("gravatar"))
                                {
                                    Debug.LogWarning($"AssetMemory: Failed to download {path}");
                                    Debug.LogWarning(request.error);
                                }
                                else
                                {
                                    Debug.LogError($"AssetMemory: Failed to download {path}");
                                    Debug.LogError(request.error);
                                }
                            }
                            isLoading.Remove(variantPath);
                            return default;
                        }
                       
                        if (PrintDebugMessages) Debug.Log($"AssetMemory: Saved {path} to {cachePath}");
                    }
                }
                else
                {
                    isLoading.Remove(variantPath);
                    return default;
                }
            }

            if (PrintDebugMessages) Debug.Log($"AssetMemory: Cached at {cachePath}");
            path = "file://" + cachePath;
        }

        T asset = default;
        if (typeof(T) == typeof(Sprite))
        {
            // Fit crop
            var loadPath = variantExists ? variantPath : path;
            
            using (var request = UnityWebRequest.Get(loadPath))
            {
                await request.SendWebRequest();

                if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                {
                    isLoading.Remove(variantPath);
                    return default;
                }

                if (request.isNetworkError || request.isHttpError)
                {
                    // TODO: Neo, fix your image CDN :)
                    if (request.responseCode != 422)
                    {
                        Debug.LogError(
                            $"AssetMemory: Failed to load {loadPath}");
                        Debug.LogError(request.error);
                        isLoading.Remove(variantPath);
                        return default;
                    }
                }

                var bytes = request.downloadHandler.data;
                if (bytes == null)
                {
                    isLoading.Remove(variantPath);
                    return default;
                }

                var texture = request.downloadHandler.data.ToTexture2D();
                texture.name = variantPath;
                
                // Fit crop
                if (!variantExists && options != default)
                {
                    if (!(options is SpriteAssetOptions spriteOptions))
                    {
                        throw new ArgumentException();
                    }

                    if (texture.width != spriteOptions.FitCropSize[0] || texture.height != spriteOptions.FitCropSize[1])
                    {
                        var croppedTexture = TextureScaler.FitCrop(texture, spriteOptions.FitCropSize[0],
                            spriteOptions.FitCropSize[1]);
                        croppedTexture.name = variantPath;
                        Object.Destroy(texture);
                        texture = croppedTexture;
                        bytes = texture.EncodeToJPG();

                        async void Task()
                        {
                            await UniTask.SwitchToThreadPool();
                            File.WriteAllBytes(variantPath.Substring("file://".Length), bytes);
                        }
                        Task();
                    }
                    else
                    {
                        async void Task()
                        {
                            await UniTask.SwitchToThreadPool();
                            File.Copy(loadPath.Substring("file://".Length), variantPath.Substring("file://".Length));
                        }
                        Task();
                    }
                }

                var sprite = texture.CreateSprite();
                memoryCache[variantPath] = new SpriteEntry(variantPath, tag, sprite);
                asset = (T) Convert.ChangeType(sprite, typeof(T));
            }
        }
        else if (typeof(T) == typeof(AudioClip))
        {
            var loader = new AudioClipLoader(variantPath);
            await loader.Load();
            
            if (cancellationToken != default && cancellationToken.IsCancellationRequested)
            {
                isLoading.Remove(variantPath);
                loader.Unload();
                return default;
            }
            
            if (loader.Error != null)
            {
                isLoading.Remove(variantPath);
                Debug.LogError($"AssetMemory: Failed to download audio from {variantPath}");
                Debug.LogError(loader.Error);
                return default;
            }

            var audioClip = loader.AudioClip;
            memoryCache[variantPath] = new AudioEntry(variantPath, tag, loader);
            asset = (T) Convert.ChangeType(audioClip, typeof(T));
        }
        
        taggedMemoryCache[tag].Enqueue(memoryCache[variantPath], queuePriority++);
        
        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        if (PrintDebugMessages) Debug.Log($"AssetMemory: Loaded {variantPath} in {time}ms");

        isLoading.Remove(variantPath);
        return asset;
    }

    public bool DisposeAsset(string path, AssetTag tag)
    {
        if (!memoryCache.ContainsKey(path)) return false;
        
        var entry = memoryCache[path];
        entry.Tags.Remove(tag);
        if (entry.Tags.Count == 0)
        {
            entry.Dispose();
            memoryCache.Remove(entry.Key);
        }
        taggedMemoryCache[tag].Remove(entry);

        return true;
    }

    public void DisposeTaggedCacheAssets(AssetTag tag)
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new SimplePriorityQueue<Entry>();

        var removals = new List<string>();
        foreach (var pair in memoryCache)
        {
            var entry = pair.Value;
            if (entry.Tags.Contains(tag))
            {
                entry.Tags.Remove(tag);
                if (entry.Tags.Count == 0)
                {
                    removals.Add(pair.Key);
                    entry.Dispose();
                }
            }
        }

        removals.ForEach(it => memoryCache.Remove(it));
        if (PrintDebugMessages) Debug.Log($"AssetMemory: Unloaded {removals.Count} assets with tag {tag}");
        taggedMemoryCache[tag].Clear();
    }

    public void DisposeAllAssets()
    {
        foreach (var pair in memoryCache)
        {
            pair.Value.Dispose();
        }
        memoryCache.Clear();
        taggedMemoryCache.Clear();
    }

    private void CheckIfExceedTagLimit(AssetTag tag)
    {
        var taggedMemory = taggedMemoryCache[tag];
        if (TagLimits.ContainsKey(tag) && taggedMemory.Count > TagLimits[tag])
        {
            var exceeded = taggedMemory.Count - TagLimits[tag];
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Unloading {exceeded} assets due to {tag} limit");
            for (var i = 0; i < exceeded; i++)
            {
                var entry = taggedMemory.Dequeue();
                entry.Tags.Remove(tag);
                if (entry.Tags.Count == 0)
                {
                    entry.Dispose();
                    memoryCache.Remove(entry.Key);
                }
            }

            if (PrintDebugMessages) Debug.Log($"AssetMemory: Unloaded {exceeded} assets due to {tag} limit");
        }
    }

    public int CountTagUsage(AssetTag tag)
    {
        return taggedMemoryCache.ContainsKey(tag) ? taggedMemoryCache[tag].Count : 0;
    }

    public int GetTagLimit(AssetTag tag)
    {
        return TagLimits.ContainsKey(tag) ? TagLimits[tag] : -1;
    }

    private string GetCacheFilePath(string uri)
    {
        uri = uri
            .Replace("https://", "")
            .Replace("http://", "")
            .Replace("?", "~")
            .Replace("|", ".")
            .Replace("&", ".")
            .Replace("%", ".");
        var path = Application.temporaryCachePath + "/Online/" + uri;
        var dirPath = Path.GetDirectoryName(path);
        Directory.CreateDirectory(dirPath ?? throw new Exception());
        return path;
    }

    public abstract class Entry
    {
        public string Key;
        public HashSet<AssetTag> Tags = new HashSet<AssetTag>();
        public abstract void Dispose();
    }

    public abstract class Entry<T> : Entry
    {
        public T Asset;
        public Entry(string key, AssetTag initialTag, T asset)
        {
            Key = key;
            Tags.Add(initialTag);
            Asset = asset;
        }
    }

    public class SpriteEntry : Entry<Sprite>
    {
        public SpriteEntry(string key, AssetTag tag, Sprite sprite) : base(key, tag, sprite)
        {
        }

        public override void Dispose()
        {
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Disposed {Key} with tag {string.Join(",", Tags.Select(it => Enum.GetName(typeof(AssetTag), it)))}");
            
            try
            {
                Object.Destroy(Asset.texture);
                Object.Destroy(Asset);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            Asset = null;
        }
    }

    public class AudioEntry : Entry<AudioClip>
    {
        public AudioClipLoader Loader;

        public AudioEntry(string key, AssetTag tag, AudioClipLoader loader) : base(key, tag, loader.AudioClip)
        {
            Loader = loader;
        }

        public override void Dispose()
        {
            Loader.Unload();
            Asset = null;
        }
    }

}

public enum AssetTag
{
    Avatar,
    PlayerAvatar,
    LocalLevelCoverThumbnail,
    RemoteLevelCoverThumbnail,
    CollectionCoverThumbnail,
    RecordCoverThumbnail,
    GameCover,
    TierCover,
    CharacterThumbnail,
    Storyboard,
    PreviewMusic,
    EventCover,
    EventLogo,
    CollectionCover
}

public class AssetOptions
{
    protected AssetOptions()
    {
            
    }
}

public class SpriteAssetOptions : AssetOptions
{
    public int[] FitCropSize { get; }

    public SpriteAssetOptions(int[] fitCropSize)
    {
        FitCropSize = fitCropSize;
    }
}