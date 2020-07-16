using System;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.NiceVibrations;
using Proyecto26;
using Cysharp.Threading.Tasks;
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
            Context.Haptic(HapticTypes.SoftImpact, true);
            Context.AudioManager.Get("Navigate1").Play(ignoreDsp: true);
            switch (action)
            {
                case AvatarAction.OpenProfile:
                    if (Context.IsOffline())
                    {
                        Dialog.PromptAlert("DIALOG_OFFLINE_FEATURE_NOT_AVAILABLE".Get());
                    }
                    else
                    {
                        Context.ScreenManager.ChangeScreen(ProfileScreen.Id, ScreenTransition.In,
                            payload: new ProfileScreen.Payload {Id = owner.Id});
                    }
                    break;
                case AvatarAction.OpenExternalProfile:
                    Application.OpenURL($"{Context.WebsiteUrl}/profile/{owner.Uid}");
                    break;
                case AvatarAction.ViewLevels:
                    var screen = Context.ScreenManager.GetScreen<CommunityLevelSelectionScreen>();
                    var payload =
                        new CommunityLevelSelectionScreen.Payload
                        {
                            Query = new OnlineLevelQuery
                            {
                                sort = "creation_date",
                                order = "desc",
                                category = "all",
                                owner = owner.Uid
                            }
                        };
                    if (screen.State != ScreenState.Active)
                    {
                        Context.ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.In,
                            payload: payload);
                    }
                    else
                    {
                        // In-place modification
                        screen.LoadedPayload.Query = payload.Query;
                        screen.LoadedPayload.Levels = new List<OnlineLevel>();
                        screen.LoadedPayload.LastPage = 0;
                        screen.LoadedPayload.IsLastPageLoaded = false;
                        screen.LoadedPayload.CanLoadMore = true;
                        screen.LoadedPayload.ScrollPosition = 0;
                        
                        screen.LoadLevels(true);
                    }
                    break;
            }
        });
    }

    public async void SetModel(OnlineUser user)
    {
        Dispose();
        owner = user;
        avatarSpinner.IsSpinning = true;
        
        asyncRequestToken = DateTime.Now;

        var token = asyncRequestToken;

        var size = highQuality ? 256 : 64;
        var url = highQuality ? user.Avatar.LargeUrl : user.Avatar.SmallUrl;
            
        var sprite = await Context.AssetMemory.LoadAsset<Sprite>(
            url, 
            AssetTag.Avatar,
            options: new SpriteAssetOptions(new []{ size, size })
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
    OpenProfile, OpenExternalProfile, ViewLevels
}