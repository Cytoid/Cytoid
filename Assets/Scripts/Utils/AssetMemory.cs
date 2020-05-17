using System;
using System.Collections.Generic;
using System.IO;
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
        {AssetTag.LocalLevelCoverThumbnail, 24},
        {AssetTag.RemoteLevelCoverThumbnail, 24},
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

    public T GetCachedAsset<T>(string path)
    {
        if (memoryCache.ContainsKey(path))
        {
            var entry = memoryCache[path];
            if (entry is Entry<T> typedEntry && typedEntry.Asset != null)
            {
                // Update priority
                taggedMemoryCache[typedEntry.Tag].UpdatePriority(entry, queuePriority++);
                return typedEntry.Asset;
            }
        }

        return default;
    }

    public bool HasCachedAsset<T>(string path) => GetCachedAsset<T>(path) != null;

    public static bool PrintDebugMessages = false;

    public async UniTask<T> LoadAsset<T>(string path, AssetTag tag, CancellationToken cancellationToken = default,
        bool useFileCache = false, AssetOptions options = default) where T : Object
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new SimplePriorityQueue<Entry>();

        var cachedSprite = GetCachedAsset<T>(path);
        if (cachedSprite != null)
        {
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Returning cached asset {path}");
            return cachedSprite;
        }

        // Currently loading
        if (isLoading.Contains(path))
        {
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Already loading {path}. Waiting...");
            await UniTask.WaitUntil(() => !isLoading.Contains(path), cancellationToken: cancellationToken);
            if (PrintDebugMessages) Debug.Log($"AssetMemory: Wait {path} complete.");
            return await LoadAsset<T>(path, tag, cancellationToken, useFileCache, options);
        }

        CheckIfExceedTagLimit(tag);

        if (PrintDebugMessages) Debug.Log($"AssetMemory: Started loading {path}.");
        isLoading.Add(path);

        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var hasFileCache = false;
        string fileCachePath = null;
        if (useFileCache && !path.StartsWith("file://"))
        {
            fileCachePath = GetCacheFilePath(path);
            hasFileCache = File.Exists(fileCachePath);

            if (!hasFileCache)
            {
                using (var request = UnityWebRequest.Get(path))
                {
                    request.downloadHandler =
                        new DownloadHandlerFile(fileCachePath).Also(it => it.removeFileOnAbort = true);
                    await request.SendWebRequest();
                    if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                    {
                        isLoading.Remove(path);
                        return default;
                    }

                    if (request.isNetworkError || request.isHttpError)
                    {
                        // TODO: Neo, fix your image CDN :)
                        if (request.responseCode != 422)
                        {
                            Debug.LogError($"AssetMemory: Failed to download {path}");
                            Debug.LogError(request.error);
                            isLoading.Remove(path);
                            return default;
                        }
                    }
                    else
                    {
                        hasFileCache = true;
                        if (PrintDebugMessages) Debug.Log($"AssetMemory: Saved {path} to {fileCachePath}");
                    }
                }
            }

            if (hasFileCache) if (PrintDebugMessages) Debug.Log($"AssetMemory: Cached at {fileCachePath}");
        }

        T asset = default;
        if (typeof(T) == typeof(Sprite))
        {
            using (var request = UnityWebRequest.Get(hasFileCache ? ("file://" + fileCachePath) : path))
            {
                await request.SendWebRequest();

                if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                {
                    isLoading.Remove(path);
                    return default;
                }

                if (request.isNetworkError || request.isHttpError)
                {
                    // TODO: Neo, fix your image CDN :)
                    if (request.responseCode != 422)
                    {
                        Debug.LogError(
                            $"AssetMemory: Failed to load {(hasFileCache ? fileCachePath : path)}");
                        Debug.LogError(request.error);
                        isLoading.Remove(path);
                        return default;
                    }
                }

                var bytes = request.downloadHandler.data;
                if (bytes == null)
                {
                    isLoading.Remove(path);
                    return default;
                }

                var texture = request.downloadHandler.data.ToTexture2D();
                texture.name = path;

                // Fit crop
                if (options is SpriteAssetOptions spriteOptions)
                {
                    if (spriteOptions.FitCropSize != default &&
                        (texture.width != spriteOptions.FitCropSize[0] ||
                         texture.height != spriteOptions.FitCropSize[1]))
                    {
                        var croppedTexture = TextureScaler.FitCrop(texture, spriteOptions.FitCropSize[0], spriteOptions.FitCropSize[1]);
                        croppedTexture.name = path;
                        Object.Destroy(texture);
                        texture = croppedTexture;
                    }
                }

                var sprite = texture.CreateSprite();
                memoryCache[path] = new SpriteEntry(path, tag, sprite);
                asset = (T) Convert.ChangeType(sprite, typeof(T));
            }
        }
        else if (typeof(T) == typeof(AudioClip))
        {
            var loader = new AudioClipLoader(path);
            await loader.Load();
            
            if (cancellationToken != default && cancellationToken.IsCancellationRequested)
            {
                isLoading.Remove(path);
                loader.Unload();
                return default;
            }
            
            if (loader.Error != null)
            {
                Debug.LogError($"AssetMemory: Failed to download audio from {path}");
                Debug.LogError(loader.Error);
                return default;
            }

            var audioClip = loader.AudioClip;
            memoryCache[path] = new AudioEntry(path, tag, loader);
            asset = (T) Convert.ChangeType(audioClip, typeof(T));
        }
        
        taggedMemoryCache[tag].Enqueue(memoryCache[path], queuePriority++);
        
        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        if (PrintDebugMessages) Debug.Log($"AssetMemory: Loaded {path} in {time}ms");

        isLoading.Remove(path);
        return asset;
    }

    public void DisposeTaggedCacheAssets(AssetTag tag)
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new SimplePriorityQueue<Entry>();

        var removals = new List<string>();
        foreach (var pair in memoryCache)
        {
            if (pair.Value.Tag == tag)
            {
                removals.Add(pair.Key);
                pair.Value.Dispose();
            }
        }

        removals.ForEach(it => memoryCache.Remove(it));
        if (PrintDebugMessages) Debug.Log($"AssetMemory: Unloaded {removals.Count} assets with tag {tag}");
        taggedMemoryCache[tag] = new SimplePriorityQueue<Entry>();
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
        if (TagLimits.ContainsKey(tag) && taggedMemoryCache[tag].Count > TagLimits[tag])
        {
            var exceeded = taggedMemoryCache[tag].Count - TagLimits[tag];
            for (var i = 0; i < exceeded; i++)
            {
                var entry = taggedMemoryCache[tag].Dequeue();
                entry.Dispose();
                memoryCache.Remove(entry.Key);
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
        public AssetTag Tag;
        public abstract void Dispose();
    }

    public abstract class Entry<T> : Entry
    {
        public T Asset;
        public Entry(string key, AssetTag tag, T asset)
        {
            Key = key;
            Tag = tag;
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
    public int[] FitCropSize;

    public SpriteAssetOptions(int[] fitCropSize)
    {
        FitCropSize = fitCropSize;
    }
}