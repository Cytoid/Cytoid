using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SpriteCache
{
    private static Dictionary<string, int> tagLimits = new Dictionary<string, int>
    {
        {"LocalLevelCoverThumbnail", 96},
        {"RemoteLevelCoverThumbnail", 96},
        {"Avatar", 100}
    };

    private readonly Dictionary<string, List<Entry>> taggedCache = new Dictionary<string, List<Entry>>();
    private readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();

    public Sprite GetCachedSprite(string path)
    {
        if (cache.ContainsKey(path))
        {
            var entry = cache[path];
            if (entry.Sprite != null)
            {
                // Update priority
                taggedCache[entry.Tag].Remove(entry);
                taggedCache[entry.Tag].Add(entry);
                return entry.Sprite;
            }
        }

        return null;
    }

    public bool HasCachedSprite(string path) => GetCachedSprite(path) != null;

    public async UniTask<Sprite> CacheSprite(string path, string tag, CancellationToken cancellationToken = default,
        int[] fitCropSize = default)
    {
        if (!taggedCache.ContainsKey(tag)) taggedCache[tag] = new List<Entry>();

        bool isLoading;
        var cachedSprite = GetCachedSprite(path);
        if (cachedSprite != null) return cachedSprite;
        isLoading = cache.ContainsKey(path);

        // Currently loading
        if (isLoading)
        {
            await UniTask.WaitUntil(() => GetCachedSprite(path) != null, cancellationToken: cancellationToken);
            return GetCachedSprite(path);
        }

        if (cache.ContainsKey(path))
        {
            Dispose(cache[path].Sprite);
            taggedCache[tag].Remove(cache[path]);
            cache.Remove(path);
        }
        else
        {
            CheckIfExceedTagLimit(tag);
        }

        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        Sprite sprite;
        using (var request = UnityWebRequestTexture.GetTexture(path))
        {
            await request.SendWebRequest();
            if (cancellationToken != default && cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (request.isNetworkError || request.isHttpError)
            {
                // TODO: Neo, fix your image CDN :)
                if (request.responseCode != 422)
                {
                    Debug.LogError($"SpriteCache: Failed to load {path}");
                    Debug.LogError(request.error);
                    return null;
                }
            }

            var coverTexture = DownloadHandlerTexture.GetContent(request);
            if (coverTexture == null) return null;

            // Fit crop
            // TODO: For some reasons, the texture read would be black unless I do stupid I/O like this...
            if (fitCropSize != default && (coverTexture.width != fitCropSize[0] || coverTexture.height != fitCropSize[1]))
            {
                Debug.Log("start cropping!!!");
                
                Directory.CreateDirectory(Context.DataPath + "/.cache");
                var filename = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                
                var bytes = coverTexture.EncodeToJPG();
                var innerPath = $"{Context.DataPath}/.cache/{filename}";
                File.WriteAllBytes(innerPath, bytes);
                Object.Destroy(coverTexture);

                using (var request2 = UnityWebRequestTexture.GetTexture(innerPath))
                {
                    await request2.SendWebRequest();
                    coverTexture = DownloadHandlerTexture.GetContent(request2);
                    coverTexture = TextureScaler.FitCrop(coverTexture, fitCropSize[0], fitCropSize[1]);
                    bytes = coverTexture.EncodeToJPG();
                    File.WriteAllBytes(innerPath, bytes);
                    Object.Destroy(coverTexture);
                
                    using (var request3 = UnityWebRequestTexture.GetTexture(innerPath))
                    {
                        await request3.SendWebRequest();
                        coverTexture = DownloadHandlerTexture.GetContent(request3);
                        Debug.Log($"size: {coverTexture.width}/{coverTexture.height}");
                    }
                    if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                    {
                        File.Delete(innerPath);
                        return null;
                    }
                }
                if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                {
                    File.Delete(innerPath);
                    return null;
                }
                File.Delete(innerPath);
            }

            sprite = coverTexture.CreateSprite();
            cache[path] = new Entry {Key = path, Sprite = sprite, Tag = tag};
            taggedCache[tag].Add(cache[path]);
        }

        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        // Debug.Log($"SpriteCache: Loaded {path} in {time}ms");

        return sprite;
    }

    public void PutSprite(string path, string tag, Sprite sprite)
    {
        if (!taggedCache.ContainsKey(tag)) taggedCache[tag] = new List<Entry>();

        if (cache.ContainsKey(path))
        {
            Dispose(cache[path].Sprite);
            taggedCache[tag].Remove(cache[path]);
            cache.Remove(path);
        }
        else
        {
            CheckIfExceedTagLimit(tag);
        }

        cache[path] = new Entry {Key = path, Sprite = sprite, Tag = tag};
        taggedCache[tag].Add(cache[path]);
    }

    public void DisposeTagged(string tag)
    {
        if (!taggedCache.ContainsKey(tag)) taggedCache[tag] = new List<Entry>();

        var removals = new List<string>();
        foreach (var pair in cache)
        {
            if (pair.Value.Tag == tag)
            {
                removals.Add(pair.Key);
                Dispose(pair.Value.Sprite);
            }
        }

        removals.ForEach(it => cache.Remove(it));
        taggedCache[tag] = new List<Entry>();
    }

    public void DisposeAll()
    {
        foreach (var pair in cache)
        {
            Dispose(pair.Value.Sprite);
        }

        cache.Clear();
        taggedCache.Clear();
    }

    private void CheckIfExceedTagLimit(string tag)
    {
        if (tagLimits.ContainsKey(tag) && taggedCache[tag].Count > tagLimits[tag])
        {
            var exceeded = taggedCache[tag].Count - tagLimits[tag];
            for (var i = 0; i < exceeded; i++)
            {
                var entry = taggedCache[tag][i];
                Dispose(entry.Sprite);
                cache.Remove(entry.Key);
            }

            taggedCache[tag].RemoveRange(0, exceeded);
        }
    }

    private void Dispose(Sprite sprite)
    {
        Object.Destroy(sprite.texture);
        Object.Destroy(sprite);
    }

    public class Entry
    {
        public string Key;
        public string Tag;
        public Sprite Sprite;
    }
}