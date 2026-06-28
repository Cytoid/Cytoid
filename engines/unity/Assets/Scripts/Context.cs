using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Newtonsoft.Json;
using Polyglot;
using Tayx.Graphy;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class Context : SingletonMonoBehavior<Context>
{
    public const string VersionIdentifier = "2.1.4";
    public const string VersionName = "2.1.4 BUILD.1";
    public const int VersionCode = 123;

    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    public const int LevelThumbnailWidth = 576;
    public const int LevelThumbnailHeight = 360;

    public static int AndroidVersionCode = -1;

    public static readonly PreSceneChangedEvent PreSceneChanged = new PreSceneChangedEvent();
    public static readonly PostSceneChangedEvent PostSceneChanged = new PostSceneChangedEvent();
    public static readonly UnityEvent OnApplicationInitialized = new UnityEvent();
    public static bool IsInitialized { get; private set; }

    public static readonly LevelEvent
        OnSelectedLevelChanged = new LevelEvent(); // TODO: This feels definitely unnecessary. Integrate with screen?

    public static readonly UnityEvent OnLanguageChanged = new UnityEvent();

    public static string UserDataPath;
    public static string iOSTemporaryInboxPath;
    public static int InitialWidth;
    public static int InitialHeight;
    public static int DefaultDspBufferSize { get; private set; }

    public static AudioManager AudioManager;
    public static ScreenManager ScreenManager;

    public static readonly FontManager FontManager = new FontManager();
    public static readonly LevelManager LevelManager = new LevelManager();
    public static readonly AssetMemory AssetMemory = new AssetMemory();

    public static Level SelectedLevel
    {
        get => selectedLevel;
        set
        {
            selectedLevel = value;
            OnSelectedLevelChanged.Invoke(value);
        }
    }

    public static Difficulty SelectedDifficulty = Difficulty.Easy;
    public static Difficulty PreferredDifficulty = Difficulty.Easy;
    public static HashSet<Mod> SelectedMods = new HashSet<Mod>();
    public static GameMode SelectedGameMode;
    public static IGameContentProvider GameContentProvider;

    public static InitializationState InitializationState;
    public static GameState GameState;
    public static TierPlayLaunch PendingTierPlay;
    public static TierPlaySession ActiveTierPlaySession;

    public static readonly Player Player = new Player();

    public static GameErrorState GameErrorState;

    private static Level selectedLevel;
    private static GraphyManager graphyManager;

    protected override void Awake()
    {
        base.Awake();
        Vibration.Init();

        if (GameObject.FindGameObjectsWithTag("Context").Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        InitializeApplication();
    }

    private static void OnLowMemory()
    {
        Debug.LogWarning("Low memory warning received.");
    }

    private async void InitializeApplication()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            // Get Android version
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                AndroidVersionCode = version.GetStatic<int>("SDK_INT");
                print("Android version code: " + AndroidVersionCode);
            }
        }
        InitializationState = new InitializationState();

        UserDataPath = Application.persistentDataPath;

        if (Application.platform == RuntimePlatform.Android)
        {
            var dir = GetAndroidStoragePath();
            if (dir == null)
            {
                Application.Quit();
                return;
            }

            UserDataPath = dir + "/Cytoid";
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // iOS 13 fix
            iOSTemporaryInboxPath = UserDataPath
                .Replace("Documents/", "")
                .Replace("Documents", "") + "/tmp/me.tigerhix.cytoid-Inbox/";
        }
        print("User data path: " + UserDataPath);

#if UNITY_EDITOR
        Application.runInBackground = true;
#endif

        if (SceneManager.GetActiveScene().name == "Navigation" && StartupLogger.Instance != null)
        {
            StartupLogger.Instance.Initialize();
        }
        Debug.Log($"Package name: {Application.identifier}");

        Application.lowMemory += OnLowMemory;
        Application.targetFrameRate = 120;
        GameInputCompat.SetGyroscopeEnabled(true);
        DOTween.defaultEaseType = Ease.OutCubic;
        UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new UnityColorConverter()
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        FontManager.LoadFonts();

        if (Application.platform == RuntimePlatform.Android)
        {
            // Try to write to ensure we have write permissions
            try
            {
                // Create an empty folder if it doesn't already exist
                Directory.CreateDirectory(UserDataPath);
                File.Create(UserDataPath + "/.nomedia").Dispose();
                // Create and delete test file
                var file = UserDataPath + "/" + Path.GetRandomFileName();
                File.Create(file);
                File.Delete(file);
                Debug.Log("Write permission granted");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Could not start game: write permission check failed.");
                return;
            }
        }

        Player.Initialize();

        // Initialize audio
        var audioConfig = AudioSettings.GetConfiguration();
        DefaultDspBufferSize = audioConfig.dspBufferSize;

        if (Application.isEditor)
        {
            audioConfig.dspBufferSize = 2048;
        }
        else if (Application.platform == RuntimePlatform.Android && Player.Settings.AndroidDspBufferSize > 0)
        {
            audioConfig.dspBufferSize = Player.Settings.AndroidDspBufferSize;
        }
        AudioSettings.Reset(audioConfig);

        if (IsCoreHostBootstrapScene())
        {
            Debug.Log("[Context] Core host bootstrap initialized without scene-bound AudioManager.");
        }
        else
        {
            await UniTask.WaitUntil(() => AudioManager != null);
            AudioManager.Initialize();
        }

        InitialWidth = UnityEngine.Screen.width;
        InitialHeight = UnityEngine.Screen.height;
        UpdateGraphicsQuality();

        SelectedMods = new HashSet<Mod>(Player.Settings.EnabledMods);

        PreSceneChanged.AddListener(OnPreSceneChanged);
        PostSceneChanged.AddListener(OnPostSceneChanged);

        OnLanguageChanged.AddListener(FontManager.UpdateSceneTexts);
        Localization.Instance.SelectLanguage((Language)Player.Settings.Language);
        OnLanguageChanged.Invoke();

        switch (SceneManager.GetActiveScene().name)
        {
            case "Navigation":
                if (GameEmbedMode.IsBridgeEmbedded)
                {
                    InitializeFlutterHost();
                }
                else
                {
                    InitializeDebugNavigation();
                }

                break;
            case "CoreHostBootstrap":
                InitializeFlutterHost();
                break;
            case "Game":
                break;
        }

        await UniTask.DelayFrame(0);

        graphyManager = GraphyManager.Instance;
        UpdateProfilerDisplay();

        IsInitialized = true;
        OnApplicationInitialized.Invoke();
    }

    private static void InitializeDebugNavigation()
    {
        InitializationState.IsInitialized = true;
        SelectedGameMode = GameMode.Unspecified;
        SelectedLevel = null;
        SelectedDifficulty = Difficulty.Easy;
        SelectedMods.Clear();
        GameContentProvider?.Dispose();
        GameContentProvider = null;
    }

    private static void InitializeFlutterHost()
    {
        InitializationState.IsInitialized = true;
        GameContentProvider?.Dispose();
        GameContentProvider = null;
    }

    private static bool IsCoreHostBootstrapScene()
    {
        return SceneManager.GetActiveScene().name == "CoreHostBootstrap";
    }

    public static void OnPreSceneChanged(string prev, string next)
    {
        switch (prev)
        {
            case "Navigation" when next == "Game":
                GameInputCompat.SetGyroscopeEnabled(false);
                break;
        }
    }

    public static async void OnPostSceneChanged(string prev, string next)
    {
        switch (prev)
        {
            case "Game" when next == "Navigation":
                {
                    GameInputCompat.SetGyroscopeEnabled(true);
                    AudioManager?.Initialize();
                    UpdateGraphicsQuality();

                    SaveLastCompletedGameResult();
                    InitializeDebugNavigation();
                    await UniTask.DelayFrame(0);
                    DebugNavigationController controller = null;
                    await UniTask.WaitUntil(() =>
                    {
                        controller = UnityEngine.Object.FindObjectOfType<DebugNavigationController>();
                        return controller != null;
                    });
                    controller.RefreshResultText();
                    break;
                }
        }

        if (next == "Game")
        {
            UpdateProfilerDisplay();
        }

        FontManager.UpdateSceneTexts();
    }

    public static void EmitExternalGameResult()
    {
        SaveLastCompletedGameResult();
    }

    private static void SaveLastCompletedGameResult()
    {
        if (GameState == null) return;

        GameResultBridge.Emit(GameState, ActiveTierPlaySession);

        var usedAuto = GameResultBridge.HasAutoMod(GameState);
        if (GameState.IsCompleted && GameState.Mode == GameMode.Standard && !usedAuto)
        {
            var lastPlayResult = LastPlayResult.FromGameState(GameState);
            LastPlayResult.Save(lastPlayResult);
            Debug.Log($"[DebugNavigation] Last play result cached: {lastPlayResult.ToJson()}");
        }

        GameState = null;
        PendingTierPlay = null;
        ActiveTierPlaySession = null;
    }

    public static void Haptic(HapticTypes type, bool menu)
    {
        if (Application.isEditor) return;
        if (!(menu ? Player.Settings.MenuTapticFeedback : Player.Settings.HitTapticFeedback)) return;

        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
                HapticIOS(type);
                break;
            case RuntimePlatform.Android:
                HapticAndroid(type);
                break;
        }
    }

    static void HapticIOS(HapticTypes type)
    {
        switch (type)
        {
            case HapticTypes.Selection:
                Vibration.VibrateIOS_SelectionChanged();
                break;
            case HapticTypes.Success:
                Vibration.VibrateIOS(NotificationFeedbackStyle.Success);
                break;
            case HapticTypes.Warning:
                Vibration.VibrateIOS(NotificationFeedbackStyle.Warning);
                break;
            case HapticTypes.Failure:
                Vibration.VibrateIOS(NotificationFeedbackStyle.Error);
                break;
            case HapticTypes.LightImpact:
                Vibration.VibrateIOS(ImpactFeedbackStyle.Light);
                break;
            case HapticTypes.MediumImpact:
                Vibration.VibrateIOS(ImpactFeedbackStyle.Medium);
                break;
            case HapticTypes.HeavyImpact:
                Vibration.VibrateIOS(ImpactFeedbackStyle.Heavy);
                break;
            case HapticTypes.RigidImpact:
                Vibration.VibrateIOS(ImpactFeedbackStyle.Rigid);
                break;
            case HapticTypes.SoftImpact:
                Vibration.VibrateIOS(ImpactFeedbackStyle.Soft);
                break;
        }
    }

    static void HapticAndroid(HapticTypes type)
    {
        if (!Vibration.IsAvailable()) return;

        switch (type)
        {
            case HapticTypes.Selection:
            case HapticTypes.LightImpact:
            case HapticTypes.SoftImpact:
                Vibration.VibratePop();
                break;
            case HapticTypes.MediumImpact:
            case HapticTypes.RigidImpact:
            case HapticTypes.Success:
                Vibration.VibratePeek();
                break;
            case HapticTypes.HeavyImpact:
            case HapticTypes.Warning:
            case HapticTypes.Failure:
                Vibration.VibratePeek();
                break;
        }
    }

    public static void SetAutoRotation(bool autoRotation)
    {
        if (autoRotation)
        {
            UnityEngine.Screen.autorotateToLandscapeLeft = true;
            UnityEngine.Screen.autorotateToLandscapeRight = true;
        }
        else
        {
            if (UnityEngine.Screen.orientation != ScreenOrientation.LandscapeLeft)
                UnityEngine.Screen.autorotateToLandscapeLeft = false;
            if (UnityEngine.Screen.orientation != ScreenOrientation.LandscapeRight)
                UnityEngine.Screen.autorotateToLandscapeRight = false;
        }
    }

    public string GetAndroidStoragePath()
    {
#if UNITY_ANDROID
        if (
            AndroidVersionCode <= 29
            && Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)
            && Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)
        )
        {
            return GetAndroidLegacyStoragePath();
        }

        return Application.persistentDataPath;
#else
        return "";
#endif
    }

    public string GetAndroidLegacyStoragePath()
    {
        try
        {
            using var javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activityClass = javaClass.GetStatic<AndroidJavaObject>("currentActivity");
            return activityClass.Call<AndroidJavaObject>("getAndroidStorageFile")
                .Call<string>("getAbsolutePath");
        }
        catch (Exception e)
        {
            Debug.LogError("Could not get Android storage path: " + e.Message);
            return null;
        }
    }

    public static void UpdateProfilerDisplay()
    {
        if (Player.Settings == null) return;

        graphyManager = GraphyManager.Instance;
        print("Profiler display: " + Player.Settings.DisplayProfiler);
        if (graphyManager == null) return;
        if (Player.Settings.DisplayProfiler)
        {
            graphyManager.Enable();
            graphyManager.FpsModuleState = GraphyManager.ModuleState.FULL;
            graphyManager.RamModuleState = GraphyManager.ModuleState.FULL;
            graphyManager.AudioModuleState = GraphyManager.ModuleState.FULL;
        }
        else
        {
            graphyManager.Disable();
        }
    }

    public static void UpdateGraphicsQuality()
    {
        var quality = Player.Settings.GraphicsQuality;
        switch (quality)
        {
            case GraphicsQuality.Ultra:
            case GraphicsQuality.High:
                UnityEngine.Screen.SetResolution(InitialWidth, InitialHeight, FullScreenMode.ExclusiveFullScreen);
                break;
            case GraphicsQuality.Medium:
                UnityEngine.Screen.SetResolution((int)(InitialWidth * 0.7f),
                    (int)(InitialHeight * 0.7f), FullScreenMode.ExclusiveFullScreen);
                break;
            case GraphicsQuality.Low:
                UnityEngine.Screen.SetResolution((int)(InitialWidth * 0.5f),
                    (int)(InitialHeight * 0.5f), FullScreenMode.ExclusiveFullScreen);
                break;
            case GraphicsQuality.VeryLow:
                UnityEngine.Screen.SetResolution((int)(InitialWidth * 0.3f),
                    (int)(InitialHeight * 0.3f), FullScreenMode.ExclusiveFullScreen);
                break;
        }

    }

    public static void SetMajorCanvasBlockRaycasts(bool blocksRaycasts)
    {
        if (ScreenManager == null) return;
        if (ScreenManager.ActiveScreenId != null)
        {
            ScreenManager.ActiveScreen.SetBlockRaycasts(blocksRaycasts);
        }
    }

    public static bool ShouldDisableMenuTransitions()
    {
        return SceneManager.GetActiveScene().name == "Navigation" && !Player.Settings.UseMenuTransitions;
    }

    public static Distribution Distribution
    {
        get
        {
            return Distribution.Global;
        }
    }
}

public enum HapticTypes
{
    Selection,
    Success,
    Warning,
    Failure,
    LightImpact,
    MediumImpact,
    HeavyImpact,
    RigidImpact,
    SoftImpact,
}

public enum Distribution
{
    Global, TapTap
}

public class GameErrorState
{
    public string Message;
    public Exception Exception;
}
