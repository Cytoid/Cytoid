using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerGame : Game
{
    public static float StartAt;
    
    public ExtendedSlider slider;
    public List<CanvasGroup> uiCanvasGroups;

    public bool HideInterface { get; set; }

    public bool PlayerPaused { get; set; }
    
    private FileSystemWatcher watcher;
    private ChartModel originalChartModel;

    protected override void Awake()
    {
        base.Awake();
        Application.runInBackground = true;
        UnityEngine.Screen.fullScreen = false;
        var audioConfig = AudioSettings.GetConfiguration();
        audioConfig.dspBufferSize = 2048;
        slider.onValueChanged.AddListener(OnSliderSeek);
    }
    
    protected override async void Start()
    {
        await UniTask.WaitUntil(() => Context.IsInitialized);
        
        var path = Application.isEditor
            ? $"C:/Cytoid/Cytoid/Builds/Player/player/level.json"
            : $"{Directory.GetParent(Application.dataPath).FullName}/player/level.json";

        if (Context.LevelManager.LoadedLocalLevels.Count == 0)
        {
            await Context.LevelManager.LoadFromMetadataFiles(LevelType.User,
                new List<string> {path});
        }
        var level = Context.LevelManager.LoadedLocalLevels.First().Value;
        Context.SelectedLevel = level;
        Context.SelectedDifficulty = level.Meta.GetHardestDifficulty();
        Context.SelectedGameMode = GameMode.Standard;
        Context.SelectedMods = new HashSet<Mod>{Mod.Auto};

        await Initialize();

        if (Storyboard != null)
        {
            originalChartModel = Chart.Model.JsonDeepCopy();
            Storyboard.Config.UseEffects = true;
            // Watch for file changes
            print($"Enabling file watcher on {StoryboardPath}");
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
            watcher = new FileSystemWatcher
            {
                Filter = Path.GetFileName(StoryboardPath),
                Path = Path.GetDirectoryName(StoryboardPath),
                NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += delegate
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    print("Detected storyboard change!");

                    ReloadStoryboard();
                });
            };
            watcher.EnableRaisingEvents = true;
        }
    }
    
    public void ReloadStoryboard()
    {
        if (Storyboard == null) return;
        
        Storyboard.Dispose();
        Storyboard.Renderer.Dispose();

        foreach (var (id, note) in Chart.Model.note_map.Select(it => (it.Key, it.Value)))
        {
            note.PasteFrom(originalChartModel.note_map[id]);
        }

        Storyboard = new Cytoid.Storyboard.Storyboard(this, File.ReadAllText(StoryboardPath));
        Storyboard.Parse();
        Storyboard.Initialize();
    }

    public async void ReloadAll()
    {
        StartAt = Time;
        State.IsPlaying = false;
        
        Context.AudioManager.Unload("Level");
        Dispose();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Music.IsPlaying())
            {
                PlayerPaused = true;
                Music.Pause();
            }
            else
            {
                PlayerPaused = false;
                Music.Resume();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            HideInterface = !HideInterface;
            uiCanvasGroups.ForEach(it => it.alpha = HideInterface ? 0 : 1);
        }
    }

    protected void OnSliderSeek(float value)
    {
        Music.PlaybackTime = value * MusicLength;
        Storyboard?.Renderer.Clear();

        Chart.Model.note_list.LastOrDefault(it => it.intro_time - 1f < Music.PlaybackTime)?.Apply(it =>
        {
            Chart.CurrentNoteId = it.id;
        });
    }

    protected override async void StartGame()
    {
        await UniTask.WhenAll(BeforeStartTasks);
        
        Music.Play(AudioTrackIndex.Reserved1);
        if (StartAt > 0)
        {
            Music.PlaybackTime = StartAt;
            MusicStartedTimestamp -= StartAt;
        }

        PlayerPaused = false;

        State.IsStarted = true;
        State.IsPlaying = true;
        onGameStarted.Invoke(this);
    }

    protected override void SynchronizeMusic()
    {
        if (Music.IsFinished())
        {
            if (!PlayerPaused)
            {
                Storyboard?.Renderer.Clear();
                Time = 0;
                Music.PlaybackTime = 0;
                Music.Play(AudioTrackIndex.Reserved1);

                Chart.CurrentNoteId = 0;
            }
        }
        Time = Music.PlaybackTime - Config.ChartOffset + Chart.MusicOffset;
        slider.SetWithoutCallback(MusicProgress);
    }

    protected override void OnApplicationPause(bool willPause) => Expression.Empty();

    public override bool Pause() => false;

    public override void WillUnpause() => Expression.Empty();
    
    public override void Unpause() => Expression.Empty();

    public override void Abort() => Expression.Empty();

    public override void Retry() => Expression.Empty();

    public override void Complete(bool? skipMusic = null) => Expression.Empty();

    public override void Dispose() => Expression.Empty();

}