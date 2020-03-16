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
    public const string Version = "2.0 Alpha 5";
    
    public const string ApiUrl = "https://api.cytoid.io";
    public const string ServicesUrl = "http://192.168.3.13:4000";
    public const string WebsiteUrl = "https://cytoid.io";
    
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    public const int ThumbnailWidth = 576;
    public const int ThumbnailHeight = 360;

    public static readonly LevelEvent OnSelectedLevelChanged = new LevelEvent(); // TODO: This feels definitely unnecessary. Integrate with screen?
    public static readonly UnityEvent OnLanguageChanged = new UnityEvent();
    
    public static string DataPath;
    public static string TierDataPath;
    public static string iOSTemporaryInboxPath;
    public static int InitialWidth;
    public static int InitialHeight;
    public static int DefaultDspBufferSize { get; private set; }
    
    public static AudioManager AudioManager;
    public static ScreenManager ScreenManager;

    public static readonly Library Library = new Library();
    public static readonly FontManager FontManager = new FontManager();
    public static readonly LevelManager LevelManager = new LevelManager();
    public static readonly RemoteResourceManager RemoteResourceManager = new RemoteResourceManager();
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

    public static GameState GameState;
    public static TierState TierState;

    public static readonly LocalPlayer LocalPlayer = new LocalPlayer();
    public static readonly OnlinePlayer OnlinePlayer = new OnlinePlayer();

    private static Level selectedLevel;
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

    private static void OnLowMemory()
    {
        Resources.UnloadUnusedAssets();
    }
    
    private async void InitializeApplication()
    {
        Application.lowMemory += OnLowMemory;
        
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

        await RemoteResourceManager.UpdateCatalog(); // TODO TODO

        if (SceneManager.GetActiveScene().name == "Navigation")
        {
            await UniTask.WaitUntil(() => ScreenManager != null);
            if (false)
            {
                ScreenManager.ChangeScreen(CharacterSelectionScreen.Id, ScreenTransition.None);
            }
            
            if (true)
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

            if (false)
            {
                // Load result
                await LevelManager.LoadFromMetadataFiles(new List<string> { DataPath + "/suconh_typex.alice/level.json" });
                SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
                SelectedDifficulty = Difficulty.Hard;
                ScreenManager.ChangeScreen("Result", ScreenTransition.None);
            }
            
            if (false)
            {
                // Load result
                ScreenManager.ChangeScreen(TierResultScreen.Id, ScreenTransition.None);
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
            // Restore history
            ScreenManager.History = new Stack<string>(navigationScreenHistory);

            if (TierState != null)
            {
                if (TierState.CurrentStage.IsCompleted)
                {
                    // Show tier result screen
                    ScreenManager.ChangeScreen(TierBreakScreen.Id, ScreenTransition.None, addToHistory: false);
                }
                else
                {
                    TierState = null;
                    // Show tier selection screen
                    ScreenManager.ChangeScreen(TierSelectionScreen.Id, ScreenTransition.None);
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
                QualitySettings.masterTextureLimit = 0;
                break;
            case "medium":
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.7f),
                    (int) (InitialHeight * 0.7f), true);
                QualitySettings.masterTextureLimit = 0;
                break;
            case "low":
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.5f),
                    (int) (InitialHeight * 0.5f), true);
                QualitySettings.masterTextureLimit = 1;
                break;
        }
    }

    public static void SetMajorCanvasBlockRaycasts(bool blocksRaycasts)
    {
        if (ScreenManager.ActiveScreenId != null)
        {
            ScreenManager.ActiveScreen.CanvasGroup.interactable = ScreenManager.ActiveScreen.CanvasGroup.blocksRaycasts = blocksRaycasts;
        }
        if (ProfileWidget.Instance != null)
        {
            var currentScreenId = ScreenManager.ActiveScreenId;
            ProfileWidget.Instance.canvasGroup.interactable = ProfileWidget.Instance.canvasGroup.blocksRaycasts = 
                blocksRaycasts 
                && !ProfileWidget.HiddenScreenIds.Contains(currentScreenId) 
                && !ProfileWidget.StaticScreenIds.Contains(currentScreenId);
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(Context))]
public class ContextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            if (Context.AssetMemory != null)
            {
                GUILayout.Label($"Asset memory usage:");
                foreach (AssetTag tag in Enum.GetValues(typeof(AssetTag)))
                {
                    GUILayout.Label(
                        $"{tag}: {Context.AssetMemory.CountTagUsage(tag)}/{(Context.AssetMemory.GetTagLimit(tag) > 0 ? Context.AssetMemory.GetTagLimit(tag).ToString() : "∞")}");
                }
            }

            if (GUILayout.Button("Unload unused assets"))
            {
                Resources.UnloadUnusedAssets();
            }
            if (GUILayout.Button("Upload test"))
            {
                Test.UploadTest();
            }
            EditorUtility.SetDirty(target);
        }
    }
}
#endif