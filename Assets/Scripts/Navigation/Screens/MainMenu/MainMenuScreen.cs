using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Ink.Runtime;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : Screen
{
    public const string Id = "MainMenu";
    public static bool CheckedAnnouncement = false;
    public static bool PromptCachedCharacterDataCleared = false;
    public static bool LaunchedFirstLaunchDialogue = false;
    
    public RectTransform layout;
    public Text freePlayText;
    public InteractableMonoBehavior aboutButton;
    public BadgeNotification eventNotification;

    public Image upperLeftOverlayImage;
    public Image overlayImage;
    public RectTransform overlayHolder;
    
    public override string GetId() => Id;

    public override void OnScreenInitialized()
    {
        base.OnScreenInitialized();
        aboutButton.onPointerClick.SetListener(_ =>
        {
            WebViewOverlay.Show(Context.ApiUrl + "/credits");
        });
    }

    public override async void OnScreenBecameActive()
    {
        base.OnScreenBecameActive();

        //WebViewOverlay.Show();
        if (StartupLogger.Instance != null)
        {
            StartupLogger.Instance.Dispose();
        }

        upperLeftOverlayImage.SetAlpha(Context.CharacterManager.GetActiveCharacterAsset().mainMenuUpperLeftOverlayAlpha);
        overlayImage.SetAlpha(Context.CharacterManager.GetActiveCharacterAsset().mainMenuRightOverlayAlpha);

        var levelCount = Context.LevelManager.LoadedLocalLevels.Count(it =>
            (it.Value.Type == LevelType.User || it.Value.Type == LevelType.BuiltIn) && !BuiltInData.TrainingModeLevelIds.Contains(it.Value.Id));
        freePlayText.text = "MAIN_LEVELS_LOADED".Get(levelCount);
        freePlayText.transform.RebuildLayout();

        ProfileWidget.Instance.Enter();

        if (Context.CharacterManager.GetActiveCharacterAsset().mirrorLayout)
        {
            layout.anchorMin = new Vector2(0, 0.5f);
            layout.anchorMax = new Vector2(0, 0.5f);
            layout.pivot = new Vector2(0, 0.5f);
            layout.anchoredPosition = new Vector2(96, -90);
            overlayHolder.SetLeft(280);
            overlayHolder.SetRight(840);
            overlayHolder.SetLocalScaleX(-2); 
        }
        else
        {
            layout.anchorMin = new Vector2(1, 0.5f);
            layout.anchorMax = new Vector2(1, 0.5f);
            layout.pivot = new Vector2(1, 0.5f);
            layout.anchoredPosition = new Vector2(-96, -90);
            overlayHolder.SetLeft(840);
            overlayHolder.SetRight(280);
            overlayHolder.SetLocalScaleX(2);
        }
        
        // Check new events
        if (Context.IsOnline() && Context.OnlinePlayer.IsAuthenticated)
        {
            RestClient.GetArray<EventMeta>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/events",
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
                EnableDebug = true
            }).AbortOnScreenBecameInactive(this).Then(events =>
            {
                var hasUnseenEvent = false;
                foreach (var meta in events)
                {
                    if (Context.Player.Settings.SeenEvents.Contains(meta.uid))
                    {
                        continue;
                    }
                    hasUnseenEvent = true;
                }
                if (hasUnseenEvent)
                {
                    eventNotification.Show();
                }
            }).CatchRequestError(Debug.LogWarning);
        }

        if (Context.InitializationState.IsAfterFirstLaunch() && !LaunchedFirstLaunchDialogue)
        {
            LaunchedFirstLaunchDialogue = true;
            
            var text = Resources.Load<TextAsset>("Stories/Intro");
            LevelSelectionScreen.HighlightedLevelId = BuiltInData.TutorialLevelId;
            var story = new Story(text.text);
            Resources.UnloadAsset(text);
            story.variablesState["IsBeginner"] = levelCount < 10;
            await DialogueOverlay.Show(story);
            LevelSelectionScreen.HighlightedLevelId = null;

            Context.Player.ShouldOneShot("Android Migration Info");
        }
        else
        {
            if (DialogueOverlay.IsShown())
            {
                await UniTask.WaitUntil(() => !DialogueOverlay.IsShown());
            }
         
            if (Context.AndroidVersionCode >= 30 || Application.isEditor)
            {
                if (Context.Player.ShouldOneShot("Android Migration Info"))
                {
                    await AndroidMigrationInfoOverlay.Show();
                }
            }
            
            if (PromptCachedCharacterDataCleared)
            {
                PromptCachedCharacterDataCleared = false;
                Dialog.PromptAlert("DIALOG_CACHED_CHARACTER_DATA_CLEARED".Get());
            }

            // Check announcement
            if (!CheckedAnnouncement && Context.IsOnline())
            {
                RestClient.Get<Announcement>(new RequestHelper
                {
                    Uri = $"{Context.ApiUrl}/announcements",
                    Headers = Context.OnlinePlayer.GetRequestHeaders(),
                    EnableDebug = true
                }).Then(it =>
                {
                    CheckedAnnouncement = true;
                    if (it.message != null)
                    {
                        Dialog.PromptAlert(it.message);
                    }

                    print($"local: {Application.version}, latest: {it.currentVersion}, min supported: {it.minSupportedVersion}");
                    var localVersion = new Version(Application.version);
                    var currentVersion = new Version(it.currentVersion);
                    var minSupportedVersion = new Version(it.minSupportedVersion);
                    if (localVersion < minSupportedVersion)
                    {
                        Dialog.PromptUnclosable("DIALOG_UPDATE_REQUIRED".Get(),
                            () => Application.OpenURL(Context.StoreUrl));
                        return;
                    }

                    if (localVersion < currentVersion)
                    {
                        Dialog.Prompt("DIALOG_UPDATE_AVAILABLE_X_Y".Get(currentVersion, localVersion),
                            () => Application.OpenURL(Context.StoreUrl));
                    }
                }).CatchRequestError(Debug.LogError);
            }
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

    public override void OnScreenChangeFinished(Screen from, Screen to)
    {
        base.OnScreenChangeFinished(from, to);
        if (to == this)
        {
            foreach (var assetTag in (AssetTag[]) Enum.GetValues(typeof(AssetTag)))
            {
                if (assetTag == AssetTag.PlayerAvatar) continue;
                Context.AssetMemory.DisposeTaggedCacheAssets(assetTag);
            }
        }
    }
}