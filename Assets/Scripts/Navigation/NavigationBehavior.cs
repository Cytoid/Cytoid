using System.Linq;
using UnityEngine;

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

}