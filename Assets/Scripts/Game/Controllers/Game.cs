using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UniRx;
using UniRx.Async;
using UniRx.Triggers;
using UnityEngine.Events;
using UnityEngine.Networking;

public class Game : MonoBehaviour
{
    public EffectController effectController;
    public InputController inputController;

    public bool IsLoaded { get; protected set; }
    
    public GameConfig Config { get; protected set; }
    public GameState State { get; protected set; }
    public GameRenderer Renderer { get; protected set; }

    public Level Level { get; protected set; }
    public Chart Chart { get; protected set; }
    public Dictionary<int, Note> Notes { get; } = new Dictionary<int, Note>();
    
    public float Time { get; protected set; }
    public float MusicDuration { get; protected set; }
    public float MusicStartedTimestamp { get; protected set; } // When was the music started to play?
    public float MusicStartedAt { get; protected set; } // When the music was played, from when was it played?
    public float MainMusicProgress { get; protected set; }

    public AudioManager.Controller Music { get; protected set; }
    public AudioManager.Controller HitSound { get; protected set; }

    public GameEvent onGameLoaded = new GameEvent();
    public GameEvent onGameStarted = new GameEvent();
    public GameEvent onGameUpdate = new GameEvent();
    public GameEvent onGamePaused = new GameEvent();
    public GameEvent onGameUnpaused = new GameEvent();
    public GameEvent onGameFailed = new GameEvent();
    public GameEvent onGameCompleted = new GameEvent();
    public GameEvent onGameAborted = new GameEvent();
    public NoteEvent onNoteClear = new NoteEvent();
    public GameEvent onGameSpeedUp = new GameEvent();
    public GameEvent onGameSpeedDown = new GameEvent();
    public GameEvent onTopBoundaryBounded = new GameEvent();
    public GameEvent onBottomBoundaryBounded = new GameEvent();

    private void Awake()
    {
        Renderer = new GameRenderer(this);
    }

    public async void Initialize(Level level, Difficulty difficulty, bool startAutomatically = true)
    {
        Level = level;

        // Load chart
        print("Loading chart");
        var chartMeta = level.Meta.GetChartSection(difficulty.Id);
        var chartPath = "file://" + level.Path + chartMeta.path;
        string chartText;
        using (var request = UnityWebRequest.Get(chartPath))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"Cannot download chart from {chartPath}");
                Debug.LogError(request.error);
                return;
            }

            chartText = Encoding.UTF8.GetString(request.downloadHandler.data);
        }

        var mods = Context.LocalPlayer.EnabledMods;
        Chart = new Chart(
            chartText,
            mods.Contains(Mod.FlipX) || mods.Contains(Mod.FlipAll),
            mods.Contains(Mod.FlipY) || mods.Contains(Mod.FlipAll),
            true,
            mods.Contains(Mod.Fast) ? 1.5f : (mods.Contains(Mod.Slow) ? 0.75f : 1),
            0.8f + (5 - (int) Context.LocalPlayer.HorizontalMargin - 1) * 0.025f,
            (5.5f + (5 - (int) Context.LocalPlayer.VerticalMargin) * 0.5f) / 9.0f
        );

        // Load audio
        print("Loading audio");
        var audioPath = "file://" + Level.Path + Level.Meta.GetMusicPath(difficulty.Id);
        AudioClip audioClip;
        using (var request = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.MPEG))
        {
            await request.SendWebRequest();
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError($"Cannot download audio from {audioPath}");
                Debug.LogError(request.error);
                return;
            }

            audioClip = DownloadHandlerAudioClip.GetContent(request);
        }

        MusicDuration = audioClip.length;
        Music = Context.AudioManager.Load("main", audioClip);

        if (Context.LocalPlayer.HitSound != "None")
        {
            // Load hit sound
            var hitSoundPath = Application.streamingAssetsPath + "/HitSounds/" + Context.LocalPlayer.HitSound +
                               ".wav";
            using (var request = UnityWebRequestMultimedia.GetAudioClip(hitSoundPath, AudioType.WAV))
            {
                await request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError($"Cannot load hit sound from {hitSoundPath}");
                    Debug.LogError(request.error);
                    return;
                }

                audioClip = DownloadHandlerAudioClip.GetContent(request);
            }

            HitSound = Context.AudioManager.Load("hitSound", audioClip);
        }

        // State & config
        var isRanked = Context.LocalPlayer.PlayRanked && Context.OnlinePlayer.IsAuthenticated;
        var maxHealth = chartMeta.difficulty * 75;
        if (maxHealth < 0) maxHealth = 1000;
        State = new GameState(this, isRanked, mods, maxHealth);
        Config = new GameConfig(this);

        // Touch handlers
        if (!mods.Contains(Mod.Auto))
        {
            inputController.EnableInput();
        }

        // System config
        Context.SetAutoRotation(false);

        IsLoaded = true;
        onGameLoaded.Invoke(this);

        // Wait for storyboard
        // await UniTask.WaitUntil(() => StoryboardController.Instance.Loaded);

        if (startAutomatically) StartGame();
    }

    protected async void StartGame()
    {
        Music.Play(AudioTrackIndex.Reserved1);

        await UniTask.WaitUntil(() =>
        {
            // Wait until the audio actually starts playing
            var playbackTime = Music.PlaybackTime;
            return playbackTime > 0 && playbackTime < MusicDuration;
        });

        MusicStartedTimestamp = UnityEngine.Time.realtimeSinceStartup;
        MusicStartedAt = Music.PlaybackTime;

        State.IsStarted = true;
        State.IsPlaying = true;
        onGameStarted.Invoke(this);
    }

    protected virtual void Update()
    {
        if (!IsLoaded || !State.IsStarted) return;

        if (Input.GetKeyDown(KeyCode.Escape) && !(this is StoryboardGame))
        {
            Pause();
            return;
        }
        
        if (State.ShouldFail) Fail();
        if (State.IsFailed) Music.PlaybackTime -= 1f / 120f;
        if (State.IsPlaying)
        {
            if (State.ClearedNotes >= Chart.Model.note_list.Count) Complete();

            // Update current states
            if (this is StoryboardGame)
            {
                // PlaybackTime is accurate enough on desktop
                Time = Music.PlaybackTime - Config.ChartOffset + Chart.MusicOffset + MusicStartedAt;
            }
            else
            {
                Time = UnityEngine.Time.realtimeSinceStartup - MusicStartedTimestamp + MusicStartedAt
                       - Config.ChartOffset + Chart.MusicOffset;
            }

            MainMusicProgress = Time / MusicDuration;

            // Process chart elements
            while (Chart.CurrentEventId < Chart.Model.event_order_list.Count &&
                   Chart.Model.event_order_list[Chart.CurrentEventId].time < Time)
            {
                // TODO: Speed up/down text
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
                    onTopBoundaryBounded.Invoke(this);
                }
                else
                {
                    onBottomBoundaryBounded.Invoke(this);
                }

                Chart.CurrentPageId++;
            }

            var notes = Chart.Model.note_list;
            while (Chart.CurrentNoteId < notes.Count && notes[Chart.CurrentNoteId].start_time - 2.0f < Time)
                switch ((NoteType) notes[Chart.CurrentNoteId].type)
                {
                    case NoteType.DragHead:
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
    }

    public virtual void SpawnNote(ChartModel.Note model)
    {
        var provider = GameObjectProvider.Instance;
        Note note;
        switch ((NoteType) model.type)
        {
            case NoteType.Click:
                note = Instantiate(provider.clickNotePrefab, transform.parent).GetComponent<Note>();
                break;
            case NoteType.Hold:
                note = Instantiate(provider.holdNotePrefab, transform.parent).GetComponent<Note>();
                break;
            case NoteType.LongHold:
                note = Instantiate(provider.longHoldNotePrefab, transform.parent).GetComponent<Note>();
                break;
            case NoteType.DragHead:
                note = Instantiate(provider.dragHeadNotePrefab, transform.parent).GetComponent<Note>();
                break;
            case NoteType.DragChild:
                note = Instantiate(provider.dragChildNotePrefab, transform.parent).GetComponent<Note>();
                break;
            case NoteType.Flick:
                note = Instantiate(provider.flickNotePrefab, transform.parent).GetComponent<Note>();
                break;
            default:
                note = Instantiate(provider.clickNotePrefab, transform.parent).GetComponent<Note>();
                break;
        }

        note.SetData(this, model.id);
        Notes[model.id] = note;
    }

    public virtual void SpawnDragLine(ChartModel.Note from, ChartModel.Note to)
    {
        var dragLineView = Instantiate(GameObjectProvider.Instance.dragLinePrefab, transform.parent)
            .GetComponent<DragLineElement>();
        dragLineView.SetData(this, from, to);
    }

    protected virtual void OnApplicationPause(bool willPause)
    {
        if (IsLoaded && State.IsStarted && willPause) Pause();
    }

    public void Pause()
    {
        if (!IsLoaded || !State.IsPlaying || State.IsCompleted || State.IsFailed) return;

        State.IsPlaying = false;
        Music.Pause();
        unpause?.Cancel();

        onGamePaused.Invoke(this);
    }
    
    private CancellationTokenSource unpause;

    public async void Unpause()
    {
        if (!IsLoaded || State.IsPlaying || State.IsCompleted || State.IsFailed) return;
        var countdown = 3;
        while (countdown > 0)
        {
            // TODO: Update text?

            unpause = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: unpause.Token);
            }
            catch
            {
                return;
            }
            countdown--;
        }
        UnpauseImmediately();
    }

    public virtual async void UnpauseImmediately()
    { 
        State.IsPlaying = true;
        Music.Resume();
        
        await UniTask.WaitUntil(() =>
        {
            // Wait until the audio actually starts playing
            var playbackTime = Music.PlaybackTime;
            return playbackTime > 0 && playbackTime < MusicDuration;
        });

        MusicStartedTimestamp = UnityEngine.Time.realtimeSinceStartup;
        MusicStartedAt = Music.PlaybackTime;

        onGameUnpaused.Invoke(this);
    }

    public void Fail()
    {
        if (State.IsFailed) return;
        State.IsFailed = true;
        inputController.DisableInput();

        onGameFailed.Invoke(this);
        // TODO: Show fail UI
    }

    public virtual async void Complete()
    {
        if (State.IsCompleted || State.IsFailed) return;
        print("Game completed");
        
        State.IsCompleted = true;
        inputController.DisableInput();

        if (State.Mods.Contains(Mod.Auto) || State.Mods.Contains(Mod.AutoDrag)
                                          || State.Mods.Contains(Mod.AutoHold) || State.Mods.Contains(Mod.AutoFlick))
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            Cleanup();

            // TODO: Go back
        }
        else
        {
            // Wait for audio to finish
            await UniTask.WaitUntil(() => Mathf.Approximately(Music.PlaybackTime, MusicDuration));
            print("Audio ended");

            // TODO: Result
        }
    }

    private void Cleanup()
    {
        Notes.Select(entry => entry.Value).Where(note => note != null).ForEach(Destroy);
    }
}

public class GameEvent : UnityEvent<Game>
{
}

public class NoteEvent : UnityEvent<Game, Note>
{
}