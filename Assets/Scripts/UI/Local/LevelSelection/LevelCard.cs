using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LevelCard : MonoBehaviour
{
    
    public Image cover;

    public Text title;
    public Text artist;

    private Level level;

    public void ScrollCellContent(object levelObject)
    {
        level = (Level) levelObject;
        title.text = level.meta.title;
        artist.text = level.meta.artist;

        StartCoroutine(LoadCover());
    }

    public IEnumerator LoadCover()
    {
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        var path = "file://" + level.path + ".thumbnail";
        using (var request = UnityWebRequestTexture.GetTexture(path))
        {
            yield return request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                print(path);
                print(request.error);
            }
            else
            {
                var coverTexture = DownloadHandlerTexture.GetContent(request);
                var sprite = Sprite.Create(coverTexture, new Rect(0, 0, coverTexture.width, coverTexture.height),
                    Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);
                cover.sprite = sprite;
                cover.GetComponent<AspectRatioFitter>().aspectRatio = sprite.texture.width * 1.0f / sprite.texture.height;
            }
        }

        time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - time;
        print("Cover for " + level.meta.id + ": " + time + "ms");
    }
    
}