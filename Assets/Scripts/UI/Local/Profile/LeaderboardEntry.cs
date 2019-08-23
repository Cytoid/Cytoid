using System;
using DG.Tweening;
using Proyecto26;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardEntry : MonoBehaviour
{
    public Text rank;
    public Image avatarBackground;
    public SpinnerElement avatarSpinner;
    public Image avatarImage;
    public new Text name;
    public Text rating;

    private string profileUrl;

    protected void Awake()
    {
        avatarSpinner.onPointerClick.AddListener(_ =>
        {
            print(profileUrl);
            Application.OpenURL(profileUrl);
        });
    }

    public void SetModel(Leaderboard.Entry entry)
    {
        rank.text = entry.rank + ".";
        avatarSpinner.IsSpinning = true;
        
        var cachedSprite = Context.SpriteCache.GetCachedSprite("avatar://" + entry.owner.uid);
        if (cachedSprite != null)
        {
            SetAvatarSprite(cachedSprite);
        }
        else
        {
            RestClient.Get(new RequestHelper
                {
                    Uri = entry.owner.avatarURL,
                    DownloadHandler = new DownloadHandlerTexture()
                }).Then(response =>
                {
                    var texture = ((DownloadHandlerTexture) response.Request.downloadHandler).texture;
                    var sprite = texture.CreateSprite();
                    Context.SpriteCache.PutSprite("avatar://" + entry.owner.uid, "avatar", sprite);
                    SetAvatarSprite(sprite);
                }).Finally(() => avatarSpinner.IsSpinning = false);
        }

        name.text = entry.owner.uid;
        rating.text = "Rating " + entry.rating.ToString("N2");
        profileUrl = Context.WebsiteUrl + "/profile/" + entry.owner.uid;
    }
    
    public void SetAvatarSprite(Sprite sprite)
    {
        avatarImage.sprite = sprite;
        avatarImage.DOColor(Color.white, 0.4f).OnComplete(() =>
        {
            avatarBackground.color = Color.clear;
        });
    }

}