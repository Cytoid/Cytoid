using System;
using System.Collections;
using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SpriteCache
{

    private Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public Sprite GetCachedSprite(string path)
    {
        return cache.ContainsKey(path) ? cache[path] : null;
    }
    
    public async UniTask<Sprite> GetSprite(string path)
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

        Sprite sprite = null;
        using (var request = UnityWebRequestTexture.GetTexture(path))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"SpriteCache: Failed to load {path}");
                Debug.LogError(request.error);
            }
            else
            {
                var coverTexture = DownloadHandlerTexture.GetContent(request);
                sprite = Sprite.Create(coverTexture, new Rect(0, 0, coverTexture.width, coverTexture.height),
                    Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);
                cache[path] = sprite;
            }
        }
        
        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        Debug.Log($"SpriteCache: Loaded {path} in {time}ms");

        return sprite;
    }
    
    public void Clear()
    {
        foreach (var pair in cache)
        {
            Object.Destroy(pair.Value);
        }
        cache.Clear();
    }
}