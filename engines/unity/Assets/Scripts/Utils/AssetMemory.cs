using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Session-local cache for engine-rendered sprites.
/// Asset acquisition and disk caching are owned by Flutter; paths must be local file URIs.
/// </summary>
public class AssetMemory
{
    private static readonly Dictionary<AssetTag, int> TagLimits = new Dictionary<AssetTag, int>
    {
        {AssetTag.GameCover, 1}
    };

    private readonly Dictionary<string, Entry> memoryCache = new Dictionary<string, Entry>();
    private readonly HashSet<string> loadingPaths = new HashSet<string>();

    public static bool PrintDebugMessages;

    public Entry<T> GetCachedAssetEntry<T>(string path) where T : Object
    {
        return memoryCache.TryGetValue(path, out var entry) && entry is Entry<T> typed && typed.Asset != null
            ? typed
            : null;
    }

    public bool HasCachedAsset<T>(string path) where T : Object => GetCachedAssetEntry<T>(path) != null;

    public async UniTask<T> LoadAsset<T>(
        string path,
        AssetTag tag,
        CancellationToken cancellationToken = default) where T : Object
    {
        if (typeof(T) != typeof(Sprite))
        {
            throw new NotSupportedException($"AssetMemory only loads sprites, not {typeof(T).Name}");
        }
        if (!TryResolveLocalPath(path, out var localPath))
        {
            throw new ArgumentException("Asset paths must be local file URIs supplied by the host.", nameof(path));
        }

        var cached = GetCachedAssetEntry<T>(path);
        if (cached != null)
        {
            cached.Tags.Add(tag);
            return cached.Asset;
        }

        if (loadingPaths.Contains(path))
        {
            await UniTask.WaitUntil(() => !loadingPaths.Contains(path), cancellationToken: cancellationToken);
            var loaded = GetCachedAssetEntry<T>(path);
            if (loaded != null)
            {
                loaded.Tags.Add(tag);
                return loaded.Asset;
            }

            // Leader failed or disposed the entry before waiters woke; retry as a fresh load.
            return await LoadAsset<T>(path, tag, cancellationToken);
        }

        EnforceTagLimit(tag);
        loadingPaths.Add(path);
        try
        {
            byte[] bytes;
            await UniTask.SwitchToThreadPool();
            try
            {
                bytes = File.ReadAllBytes(localPath);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
            cancellationToken.ThrowIfCancellationRequested();

            var texture = bytes.ToTexture2D();
            if (texture == null)
            {
                throw new InvalidDataException($"Unable to decode image: {localPath}");
            }

            texture.name = path;
            var sprite = texture.CreateSprite();
            var entry = new SpriteEntry(path, tag, sprite);
            memoryCache[path] = entry;
            return (T)(Object)sprite;
        }
        finally
        {
            loadingPaths.Remove(path);
        }
    }

    public bool DisposeAsset(string path, AssetTag tag)
    {
        if (!memoryCache.TryGetValue(path, out var entry))
        {
            return false;
        }

        entry.Tags.Remove(tag);
        if (entry.Tags.Count == 0)
        {
            entry.Dispose();
            memoryCache.Remove(path);
        }
        return true;
    }

    public void DisposeTaggedCacheAssets(AssetTag tag)
    {
        var removals = memoryCache
            .Where(pair => pair.Value.Tags.Contains(tag))
            .Select(pair => pair.Key)
            .ToList();
        foreach (var key in removals)
        {
            DisposeAsset(key, tag);
        }
    }

    public void DisposeAllAssets()
    {
        foreach (var entry in memoryCache.Values)
        {
            entry.Dispose();
        }
        memoryCache.Clear();
        loadingPaths.Clear();
    }

    public int CountTagUsage(AssetTag tag) =>
        memoryCache.Values.Count(entry => entry.Tags.Contains(tag));

    public int GetTagLimit(AssetTag tag) =>
        TagLimits.TryGetValue(tag, out var limit) ? limit : -1;

    private void EnforceTagLimit(AssetTag tag)
    {
        if (!TagLimits.TryGetValue(tag, out var limit))
        {
            return;
        }

        var taggedKeys = memoryCache
            .Where(pair => pair.Value.Tags.Contains(tag))
            .Select(pair => pair.Key)
            .ToList();
        while (taggedKeys.Count >= limit && taggedKeys.Count > 0)
        {
            var key = taggedKeys[0];
            taggedKeys.RemoveAt(0);
            DisposeAsset(key, tag);
        }
    }

    private static bool TryResolveLocalPath(string path, out string localPath)
    {
        localPath = null;
        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri) || !uri.IsFile)
        {
            return false;
        }

        localPath = uri.LocalPath;
        return File.Exists(localPath);
    }

    public abstract class Entry
    {
        protected Entry(string key, AssetTag initialTag)
        {
            Key = key;
            Tags.Add(initialTag);
        }

        public string Key { get; }
        public HashSet<AssetTag> Tags { get; } = new HashSet<AssetTag>();
        public abstract void Dispose();
    }

    public abstract class Entry<T> : Entry where T : Object
    {
        protected Entry(string key, AssetTag initialTag, T asset) : base(key, initialTag)
        {
            Asset = asset;
        }

        public T Asset { get; protected set; }
    }

    private sealed class SpriteEntry : Entry<Sprite>
    {
        public SpriteEntry(string key, AssetTag tag, Sprite sprite) : base(key, tag, sprite)
        {
        }

        public override void Dispose()
        {
            if (Asset == null)
            {
                return;
            }

            Object.Destroy(Asset.texture);
            Object.Destroy(Asset);
            Asset = null;
        }
    }
}

public enum AssetTag
{
    GameCover,
    Storyboard
}
