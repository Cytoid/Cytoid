using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SpriteCache
{
    private static readonly Dictionary<SpriteTag, int> TagLimits = new Dictionary<SpriteTag, int>
    {
        {SpriteTag.LocalCoverThumbnail, 96},
        {SpriteTag.OnlineCoverThumbnail, 96},
        {SpriteTag.Avatar, 100}
    };

    private readonly Dictionary<SpriteTag, List<Entry>> taggedMemoryCache = new Dictionary<SpriteTag, List<Entry>>();
    private readonly Dictionary<string, Entry> memoryCache = new Dictionary<string, Entry>();
    private readonly HashSet<string> isLoading = new HashSet<string>();

    public Sprite GetCachedSpriteFromMemory(string path)
    {
        if (memoryCache.ContainsKey(path))
        {
            var entry = memoryCache[path];
            if (entry.Sprite != null)
            {
                // Update priority
                taggedMemoryCache[entry.Tag].Remove(entry);
                taggedMemoryCache[entry.Tag].Add(entry);
                return entry.Sprite;
            }
        }

        return null;
    }

    public bool HasCachedSpriteInMemory(string path) => GetCachedSpriteFromMemory(path) != null;
    private const bool debug = false;

    public async UniTask<Sprite> CacheSpriteInMemory(string path, SpriteTag tag, CancellationToken cancellationToken = default,
        int[] fitCropSize = default, bool useFileCache = false)
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new List<Entry>();

        var cachedSprite = GetCachedSpriteFromMemory(path);
        if (cachedSprite != null) return cachedSprite;

        // Currently loading
        if (isLoading.Contains(path))
        {
            if (debug) Debug.Log($"SpriteCache: Already loading {path}. Waiting...");
            await UniTask.WaitUntil(() => !isLoading.Contains(path), cancellationToken: cancellationToken);
            if (debug) Debug.Log($"SpriteCache: Wait {path} complete.");
            return await CacheSpriteInMemory(path, tag, cancellationToken, fitCropSize, useFileCache);
        }
        
        if (!memoryCache.ContainsKey(path))
        {
            CheckIfExceedTagLimit(tag);
        }

        if (debug) Debug.Log($"SpriteCache: Started loading {path}.");
        isLoading.Add(path);

        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        Sprite sprite;
        var hasFileCache = false;
        string fileCachePath = null;
        if (useFileCache)
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
                        return null;
                    }

                    if (request.isNetworkError || request.isHttpError)
                    {
                        // TODO: Neo, fix your image CDN :)
                        if (request.responseCode != 422)
                        {
                            Debug.LogError($"SpriteCache: Failed to download {path}");
                            Debug.LogError(request.error);
                            isLoading.Remove(path);
                            return null;
                        }
                    }
                    if (debug) Debug.Log($"SpriteCache: Saved {path} to {fileCachePath}");
                }

                hasFileCache = true;
            }
        }
        
        using (var request = UnityWebRequestTexture.GetTexture(hasFileCache ? ("file://" + fileCachePath) : path))
        {
            await request.SendWebRequest();
            if (cancellationToken != default && cancellationToken.IsCancellationRequested)
            {
                isLoading.Remove(path);
                return null;
            }

            if (request.isNetworkError || request.isHttpError)
            {
                // TODO: Neo, fix your image CDN :)
                if (request.responseCode != 422)
                {
                    Debug.LogError($"SpriteCache: Failed to load {(hasFileCache ? fileCachePath : path)}");
                    Debug.LogError(request.error);
                    isLoading.Remove(path);
                    return null;
                }
            }

            var coverTexture = DownloadHandlerTexture.GetContent(request);
            if (coverTexture == null)
            {
                isLoading.Remove(path);
                return null;
            }

            // Fit crop
            // TODO: For some reasons, the texture read would be black unless I do stupid I/O like this...
            // TODO: Apparently still partially broken on iOS
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
                        isLoading.Remove(path);
                        return null;
                    }
                }
                if (cancellationToken != default && cancellationToken.IsCancellationRequested)
                {
                    File.Delete(innerPath);
                    isLoading.Remove(path);
                    return null;
                }
                File.Delete(innerPath);
            }

            sprite = coverTexture.CreateSprite();
            memoryCache[path] = new Entry {Key = path, Sprite = sprite, Tag = tag};
            taggedMemoryCache[tag].Add(memoryCache[path]);
        }

        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        if (debug) Debug.Log($"SpriteCache: Loaded {path} in {time}ms");

        isLoading.Remove(path);
        return sprite;
    }

    public void PutSpriteInMemory(string path, SpriteTag tag, Sprite sprite)
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new List<Entry>();

        if (memoryCache.ContainsKey(path))
        {
            Dispose(memoryCache[path].Sprite);
            taggedMemoryCache[tag].Remove(memoryCache[path]);
            memoryCache.Remove(path);
        }
        else
        {
            CheckIfExceedTagLimit(tag);
        }

        memoryCache[path] = new Entry {Key = path, Sprite = sprite, Tag = tag};
        taggedMemoryCache[tag].Add(memoryCache[path]);
    }

    public void DisposeTaggedSpritesInMemory(SpriteTag tag)
    {
        if (!taggedMemoryCache.ContainsKey(tag)) taggedMemoryCache[tag] = new List<Entry>();

        var removals = new List<string>();
        foreach (var pair in memoryCache)
        {
            if (pair.Value.Tag == tag)
            {
                removals.Add(pair.Key);
                Dispose(pair.Value.Sprite);
            }
        }

        removals.ForEach(it => memoryCache.Remove(it));
        taggedMemoryCache[tag] = new List<Entry>();
    }

    public void DisposeAllInMemory()
    {
        foreach (var pair in memoryCache)
        {
            Dispose(pair.Value.Sprite);
        }

        memoryCache.Clear();
        taggedMemoryCache.Clear();
    }

    private void CheckIfExceedTagLimit(SpriteTag tag)
    {
        if (TagLimits.ContainsKey(tag) && taggedMemoryCache[tag].Count > TagLimits[tag])
        {
            var exceeded = taggedMemoryCache[tag].Count - TagLimits[tag];
            for (var i = 0; i < exceeded; i++)
            {
                var entry = taggedMemoryCache[tag][i];
                Dispose(entry.Sprite);
                memoryCache.Remove(entry.Key);
            }

            taggedMemoryCache[tag].RemoveRange(0, exceeded);
        }
    }

    private void Dispose(Sprite sprite)
    {
        // May fail because multiple cache may refer to the same sprite
        try
        {
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }
        catch (Exception ignore)
        {
            // ignored
        }
    }

    private string GetCacheFilePath(string uri)
    {
        uri = uri
            .Replace("https://", "")
            .Replace("http://", "")
            .Replace("?", "~")
            .Replace("&", ".")
            .Replace("%", ".");
        var path = Path.Combine(Application.temporaryCachePath, uri);
        var dirPath = Path.GetDirectoryName(path);
        Directory.CreateDirectory(dirPath);
        return path;
    }

    public class Entry
    {
        public string Key;
        public SpriteTag Tag;
        public Sprite Sprite;
    }
}

public enum SpriteTag {
    Avatar, PlayerAvatar, LocalCoverThumbnail, OnlineCoverThumbnail, GameCover, CharacterThumbnail, Storyboard
}