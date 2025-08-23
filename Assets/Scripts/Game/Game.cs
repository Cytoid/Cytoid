using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Cytoid.Storyboard;
using LiteDB;
using Polyglot;
using Sentry;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.Networking;

public class Game : MonoBehaviour
{
    public new Camera camera;
    public GameObject contentParent;
    public GameObject levelInfoParent;
    public GameObject modHolderParent;
    public EffectController effectController;
    public InputController inputController;

    public bool IsLoaded { get; protected set; }

    public GameConfig Config { get; protected set; }
    public GameState State { get; protected set; }
    public GameRenderer Renderer { get; protected set; }

    public Level Level { get; protected set; }

    public Difficulty Difficulty { get; protected set; }
    public Chart Chart { get; protected set; }

    public Cytoid.Storyboard.Storyboard Storyboard { get; protected set; }

    public string StoryboardPath { get; protected set; }

    public float Time { get; protected set; }
    public float MusicLength { get; protected set; }
    public float ChartLength { get; protected set; }
    public float GameStartedOrResumedTimestamp { get; protected set; }
    public double MusicStartedTimestamp { get; protected set; } // When did the music start playing?
    public float MusicProgress { get; protected set; }
    public float ChartProgress { get; protected set; }
    public float UnpauseCountdown { get; protected set; }

    public bool ResynchronizeChartOnNextFrame { get; set; }

    public int ContentLayer { get; private set; }

    public ObjectPool ObjectPool { get; set; }

    public SortedDictionary<int, Note> SpawnedNotes => ObjectPool.SpawnedNotes;

    public string EditorDefaultLevelDirectory = "yy.badapple";
    public float EditorMusicInitialPosition;
    public bool EditorForceAutoMod;
    public GameMode EditorGameMode = GameMode.Unspecified;
    public bool EditorImmediatelyComplete;
    public float EditorCompletionDelay;
    public bool EditorImmediatelyCompleteFail;

    public AudioManager.Controller Music { get; protected set; }

    public List<UniTask> BeforeStartTasks { get; protected set; } = new List<UniTask>();
    public List<UniTask> BeforeExitTasks { get; protected set; } = new List<UniTask>();

    public GameEvent onGameReadyToLoad = new GameEvent();
    public GameEvent onGameLoaded = new GameEvent();
    public GameEvent onGameStarted = new GameEvent();
    public GameEvent onGameUpdate = new GameEvent();
    public GameEvent onGameLateUpdate = new GameEvent();
    public NoteJudgeEvent onNoteJudged = new NoteJudgeEvent();
    public GameEvent onGamePaused = new GameEvent();
    public GameEvent onGameWillUnpause = new GameEvent();
    public GameEvent onGameUnpaused = new GameEvent();
    public GameEvent onGameFailed = new GameEvent();
    public GameEvent onGameCompleted = new GameEvent();
    public GameEvent onGameBeforeExit = new GameEvent();
    public GameEvent onGameAborted = new GameEvent();
    public GameEvent onGameRetried = new GameEvent();
    public NoteEvent onNoteClear = new NoteEvent();
    public GameEvent onGameSpeedUp = new GameEvent();
    public GameEvent onGameSpeedDown = new GameEvent();
    public GameEvent onGameDisposed = new GameEvent();
    public GameEvent onTopBoundaryBounded = new GameEvent();
    public GameEvent onBottomBoundaryBounded = new GameEvent();

    private GlobalCalibrator globalCalibrator;

    protected virtual void Awake()
    {
        ContentLayer = LayerMask.NameToLayer("Content");
        Renderer = new GameRenderer(this);
#if !UNITY_EDITOR
        EditorMusicInitialPosition = 0;
        EditorGameMode = GameMode.Unspecified;
        EditorForceAutoMod = false;
        EditorImmediatelyComplete = false;
        EditorImmediatelyCompleteFail = false;
#endif
    }

    protected virtual async void Start()
    {
        await UniTask.WaitUntil(() => Context.IsInitialized);
        try
        {
            await Initialize();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            // Not editor
            if (Context.SelectedGameMode != GameMode.Unspecified)
            {
                Context.GameErrorState = new GameErrorState {Message = "DIALOG_LEVEL_LOAD_ERROR".Get(), Exception = e};

                await UniTask.Delay(TimeSpan.FromSeconds(3));

                var sceneLoader = new SceneLoader("Navigation");
                sceneLoader.Load();
                var transitioned = false;
                Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
                    onFinished: screen => transitioned = true);
                await UniTask.WaitUntil(() => transitioned && sceneLoader.IsLoaded);
                sceneLoader.Activate();
            }
        }
    }

    public async UniTask Initialize(bool startAutomatically = true)
    {
        ObjectPool = new ObjectPool(this);

        // Decide game mode
        var mode = Context.SelectedGameMode;
        if (mode == GameMode.Unspecified)
        {
            if (EditorGameMode != GameMode.Unspecified)
            {
                mode = EditorGameMode;
            }
            else
            {
                throw new Exception("Game mode not specified");
            }
        }

        if (mode == GameMode.Tier)
        {
            var tierState = Context.TierState;
            if (tierState == null)
            {
                await Context.LevelManager.LoadLevelsOfType(LevelType.Tier);
                tierState = new TierState(MockData.Season.tiers[0]);
                Context.TierState = tierState;
            }

            if (tierState.IsFailed || tierState.IsCompleted)
            {
                // Reset tier state
                tierState = new TierState(tierState.Tier);
                Context.TierState = tierState;
            }

            tierState.CurrentStageIndex++;

            Level = tierState.Tier.Meta.stages[Context.TierState.CurrentStageIndex].ToLevel(LevelType.Tier);
            Difficulty = Difficulty.Parse(Level.Meta.charts.Last().type);
        }
        else if (mode == GameMode.GlobalCalibration)
        {
            // Load global calibration level
            Level = await Context.LevelManager.LoadOrInstallBuiltInLevel(BuiltInData.GlobalCalibrationModeLevelId,
                LevelType.Temp);

            Difficulty = Level.Meta.GetEasiestDifficulty();

            // Initialize global calibrator
            globalCalibrator = new GlobalCalibrator(this);
        }
        else
        {
            if (Context.SelectedLevel == null && Application.isEditor)
            {
                // Load test level
                await Context.LevelManager.LoadFromMetadataFiles(LevelType.User, new List<string>
                {
                    $"{Context.UserDataPath}/{EditorDefaultLevelDirectory}/level.json"
                });
                Context.SelectedLevel = Context.LevelManager.LoadedLocalLevels.Values.First();
                Context.SelectedDifficulty = Context.SelectedLevel.Meta.GetHardestDifficulty();
            }

            Level = Context.SelectedLevel;
            Difficulty = Context.SelectedDifficulty;
        }

        onGameReadyToLoad.Invoke(this);

        await Resources.UnloadUnusedAssets();
        
        SentrySdk.AddBreadcrumb(
            "Game preload",
            data: new Dictionary<string, string>
            {
                { "Level", Level.Id },
                { "Difficulty", Difficulty.ToString() }
            },
            level: BreadcrumbLevel.Info);

        // Load chart
        print("Loading chart");
        var chartMeta = Level.Meta.GetChartSection(Difficulty.Id);
        var chartPath = "file://" + Level.Path + chartMeta.path;
        string chartText;
        using (var request = UnityWebRequest.Get(chartPath))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                throw new Exception($"Failed to download chart from {chartPath}");
            }

            chartText = Encoding.UTF8.GetString(request.downloadHandler.data);
        }

        var mods = new HashSet<Mod>(Context.SelectedMods);
        if (Application.isEditor && EditorForceAutoMod)
        {
            mods.Add(Mod.Auto);
        }
        if (mode == GameMode.GlobalCalibration)
        {
            mods.Clear();   
        }

        Chart = new Chart(
            chartText,
            mods.Contains(Mod.FlipX) || mods.Contains(Mod.FlipAll),
            mods.Contains(Mod.FlipY) || mods.Contains(Mod.FlipAll),
            true,
            Context.Player.Settings.UseExperimentalNoteAr,
            mods.Contains(Mod.Fast) ? 1.5f : (mods.Contains(Mod.Slow) ? 0.75f : 1),
            camera.orthographicSize
        );
        ChartLength = Chart.Model.note_list.Max(it => it.end_time);
        foreach (var type in (NoteType[]) Enum.GetValues(typeof(NoteType)))
        {
            ObjectPool.UpdateNoteObjectCount(type, Chart.MaxSamePageNoteCountByType[type] * 3);
        }

        // Load audio
        print("Loading audio");
        AudioListener.pause = false;

        if (Context.AudioManager == null) await UniTask.WaitUntil(() => Context.AudioManager != null);
        Context.AudioManager.Initialize();
        var audioPath = "file://" + Level.Path + Level.Meta.GetMusicPath(Difficulty.Id);
        var loader = new AudioClipLoader(audioPath);
        await loader.Load();
        if (loader.Error != null)
        {
            Debug.LogError(loader.Error);
            throw new Exception($"Failed to download audio from {audioPath}");
        }

        Music = Context.AudioManager.Load("Level", loader.AudioClip, false, false, true);
        MusicLength = Music.Length;

        // Load storyboard
        string sbFile = null;
        if (chartMeta.storyboard != null)
        {
            if (chartMeta.storyboard.localizations != null)
            {
                Debug.Log("Using localized storyboard");
                sbFile = chartMeta.storyboard.localizations.GetOrDefault(Localization.Instance.SelectedLanguage.ToString());
                if (sbFile == null)
                {
                    Debug.Log($"Localized storyboard for language {Localization.Instance.SelectedLanguage.ToString()} does not exist");
                }
            }
            if (sbFile == null)
            {
                sbFile = chartMeta.storyboard.path;
            }
        }
        if (sbFile == null)
        {
            sbFile = "storyboard.json";
        }

        StoryboardPath = Level.Path + sbFile;
        
        // if (Level.Id == "io.cytoid.searchingforyourlove")
        // {
        //     SentrySdk.CaptureMessage(
        //         $"Language: {Localization.Instance.SelectedLanguage.ToString()}, Storyboard path: {StoryboardPath}, Exists: {File.Exists(StoryboardPath)}",
        //         SentryLevel.Error);
        // }

        if (File.Exists(StoryboardPath))
        {
            // Initialize storyboard
            // TODO: Why File.ReadAllText() works but not UnityWebRequest?
            // (UnityWebRequest downloaded text could not be parsed by Newtonsoft.Json)
            try
            {
                var storyboardText = File.ReadAllText(StoryboardPath);
                Storyboard = new Cytoid.Storyboard.Storyboard(this, storyboardText);
                Storyboard.Parse();
                await Storyboard.Initialize();
                print($"Loaded storyboard from {StoryboardPath}");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogError("Could not load storyboard.");
            }
        }

        // Load hit sound
        if (Context.Player.Settings.HitSound != "none")
        {
            var resource = await Resources.LoadAsync<AudioClip>("Audio/HitSounds/" + Context.Player.Settings.HitSound);
            Context.AudioManager.Load("HitSound", resource as AudioClip, isResource: true);
        }

        // State & config
        State = new GameState(this, mode, mods);
        Context.GameState = State;

        if (Application.isEditor) Debug.Log("Chart checksum: " + State.ChartChecksum);

        Config = new GameConfig(this);

        // Touch handlers
        if (mode != GameMode.GlobalCalibration && !State.Mods.Contains(Mod.Auto))
        {
            inputController.EnableInput();
        }

        // System config
        Application.targetFrameRate = 120;
        Context.SetAutoRotation(false);

        // Update last played time
        Level.Record.LastPlayedDate = DateTimeOffset.UtcNow;
        Level.SaveRecord();

        // Initialize note pool
        ObjectPool.Initialize();

        IsLoaded = true;
        if (mode != GameMode.GlobalCalibration)
        {
            Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None);
        }

        onGameLoaded.Invoke(this);

        levelInfoParent.transform.RebuildLayout();

        if (startAutomatically)
        {
            StartGame();
        }
    }

    protected virtual async void StartGame()
    {
        await UniTask.WhenAll(BeforeStartTasks);

        MusicStartedTimestamp = Music.PlayScheduled(AudioTrackIndex.Reserved1, 1.0f);

        await UniTask.Delay(TimeSpan.FromSeconds(1));

        if (Application.isEditor && EditorMusicInitialPosition > 0)
        {
            Music.PlaybackTime = EditorMusicInitialPosition;
            MusicStartedTimestamp -= EditorMusicInitialPosition;
        }

        GameStartedOrResumedTimestamp = UnityEngine.Time.realtimeSinceStartup;
        State.IsStarted = true;
        State.IsPlaying = true;
        LayoutStaticizer.Staticize(levelInfoParent.transform);
        LayoutStaticizer.Staticize(modHolderParent.transform);
        onGameStarted.Invoke(this);

        if (Application.isEditor && EditorImmediatelyComplete && State.Mode != GameMode.GlobalCalibration &&
            State.Mode != GameMode.Calibration)
        {
            if (EditorImmediatelyCompleteFail)
            {
                Fail();
            }
            else
            {
                if (EditorCompletionDelay > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(EditorCompletionDelay));
                }

                State.FillTestData(Chart.Model.note_list.Count);
                Complete();
            }
        }
    }

    private double lastDspTime = -1;

    private int ticksBeforeSynchronization = 600;

    protected virtual void SynchronizeMusic()
    {
        // Update current states
        ticksBeforeSynchronization--;
        var resumeElapsedTime = UnityEngine.Time.realtimeSinceStartup - GameStartedOrResumedTimestamp;
        var nowDspTime = AudioSettings.dspTime;
        // Sync: every 600 ticks (=10 seconds) and every tick within the first 0.5 seconds after start/unpause
        if ((ResynchronizeChartOnNextFrame || ticksBeforeSynchronization <= 0 || resumeElapsedTime < 0.5f) &&
            nowDspTime != lastDspTime)
        {
            ResynchronizeChartOnNextFrame = false;
            Time = (float) nowDspTime;
            lastDspTime = nowDspTime;
            ticksBeforeSynchronization = 600;
            Time = (float) (Time - Config.ChartOffset + Chart.MusicOffset - MusicStartedTimestamp);
        }
        else
        {
            Time += UnityEngine.Time.unscaledDeltaTime;
        }
    }

    protected virtual void Update()
    {
        if (!IsLoaded) return;

        Renderer.OnUpdate();

        if (!State.IsPlaying) return;

        if (Input.GetKeyDown(KeyCode.Escape) && !(this is PlayerGame) && State.Mode != GameMode.Tier)
        {
            Pause();
            return;
        }

        if (!State.IsFailed && State.ShouldFail) Fail();
        if (State.IsFailed) Music.Volume -= 1f / 60f;
        if (State.IsPlaying)

        {
            if (State.ClearCount >= Chart.Model.note_list.Count) Complete();

            if (!State.IsCompleted || !Music.IsFinished())
            {
                SynchronizeMusic();
            }

            MusicProgress = Time / MusicLength;
            ChartProgress = Time / ChartLength;

            if (!State.IsCompleted && !State.IsFailed)
            {
                // Process chart elements
                while (Chart.CurrentEventId < Chart.Model.event_order_list.Count &&
                       Chart.Model.event_order_list[Chart.CurrentEventId].time < Time)
                {
                    if (Chart.Model.event_order_list[Chart.CurrentEventId].event_list[0].type == 0)
                    {
                        onGameSpeedUp.Invoke(this);
                    }
                    else
                    {
                        onGameSpeedDown.Invoke(this);
                    }

                    Chart.CurrentEventId++;
                }

                while (Chart.CurrentPageId < Chart.Model.page_list.Count &&
                       Chart.Model.page_list[Chart.CurrentPageId].end_time <= Time)
                {
                    if (Chart.Model.page_list[Chart.CurrentPageId].scan_line_direction == 1)
                    {
                        if (!State.IsCompleted) onTopBoundaryBounded.Invoke(this);
                    }
                    else
                    {
                        if (!State.IsCompleted) onBottomBoundaryBounded.Invoke(this);
                    }

                    Chart.CurrentPageId++;
                }

                var notes = Chart.Model.note_map;
                while (Chart.CurrentNoteId < notes.Count && notes[Chart.CurrentNoteId].intro_time - 1f < Time)
                    switch ((NoteType) notes[Chart.CurrentNoteId].type)
                    {
                        case NoteType.DragHead:
                        case NoteType.CDragHead:
                            var id = Chart.CurrentNoteId;
                            while (notes[id].next_id > 0)
                            {
                                ObjectPool.SpawnDragLine(notes[id], notes[notes[id].next_id]);
                                id = notes[id].next_id;
                            }

                            ObjectPool.SpawnNote(notes[Chart.CurrentNoteId]);
                            Chart.CurrentNoteId++;
                            break;
                        default:
                            ObjectPool.SpawnNote(notes[Chart.CurrentNoteId]);
                            Chart.CurrentNoteId++;
                            break;
                    }
            }
        }

        onGameUpdate.Invoke(this);
        onGameLateUpdate.Invoke(this);
    }

    protected virtual void OnApplicationPause(bool willPause)
    {
        if (IsLoaded && State.IsStarted && willPause)
        {
            Pause();
        }
    }
    protected virtual void OnApplicationFocus(bool hasFocus)
    {
        if (IsLoaded && State.IsStarted && !hasFocus)
        {
            Pause();
        }
    }

    public virtual bool Pause()
    {
        if (State.Mode == GameMode.GlobalCalibration)
        {
            globalCalibrator.Restart();
            return false;
        }

        if (!IsLoaded || !State.IsPlaying || State.IsCompleted || State.IsFailed) return false;
        print("Game paused");

        unpauseToken?.Cancel();
        UnpauseCountdown = 0;
        State.IsPlaying = false;
        AudioListener.pause = true;

        if (State.Mode == GameMode.Tier)
        {
            Fail();
        }
        else
        {
            Context.AudioManager.Get("Navigate2").Play(ignoreDsp: true);

            Context.ScreenManager.ChangeScreen(PausedScreen.Id, ScreenTransition.None);
            Context.SetAutoRotation(true);

            onGamePaused.Invoke(this);
        }

        return true;
    }

    private CancellationTokenSource unpauseToken;

    public virtual async void WillUnpause()
    {
        if (!IsLoaded || State.IsPlaying || State.IsCompleted || State.IsFailed || UnpauseCountdown > 0) return;
        if (State.Mode == GameMode.Tier) throw new InvalidOperationException();

        print("Game ready to unpause");
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1);
        Context.SetAutoRotation(false);

        onGameWillUnpause.Invoke(this);

        UnpauseCountdown = 3;
        while (UnpauseCountdown > 0)
        {
            unpauseToken = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.1), cancellationToken: unpauseToken.Token);
            }
            catch
            {
                print("Game unpause cancelled");
                return;
            }

            UnpauseCountdown -= 0.1f;
        }

        Unpause();
    }

    public virtual void Unpause()
    {
        if (!IsLoaded || State.IsPlaying || State.IsCompleted || State.IsFailed) return;
        if (State.Mode == GameMode.Tier) throw new InvalidOperationException();
        print("Game unpaused");

        GameStartedOrResumedTimestamp = UnityEngine.Time.realtimeSinceStartup;
        AudioListener.pause = false;
        State.IsPlaying = true;

        onGameUnpaused.Invoke(this);
    }

    public virtual async void Abort()
    {
        print("Game aborted");

        Music.Stop();
        // Resume DSP
        AudioListener.pause = false;

        // Unload resources
        Context.AudioManager.Unload("Level");

        onGameAborted.Invoke(this);

        Dispose();

        var sceneLoader = new SceneLoader("Navigation");
        sceneLoader.Load();
        var transitioned = false;
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
            onFinished: screen => transitioned = true);
        await UniTask.WaitUntil(() => transitioned && sceneLoader.IsLoaded);
        sceneLoader.Activate();
    }

    public virtual async void Retry()
    {
        print("Game retried");

        // Unload resources
        Context.AudioManager.Unload("Level");

        onGameRetried.Invoke(this);

        Dispose();

        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        var transitioned = false;
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
            onFinished: screen => transitioned = true);
        await UniTask.WaitUntil(() => transitioned && sceneLoader.IsLoaded);
        sceneLoader.Activate();
    }

    public void Fail()
    {
        if (State.IsFailed) return;
        print("Game failed");

        State.IsFailed = true;
        State.OnFail();
        inputController.DisableInput();

        Context.ScreenManager.ChangeScreen(FailedScreen.Id, ScreenTransition.None);
        Context.AudioManager.Get("LevelFailed").Play();

        onGameFailed.Invoke(this);
    }

    public virtual async void Complete(bool? skipMusic = null)
    {
        if (State.IsCompleted || State.IsFailed) return;
        print("Game completed");

        if (skipMusic == null) skipMusic = Chart.SkipMusicOnCompletion;

        State.IsCompleted = true;

        State.OnComplete();
        if (State.Mode == GameMode.Tier)
        {
            Context.TierState.OnStageComplete();
        }

        inputController.DisableInput();

        onGameCompleted.Invoke(this);

        if (!EditorImmediatelyComplete)
        {
            var maxVolume = Music.Volume;
            var volume = Music.Volume * 3f;

            // Wait for audio to finish
            var remainingLength = MusicLength - Music.PlaybackTime;
            var startTime = DateTime.Now;
            await UniTask.WaitUntil(() =>
            {
                if (skipMusic.Value)
                {
                    volume -= 1 / 60f;
                    if (volume < 1)
                    {
                        Music.Volume = Math.Min(maxVolume, volume);
                    }

                    return Music.IsFinished() || volume <= 0;
                }
                else
                {
                    return Music.IsFinished() ||
                           DateTime.Now - startTime > TimeSpan.FromSeconds(remainingLength); // Just as a fail-safe
                }
            });
        }

        State.IsReadyToExit = true;

        print("Audio ended");
        Context.AudioManager.Unload("Level");

        try
        {
            await UniTask.WhenAll(BeforeExitTasks);
        }
        catch (OperationCanceledException)
        {
        }

        onGameBeforeExit.Invoke(this);

        await Resources.UnloadUnusedAssets();
        Dispose();
        globalCalibrator?.Dispose();

        var sceneLoader = new SceneLoader("Navigation");
        sceneLoader.Load();

        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        if (!sceneLoader.IsLoaded) await UniTask.WaitUntil(() => sceneLoader.IsLoaded);

        sceneLoader.Activate();
    }

    public virtual void Dispose()
    {
        onGameUpdate.RemoveAllListeners();
        onGameLateUpdate.RemoveAllListeners();

        inputController.DisableInput();
        ObjectPool.Dispose();

        onGameDisposed.Invoke(this);
    }
}

public class GameEvent : UnityEvent<Game>
{
}

public class NoteEvent : UnityEvent<Game, Note>
{
}

public class NoteJudgeEvent : UnityEvent<Game, Note, JudgeData>
{
}

#if UNITY_EDITOR

[CustomEditor(typeof(Game), true)]
public class GameEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            GUILayout.Label($"DSP time: {AudioSettings.dspTime}");
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
