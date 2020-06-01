using DG.Tweening;
using UniRx.Async;
using UnityEngine.UI;

public class InitializationScreen : Screen
{
    public const string Id = "Initialization";
    
    public SpinnerElement spinnerElement;
    public Text statusText;
    public Text versionText;
    public InteractableMonoBehavior detectionArea;

    public override string GetId() => Id;

    public override async void OnScreenBecameActive()
    {
        versionText.text = "INIT_VERSION".Get(Context.VersionName.ToUpper());
        
        TranslucentCover.DarkMode();
        TranslucentCover.Show(0.5f, 2f);
        spinnerElement.IsSpinning = true;
        statusText.text = "";

        await UniTask.DelayFrame(10);
        
        Context.LevelManager.OnLevelInstallProgress.AddListener(OnLevelInstallProgress);
        await Context.LevelManager.InstallUserCommunityLevels();
        Context.LevelManager.OnLevelInstallProgress.RemoveListener(OnLevelInstallProgress);
        
        Context.LevelManager.OnLevelLoadProgress.AddListener(OnLevelLoadProgress);
        await Context.LevelManager.LoadLevelsOfType(LevelType.Community);
        await Context.LevelManager.LoadLevelsOfType(LevelType.Library);
        Context.LevelManager.OnLevelLoadProgress.RemoveListener(OnLevelLoadProgress);

        if (Context.Player.ShouldMigrate)
        {
            statusText.text = "INIT_MIGRATING_DATA".Get();
            statusText.transform.RebuildLayout();
            await Context.Player.Migrate();
        }

        spinnerElement.gameObject.SetActive(false);
        statusText.text = "INIT_TOUCH_TO_START".Get();
        statusText.DOFade(0, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        detectionArea.onPointerDown.AddListener(_ =>
        {
            TranslucentCover.Hide(0.2f);
            Context.AudioManager.Get("LevelStart").Play();
            Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
        });
        
        MainTranslucentImage.Instance.Initialize();
    }

    private void OnLevelInstallProgress(string fileName, int current, int total)
    {
        statusText.text = "INIT_UNPACKING_X_Y".Get(fileName, current, total);
        statusText.transform.RebuildLayout();
    }
    
    private void OnLevelLoadProgress(string levelId, int current, int total)
    {
        statusText.text = "INIT_LOADING_X_Y".Get(levelId, current, total);
        statusText.transform.RebuildLayout();
    }
    
}