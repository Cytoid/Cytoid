using System.Collections;
using System.Linq.Expressions;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GamePreparationScreen : Screen
{
    [GetComponentInChildrenName] public Text title;
    [GetComponentInChildrenName] public Text artist;
    [GetComponentInChildrenName] public Image cover;

    private Sprite sprite;

    public override string GetId() => "GamePreparation";

    public override void OnScreenBecomeActive()
    {
        base.OnScreenBecomeActive();
        
        var selectedLevel = Context.activeLevel;
        if (selectedLevel == null)
        {
            Debug.LogWarning("Context.activeLevel is null");
            return;
        }

        title.text = selectedLevel.meta.title;
        artist.text = selectedLevel.meta.artist;
        cover.color = Color.black;

        LoadCover();
    }

    private async void LoadCover()
    {
        var selectedLevel = Context.activeLevel;
        var path = "file://" + selectedLevel.path + selectedLevel.meta.background.path;
        using (var request = UnityWebRequestTexture.GetTexture(path))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                print(path);
                print(request.error);
            }
            else
            {
                var coverTexture = DownloadHandlerTexture.GetContent(request);
                sprite = Sprite.Create(coverTexture, new Rect(0, 0, coverTexture.width, coverTexture.height),
                    Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);
                cover.sprite = sprite;
                cover.GetComponent<AspectRatioFitter>().aspectRatio =
                    coverTexture.width * 1.0f / coverTexture.height;
                cover.color = Color.white;
            }
        }
    }

    public override void OnScreenDestroyed()
    {
        base.OnScreenDestroyed();
        
        cover.color = Color.black;
        cover.sprite = null;
        Destroy(sprite);
        sprite = null;
    }
    
}