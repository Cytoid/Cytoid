using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class InitializationScreen : Screen
{
    public const string Id = "Initialization";
    
    public SpinnerElement spinnerElement;
    public Text statusText;
    public Text versionText;
    public InteractableMonoBehavior detectionArea;
    
    public bool IsInitialized { get; private set; }

    public override string GetId() => Id;

    public override async void OnScreenBecameActive()
    {
        versionText.text = "INIT_VERSION".Get(Context.VersionName.ToUpper());
        
        spinnerElement.IsSpinning = true;
        statusText.text = "";

        await UniTask.DelayFrame(10);
        
        Context.LevelManager.OnLevelInstallProgress.AddListener(OnLevelInstallProgress);
        await Context.LevelManager.InstallUserCommunityLevels();
        Context.LevelManager.OnLevelInstallProgress.RemoveListener(OnLevelInstallProgress);
        
        Context.LevelManager.OnLevelLoadProgress.AddListener(OnLevelLoadProgress);
        await Context.LevelManager.LoadLevelsOfType(LevelType.BuiltIn);
        await Context.LevelManager.LoadLevelsOfType(LevelType.User);
        Context.LevelManager.OnLevelLoadProgress.RemoveListener(OnLevelLoadProgress);
        
        Context.LevelManager.OnLevelInstallProgress.AddListener(OnLevelInstallProgress);
        await Context.LevelManager.LoadOrInstallBuiltInLevels();
        Context.LevelManager.OnLevelInstallProgress.RemoveListener(OnLevelInstallProgress);

        if (Context.Player.ShouldMigrate)
        {
            statusText.text = "INIT_MIGRATING_DATA".Get();
            statusText.transform.RebuildLayout();
            await Context.Player.Migrate();
        }
        
        // Check region
        if (Context.Player.ShouldTrigger("Reset Server CDN To CN"))
        {
            Debug.Log("Reset server CDN to CN");
            Context.Player.Settings.CdnRegion = CdnRegion.MainlandChina;
        }
        
        statusText.text = "INIT_CONNECTING_TO_SERVER".Get();
        statusText.transform.RebuildLayout();
        await Context.Instance.DetectServerCdn();
        await Context.Instance.CheckServerCdn();
        await Context.BundleManager.DownloadAndSaveCatalog();

        spinnerElement.gameObject.SetActive(false);
        statusText.text = "INIT_TOUCH_TO_START".Get();
        statusText.DOFade(0, 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutFlash);
        detectionArea.onPointerDown.AddListener(_ =>
        {
            Context.AudioManager.Get("LevelStart").Play();
            Context.ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
        });
        
        IsInitialized = true;
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