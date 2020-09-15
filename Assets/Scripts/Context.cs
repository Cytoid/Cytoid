using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using LunarConsolePlugin;
using MoreMountains.NiceVibrations;
using Newtonsoft.Json;
using Polyglot;
using Proyecto26;
using Tayx.Graphy;
using Cysharp.Threading.Tasks;
using LiteDB;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Context : SingletonMonoBehavior<Context>
{
    public const string VersionName = "2.0.0";
    public const string VersionString = "2.0.0";
    public const int VersionCode = 88;

    public static string MockApiUrl;

    public static CdnRegion CdnRegion => Player.Settings.CdnRegion;
    public static string ApiUrl => MockApiUrl ?? CdnRegion.GetApiUrl();
    public static string WebsiteUrl => CdnRegion.GetWebsiteUrl();

    public static string BundleRemoteBaseUrl
    {
        get
        {
            if (Application.isEditor && Instance.editorUseLocalAssetBundles)
            {
                return $"file://{Application.dataPath.Replace("/Assets", "")}/AssetBundles";
            }
            else
            {
                return $"{CdnRegion.GetBundleRemoteBaseUrl()}/platforms";
            }
        }
    }
    public static string StoreUrl => CdnRegion.GetStoreUrl();
    
    public static string BundleRemoteFullUrl
    {
        get
        {
#if UNITY_ANDROID
                return $"{BundleRemoteBaseUrl}/Android/";
#elif UNITY_IOS
                return $"{BundleRemoteBaseUrl}/iOS/";
#else
                throw new InvalidOperationException();
#endif
        }
    }
    
    public const string OfficialAccountId = "cytoid";

    public const int ReferenceWidth = 1920;
    public const int ReferenceHeight = 1080;

    public const int LevelThumbnailWidth = 576;
    public const int LevelThumbnailHeight = 360;
    
    public const int CollectionThumbnailWidth = 576;
    public const int CollectionThumbnailHeight = 216;

    public static int AndroidVersionCode = -1;

    public static readonly PreSceneChangedEvent PreSceneChanged = new PreSceneChangedEvent();
    public static readonly PostSceneChangedEvent PostSceneChanged = new PostSceneChangedEvent();
    public static readonly UnityEvent OnApplicationInitialized = new UnityEvent();
    public static bool IsInitialized { get; private set; }

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
    public static readonly BundleManager BundleManager = new BundleManager();
    public static readonly AssetMemory AssetMemory = new AssetMemory();

    public static LiteDatabase Database
    {
        get => database ?? (database = CreateDatabase());
        private set => database = value;
    }

    private static LiteDatabase database;

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

    public static InitializationState InitializationState;
    public static GameState GameState;
    public static TierState TierState;

    public static readonly Player Player = new Player();
    public static readonly OnlinePlayer OnlinePlayer = new OnlinePlayer();

    public static GameErrorState GameErrorState;

    private static bool offline;
    private static Level selectedLevel;
    private static GraphyManager graphyManager;
    private static Stack<Intent> navigationScreenHistory = new Stack<Intent>();

    public bool editorUseLocalAssetBundles = true;
    
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
        if (SceneManager.GetActiveScene().name == "Navigation")
        {
            Resources.UnloadUnusedAssets();
            
            AudioManager.Get("ActionError").Play(ignoreDsp: true);
            LoopAudioPlayer.Instance.StopAudio(0);
            Dialog.PromptAlert("DIALOG_LOW_MEMORY".Get());
        }
        // TODO: Investigate on this
        // Resources.UnloadUnusedAssets();
        return;
        AssetMemory.DisposeAllAssets();
        if (SceneManager.GetActiveScene().name == "Navigation")
        {
            AudioManager.Get("ActionError").Play(ignoreDsp: true);
            ScreenManager.History.Clear();
            ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
            Dialog.PromptAlert("DIALOG_LOW_MEMORY".Get());
        }
    }

    private void OnApplicationQuit()
    {
        Database?.Dispose();
    }

    private async void InitializeApplication()
    {
        LunarConsole.SetConsoleEnabled(true); // Enable startup debug
        
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
        
        if (SceneManager.GetActiveScene().name == "Navigation") StartupLogger.Instance.Initialize();
        Debug.Log($"Package name: {Application.identifier}");

        Application.lowMemory += OnLowMemory;
        Application.targetFrameRate = 120;
        Input.gyro.enabled = true;
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
        BsonMapper.Global.RegisterType
        (
            color => "#" + ColorUtility.ToHtmlStringRGB(color),
            s => s.AsString.ToColor()
        );
        FontManager.LoadFonts();
        
        if (Application.platform == RuntimePlatform.Android)
        {
            // Get Android version
            using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
                AndroidVersionCode = version.GetStatic<int>("SDK_INT");
            }
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
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Dialog.PromptUnclosable("DIALOG_CRITICAL_ERROR_COULD_NOT_START_GAME_REASON_X".Get(
                    "DIALOG_CRITICAL_ERROR_REASON_WRITE_PERMISSION".Get()));
                return;
            }
        }

        try
        {
            var timer = new BenchmarkTimer("LiteDB");
            Database = CreateDatabase();
            // Database.Checkpoint();
            timer.Time();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Dialog.Instantiate().Also(it =>
            {
                it.UseNegativeButton = false;
                it.UsePositiveButton = false;
                it.Message =
                    "DIALOG_CRITICAL_ERROR_COULD_NOT_START_GAME_REASON_X".Get(
                        "DIALOG_CRITICAL_ERROR_REASON_DATABASE".Get());
            }).Open();
            return;
        }

        // LiteDB warm-up
        Library.Initialize();
        
        // Load settings
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

        await UniTask.WaitUntil(() => AudioManager != null);
        AudioManager.Initialize();

        InitialWidth = UnityEngine.Screen.width;
        InitialHeight = UnityEngine.Screen.height;
        UpdateGraphicsQuality();

        SelectedMods = new HashSet<Mod>(Player.Settings.EnabledMods);

        PreSceneChanged.AddListener(OnPreSceneChanged);
        PostSceneChanged.AddListener(OnPostSceneChanged);

        OnLanguageChanged.AddListener(FontManager.UpdateSceneTexts);
        Localization.Instance.SelectLanguage((Language) Player.Settings.Language);
        OnLanguageChanged.Invoke();
        
        // TODO: Add standalone support?
#if UNITY_IOS || UNITY_ANDROID
        await BundleManager.Initialize();
#endif

        if (Player.ShouldOneShot(StringKey.FirstLaunch))
        {
            Player.SetTrigger(StringKey.FirstLaunch);
        }

        switch (SceneManager.GetActiveScene().name)
        {
            case "Navigation":
                if (Player.ShouldTrigger(StringKey.FirstLaunch, false))
                {
                    InitializationState.FirstLaunchPhase = FirstLaunchPhase.GlobalCalibration;

                    // Global calibration
                    SelectedGameMode = GameMode.GlobalCalibration;
                    var sceneLoader = new SceneLoader("Game");
                    await sceneLoader.Load();
                    sceneLoader.Activate();
                }
                else
                {
                    await InitializeNavigation();
                }
                break;
            case "Game":
                break;
        }

        await UniTask.DelayFrame(0);

        graphyManager = GraphyManager.Instance;
        UpdateProfilerDisplay();

        IsInitialized = true;
        OnApplicationInitialized.Invoke();
        
        LunarConsole.SetConsoleEnabled(Player.Settings.UseDeveloperConsole);
    }

    private static async UniTask InitializeNavigation()
    {
        InitializationState.IsInitialized = true;
        
        Debug.Log("Initializing character asset");
        var timer = new BenchmarkTimer("Character");
        if (await CharacterManager.SetActiveCharacter(CharacterManager.SelectedCharacterId) == null)
        {
            // Reset to default
            CharacterManager.SelectedCharacterId = null;
            MainMenuScreen.PromptCachedCharacterDataCleared = true;
            await CharacterManager.SetActiveCharacter(CharacterManager.SelectedCharacterId);
        }

        timer.Time();
        await UniTask.WaitUntil(() => ScreenManager != null);
        
        ScreenManager.ChangeScreen(InitializationScreen.Id, ScreenTransition.None);
        /*if (false)
        {
            ScreenManager.ChangeScreen(TrainingSelectionScreen.Id, ScreenTransition.None);
        }

        if (false)
        {
            // Load f.fff
            await LevelManager.LoadFromMetadataFiles(LevelType.User,
                new List<string> {UserDataPath + "/f.fff/level.json"});
            SelectedLevel = LevelManager.LoadedLocalLevels.Values.First();
            SelectedDifficulty = Difficulty.Parse(SelectedLevel.Meta.charts[0].type);
            ScreenManager.ChangeScreen("GamePreparation", ScreenTransition.None);
        }

        if (false)
        {
            // Load result
            await LevelManager.LoadFromMetadataFiles(LevelType.User, new List<string>
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
        }*/
    }

    public async UniTask DetectServerCdn()
    {
        if (!Player.ShouldOneShot("Detect Server CDN")) return;

        Debug.Log("Detecting server CDN");

        if (Distribution == Distribution.Global)
        {
            var resolved = false;
            var startTime = DateTimeOffset.Now;

            RestClient.Get<RegionInfo>(new RequestHelper
            {
                Uri = "https://services.cytoid.io/ping",
                Timeout = 5,
                EnableDebug = true
            }).Then(it =>
            {
                Player.Settings.CdnRegion = it.countryCode == "CN" ? CdnRegion.MainlandChina : CdnRegion.International;
                resolved = true;
            }).CatchRequestError(error =>
            {
                Debug.LogWarning(error);
                RestClient.Get(new RequestHelper
                {
                    Uri = "https://api.cytoid.cn/ping",
                    Timeout = 5,
                    EnableDebug = true
                }).Then(x => { Player.Settings.CdnRegion = CdnRegion.MainlandChina; }).CatchRequestError(x =>
                {
                    Debug.LogWarning(x);
                    Player.ClearOneShot("Detect Server CDN");
                }).Finally(() => resolved = true);
            });
            await UniTask.WaitUntil(() => resolved || DateTimeOffset.Now - startTime > TimeSpan.FromSeconds(10));
            if (!resolved)
            {
                Player.Settings.CdnRegion = CdnRegion.MainlandChina;
            }
        } 
        else if (Distribution == Distribution.China)
        {
            Player.Settings.CdnRegion = CdnRegion.MainlandChina;
        }

        Debug.Log($"Detected: {Player.Settings.CdnRegion}");
    }

    public async UniTask CheckServerCdn()
    {
        void SwitchToOffline()
        {
            Toast.Enqueue(Toast.Status.Success, "TOAST_SWITCHED_TO_OFFLINE_MODE".Get());
            SetOffline(true);
            OnlinePlayer.FetchProfile().Then(it =>
            {
                if (it == null)
                {
                    OnlinePlayer.Deauthenticate();
                }
                else
                {
                    OnlinePlayer.LastProfile = it;
                    OnlinePlayer.IsAuthenticated = true;
                }
            }).Catch(exception => throw new InvalidOperationException()); // Impossible
        }
        
        var resolved = false;
        var startTime = DateTimeOffset.Now;
        Debug.Log("Checking server CDN");
        
        if (Player.Settings.CdnRegion == CdnRegion.MainlandChina)
        {
            RestClient.Get(new RequestHelper
            {
                Uri = "https://api.cytoid.cn/ping",
                Timeout = 5,
                EnableDebug = true
            }).Then(_ =>
            {
                resolved = true;
            }).CatchRequestError(error =>
            {
                Debug.LogWarning("Could not connect to CN");
                Debug.LogWarning(error);

                RestClient.Get<RegionInfo>(new RequestHelper
                {
                    Uri = "https://services.cytoid.io/ping",
                    Timeout = 5,
                    EnableDebug = true
                }).Then(it =>
                {
                    resolved = true;
                    Player.Settings.CdnRegion = CdnRegion.International;
                    Dialog.PromptAlert("中国大陆服务器暂不可用。\n已自动切换到国际服务器。");
                    Player.SetTrigger("Reset Server CDN To CN");
                }).CatchRequestError(it =>
                {
                    Debug.LogWarning("Could not connect to IO");
                    Debug.LogWarning(it);
                    SwitchToOffline();
                }).Finally(() => resolved = true);
            });
                
            await UniTask.WaitUntil(() => resolved || DateTimeOffset.Now - startTime > TimeSpan.FromSeconds(10));
        } 
        else if (Player.Settings.CdnRegion == CdnRegion.International)
        {
            RestClient.Get<RegionInfo>(new RequestHelper
            {
                Uri = "https://services.cytoid.io/ping",
                Timeout = 5,
                EnableDebug = true
            }).CatchRequestError(it =>
            {
                Debug.LogWarning("Could not connect to IO");
                Debug.LogWarning(it);
                SwitchToOffline();
            }).Finally(() => resolved = true);
            
            await UniTask.WaitUntil(() => resolved || DateTimeOffset.Now - startTime > TimeSpan.FromSeconds(5));
        }
    }

    public static void OnPreSceneChanged(string prev, string next)
    {
        switch (prev)
        {
            case "Navigation" when next == "Game":
                Input.gyro.enabled = false;
                // Save history
                navigationScreenHistory = new Stack<Intent>(ScreenManager.History);
                break;
        }
    }

    public static async void OnPostSceneChanged(string prev, string next)
    {
        switch (prev)
        {
            case "Navigation" when next == "Game":
                OnlinePlayer.IsAuthenticating = false;
                CharacterManager.UnloadActiveCharacter();
                BundleManager.ReleaseAll();
                break;
            case "Game" when next == "Navigation":
            {
                Input.gyro.enabled = true;
                AudioManager.Initialize();
                UpdateGraphicsQuality();
                
                if (InitializationState.IsDuringFirstLaunch())
                {
                    switch (InitializationState.FirstLaunchPhase)
                    {
                        case FirstLaunchPhase.GlobalCalibration:
                            // Proceed to basic tutorial
                            InitializationState.FirstLaunchPhase = FirstLaunchPhase.BasicTutorial;
                            SelectedGameMode = GameMode.Practice;
                            SelectedLevel = await LevelManager.LoadOrInstallBuiltInLevel(BuiltInData.TutorialLevelId,
                                LevelType.BuiltIn, true);
                            SelectedDifficulty = Difficulty.Easy;
                            SelectedMods.Clear();
                            
                            Player.Settings.DisplayBoundaries = true;
                           
                            var sceneLoader = new SceneLoader("Game");
                            await UniTask.WhenAll(sceneLoader.Load(), UniTask.Delay(TimeSpan.FromSeconds(1f)));
                            sceneLoader.Activate();
                            break;
                        case FirstLaunchPhase.BasicTutorial:
                            // Save the high score, but let's not show the results screen
                            if (GameState.IsCompleted)
                            {
                                var record = GameState.Level.Record;
                                record.IncrementPlayCountByOne(GameState.Difficulty);
                                record.TrySaveBestPerformance(GameState.Mode, GameState.Difficulty,
                                    (int) GameState.Score, GameState.Accuracy);
                                GameState.Level.SaveRecord();
                            }

                            Player.ClearTrigger(StringKey.FirstLaunch);
                            InitializationState.FirstLaunchPhase = FirstLaunchPhase.Completed;

                            await UniTask.Delay(TimeSpan.FromSeconds(1f));
                            
                            // Initialize navigation like normal
                            await InitializeNavigation();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    // Wait until character is loaded
                    await CharacterManager.SetSelectedCharacterActive();

                    // Restore history
                    ScreenManager.History = new Stack<Intent>(navigationScreenHistory);

                    var gotoResult = false;
                    var isSpecialGameMode = false;
                    if (TierState != null)
                    {
                        if (TierState.CurrentStage.IsCompleted)
                        {
                            gotoResult = true;
                            // Show tier break screen
                            ScreenManager.ChangeScreen(TierBreakScreen.Id, ScreenTransition.None,
                                addTargetScreenToHistory: false);
                        }
                        else
                        {
                            TierState = null;
                            OnlinePlayer.LastFullProfile = null; // Allow full profile to update
                            // Show tier selection screen
                            ScreenManager.ChangeScreen(ScreenManager.PeekHistory(), ScreenTransition.None,
                                addTargetScreenToHistory: false);
                        }
                    }
                    else if (GameState != null)
                    {
                        if (GameState.Mode == GameMode.GlobalCalibration)
                        {
                            isSpecialGameMode = true;
                            // Clear history and just go to main menu
                            ScreenManager.History = new Stack<Intent>();
                            ScreenManager.ChangeScreen(MainMenuScreen.Id, ScreenTransition.In);
                        }
                        else
                        {
                            var usedAuto = GameState.Mods.Contains(Mod.Auto) || GameState.Mods.Contains(Mod.AutoDrag) ||
                                           GameState.Mods.Contains(Mod.AutoHold) ||
                                           GameState.Mods.Contains(Mod.AutoFlick);
                            if (GameState.IsCompleted &&
                                (GameState.Mode == GameMode.Standard || GameState.Mode == GameMode.Practice) &&
                                !usedAuto)
                            {
                                gotoResult = true;
                                OnlinePlayer.LastFullProfile = null; // Allow full profile to update
                                // Show result screen
                                ScreenManager.ChangeScreen(ResultScreen.Id, ScreenTransition.None,
                                    addTargetScreenToHistory: false);
                            }
                            else
                            {
                                // Show game preparation screen
                                ScreenManager.ChangeScreen(ScreenManager.PeekHistory(), ScreenTransition.None,
                                    addTargetScreenToHistory: false);
                            }
                        }
                    }
                    else
                    {
                        // There must have been an error, show last screen
                        ScreenManager.ChangeScreen(ScreenManager.PeekHistory(), ScreenTransition.None,
                            addTargetScreenToHistory: false);
                    }

                    if (!gotoResult && !isSpecialGameMode)
                    {
                        var backdrop = NavigationBackdrop.Instance;
                        backdrop.IsVisible = false;
                        backdrop.IsBlurred = true;
                        backdrop.FadeBrightness(1);
                    }

                    if (GameErrorState != null)
                    {
                        Dialog.PromptAlert(GameErrorState.Message);
                        GameErrorState = null;
                    }
                }
                break;
            }
        }

        FontManager.UpdateSceneTexts();
        // Database.Checkpoint();
    }

    public static void Haptic(HapticTypes type, bool menu)
    {
        if (!Application.isEditor && Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (!(menu ? Player.Settings.MenuTapticFeedback : Player.Settings.HitTapticFeedback)) return;
            MMVibrationManager.Haptic(type);
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
            case GraphicsQuality.VeryLow:
                UnityEngine.Screen.SetResolution((int) (InitialWidth * 0.3f),
                    (int) (InitialHeight * 0.3f), true);
                QualitySettings.masterTextureLimit = 1;
                break;
        }

        var backdrop = NavigationBackdrop.Instance;
        if (backdrop != null)
        {
            backdrop.HighQuality = quality >= GraphicsQuality.High;
        }
    }

    public static void SetMajorCanvasBlockRaycasts(bool blocksRaycasts)
    {
        if (ScreenManager == null) return;
        if (ScreenManager.ActiveScreenId != null)
        {
            ScreenManager.ActiveScreen.SetBlockRaycasts(blocksRaycasts);
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

    public static bool ShouldDisableMenuTransitions()
    {
        return SceneManager.GetActiveScene().name == "Navigation" && !Player.Settings.UseMenuTransitions;
    }

    public static bool IsOffline() => offline;

    public static bool IsOnline() => !IsOffline();

    public static void SetOffline(bool offline)
    {
        Context.offline = offline;
        OnOfflineModeToggled.Invoke(offline);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && Database != null)
        {
            // Database.Checkpoint();
            Database.Dispose();
            Database = null;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && Database != null)
        {
            // Database.Checkpoint();
            Database.Dispose();
            Database = null;
        }
    }

    private static LiteDatabase CreateDatabase()
    {
        var dbPath = Path.Combine(Application.persistentDataPath, "Cytoid.db");
        var dbBackupPath = Path.Combine(Application.persistentDataPath, "Cytoid.db.bak");
        var db = new LiteDatabase(
            new ConnectionString
            {
                Filename = dbPath,
                // Password = SecuredConstants.DbSecret,
                Connection = ConnectionType.Direct
            }
        );
        if (db.GetCollection<LocalPlayerSettings>("settings").FindOne(Query.All()) != null)
        {
            // Make a backup
            File.Copy(dbPath, dbBackupPath, true);
            Debug.Log("Database backup complete.");
        }
        else
        {
            // Is there a backup?
            if (File.Exists(Path.Combine(Application.persistentDataPath, "Cytoid.db.bak")))
            {
                File.Copy(dbBackupPath, dbPath, true);
                Debug.Log("Database rollback complete.");
                
                var bakDb = new LiteDatabase(
                    new ConnectionString
                    {
                        Filename = dbPath,
                        // Password = SecuredConstants.DbSecret,
                        Connection = ConnectionType.Direct
                    }
                );
                if (bakDb.GetCollection<LocalPlayerSettings>("settings").FindOne(Query.All()) != null)
                {
                    db.Dispose();
                    db = bakDb;
                }
                else
                {
                    bakDb.Dispose();
                }
            }
        }
        return db;
    }

    public static Distribution Distribution
    {
        get
        {
            switch (Application.identifier)
            {
                case "me.tigerhix.cytoid": return Distribution.Global;
                case "me.tigerhix.cytoid.cn": return Distribution.China;
            }
            throw new InvalidOperationException();
        }
    }
}

public enum Distribution
{
    Global, China
}

public class OfflineModeToggleEvent : UnityEvent<bool>
{
}

public class GameErrorState
{
    public string Message;
    public Exception Exception;
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
            if (Context.ScreenManager != null)
            {
                GUILayout.Label("Screen history:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (var intent in Context.ScreenManager.History)
                {
                    GUILayout.Label(intent.ScreenId);
                }
                GUILayout.Label("");
            }
            
            if (Context.AssetMemory != null)
            {
                GUILayout.Label("Asset memory usage:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (AssetTag tag in Enum.GetValues(typeof(AssetTag)))
                {
                    GUILayout.Label(
                        $"{tag}: {Context.AssetMemory.CountTagUsage(tag)}/{(Context.AssetMemory.GetTagLimit(tag) > 0 ? Context.AssetMemory.GetTagLimit(tag).ToString() : "∞")}");
                }
                GUILayout.Label("");
            }
            
            if (Context.BundleManager != null)
            {
                GUILayout.Label("Loaded bundles:", new GUIStyle().Also(it => it.fontStyle = FontStyle.Bold));
                foreach (var pair in Context.BundleManager.LoadedBundles)
                {
                    GUILayout.Label($"{pair.Key}: {pair.Value.RefCount}");
                }
                GUILayout.Label("");
            }

            if (GUILayout.Button("Unload unused assets"))
            {
                Resources.UnloadUnusedAssets();
            }

            if (GUILayout.Button("Toggle offline mode"))
            {
                Context.SetOffline(!Context.IsOffline());
            }

            if (GUILayout.Button("Make API work/not work"))
            {
                Context.MockApiUrl = Context.MockApiUrl == null ? "https://servicessss.cytoid.io" : null;
            }

            if (GUILayout.Button("Reward Overlay"))
            {
                RewardOverlay.Show(new List<OnlinePlayerStateChange.Reward>
                {
                    JsonConvert.DeserializeObject<OnlinePlayerStateChange.Reward>(@"{""type"":""character"",""value"":{""illustrator"":{""name"":""しがらき"",""url"":""https://www.pixiv.net/en/users/1004274""},""designer"":{""name"":"""",""url"":""""},""name"":""Mafumafu"",""description"":""何でも屋です。"",""_id"":""5e6f90dcdab3462655fb93a4"",""levelId"":4101,""asset"":""Mafu"",""tachieAsset"":""MafuTachie"",""id"":""5e6f90dcdab3462655fb93a4""}}"),
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "level",
                        onlineLevelValue = new Lazy<OnlineLevel>(() => MockData.OnlineLevel)
                    },
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f38e922fe1dfb383c7b93fa"",""uid"":""sora-1"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora1.jpg""},""type"":""event"",""id"":""5f38e922fe1dfb383c7b93fa""}"))
                    },
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f390f2cfe1dfb383c7b93fb"",""uid"":""sora-2"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora2.jpg"",""overrides"":[""sora-1""]},""type"":""event"",""id"":""5f390f2cfe1dfb383c7b93fb""}"))
                    },
                    new OnlinePlayerStateChange.Reward
                    {
                        type = "badge",
                        badgeValue = new Lazy<Badge>(() => JsonConvert.DeserializeObject<Badge>(@"{""_id"":""5f390f57fe1dfb383c7b93fc"",""uid"":""sora-3"",""listed"":false,""metadata"":{""imageUrl"":""http://artifacts.cytoid.io/badges/sora3.jpg"",""overrides"":[""sora-1"",""sora-2""]},""type"":""event"",""id"":""5f390f57fe1dfb383c7b93fc""}"))
                    },
                });
            }
            
            if (GUILayout.Button("Update NavigationBackdrop Blur"))
            {
                NavigationBackdrop.Instance.UpdateBlur();
            }

            EditorUtility.SetDirty(target);
        }
    }
}
#endif