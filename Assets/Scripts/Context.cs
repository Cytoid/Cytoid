using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using LiteDB;
using LunarConsolePlugin;
using MoreMountains.NiceVibrations;
using Newtonsoft.Json;
using Polyglot;
using Proyecto26;
using Tayx.Graphy;
using UniRx.Async;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Context : SingletonMonoBehavior<Context>
{
    public const string VersionName = "2.0.0 Beta 2.0";
    public const string VersionString = "2.0.0";
    
    public static string MockApiUrl;

    public static string ApiUrl => MockApiUrl ?? Player.Settings.CdnRegion.GetApiUrl();
    public static string WebsiteUrl => Player.Settings.CdnRegion.GetWebsiteUrl();
    public static string BundleRemoteBaseUrl => Player.Settings.CdnRegion.GetBundleRemoteBaseUrl();
    public static string StoreUrl => Player.Settings.CdnRegion.GetStoreUrl();
    
    public static string BundleRemoteFullUrl
    {
        get
        {
#if UNITY_ANDROID
                return $"{BundleRemoteBaseUrl}/bundles/Android/";
#elif UNITY_IOS
                return $"{AddressableRemoteBaseUrl}/bundles/iOS/";
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
    public const int CollectionThumbnailHeight = 192;

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

    public static readonly Player Player = new Player();
    public static readonly OnlinePlayer OnlinePlayer = new OnlinePlayer();

    public static GameErrorState GameErrorState;

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

        if (Application.platform == RuntimePlatform.Android)
        {
            // Get Android version
            using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
                AndroidVersionCode = version.GetStatic<int>("SDK_INT");
            }
        }
        
        // Initialize fonts
        FontManager.LoadFonts();

        try
        {
            var timer = new BenchmarkTimer("LiteDB");
            Database = new LiteDatabase(
                new ConnectionString
                {
                    Filename = Path.Combine(Application.persistentDataPath, "Cytoid.db"),
                    // Password = SecuredConstants.DbSecret,
                    Connection = Application.isEditor ? ConnectionType.Shared : ConnectionType.Direct
                }
            );
            timer.Time();
            Database.Checkpoint();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Haptic(HapticTypes.Failure, true);
            Dialog.Instantiate().Also(it =>
            {
                it.UseNegativeButton = false;
                it.UsePositiveButton = false;
                it.Message =
                    "DIALOG_CRITICAL_ERROR_COULD_NOT_START_GAME_REASON_X".Get("DIALOG_CRITICAL_ERROR_REASON_DATABASE"
                        .Get());
            }).Open();
            return;
        }

        // LiteDB warm-up
        Library.Initialize();
        
        // Load settings
        Player.Initialize();
        
        // Check region
        await DetectServerCdn();

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
            AudioSettings.Reset(audioConfig);
        }

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
        
        await BundleManager.Initialize();

        switch (SceneManager.GetActiveScene().name)
        {
            case "Navigation":
                Debug.Log("Initializing character asset");
                var timer = new BenchmarkTimer("Character");
                if (await CharacterManager.SetActiveCharacter(CharacterManager.SelectedCharacterId) == null)
                {
                    // Reset to default
                    CharacterManager.SelectedCharacterId = null;
                    await CharacterManager.SetActiveCharacter(CharacterManager.SelectedCharacterId);
                }
                timer.Time();
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

    public async UniTask DetectServerCdn()
    {
        if (!Player.ShouldOneShot("Detect Server CDN")) return;

        Debug.Log("Detecting server CDN");

        if (Application.identifier == "me.tigerhix.cytoid")
        {
            var resolved = false;
            var startTime = DateTimeOffset.Now;

            RestClient.Get<RegionInfo>(new RequestHelper
            {
                Uri = "https://services.cytoid.io/ping",
                Timeout = 3,
                EnableDebug = true
            }).Then(it =>
            {
                Player.Settings.CdnRegion = it.countryCode == "CN" ? CdnRegion.MainlandChina : CdnRegion.International;
                resolved = true;
            }).CatchRequestError(it =>
            {
                Debug.LogWarning(it);
                RestClient.Get(new RequestHelper
                {
                    Uri = "https://api.cytoid.cn/ping",
                    Timeout = 3,
                    EnableDebug = true
                }).Then(x => { Player.Settings.CdnRegion = CdnRegion.MainlandChina; }).CatchRequestError(x =>
                {
                    Debug.LogWarning(x);
                    Player.ClearOneShot("Detect Server CDN");
                }).Finally(() => resolved = true);
            }).Finally(() => resolved = true);
            await UniTask.WaitUntil(() => resolved || DateTimeOffset.Now - startTime > TimeSpan.FromSeconds(10));
            if (!resolved)
            {
                Player.Settings.CdnRegion = CdnRegion.MainlandChina;
            }
        } 
        else if (Application.identifier == "me.tigerhix.cytoid.cn")
        {
            Player.Settings.CdnRegion = CdnRegion.MainlandChina;
        }

        Debug.Log($"Detected: {Player.Settings.CdnRegion}");
    }

    public static void OnPreSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            Input.gyro.enabled = false;
            // Save history
            navigationScreenHistory = new Stack<string>(ScreenManager.History);
        }
    }

    public static async void OnPostSceneChanged(string prev, string next)
    {
        if (prev == "Navigation" && next == "Game")
        {
            OnlinePlayer.IsAuthenticating = false;
            CharacterManager.UnloadActiveCharacter();
            BundleManager.ReleaseAll();
        }

        if (prev == "Game" && next == "Navigation")
        {
            Input.gyro.enabled = true;
            AudioManager.Initialize();

            // Wait until character is loaded
            await CharacterManager.SetSelectedCharacterActive();

            UpdateGraphicsQuality();

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
            
            if (GameErrorState != null)
            {
                Dialog.PromptAlert(GameErrorState.Message);
                Debug.LogError(GameErrorState.Exception);
                GameErrorState = null;
            }
        }

        FontManager.UpdateSceneTexts();
        Database.Checkpoint();
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
        switch (Player.Settings.GraphicsQuality)
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

        if (MainTranslucentImage.Instance != null)
        {
            MainTranslucentImage.Instance.WillUpdateTranslucentImage();
            MainTranslucentImage.Instance.SetUseOpaqueBackground(Player.Settings.GraphicsQuality <= GraphicsQuality.Medium);
            MainTranslucentImage.Static = Player.Settings.GraphicsQuality != GraphicsQuality.Ultra;
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
        if (!hasFocus) Database.Checkpoint();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) Database.Checkpoint();
    }
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
                        onlineLevelValue = new Lazy<OnlineLevel>(() => JsonConvert.DeserializeObject<OnlineLevel>(@"{""id"":2438,""version"":1,""uid"":""wz.angel"",""title"":""アンヘル"",""metadata"":{""title"":""アンヘル"",""artist"":{""url"":""https://www.nicovideo.jp/watch/sm34871494"",""name"":""かいりきベア"",""localized_name"":""Kairiki Bear""},""charter"":{""name"":""Qiram & Qurox""},""illustrator"":{""url"":""https://www.pixiv.net/member_illust.php?mode=medium&illust_id=73953599"",""name"":""のう""},""title_localized"":""Ángel""},""duration"":220.056,""size"":32027548,""description"":""Sub for CCC 11. Played around with scanline manipulation and a \\\""fuck with the player\\\"" theme in the higher difficulties.\n\n\\\\[Hard] [しんたろ (Shintaro)](https://www.youtube.com/watch?v=t-r-VNUoGdA)\n\n\\\\[Ex] [宮下遊 (Yuu Miyashita)](https://www.youtube.com/watch?v=NHZAxCoy_t8)\n\n---\n\n\\\""What do you want?\\\"" Firi quivered, backing into the door. She clutched the doll to her chest and waited for her eyes to adjust. \\\""You aren't supposed to be here today. Why are you here?\\\""\n\nA pair of giggles came from the dark room. She saw him on the right, black eyes radiating in the dark. He followed her movements lazily and seemed to enjoy her fear. The other seemed to be on her left, though she couldn't see her in the dark corner. \\\""Why,\\\"" Quro said, \\\""We just wanted to spend some time with our dear loving sister, that's all.\\\""\n\n\\\""Burn in hell,\\\"" she growled. She reached for the doorknob.\n\n\\\""Leaving so soon?\\\"" There, from the fireplace. \\\""We were waiting so long for you to arrive. Please.\\\"" A gust forced the door closed as she tried to open it. A chair scratched its way across the floor. \\\""Have a seat, love.\\\"" Firi crossed her arms and sat down at the door. Quro sighed. \\\""Stubborn brat,\\\"" he muttered. He glanced towards his sister, who called a flame in the furnace. A slender shadow sat in its flickering light. \\\""Is this more comfortable?\\\"" No response. \\\""*Savrel*, fine.\\\"" The twins got off their perches and climbed onto the worn couch.\n\nFiri reluctantly moved towards the fire. \\\""You two couldn't have a bit more respect towards your elders, huh?\\\""\n\n\\\""Five years is nothing in the sands of time,\\\"" Qira said as she reclined on her half of the chair. \\\""What's that?\\\"" she asked, pointing at the doll.\n\n\\\""Nothing important,\\\"" she muttered. \\\""So wha-\\\""\n\n\\\""Can I see it?\\\""\n\n\\\""No.\\\"" She hugged it closer, but something wrenched it out of hands. Qira smiled as it fell onto her lap. \\\""Now let's begin, darling,\\\"" she cooed, stroking its head.\n\n\\\""Fuck you. Give it back.\\\""\n\n\\\""So,\\\"" she said, gently tugging at its hair. \\\""Where is the mirror?\\\""\n\n\\\""If you don't know already, I'm not telling.\\\"" She lunged at the girl but she flapped away just in time.\n\n\\\""Omnipotence can only go so far,\\\"" she sighed as she settled above the fireplace once more. \\\""Mother really didn't want us into her head, or,\\\"" she looked down at her with disain, \\\""her favorite child either.\\\"" The grate squeaked open. \\\""How long have you been working on this, love?\\\""\n\n\\\""Stop.\\\""\n\n\\\""Oh, I see. An entire week already?\\\"" She brought it closer to her face as if studying it. \\\""It looks kinda cute. Bit messy, but one can expect that for your age.\\\"" Says someone who's practically a toddler, Firi thought. \\\""What's his name, I wonder?\\\""\n\n\\\""I haven't given him one.\\\""\n\n\\\""Oh, a him? Let's see, how about...\\\"" her eyes unfocused, just for a second. Firi reached in her pocket for her needle, but was surprised to find nothing there. She whirled around to see Quro sticking them into the wall. \\\""You looking for these?\\\"" he teased. She snatched them back from him without protest.\n\n\\\""Tas!\\\"" came Qira's voice from the fireplace. \\\""Bet he likes fire, huh?\\\""\n\n\\\""Give him back!\\\"" She flung a needle in her direction, ramming into the wall by her ear with a quiver. Qira rambled on as if nothing happened.\n\n\\\""Looks a bit too clean for someone as wild as him. He could use a pass through the smoke, don't you think?\\\""\n\n\\\""You put him in the fire and your wing goes in too,\\\"" she snarled.\n\n\\\""Now, that would be pretty unpleasant, wouldn't it? Tell you what, I'll give Tas back unharmed if you tell us where the mirror is. That sound fair?\\\""\n\n\\\""I'm not telling.\\\""\n\nQiram's voice turned cold. \\\""Are you sure about that, Anafira?\\\"" She plucked the needle out of the wall. \\\""You have powers unlike anyone else in this side of the world - lamros - this entire world, perhaps.\\\"" She put the needle under a button like a lever. \\\""This doll is more than a plaything. This is a life somewhere outside, living a wonderful and peaceful life.\\\"" She lifted the needle slowly. Ana cringed as she put more strain on the thread. \\\""We don't want them to have a horrible accident, would we? I will ask again. Where is the mirror?\\\""\n\nShe took a deep breath. \\\""I don't-\\\""\n\nClink. The button fell to the ground. The air in her chest left her, making her gasp for breath. There was a sound behind her - Quro, leaning against the wall. He felt it too.\n\n\\\""Oh dear. The poor thing is blind. Where was I? Oh, right. Where is the mirror?\\\"" \n\n\\\""Never.\\\""\n\nClink. She fell on the floor gasping. The fire in the furnace roared with new life, as if it was hungry.\n\n\\\""Let's try something else. Tell me where the mirror is. Or Tas burns.\\\""\n\n\\\""Then let him,\\\"" she sputtered.\n\n\\\""Is the secret of such an irrelevant object worth the life of a boy?\\\""\n\n\\\""If it keeps a sadist like you from getting to it, I would gladly let him die.\\\""\n\n\\\""Then I will ask you one last time.\\\"" Qiram lifted her tiny hand, pulling Firi towards her. She stared into her with eyes roiling with shadows behind them. Whatever happens, she cannot get to the mirror. \\\""Where is it?\\\""\n\n\\\""Enough.\\\""\n\nQira dropped her in surprise. The fire died out, leaving a lone beam of light from the afternoon sun leaking through a hole in the ceiling. Quro stood shakily, his black eyes squinting in pain.\n\n\\\""She's not going to give us any answers, sister. Let him live.\\\"" Qira stared at him emotionlessly, something wild thrashing in her eyes. She looked at the doll in disgust and tossed it to Firi. She scrambled for it and hugged it fiercely. \\\""We'll find it eventually. No need to rush.\\\""\n\n\\\""Patience doesn't suit you, brother,\\\"" she seethed. She looked down at Firi, balled up on the floor. \\\""We'll let you go this time. Perhaps you'll have a different opinion when we meet again.\\\"" She spread her wings and shot into the sky.\n\nThe two watched her exit. A moment of silence passed.\n\nShe glanced at Qurox. \\\""Well? You going after her?\\\""\n\nHe hesitated for a moment. Then he hung his head. \\\""I'm sorry about that. Qira shut me out. I didn't know what she wanted to do.\\\""\n\n\\\""Get a better sense of control over her, eh?\\\"" she said unenthusiastically. \n\n\\\""I'll do my best.\\\"" He motioned for the doll. Firi thought about it, then gave it to him. While he looked at it, Firi looked at her little brother's eyes. His too had some entity behind it, though his flowed in a sorrowful wave. \\\""I'm sorry about Tas. One day I'll have to visit him. To make up for not doing anything to help.\\\"" He closed his eyes and whispered something, a hand on its face. He gave it back to her. \\\""Take care of him, will you?\\\"" And he took off into the blazing sky.\n\nAnafira looked at the remains of her doll. Two holes bled where the buttons once were, a faint sound of sobbing coming from far away. She closed her eyes and hugged him tight, imagining a little red haired boy curled up, sobbing while his mother tried desperately to help him. He was covering his eyes, blood soaking his hands.\n\nThe little girl picked up the needle and reached for some thread in her bag. Tas needed patching up."",""censored"":null,""tags"":[""wz"",""storyboard"",""rock contest"",""rock"",""vocaloid"",""kairiki bear"",""angel"",""meika mikoto"",""shintaro"",""yuu miyashita""],""category"":[""featured""],""ownerId"":""b284bf94-4e93-44a5-b5ee-b6af29dbf18c"",""creationDate"":""2019-06-14T03:23:08.000Z"",""modificationDate"":""2019-08-11T23:07:14.000Z"",""charts"":[{""id"":3492,""name"":null,""type"":""easy"",""difficulty"":7,""notesCount"":1037},{""id"":3493,""name"":null,""type"":""hard"",""difficulty"":13,""notesCount"":1319},{""id"":3494,""name"":null,""type"":""extreme"",""difficulty"":14,""notesCount"":1448}],""owner"":{""id"":""b284bf94-4e93-44a5-b5ee-b6af29dbf18c"",""uid"":""wandererzariq"",""name"":""Wanderer Zariq"",""role"":""moderator"",""avatar"":{""original"":""https://assets.cytoid.io/avatar/bmHmeH9y8IvVS2bahdcWJiHxtDG8b0PuSoSDFujDW78ii7N9fhiA0cMclgOVPHEsh4"",""small"":""https://images.cytoid.io/avatar/bmHmeH9y8IvVS2bahdcWJiHxtDG8b0PuSoSDFujDW78ii7N9fhiA0cMclgOVPHEsh4?h=64&w=64"",""large"":""https://images.cytoid.io/avatar/bmHmeH9y8IvVS2bahdcWJiHxtDG8b0PuSoSDFujDW78ii7N9fhiA0cMclgOVPHEsh4?h=256&w=256""}},""state"":""PUBLIC"",""cover"":{""original"":""https://assets.cytoid.io/levels/bundles/lvplBPOtYnfJEZ6xsplPC8x0wRHvaDocBIXjAfVyYszxoPHoNNKwPB0NcJUTa6H8/background.jpg"",""thumbnail"":""https://images.cytoid.io/levels/bundles/lvplBPOtYnfJEZ6xsplPC8x0wRHvaDocBIXjAfVyYszxoPHoNNKwPB0NcJUTa6H8/background.jpg?h=360&w=576"",""cover"":""https://images.cytoid.io/levels/bundles/lvplBPOtYnfJEZ6xsplPC8x0wRHvaDocBIXjAfVyYszxoPHoNNKwPB0NcJUTa6H8/background.jpg?h=800&w=1280"",""stripe"":""https://images.cytoid.io/levels/bundles/lvplBPOtYnfJEZ6xsplPC8x0wRHvaDocBIXjAfVyYszxoPHoNNKwPB0NcJUTa6H8/background.jpg?h=800&w=768""},""music"":""https://assets.cytoid.io/levels/bundles/lvplBPOtYnfJEZ6xsplPC8x0wRHvaDocBIXjAfVyYszxoPHoNNKwPB0NcJUTa6H8/music.mp3"",""musicPreview"":""https://assets.cytoid.io/levels/bundles/lvplBPOtYnfJEZ6xsplPC8x0wRHvaDocBIXjAfVyYszxoPHoNNKwPB0NcJUTa6H8/preview.mp3""}"))
                    }
                });
            }

            EditorUtility.SetDirty(target);
        }
    }
}
#endif