using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cytoid.Storyboard;
using UnityEngine;
using UniRx.Async;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.Networking;

public class Game : MonoBehaviour
{
    public new Camera camera;
    public GameObject contentParent;
    public EffectController effectController;
    public InputController inputController;

    public bool IsLoaded { get; protected set; }

    public GameConfig Config { get; protected set; }
    public GameState State { get; protected set; }
    public GameRenderer Renderer { get; protected set; }

    public Level Level { get; protected set; }
    
    public Difficulty Difficulty { get; protected set; }
    public Chart Chart { get; protected set; }
    public SortedDictionary<int, Note> Notes { get; } = new SortedDictionary<int, Note>();

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

    public string EditorDefaultLevelDirectory = "yy.badapple";
    public float EditorMusicInitialPosition;
    public bool EditorForceAutoMod;
    public GameMode EditorGameMode = GameMode.Unspecified;
    public bool EditorImmediatelyComplete;
    public bool EditorImmediatelyCompleteFail;
    
    public AudioManager.Controller Music { get; protected set; }
    
    public List<UniTask> BeforeStartTasks { get; protected set; } = new List<UniTask>();
    public List<UniTask> BeforeExitTasks { get; protected set; } = new List<UniTask>();

    public GameEvent onGameReadyToLoad = new GameEvent();
    public GameEvent onGameLoaded = new GameEvent();
    public GameEvent onGameStarted = new GameEvent();
    public GameEvent onGameUpdate = new GameEvent();
    public GameEvent onGameLateUpdate = new GameEvent();
    public GameEvent onGamePaused = new GameEvent();
    public GameEvent onGameWillUnpause = new GameEvent();
    public GameEvent onGameUnpaused = new GameEvent();
    public GameEvent onGameFailed = new GameEvent();
    public GameEvent onGameCompleted = new GameEvent();
    public GameEvent onGameReadyToExit = new GameEvent();
    public GameEvent onGameAborted = new GameEvent();
    public GameEvent onGameRetried = new GameEvent();
    public NoteEvent onNoteClear = new NoteEvent();
    public GameEvent onGameSpeedUp = new GameEvent();
    public GameEvent onGameSpeedDown = new GameEvent();
    public GameEvent onGameDisposed = new GameEvent();
    public GameEvent onTopBoundaryBounded = new GameEvent();
    public GameEvent onBottomBoundaryBounded = new GameEvent();

    protected virtual void Awake()
    {
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
        await Initialize();
    }

    public async UniTask Initialize(bool startAutomatically = true)
    {
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
        else
        {
            if (Context.SelectedLevel == null && Application.isEditor)
            {
                // Load test level
                await Context.LevelManager.LoadFromMetadataFiles(LevelType.Community, new List<string> {
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
        
        var ratio = UnityEngine.Screen.width * 1.0f / UnityEngine.Screen.height;
        var height = camera.orthographicSize * 2.0f;
        var width = height * ratio;
        var topRatio = 0.0966666f;
        var bottomRatio = 0.07f;
        var verticalRatio = 1 - width * (topRatio + bottomRatio) / height + (3 - Context.Player.Settings.VerticalMargin) * 0.05f;
        var verticalOffset = -(width * (topRatio - (topRatio + bottomRatio) / 2.0f));
        Chart = new Chart(
            chartText,
            mods.Contains(Mod.FlipX) || mods.Contains(Mod.FlipAll),
            mods.Contains(Mod.FlipY) || mods.Contains(Mod.FlipAll),
            true,
            Context.Player.Settings.UseExperimentalNoteAr,
            mods.Contains(Mod.Fast) ? 1.5f : (mods.Contains(Mod.Slow) ? 0.75f : 1),
            camera.orthographicSize,
            0.8f + (5 - Context.Player.Settings.HorizontalMargin - 1) * 0.02f,
            verticalRatio,
            verticalOffset
        );
        ChartLength = Chart.Model.note_list.Max(it => it.end_time);
        
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
        StoryboardPath =
            Level.Path + (chartMeta.storyboard != null ? chartMeta.storyboard.path : "storyboard.json");

        if (File.Exists(StoryboardPath)) {
            // Initialize storyboard
            // TODO: Why File.ReadAllText() works but not UnityWebRequest?
            // (UnityWebRequest downloaded text could not be parsed by Newtonsoft.Json)
            try
            {
                var storyboardText = File.ReadAllText(StoryboardPath);
                Storyboard = new Cytoid.Storyboard.Storyboard(this, storyboardText);
                await Storyboard.Initialize();
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

        Config = new GameConfig(this);

        // Touch handlers
        if (!State.Mods.Contains(Mod.Auto))
        {
            inputController.EnableInput();
        }

        // System config
        Application.targetFrameRate = 120;
        Context.SetAutoRotation(false);
        
        // Update last played time
        Level.Record.LastPlayedDate = DateTimeOffset.UtcNow;
        Level.SaveRecord();

        IsLoaded = true;
        onGameLoaded.Invoke(this);
        
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None);
        
        if (startAutomatically)
        {
            StartGame();
        }
    }

    protected async virtual void StartGame()
    {
        await UniTask.WhenAll(BeforeStartTasks);
        
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None);
        
        MusicStartedTimestamp = Music.PlayScheduled(AudioTrackIndex.Reserved1, 1.0f);

        await UniTask.WaitUntil(
            () => AudioSettings.dspTime >= MusicStartedTimestamp,
            PlayerLoopTiming.Initialization);

        if (Application.isEditor && EditorMusicInitialPosition > 0)
        {
            Music.PlaybackTime = EditorMusicInitialPosition;
            MusicStartedTimestamp -= EditorMusicInitialPosition;
        }

        GameStartedOrResumedTimestamp = UnityEngine.Time.realtimeSinceStartup;
        State.IsStarted = true;
        State.IsPlaying = true;
        onGameStarted.Invoke(this);

        if (Application.isEditor && EditorImmediatelyComplete)
        {
            if (EditorImmediatelyCompleteFail)
            {
                Fail();
            }
            else 
            {
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
        if ((ResynchronizeChartOnNextFrame || ticksBeforeSynchronization <= 0 || resumeElapsedTime < 0.5f) && nowDspTime != lastDspTime)
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

            SynchronizeMusic();
            MusicProgress = Time / MusicLength;
            ChartProgress = Time / ChartLength;

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
            while (Chart.CurrentNoteId < notes.Count && notes[Chart.CurrentNoteId].start_time - 2.0f < Time)
                switch ((NoteType) notes[Chart.CurrentNoteId].type)
                {
                    case NoteType.DragHead:
                    case NoteType.CDragHead:
                        var id = Chart.CurrentNoteId;
                        while (notes[id].next_id > 0)
                        {
                            SpawnDragLine(notes[id], notes[notes[id].next_id]);
                            id = notes[id].next_id;
                        }

                        SpawnNote(notes[Chart.CurrentNoteId]);
                        Chart.CurrentNoteId++;
                        break;
                    default:
                        SpawnNote(notes[Chart.CurrentNoteId]);
                        Chart.CurrentNoteId++;
                        break;
                }
        }

        onGameUpdate.Invoke(this);
        onGameLateUpdate.Invoke(this);
    }

    public virtual void SpawnNote(ChartModel.Note model)
    {
        var provider = GameObjectProvider.Instance;
        Note note;
        
        switch ((NoteType) model.type)
        {
            case NoteType.Click:
                note = Instantiate(provider.clickNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            case NoteType.Hold:
                note = Instantiate(provider.holdNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            case NoteType.LongHold:
                note = Instantiate(provider.longHoldNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            case NoteType.DragHead:
                note = Instantiate(provider.dragHeadNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            case NoteType.CDragHead:
                note = Instantiate(provider.cDragChildNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            case NoteType.DragChild:
            case NoteType.CDragChild:
                note = Instantiate(provider.dragChildNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            case NoteType.Flick:
                note = Instantiate(provider.flickNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
            default:
                note = Instantiate(provider.clickNotePrefab, contentParent.transform).GetComponent<Note>();
                break;
        }
        
        // Debug.Log($"Note {model.id} is spawned as {((NoteType) model.type).ToString()}");
        note.SetData(this, model.id);
        note.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Content"));
        Notes[model.id] = note;
    }

    public virtual void SpawnDragLine(ChartModel.Note from, ChartModel.Note to)
    {
        var dragLineView = Instantiate(GameObjectProvider.Instance.dragLinePrefab, contentParent.transform)
            .GetComponent<DragLineElement>();
        dragLineView.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Content"));
        dragLineView.SetData(this, from, to);
    }

    protected virtual void OnApplicationPause(bool willPause)
    {
        if (IsLoaded && State.IsStarted && willPause)
        {
            Pause();
        }
    }

    public virtual bool Pause()
    {
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

    public virtual void Abort()
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
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
            onFinished: screen => sceneLoader.Activate());
    }

    public virtual void Retry()
    {
        print("Game retried");
        
        // Unload resources
        Context.AudioManager.Unload("Level");
        
        onGameRetried.Invoke(this);
        
        Dispose();
        
        var sceneLoader = new SceneLoader("Game");
        sceneLoader.Load();
        Context.ScreenManager.ChangeScreen(OverlayScreen.Id, ScreenTransition.None, 0.4f, 1,
            onFinished: screen => sceneLoader.Activate());
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

    public virtual async void Complete()
    {
        if (State.IsCompleted || State.IsFailed) return;
        print("Game completed");

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
            var volume = 3f;
            // Wait for audio to finish
            await UniTask.WaitUntil(() =>
            {
                volume -= 1 / 180f;
                if (volume < 1)
                {
                    Music.Volume = volume;
                }

                return volume <= 0 || Music.IsFinished();
            });
        }

        print("Audio ended");
        Context.AudioManager.Unload("Level");
        
        await UniTask.WhenAll(BeforeExitTasks);
        await Resources.UnloadUnusedAssets();

        onGameReadyToExit.Invoke(this);
        
        Dispose();
        
        var sceneLoader = new SceneLoader("Navigation");
        sceneLoader.Load();

        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));

        sceneLoader.Activate();
    }

    public virtual void Dispose()
    {
        inputController.DisableInput();

        onGameDisposed.Invoke(this);
    }

}

public class GameEvent : UnityEvent<Game>
{
}

public class NoteEvent : UnityEvent<Game, Note>
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