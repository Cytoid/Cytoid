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
        avatarSpinner.bypassOnClickHitboxCheck = false;
        avatarSpinner.onPointerClick.AddListener(_ =>
        {
            Application.OpenURL(profileUrl);
        });
    }

    public async void SetModel(OnlineUser user)
    {
        avatarSpinner.IsSpinning = true;
        var sprite = await Context.SpriteCache.CacheSpriteInMemory(
            user.avatarURL.WithSizeParam(128, 128), 
            "Avatar",
            useFileCache: true
        );
        avatarSpinner.IsSpinning = false;
        if (sprite != null)
        {
            SetAvatarSprite(sprite);
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