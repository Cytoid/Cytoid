using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using LunarConsolePlugin;
using Newtonsoft.Json;
using Polyglot;
using Proyecto26;
using Tayx.Graphy;
using Cysharp.Threading.Tasks;
using LiteDB;
using Sentry;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class Context : SingletonMonoBehavior<Context>
{
    public int forceUploadScore = -1;
    public float forceUploadAccuracy = -1f;

    public const string VersionIdentifier = "2.1.2";
    public const string VersionName = "2.1.2";
    public const int VersionCode = 118;

    public static string MockApiUrl;

    public static CdnRegion CdnRegion => Player.Settings.CdnRegion;
    public static string ApiUrl => Application.isEditor ? (MockApiUrl ?? CdnRegion.GetApiUrl()) : CdnRegion.GetApiUrl();
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
    private static bool ShouldSnapshotDatabase = false;

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
        // TODO: This pops up on startup on iOS, lol
        /*if (SceneManager.GetActiveScene().name == "Navigation")
        {
            Resources.UnloadUnusedAssets();
            
            AudioManager.Get("ActionError").Play(ignoreDsp: true);
            LoopAudioPlayer.Instance.StopAudio(0);
            Dialog.PromptAlert("DIALOG_LOW_MEMORY".Get());
        }*/
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
        if (Application.platform == RuntimePlatform.Android)
        {
            // Get Android version
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                AndroidVersionCode = version.GetStatic<int>("SDK_INT");
                print("Android version code: " + AndroidVersionCode);
            }
        }
        ConsoleManager.enable(); // Enable startup debug

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
        Localization.Instance.SelectLanguage((Language)Player.Settings.Language);
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
# if UNITY_EDITOR
                var shouldLaunchCalibrationGuide = false;
# else
                var shouldLaunchCalibrationGuide = Player.ShouldTrigger(StringKey.FirstLaunch, false);
# endif
                if (shouldLaunchCalibrationGuide)
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

        if (Player.Settings.UseDeveloperConsole)
        {
            ConsoleManager.enable();
        }
        else
        {
            ConsoleManager.disable();
        }
        ShouldSnapshotDatabase = true;
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
        else if (Distribution == Distribution.TapTap)
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
                                        (int)GameState.Score, GameState.Accuracy);
                                    GameState.Level.SaveRecord();
                                }

                                Player.ClearTrigger(StringKey.FirstLaunch);
                                InitializationState.FirstLaunchPhase = FirstLaunchPhase.Completed;

                                await UniTask.Delay(TimeSpan.FromSeconds(1f));

                                // Clear navigation history before entering tutorial
                                ScreenManager.History = new Stack<Intent>();

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
        if (Application.isEditor || Application.platform != RuntimePlatform.IPhonePlayer) return;
        if (!(menu ? Player.Settings.MenuTapticFeedback : Player.Settings.HitTapticFeedback)) return;

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
        if (ShouldSnapshotDatabase && !hasFocus && Database != null)
        {
            // Database.Checkpoint();
            Database.Dispose();
            Database = null;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (ShouldSnapshotDatabase && pauseStatus && Database != null)
        {
            // Database.Checkpoint();
            Database.Dispose();
            Database = null;
        }
    }

    private static LiteDatabase CreateDatabase()
    {
        var dbPath = Path.Combine(Application.persistentDataPath, "Cytoid.db");
        LiteDatabase NewDatabase()
        {
            return new LiteDatabase(
                new ConnectionString
                {
                    Filename = dbPath,
                    Connection = ConnectionType.Direct
                }
            );
        }

        LiteDatabase db = null;
        try
        {
            db = NewDatabase();
        }
        catch (Exception e)
        {
            Debug.Log(e);
            Debug.Log($"Could not read {dbPath}");
        }

        if (db?.GetCollection<LocalPlayerSettings>("settings").FindOne(Query.All()) != null)
        {
            // Is there too many backups already?
            var snapshots = Directory.GetFiles(Application.persistentDataPath, ".snapshot-*").ToList();
            snapshots.Sort(string.CompareOrdinal);
            if (snapshots.Count > 5)
            {
                snapshots.Take(snapshots.Count - 5).ForEach(File.Delete);
                Debug.Log($"Removed {snapshots.Count - 5} obsolete snapshots");
            }

            // Make a backup
            File.Copy(dbPath, Path.Combine(Application.persistentDataPath, ".snapshot-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), true);
            Debug.Log("Database snapshot complete");
        }
        else
        {
            // Is there backups?
            var snapshots = Directory.GetFiles(Application.persistentDataPath, ".snapshot-*").ToList();
            if (snapshots.Count == 0) return db ?? NewDatabase();

            db?.Dispose();
            File.Delete(dbPath);
            snapshots.Sort((a, b) => string.CompareOrdinal(b, a));

            string rolledBackFrom = null;
            foreach (var snapshotPath in snapshots)
            {
                Debug.Log($"Rolling back from {snapshotPath}");
                File.Copy(snapshotPath, dbPath, true);
                LiteDatabase snapshotDb = null;
                try
                {
                    snapshotDb = new LiteDatabase(
                        new ConnectionString
                        {
                            Filename = dbPath,
                            Connection = ConnectionType.Direct
                        }
                    );
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    Debug.Log($"Could not read {snapshotPath}");
                }

                if (snapshotDb?.GetCollection<LocalPlayerSettings>("settings").FindOne(Query.All()) != null)
                {
                    db = snapshotDb;
                    Debug.Log("Rollback success");
                    rolledBackFrom = snapshotPath;
                    break;
                }

                Debug.Log($"Could not roll back from {snapshotPath}");
                snapshotDb?.Dispose();
                File.Delete(snapshotPath);
            }

            if (rolledBackFrom == null)
            {
                return NewDatabase();
            }
        }

        return db;
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

public class OfflineModeToggleEvent : UnityEvent<bool>
{
}

public class GameErrorState
{
    public string Message;
    public Exception Exception;
}
