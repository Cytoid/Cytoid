using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Tayx.Graphy;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Context : SingletonMonoBehavior<Context>
{
    public const string ApiBaseUrl = "https://api.cytoid.io";
    public const string WebsiteUrl = "https://cytoid.io";
    
    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    public static readonly LevelEvent OnSelectedLevelChanged = new LevelEvent();
    
    public static string DataPath;
    public static int InitialWidth;
    public static int InitialHeight;
    
    public static AudioManager AudioManager;
    public static ScreenManager ScreenManager;
    
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

    public static GameResult LastGameResult;

    public static LocalPlayer LocalPlayer = new LocalPlayer();
    public static OnlinePlayer OnlinePlayer = new OnlinePlayer();

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

    private async void InitializeApplication()
    {
        InitialWidth = UnityEngine.Screen.width;
        InitialHeight = UnityEngine.Screen.height;
        
        DOTween.defaultEaseType = Ease.OutCubic;
        UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 120;

        DataPath = Application.persistentDataPath;
        print("Data path: " + DataPath);

        // On Android...
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
		}

#if UNITY_EDITOR
        Application.runInBackground = true;
#endif

        SelectedMods = new HashSet<Mod>(LocalPlayer.EnabledMods);

        if (SceneManager.GetActiveScene().name == "Game")
        {
            // Load test level
            await LevelManager.LoadFromMetadataFiles(new List<string> { DataPath + "/sggrkung.festival_blaze/level.json" });
            SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
            SelectedDifficulty = Difficulty.Parse(SelectedLevel.Meta.charts[0].type);
        }
        else
        {
            await UniTask.WaitUntil(() => ScreenManager != null);
            if (true)
            {
                ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.None);
            }
            
            if (false)
            {
                ScreenManager.ChangeScreen(CommunityLevelSelectionScreen.Id, ScreenTransition.None);
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
        }

        await UniTask.DelayFrame(0);

        graphyManager = GraphyManager.Instance;
        UpdateProfilerDisplay();
    }
    
    public static void PreSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            LoopAudioPlayer.Instance.StopMainLoopAudio();
            LoopAudioPlayer.Instance.FadeOutLoopPlayer(0);
            // Save history
            navigationScreenHistory = new Stack<string>(ScreenManager.History);
        }
    }

    public static void OnSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            OnlinePlayer.IsAuthenticating = false;
        }
        if (prev == "Game" && next == "Navigation")
        {
            LoopAudioPlayer.Instance.PlayMainLoopAudio();
            // Restore history
            ScreenManager.History = new Stack<string>(navigationScreenHistory);
            if (LastGameResult != null)
            {
                // Show result screen
                ScreenManager.ChangeScreen(ResultScreen.Id, ScreenTransition.None, addToHistory: false);
            }
            else
            {
                // Show game preparation screen
                ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.None);
            }
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
}