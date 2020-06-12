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
    public bool highQuality;

    public AvatarAction action = AvatarAction.OpenProfile;

    private OnlineUser owner;
    private DateTime asyncRequestToken;
    
    protected void Awake()
    {
        avatarSpinner.bypassOnClickHitboxCheck = false;
        avatarSpinner.onPointerClick.AddListener(_ =>
        {
            switch (action)
            {
                case AvatarAction.OpenProfile:
                    Application.OpenURL($"{Context.WebsiteUrl}/profile/{owner.Uid}");
                    break;
                case AvatarAction.ViewLevels:
                    CommunityLevelSelectionScreen.LoadedContent = new CommunityLevelSelectionScreen.Content
                    {
                        Query = new OnlineLevelQuery
                        {
                            sort = "creation_date",
                            order = "desc",
                            category = "all",
                            owner = owner.Uid
                        },
                        OnlineLevels = null // Signal reload
                    };
                    if (Context.ScreenManager.ActiveScreenId != CommunityLevelSelectionScreen.Id)
                    {
                        while (Context.ScreenManager.PeekHistory().Let(it => it != null && it != MainMenuScreen.Id))
                        {
                            Context.ScreenManager.PopAndPeekHistory();
                        }
                        Context.ScreenManager.History.Push(CommunityHomeScreen.Id);
                        Context.ScreenManager.History.Push(CommunityLevelSelectionScreen.Id);
                        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In);
                    }
                    else
                    {
                        ((CommunityLevelSelectionScreen) Context.ScreenManager.ActiveScreen).LoadContent();
                    }
                    break;
            }
        });
    }

    public async void SetModel(OnlineUser user)
    {
        owner = user;
        avatarSpinner.IsSpinning = true;
        
        asyncRequestToken = DateTime.Now;

        var token = asyncRequestToken;
        
        string url;
        if (highQuality)
        {
            url = user.AvatarUrl?.WithSizeParam(256, 256) ?? user.Avatar.LargeUrl;
        }
        else
        {
            url = user.AvatarUrl?.WithSizeParam(64, 64) ?? user.Avatar.SmallUrl;
        }
            
        var sprite = await Context.AssetMemory.LoadAsset<Sprite>(
            url, 
            AssetTag.Avatar,
            allowFileCache: true,
            options: new SpriteAssetOptions(new []{ 64, 64 })
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
    }

    public void Dispose()
    {
        owner = null;
        asyncRequestToken = DateTime.Now;
        avatarSpinner.IsSpinning = false;
        avatarImage.sprite = null;
        avatarBackground.enabled = true;
        avatarImage.color = Color.white.WithAlpha(0);
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public void SetAvatarSprite(Sprite sprite)
    {
        if (this == null || gameObject == null) return;
        avatarSpinner.IsSpinning = false;
        avatarImage.sprite = sprite;
        avatarBackground.enabled = false;
        avatarImage.DOColor(Color.white, 0.4f);
    }
    
}

public enum AvatarAction
{
    OpenProfile, ViewLevels
}