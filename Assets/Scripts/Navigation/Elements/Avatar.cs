using System;
using DG.Tweening;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Avatar : InteractableMonoBehavior
{
    public Image avatarBackground;
    public SpinnerElement avatarSpinner;
    public Image avatarImage;
    
    private RectTransform rectTransform;
    private string profileUrl;
    private DateTime asyncRequestToken;
    
    protected void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        avatarSpinner.bypassOnClickHitboxCheck = false;
        avatarSpinner.onPointerClick.AddListener(_ =>
        {
            Application.OpenURL(profileUrl);
        });
    }

    public async void SetModel(OnlineUser user)
    {
        avatarSpinner.IsSpinning = true;
        
        asyncRequestToken = DateTime.Now;

        var token = asyncRequestToken;

        var sprite = await Context.AssetMemory.LoadAsset<Sprite>(
            user.AvatarUrl?.WithSizeParam(64, 64) ?? user.Avatar.SmallUrl, 
            AssetTag.Avatar,
            useFileCache: true
        );

        if (token != asyncRequestToken)
        {
            return;
        }
        
        avatarSpinner.IsSpinning = false;
        if (sprite != null)
        {
            SetAvatarSprite(sprite);
        }
        profileUrl = Context.WebsiteUrl + "/profile/" + user.Uid;
    }
    
    public void SetAvatarSprite(Sprite sprite)
    {
        if (gameObject == null) return;
        avatarSpinner.IsSpinning = false;
        avatarImage.sprite = sprite;
        avatarImage.DOColor(Color.white, 0.4f).OnComplete(() =>
        {
            avatarBackground.color = Color.clear;
        });
    }
    
}