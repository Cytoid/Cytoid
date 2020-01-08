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
            var avatarUri = user.avatarURL.WithSizeParam(128, 128);
            Debug.Log("Downloading avatar from " + avatarUri);
            RestClient.Get(new RequestHelper
            {
                Uri = avatarUri,
                DownloadHandler = new DownloadHandlerTexture(),
                RedirectLimit = 10
            }).Then(response =>
            {
                var texture = ((DownloadHandlerTexture) response.Request.downloadHandler).texture;
                var sprite = texture.CreateSprite();
                Context.SpriteCache.PutSprite("avatar://" + user.uid, "Avatar", sprite);
                SetAvatarSprite(sprite);
            }).Finally(() => avatarSpinner.IsSpinning = false);
        }
        profileUrl = Context.WebsiteUrl + "/profile/" + user.uid;
    }
    
    public void SetAvatarSprite(Sprite sprite)
    {
        avatarSpinner.IsSpinning = false;
        avatarImage.sprite = sprite;
        avatarImage.DOColor(Color.white, 0.4f).OnComplete(() =>
        {
            avatarBackground.color = Color.clear;
        });
    }
    
}