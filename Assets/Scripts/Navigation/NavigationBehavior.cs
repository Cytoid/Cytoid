using System;
using System.Collections.Generic;
using System.Linq;

using Proyecto26;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationBehavior : SingletonMonoBehavior<NavigationBehavior>
{
    private void Start()
    {
        DeepLinkListener.Instance.OnDeepLinkReceived.AddListener(OnDeepLinkReceived);
    }
    
    private DateTimeOffset deepLinkToken = DateTimeOffset.MinValue;
    private static readonly HashSet<string> DeepLinkDisabledScreenIds = new HashSet<string>
    {
        InitializationScreen.Id, ResultScreen.Id, TierBreakScreen.Id, TierResultScreen.Id
    };

    private async void OnDeepLinkReceived(string url)
    {
        Debug.Log($"Deep link received: {url}");
        if (!url.StartsWith("cytoid://")) return;

        var token = deepLinkToken = DateTimeOffset.Now;
       
        await UniTask.WaitUntil(() => Context.ScreenManager != null &&
                                      Context.ScreenManager.History.Any(it => it.ScreenId == MainMenuScreen.Id));
        if (token != deepLinkToken) return;
        
        if (DeepLinkDisabledScreenIds.Contains(Context.ScreenManager.ActiveScreenId))
        {
            Debug.Log($"Ignoring, current screen: {Context.ScreenManager.ActiveScreenId}");
            return;
        }
        
        url = url.Substring("cytoid://".Length);

        if (url.StartsWith("levels/"))
        {
            var id = url.Substring("levels/".Length);
            
            SpinnerOverlay.Show();

            RestClient.Get<OnlineLevel>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/levels/{id}"
            }).Then(async level =>
            {
                try
                {
                    if (token != deepLinkToken) return;
                    
                    foreach (var tag in (AssetTag[]) Enum.GetValues(typeof(AssetTag)))
                    {
                        if (tag == AssetTag.PlayerAvatar) continue;
                        Context.AssetMemory.DisposeTaggedCacheAssets(tag);
                    }
                    
                    while (Context.ScreenManager.PeekHistory().Let(it => it != null && it.ScreenId != MainMenuScreen.Id))
                    {
                        Context.ScreenManager.PopAndPeekHistory();
                    }
                    
                    await UniTask.WaitUntil(() => !Context.ScreenManager.IsChangingScreen);

                    // Resolve level
                    if (Context.LevelManager.LoadedLocalLevels.ContainsKey(level.Uid))
                    {
                        var localLevel = Context.LevelManager.LoadedLocalLevels[level.Uid];
                        Debug.Log($"Online level {level.Uid} resolved locally");

                        Context.ScreenManager.History.Push(new Intent(LevelSelectionScreen.Id, null));
                        Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In,
                            payload: new GamePreparationScreen.Payload {Level = localLevel});
                    }
                    else
                    {
                        Context.ScreenManager.History.Push(new Intent(CommunityHomeScreen.Id, null));
                        Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In,
                            payload: new GamePreparationScreen.Payload {Level = level.ToLevel(LevelType.User)});
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Dialog.PromptAlert("DIALOG_COULD_NOT_CONNECT_TO_SERVER".Get());
                }
            }).CatchRequestError(error =>
            {
                if (token != deepLinkToken) return;
                if (error.IsHttpError) {
                    if (error.StatusCode != 404)
                    {
                        throw error;
                    }
                }
                Context.Haptic(HapticTypes.Failure, true);
                Dialog.Instantiate().Also(it =>
                {
                    it.Message = "DIALOG_COULD_NOT_OPEN_LEVEL_X".Get(id);
                    it.UsePositiveButton = true;
                    it.UseNegativeButton = false;
                }).Open();
            }).Finally(() =>
            {
                if (token != deepLinkToken) return;
                SpinnerOverlay.Hide();
            });
        }
        else
        {
            Debug.LogError("Unsupported deep link");
        }
    }

    private async void OnApplicationPause(bool pauseStatus)
    {
        // Resuming?
        if (!pauseStatus)
        {
            if (Context.ScreenManager.History.All(it => it.ScreenId != MainMenuScreen.Id)) return;
            
            // Install levels
            SpinnerOverlay.Show();
            
            Context.LevelManager.OnLevelInstallProgress.AddListener(SpinnerOverlay.OnLevelInstallProgress);
            var loadedLevelJsonFiles = await Context.LevelManager.InstallUserCommunityLevels();
            Context.LevelManager.OnLevelInstallProgress.RemoveListener(SpinnerOverlay.OnLevelInstallProgress);
            
            Context.LevelManager.OnLevelLoadProgress.AddListener(SpinnerOverlay.OnLevelLoadProgress);
            var loadedLevels = await Context.LevelManager.LoadFromMetadataFiles(LevelType.User, loadedLevelJsonFiles, true);
            Context.LevelManager.OnLevelLoadProgress.RemoveListener(SpinnerOverlay.OnLevelLoadProgress);

            SpinnerOverlay.Hide();
            
            if (loadedLevels.Count > 0)
            {
                var lastLoadedLevel = loadedLevels.Last();

                if (Context.ScreenManager.ActiveScreenId != GamePreparationScreen.Id)
                {
                    // Switch to that level
                    while (Context.ScreenManager.PeekHistory().Let(it => it != null && it.ScreenId != MainMenuScreen.Id))
                    {
                        Context.ScreenManager.PopAndPeekHistory();
                    }

                    Context.ScreenManager.History.Push(new Intent(LevelSelectionScreen.Id, null));

                    Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In,
                        payload: new GamePreparationScreen.Payload {Level = lastLoadedLevel});
                }
                else
                {
                    Context.SelectedLevel = lastLoadedLevel; // Trigger the event and screen will relaod
                }
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Context.OnOfflineModeToggled.AddListener(OnOfflineModeToggled);
        if (Context.IsOffline()) OnOfflineModeToggled(true);
    }

    private async void OnOfflineModeToggled(bool offline)
    {
        SpinnerOverlay.Show();
        async void Fix(GameObject itGameObject, bool active)
        {
            // Yes, welcome to Unity.
            // Don't change, unless you are absolutely certain what I am (and you are) doing.
            itGameObject.SetActive(active);
            LayoutFixer.Fix(itGameObject.transform);
            await UniTask.DelayFrame(5);
            itGameObject.SetActive(!itGameObject.activeSelf);
            await UniTask.DelayFrame(0);
            itGameObject.SetActive(!itGameObject.activeSelf);
            await UniTask.DelayFrame(0);
            LayoutFixer.Fix(itGameObject.transform);
            if (active) itGameObject.GetComponent<TransitionElement>()?.Apply(x => x.UseCurrentStateAsDefault());
        }
        foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObject.GetComponentsInChildren<OfflineElement>(true).ForEach(it =>
            {
                if (offline)
                {
                    it.targets.ForEach(x => x.SetActive(false));
                    it.gameObject.SetActive(true);
                }
                else
                {
                    it.gameObject.SetActive(false);
                    it.targets.ForEach(x => x.SetActive(true));
                }
                it.rebuildTransform.RebuildLayout();
                var o = it.rebuildTransform.gameObject;
                Fix(o, o.activeSelf);
            });
        }

        await UniTask.DelayFrame(10);

        foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObject.GetComponentsInChildren<HiddenIfOfflineElement>(true).ForEach(it =>
            {
                Fix(it.gameObject, !offline);
                var rebuild = it.rebuildTransform;
                if (rebuild != null)
                {
                    rebuild.RebuildLayout();
                    var o = rebuild.gameObject;
                    Fix(o, o.activeSelf);
                }
            });
        }
        
        await UniTask.DelayFrame(10);

        foreach (var screen in Context.ScreenManager.createdScreens)
        {
            LayoutFixer.Fix(screen.transform);
        }
        
        await UniTask.DelayFrame(5);

        SpinnerOverlay.Hide();
    }
}
