using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using LiteDB;
using Newtonsoft.Json;
using Polyglot;
using Tayx.Graphy;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Context : SingletonMonoBehavior<Context>
{
    public const string Version = "2.0 Alpha 6";

    public static string ApiUrl = "https://api.cytoid.io";
    public const string ServicesUrl = "http://dorm.neoto.xin:4000";
    public const string WebsiteUrl = "https://cytoid.io";

    public const string OfficialAccountId = "cytoid";

    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    public const int ThumbnailWidth = 576;
    public const int ThumbnailHeight = 360;

    public static readonly PreSceneChangedEvent PreSceneChangedEvent = new PreSceneChangedEvent();
    public static readonly PostSceneChangedEvent PostSceneChangedEvent = new PostSceneChangedEvent();

    public static readonly LevelEvent
        OnSelectedLevelChanged = new LevelEvent(); // TODO: This feels definitely unnecessary. Integrate with screen?

    public static readonly UnityEvent OnLanguageChanged = new UnityEvent();
    public static readonly OfflineModeToggleEvent OnOfflineModeToggled = new OfflineModeToggleEvent();

    public static string UserDataPath;
    public static string iOSTemporaryInboxPath;
    public static int InitialWidth;
    public static int InitialHeight;
    public static int DefaultDspBufferSize { get; private set; }

    public static AudioManager AudioManager;
    public static ScreenManager ScreenManager;

    public static readonly Library Library = new Library();
    public static readonly FontManager FontManager = new FontManager();
    public static readonly LevelManager LevelManager = new LevelManager();
    public static readonly CharacterManager CharacterManager = new CharacterManager();
    public static readonly RemoteAssetManager RemoteAssetManager = new RemoteAssetManager();
    public static readonly AssetMemory AssetMemory = new AssetMemory();

    public static LiteDatabase Database;

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

    private static bool offline;
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

    private void OnApplicationQuit()
    {
        Database?.Dispose();
    }

    private async void InitializeApplication()
    {
        Application.lowMemory += OnLowMemory;
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new UnityColorConverter()
            },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        BsonMapper.Global.RegisterType
        (
            color => "#" + ColorUtility.ToHtmlStringRGB(color),
            s => s.AsString.ToColor()
        );
        
        Database = new LiteDatabase(
            new ConnectionString
            {
                Filename = Path.Combine(Application.persistentDataPath, "Cytoid.db"),
                // Password = SecuredConstants.DbSecret,
                Connection = Application.isEditor ? ConnectionType.Shared : ConnectionType.Direct
            }
        );

        // Warm up LiteDB
        Database.GetProfile();
        // Database.DropCollection("settings");
        // Database.DropCollection("level_records"); /////// TODO TODO TODO TODO
        
        // Load settings
        LocalPlayer.LoadSettings();

        FontManager.LoadFonts();

        var audioConfig = AudioSettings.GetConfiguration();
        DefaultDspBufferSize = audioConfig.dspBufferSize;

        if (Application.platform == RuntimePlatform.Android && LocalPlayer.Settings.AndroidDspBufferSize > 0)
        {
            audioConfig.dspBufferSize = LocalPlayer.Settings.AndroidDspBufferSize;
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
            // Create an empty folder if it doesn't already exist
            Directory.CreateDirectory(UserDataPath);
            try
            {
                File.Create(UserDataPath + "/.nomedia").Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Cannot create or overwrite .nomedia file. Is it read-only?");
                Debug.LogWarning(e);
            }
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

        SelectedMods = new HashSet<Mod>(LocalPlayer.Settings.EnabledMods);

        PreSceneChangedEvent.AddListener(PreSceneChanged);
        PostSceneChangedEvent.AddListener(PostSceneChanged);

        OnLanguageChanged.AddListener(FontManager.UpdateSceneTexts);
        Localization.Instance.SelectLanguage((Language) LocalPlayer.Settings.Language);
        OnLanguageChanged.Invoke();

        await Addressables.InitializeAsync().Task;

        if (await CharacterManager.SetActiveCharacter(CharacterManager.SelectedCharacterAssetId) == null)
        {
            // Reset to default
            CharacterManager.SelectedCharacterAssetId = null;
            await CharacterManager.SetActiveCharacter(CharacterManager.SelectedCharacterAssetId);
        }

        if (SceneManager.GetActiveScene().name == "Navigation")
        {
            await UniTask.WaitUntil(() => ScreenManager != null);

            if (true)
            {
                ScreenManager.ChangeScreen(InitializationScreen.Id, ScreenTransition.None);
            }

            if (false)
            {
                ScreenManager.ChangeScreen(TrainingSelectionScreen.Id, ScreenTransition.None);
            }

            if (false)
            {
                // Load f.fff
                await LevelManager.LoadFromMetadataFiles(LevelType.Community,
                    new List<string> {UserDataPath + "/f.fff/level.json"});
                SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
                SelectedDifficulty = Difficulty.Parse(SelectedLevel.Meta.charts[0].type);
                ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.None);
            }

            if (false)
            {
                // Load result
                await LevelManager.LoadFromMetadataFiles(LevelType.Community, new List<string>
                    {UserDataPath + "/fizzest.sentimental.crisis/level.json"});
                SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
                SelectedDifficulty =
                    Difficulty.Parse(LevelManager.LoadedLocalLevels.Values.First().Meta.charts.First().type);
                
                ScreenManager.ChangeScreen(ResultScreen.Id, ScreenTransition.None);
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
            // Save history
            navigationScreenHistory = new Stack<string>(ScreenManager.History);
        }
    }

    public static async void PostSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            OnlinePlayer.IsAuthenticating = false;
            CharacterManager.UnloadActiveCharacter();
        }

        if (prev == "Game" && next == "Navigation")
        {
            Input.gyro.enabled = true;

            // Wait until character is loaded
            await CharacterManager.SetSelectedCharacterActive();

            MainTranslucentImage.Instance.WillUpdateTranslucentImage();

            // Restore history
            ScreenManager.History = new Stack<string>(navigationScreenHistory);

            if (TierState != null)
            {
                if (TierState.CurrentStage.IsCompleted)
                {
                    // Show tier result screen
                    ScreenManager.ChangeScreen(TierBreakScreen.Id, ScreenTransition.None,
                        addTargetScreenToHistory: false);
                }
                else
                {
                    TierState = null;
                    // Show tier selection screen
                    ScreenManager.ChangeScreen(TierSelectionScreen.Id, ScreenTransition.None);
                }
            }
            else if (GameState != null)
            {
                var usedAuto =  GameState.Mods.Contains(Mod.Auto) || GameState.Mods.Contains(Mod.AutoDrag) || GameState.Mods.Contains(Mod.AutoHold) || GameState.Mods.Contains(Mod.AutoFlick);
                if (GameState.IsCompleted && (GameState.Mode == GameMode.Standard || GameState.Mode == GameMode.Practice) && !usedAuto)
                {
                    // Show result screen
                    ScreenManager.ChangeScreen(ResultScreen.Id, ScreenTransition.None, addTargetScreenToHistory: false);
                }
                else
                {
                    // Show game preparation screen
                    ScreenManager.ChangeScreen(GamePreparationScreen.Id, ScreenTransition.None);
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
        print("Profiler display: " + LocalPlayer.Settings.DisplayProfiler);
        if (graphyManager == null) return;
        if (LocalPlayer.Settings.DisplayProfiler)
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
        switch (LocalPlayer.Settings.GraphicsQuality)
        {
            case GraphicsQuality.High:
                UnityEngine.Screen.SetResolution(InitialWidth, InitialHeight, true);
                QualitySettings.masterTextureLimit = 0;
                break;
            case GraphicsQuality.Medium:
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.7f),
                    (int) (InitialHeight * 0.7f), true);
                QualitySettings.masterTextureLimit = 0;
                break;
            case GraphicsQuality.Low:
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.5f),
                    (int) (InitialHeight * 0.5f), true);
                QualitySettings.masterTextureLimit = 1;
                break;
        }
        MainTranslucentImage.Static = LocalPlayer.Settings.GraphicsQuality != GraphicsQuality.High;
        if (ScreenManager != null && ScreenManager.ActiveScreenId != null)
        {
            if (MainTranslucentImage.Instance != null)
                MainTranslucentImage.Instance.WillUpdateTranslucentImage();
        }
    }

    public static void SetMajorCanvasBlockRaycasts(bool blocksRaycasts)
    {
        if (ScreenManager.ActiveScreenId != null)
        {
            ScreenManager.ActiveScreen.CanvasGroup.blocksRaycasts = blocksRaycasts;
            ScreenManager.ActiveScreen.CanvasGroup.interactable = blocksRaycasts;
        }

        if (ProfileWidget.Instance != null)
        {
            var currentScreenId = ScreenManager.ActiveScreenId;
            blocksRaycasts = blocksRaycasts
                             && !ProfileWidget.HiddenScreenIds.Contains(currentScreenId)
                             && !ProfileWidget.StaticScreenIds.Contains(currentScreenId);
            ProfileWidget.Instance.canvasGroup.blocksRaycasts = blocksRaycasts;
            ProfileWidget.Instance.canvasGroup.interactable = blocksRaycasts;
        }
    }

    public static bool IsOffline() => offline;

    public static bool IsOnline() => !IsOffline();

    public static void SetOffline(bool offline)
    {
        Context.offline = offline;
        OnOfflineModeToggled.Invoke(offline);
    }
}

public class OfflineModeToggleEvent : UnityEvent<bool>
{
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
                GUILayout.Label("Asset memory usage:");
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

            if (GUILayout.Button("Toggle offline mode"))
            {
                Context.SetOffline(!Context.IsOffline());
            }

            if (GUILayout.Button("Make API work/not work"))
            {
                if (Context.ApiUrl == "https://api.cytoid.io")
                {
                    Context.ApiUrl = "https://apissss.cytoid.io";
                }
                else
                {
                    Context.ApiUrl = "https://api.cytoid.io";
                }
            }

            if (GUILayout.Button("Reward Overlay"))
            {
                RewardOverlay.Show(new List<OnlinePlayerStateChange.Reward>
                {
                    JsonConvert.DeserializeObject<OnlinePlayerStateChange.Reward>(@"{""type"":""character"",""value"":{""illustrator"":{""name"":""しがらき"",""url"":""https://www.pixiv.net/en/users/1004274""},""designer"":{""name"":"""",""url"":""""},""name"":""Mafumafu"",""description"":""何でも屋です。"",""_id"":""5e6f90dcdab3462655fb93a4"",""levelId"":4101,""asset"":""Mafu"",""tachieAsset"":""MafuTachie"",""id"":""5e6f90dcdab3462655fb93a4""}}")
                });
            }

            EditorUtility.SetDirty(target);
        }
    }
}
#endif