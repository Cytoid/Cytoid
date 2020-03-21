using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NavigationBehavior : SingletonMonoBehavior<NavigationBehavior>
{
    private async void OnApplicationPause(bool pauseStatus)
    {
        // Resuming?
        if (!pauseStatus)
        {
            if (!Context.ScreenManager.History.Contains(MainMenuScreen.Id)) return;
            
            // Install levels
            SpinnerOverlay.Show();
            
            Context.LevelManager.OnLevelInstallProgress.AddListener(OnLevelInstallProgress);
            var loadedLevelJsonFiles = await Context.LevelManager.InstallAllFromDataPath();
            Context.LevelManager.OnLevelInstallProgress.RemoveListener(OnLevelInstallProgress);
            
            Context.LevelManager.OnLevelLoadProgress.AddListener(OnLevelLoadProgress);
            var loadedLevels = await Context.LevelManager.LoadFromMetadataFiles(LevelType.Community, loadedLevelJsonFiles, true);
            Context.LevelManager.OnLevelLoadProgress.RemoveListener(OnLevelLoadProgress);

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
    
    private void OnLevelInstallProgress(string fileName, int current, int total)
    {
        SpinnerOverlay.Instance.message.text = total > 1
            ? "INIT_UNPACKING_X_Y".Get(fileName, current, total)
            : "INIT_UNPACKING_X".Get(fileName);
    }
    
    private void OnLevelLoadProgress(string levelId, int current, int total)
    {
        SpinnerOverlay.Instance.message.text = "INIT_LOADING_X_Y".Get(levelId, current, total);
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