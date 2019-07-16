using System.Collections;
using System.Linq.Expressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GamePreparationScreen : Screen
{
    [GetComponentInChildrenName] public Text title;
    [GetComponentInChildrenName] public Text artist;
    [GetComponentInChildrenName] public Image cover;
    [GetComponentInChildrenName] public Image mask;

    private Sprite sprite;

    public override string GetId() => "GamePreparation";

    public override void OnScreenBecomeActive()
    {
        var selectedLevel = Context.activeLevel;
        title.text = selectedLevel.meta.title;
        artist.text = selectedLevel.meta.artist;
        cover.color = Color.black;
        mask.color = mask.color.WithAlpha(1f);

        StartCoroutine(LoadCover());
    }

    private IEnumerator LoadCover()
    {
        var selectedLevel = Context.activeLevel;
        var path = "file://" + selectedLevel.path + selectedLevel.meta.background.path;
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
                sprite = Sprite.Create(coverTexture, new Rect(0, 0, coverTexture.width, coverTexture.height),
                    Vector2.zero, 100f, 0U, SpriteMeshType.FullRect);
                cover.sprite = sprite;
                cover.GetComponent<AspectRatioFitter>().aspectRatio =
                    coverTexture.width * 1.0f / coverTexture.height;
                cover.color = Color.white;
            }
        }
        yield return null;

        mask.DOColor(new Color(0f, 0f, 0f, 0f), 0.4f);
    }

    public override void OnScreenDestroyed()
    {
        cover.color = Color.black;
        mask.color = mask.color.WithAlpha(1f);
        cover.sprite = null;
        Destroy(sprite);
        sprite = null;
    }
    
}