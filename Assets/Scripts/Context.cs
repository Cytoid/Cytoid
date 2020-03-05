using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Polyglot;
using Tayx.Graphy;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class Context : SingletonMonoBehavior<Context>
{
    public const string Version = "2.0 Alpha 4";
    
    public const string ApiBaseUrl = "https://api.cytoid.io";
    public const string WebsiteUrl = "https://cytoid.io";
    
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;
    
    public static int ThumbnailWidth = 576;
    public static int ThumbnailHeight = 360;

    public static readonly LevelEvent OnSelectedLevelChanged = new LevelEvent();
    public static readonly UnityEvent OnLanguageChanged = new UnityEvent();
    
    public static string DataPath;
    public static string TierDataPath;
    public static string iOSTemporaryInboxPath;
    public static int InitialWidth;
    public static int InitialHeight;
    public static int DefaultDspBufferSize { get; private set; }
    
    public static AudioManager AudioManager;
    public static ScreenManager ScreenManager;

    public static Library Library = new Library();
    public static FontManager FontManager = new FontManager();
    public static LevelManager LevelManager = new LevelManager();
    public static SpriteCache SpriteCache = new SpriteCache();

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
    public static Tier SelectedTier
    {
        get => selectedTier;
        set
        {
            selectedTier = value;
        }
    }
    public static bool WillCalibrate;

    public static GameState GameState;
    public static TierState TierState;

    public static LocalPlayer LocalPlayer = new LocalPlayer();
    public static OnlinePlayer OnlinePlayer = new OnlinePlayer();

    private static Level selectedLevel;
    private static Tier selectedTier;
    private static GraphyManager graphyManager;
    private static Stack<string> navigationScreenHistory = new Stack<string>();

    protected override void Awake()
    {
        base.Awake();

        if (GameObject.FindGameObjectsWithTag("Context").Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        InitializeApplication();
    }

    private async void InitializeApplication()
    {
        FontManager.LoadFonts();
        
        var audioConfig = AudioSettings.GetConfiguration();
        DefaultDspBufferSize = audioConfig.dspBufferSize;
        
        if (Application.platform == RuntimePlatform.Android && LocalPlayer.DspBufferSize > 0)
        {
            audioConfig.dspBufferSize = LocalPlayer.DspBufferSize;
            AudioSettings.Reset(audioConfig);
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // MMVibrationManager.iOSInitializeHaptics();
        }

        InitialWidth = UnityEngine.Screen.width;
        InitialHeight = UnityEngine.Screen.height;
        UpdateGraphicsQuality();

        DOTween.defaultEaseType = Ease.OutCubic;
        UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 120;
        Input.gyro.enabled = true;
        
        DataPath = Application.persistentDataPath;
        print("Data path: " + DataPath);

		if (Application.platform == RuntimePlatform.Android)
		{
			var dir = GetAndroidStoragePath();
			if (dir == null)
			{
				Application.Quit();
				return;
			}
			DataPath = dir + "/Cytoid";
			// Create an empty folder if it doesn't already exist
			Directory.CreateDirectory(DataPath);
            File.Create(DataPath + "/.nomedia").Dispose();
		} 
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // iOS 13 fix
            iOSTemporaryInboxPath = DataPath
                                   .Replace("Documents/", "")
                                   .Replace("Documents", "") + "/tmp/me.tigerhix.cytoid-Inbox/";
        }

#if UNITY_EDITOR
        Application.runInBackground = true;
#endif

        TierDataPath = Path.Combine(Application.persistentDataPath, ".tiers");
        Directory.CreateDirectory(TierDataPath);

        SelectedMods = new HashSet<Mod>(LocalPlayer.EnabledMods);

        OnLanguageChanged.AddListener(FontManager.UpdateSceneTexts);
        Localization.Instance.SelectLanguage((Language) LocalPlayer.Language);
        OnLanguageChanged.Invoke();

        if (SceneManager.GetActiveScene().name == "Game")
        {
            // Load test level
            await LevelManager.LoadFromMetadataFiles(new List<string> { DataPath + "/tar1412.iwannabeit/level.json" });
            SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
            SelectedDifficulty = Difficulty.Extreme;
            SelectedMods.Remove(Mod.Auto);
        }
        else
        {
            await UniTask.WaitUntil(() => ScreenManager != null);
            if (false)
            {
                ScreenManager.ChangeScreen(InitializationScreen.Id, ScreenTransition.None);
            }
            
            if (false)
            {
                ScreenManager.ChangeScreen(TierSelectionScreen.Id, ScreenTransition.None);
            }
            
            if (false)
            {
                // Load f.fff
                await LevelManager.LoadFromMetadataFiles(new List<string> { DataPath + "/f.fff/level.json" });
                SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
                SelectedDifficulty = Difficulty.Parse(SelectedLevel.Meta.charts[0].type);
                ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.None);
            }

            if (true)
            {
                // Load result
                await LevelManager.LoadFromMetadataFiles(new List<string> { DataPath + "/suconh_typex.alice/level.json" });
                SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
                SelectedDifficulty = Difficulty.Hard;
                ScreenManager.ChangeScreen("Result", ScreenTransition.None);
            }
        }
        await UniTask.DelayFrame(0);

        graphyManager = GraphyManager.Instance;
        UpdateProfilerDisplay();
    }

    public static void PreSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            Input.gyro.enabled = false;
            LoopAudioPlayer.Instance.StopMainLoopAudio();
            LoopAudioPlayer.Instance.FadeOutLoopPlayer(0);
            // Save history
            navigationScreenHistory = new Stack<string>(ScreenManager.History);
        }
    }

    public static async void OnSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            OnlinePlayer.IsAuthenticating = false;
        }
        if (prev == "Game" && next == "Navigation")
        {
            Input.gyro.enabled = true;
            WillCalibrate = false;
            // Restore history
            ScreenManager.History = new Stack<string>(navigationScreenHistory);

            if (TierState != null)
            {
                if (TierState.Stages.Last().IsCompleted)
                {
                    // Show tier result screen
                    ScreenManager.ChangeScreen(TierBreakScreen.Id, ScreenTransition.None, addToHistory: false);
                }
                else
                {
                    // Show game preparation screen
                    ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.None);
                    await UniTask.DelayFrame(5);
                    LoopAudioPlayer.Instance.PlayMainLoopAudio();
                }
            } 
            else if (GameState != null)
            {
                if (GameState.IsCompleted)
                {
                    // Show result screen
                    ScreenManager.ChangeScreen(ResultScreen.Id, ScreenTransition.None, addToHistory: false);
                }
                else
                {
                    // Show game preparation screen
                    ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.None);
                    await UniTask.DelayFrame(5);
                    LoopAudioPlayer.Instance.PlayMainLoopAudio();
                }
            }
        }

        FontManager.UpdateSceneTexts();
    }

    public static void Vibrate()
    {
        // TODO: Haptic engine
        // Handheld.Vibrate();
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

    private string GetAndroidStoragePath()
    {
        var path = "";
        if (Application.platform == RuntimePlatform.Android)
        {
            try
            {
                using (var javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var activityClass = javaClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        path = activityClass.Call<AndroidJavaObject>("getAndroidStorageFile")
                            .Call<string>("getAbsolutePath");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Could not get Android storage path: " + e.Message);
            }
        }

        return path;
    }

    public static void UpdateProfilerDisplay()
    {
        print("Profiler display: " + LocalPlayer.DisplayProfiler);
        if (graphyManager == null) return;
        if (LocalPlayer.DisplayProfiler)
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
        switch (LocalPlayer.GraphicsQuality)
        {
            case "high":
                UnityEngine.Screen.SetResolution(InitialWidth, InitialHeight, true);
                break;
            case "medium":
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.7f),
                    (int) (InitialHeight * 0.7f), true);
                break;
            case "low":
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.5f),
                    (int) (InitialHeight * 0.5f), true);
                break;
        }
    }

    public static void SetMajorCanvasBlockRaycasts(bool blocksRaycasts)
    {
        if (ScreenManager.ActiveScreenId != null)
        {
            ScreenManager.ActiveScreen.CanvasGroup.blocksRaycasts = blocksRaycasts;
        }
        if (ProfileWidget.Instance != null)
        {
            ProfileWidget.Instance.canvasGroup.blocksRaycasts = blocksRaycasts;
        }
    }
}