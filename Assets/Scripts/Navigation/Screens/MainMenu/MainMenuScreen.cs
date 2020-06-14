using System;
using System.Collections.Generic;
using System.Linq;
using LeTai.Asset.TranslucentImage;
using Newtonsoft.Json;
using Polyglot;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    public const string Id = "MainMenu";
    public static bool DisplayedAnnouncement = false;
    
    public RectTransform layout;
    public Text freePlayText;
    public InteractableMonoBehavior aboutButton;

    public Image upperLeftOverlayImage;
    public Image rightOverlayImage;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        aboutButton.onPointerClick.AddListener(it => Dialog.PromptAlert($"TEMP_MESSAGE_2.0_BETA_CREDITS".Get(Context.VersionName)));
    }

    public override void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        foreach (var tag in (AssetTag[]) Enum.GetValues(typeof(AssetTag)))
        {
            if (tag == AssetTag.PlayerAvatar) continue;
            Context.AssetMemory.DisposeTaggedCacheAssets(tag);
        }
        
        //WebViewOverlay.Show();
        StartupLogger.Instance.Dispose();

        upperLeftOverlayImage.SetAlpha(Context.CharacterManager.GetActiveCharacterAsset().mainMenuUpperLeftOverlayAlpha);
        rightOverlayImage.SetAlpha(Context.CharacterManager.GetActiveCharacterAsset().mainMenuRightOverlayAlpha);
        
        freePlayText.text = "MAIN_LEVELS_LOADED".Get(Context.LevelManager.LoadedLocalLevels.Count(it => 
            it.Value.Type == LevelType.Community || it.Value.Type == LevelType.Library));
        freePlayText.transform.RebuildLayout();
        ProfileWidget.Instance.Enter();

        if (Context.CharacterManager.GetActiveCharacterAsset().mirrorLayout)
        {
            layout.anchorMin = new Vector2(0, 0.5f);
            layout.anchorMax = new Vector2(0, 0.5f);
            layout.pivot = new Vector2(0, 0.5f);
            layout.anchoredPosition = new Vector2(96, -90);
        }
        else
        {
            layout.anchorMin = new Vector2(1, 0.5f);
            layout.anchorMax = new Vector2(1, 0.5f);
            layout.pivot = new Vector2(1, 0.5f);
            layout.anchoredPosition = new Vector2(-96, -90);
        }

        if (Context.Player.ShouldOneShot("2.0b"))
        {
            Dialog.PromptAlert("TEMP_MESSAGE_2.0_BETA".Get());
        }
        
        // Check announcement
        if (!DisplayedAnnouncement && Context.IsOnline())
        {
            RestClient.Get<Announcement>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/announcements",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            }).Then(it =>
            {
                DisplayedAnnouncement = true;
                if (it.message != null)
                {
                    Dialog.PromptAlert(it.message);
                }

                var localVersion = new Version(Context.VersionString);
                var currentVersion = new Version(it.currentVersion);
                var minSupportedVersion = new Version(it.minSupportedVersion);
                if (localVersion < minSupportedVersion)
                {
                    Dialog.PromptUnclosable("DIALOG_UPDATE_REQUIRED".Get(), () => Application.OpenURL(Context.StoreUrl));
                    return;
                }
                if (localVersion < currentVersion)
                {
                    Dialog.Prompt("DIALOG_UPDATE_AVAILABLE_X_Y".Get(currentVersion, localVersion), () => Application.OpenURL(Context.StoreUrl));
                }
            }).CatchRequestError(Debug.LogError);
        }
    }

    public override void OnScreenUpdate()
    {
        base.OnScreenUpdate();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
    
}