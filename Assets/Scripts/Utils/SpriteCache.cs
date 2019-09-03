using System;
using System.Collections;
using System.Collections.Generic;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SpriteCache
{

    private Dictionary<string, Entry> cache = new Dictionary<string, Entry>();

    public Sprite GetCachedSprite(string path)
    {
        return cache.ContainsKey(path) ? cache[path].Sprite : null;
    }

    public bool HasCachedSprite(string path) => GetCachedSprite(path) != null;
    
    public async UniTask<Sprite> CacheSprite(string path, string tag)
    {
        var cachedSprite = GetCachedSprite(path);
        if (cachedSprite != null) return cachedSprite;

        // Currently loading
        if (cache.ContainsKey(path))
        {
            await UniTask.WaitUntil(() => GetCachedSprite(path) != null);
            return GetCachedSprite(path);
        }
        cache[path] = null;
        
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        Sprite sprite;
        using (var request = UnityWebRequestTexture.GetTexture(path))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"SpriteCache: Failed to load {path}");
                Debug.LogError(request.error);
                return null;
            }

            var coverTexture = DownloadHandlerTexture.GetContent(request);
            sprite = coverTexture.CreateSprite();
            cache[path] = new Entry { Sprite = sprite, Tag = tag};
        }
        
        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        // Debug.Log($"SpriteCache: Loaded {path} in {time}ms");

        return sprite;
    }
    
    public void PutSprite(string path, string tag, Sprite sprite)
    {
        cache[path] = new Entry {Sprite = sprite, Tag = tag};
    }

    public void ClearTagged(string tag)
    {
        var removals = new List<string>();
        foreach (var pair in cache)
        {
            if (pair.Value.Tag == tag)
            {
                removals.Add(pair.Key);
                Object.Destroy(pair.Value.Sprite);
            }
        }
        removals.ForEach(it => cache.Remove(it));
    }
    
    public void ClearAll()
    {
        foreach (var pair in cache)
        {
            Object.Destroy(pair.Value.Sprite);
        }
        cache.Clear();
    }

    public class Entry
    {
        public string Tag;
        public Sprite Sprite;
    }
}