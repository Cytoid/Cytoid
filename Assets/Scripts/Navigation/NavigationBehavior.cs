using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NavigationBehavior : SingletonMonoBehavior<NavigationBehavior>
{
    private void Start()
    {
        DeepLinkListener.Instance.OnDeepLinkReceived.AddListener(OnDeepLinkReceived);
    }
    
    private DateTimeOffset deepLinkToken = DateTimeOffset.MinValue;

    private async void OnDeepLinkReceived(string url)
    {
        var token = deepLinkToken = DateTimeOffset.Now;
       
        await UniTask.WaitUntil(() => Context.ScreenManager != null &&
                                      Context.ScreenManager.History.Contains(MainMenuScreen.Id));
        if (token != deepLinkToken) return;

        if (!url.StartsWith("cytoid://")) throw new InvalidOperationException();
        url = url.Substring("cytoid://".Length);

        if (url.StartsWith("levels/"))
        {
            var id = url.Substring("levels/".Length);
            
            SpinnerOverlay.Show();

            RestClient.Get<OnlineLevel>(new RequestHelper
            {
                Uri = $"{Context.ServicesUrl}/levels/{id}"
            }).Then(async level =>
            {
                try
                {
                    if (token != deepLinkToken) return;

                    while (Context.ScreenManager.PeekHistory().Let(it => it != null && it != MainMenuScreen.Id))
                    {
                        Context.ScreenManager.PopAndPeekHistory();
                    }

                    // Resolve level
                    if (Context.LevelManager.LoadedLocalLevels.ContainsKey(level.Uid))
                    {
                        var localLevel = Context.LevelManager.LoadedLocalLevels[level.Uid];
                        Debug.Log($"Online level {level.Uid} resolved locally");

                        Context.ScreenManager.History.Push(LevelSelectionScreen.Id);
                        Context.ScreenManager.History.Push(GamePreparationScreen.Id);
                        Context.SelectedLevel = localLevel;
                    }
                    else
                    {
                        Context.ScreenManager.History.Push(CommunityHomeScreen.Id);
                        Context.ScreenManager.History.Push(GamePreparationScreen.Id);
                        Context.SelectedLevel = level.ToLevel(LevelType.Community);
                    }

                    await UniTask.WaitUntil(() => !Context.ScreenManager.IsChangingScreen);

                    Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
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
            if (!Context.ScreenManager.History.Contains(MainMenuScreen.Id)) return;
            
            // Install levels
            SpinnerOverlay.Show();
            
            Context.LevelManager.OnLevelInstallProgress.AddListener(SpinnerOverlay.OnLevelInstallProgress);
            var loadedLevelJsonFiles = await Context.LevelManager.InstallUserCommunityLevels();
            Context.LevelManager.OnLevelInstallProgress.RemoveListener(SpinnerOverlay.OnLevelInstallProgress);
            
            Context.LevelManager.OnLevelLoadProgress.AddListener(SpinnerOverlay.OnLevelLoadProgress);
            var loadedLevels = await Context.LevelManager.LoadFromMetadataFiles(LevelType.Community, loadedLevelJsonFiles, true);
            Context.LevelManager.OnLevelLoadProgress.RemoveListener(SpinnerOverlay.OnLevelLoadProgress);

            SpinnerOverlay.Hide();
            
            if (loadedLevels.Count > 0)
            {
                var lastLoadedLevel = loadedLevels.Last();
                
                // Switch to that level
                while (Context.ScreenManager.PeekHistory().Let(it => it != null && it != MainMenuScreen.Id))
                {
                    Context.ScreenManager.PopAndPeekHistory();
                }

                Context.ScreenManager.History.Push(LevelSelectionScreen.Id);
                Context.ScreenManager.History.Push(GamePreparationScreen.Id);

                Context.SelectedLevel = lastLoadedLevel;
                Context.ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.In);
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
        var tasks = new List<UniTask>();
        async void Fix(GameObject itGameObject, bool active)
        {
            // Yes, welcome to Unity.
            // Don't change, unless you are absolutely certain what I am (and you are) doing.
            itGameObject.SetActive(active);
            LayoutFixer.Fix(itGameObject.transform);
            var task = UniTask.DelayFrame(5);
            tasks.Add(task);
            await task;
            itGameObject.SetActive(!itGameObject.activeSelf);
            task = UniTask.DelayFrame(0);
            tasks.Add(task);
            await task;
            itGameObject.SetActive(!itGameObject.activeSelf);
            task = UniTask.DelayFrame(0);
            tasks.Add(task);
            await task;
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

        await UniTask.WhenAll(tasks);
        
        tasks.Clear();
        
        foreach (var gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            gameObject.GetComponentsInChildren<HiddenIfOfflineElement>(true).ForEach(it =>
            {
                Fix(it.gameObject, !offline);
                it.rebuildTransform.RebuildLayout();
                var o = it.rebuildTransform.gameObject;
                Fix(o, o.activeSelf);
            });
        }
        
        await UniTask.WhenAll(tasks);

        foreach (var screen in Context.ScreenManager.createdScreens)
        {
            LayoutFixer.Fix(screen.transform);
        }
        
        await UniTask.DelayFrame(5);

        SpinnerOverlay.Hide();
    }
}