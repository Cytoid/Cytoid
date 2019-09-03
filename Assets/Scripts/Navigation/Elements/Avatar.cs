using DG.Tweening;
using Proyecto26;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Avatar : InteractableMonoBehavior
{
    
    public Image avatarBackground;
    public SpinnerElement avatarSpinner;
    public Image avatarImage;
    
    private string profileUrl;
    
    protected void Awake()
    {
        avatarSpinner.onPointerClick.AddListener(_ =>
        {
            Application.OpenURL(profileUrl);
        });
    }

    public void SetModel(OnlineUser user)
    {
        avatarSpinner.IsSpinning = true;
        var cachedSprite = Context.SpriteCache.GetCachedSprite("avatar://" + user.uid);
        if (cachedSprite != null)
        {
            SetAvatarSprite(cachedSprite);
        }
        else
        {
            RestClient.Get(new RequestHelper
            {
                Uri = user.avatarURL.WithSizeParam(128, 128),
                DownloadHandler = new DownloadHandlerTexture()
            }).Then(response =>
            {
                var texture = ((DownloadHandlerTexture) response.Request.downloadHandler).texture;
                var sprite = texture.CreateSprite();
                Context.SpriteCache.PutSprite("avatar://" + user.uid, "avatar", sprite);
                SetAvatarSprite(sprite);
            }).Finally(() => avatarSpinner.IsSpinning = false);
        }
        profileUrl = Context.WebsiteUrl + "/profile/" + user.uid;
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