using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using DoozyUI;
using Lean.Touch;
using MoreLinq;
using QuickEngine.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class GameController : SingletonMonoBehavior<GameController>
{
    private string action = Action.Result;

    public static class Action
    {
        public const string Result = "Result";
        public const string Back = "Back";
        public const string Retry = "Retry";
    }

    public string editorLevelOverride;
    public string editorChartTypeOverride;
    public string editorLevelFallback;
    public string editorChartTypeFallback;
    public bool autoPlay;
    public bool showEarlyLateIndicator;
    public float hitboxMultiplier = 1.0f;

    public float startAt;
    public float editorStartAtOverride;

    public bool willPause;
    public float pausedAt;

    public bool isInversed { get; private set; }

    public bool IsPaused { get; private set; }

    public float StartTime { get; private set; }

    [SerializeField]
    public float TimeElapsed { get; private set; }

    [SerializeField]
    public int CurrentPage
    {
        get { return (int) ((TimeElapsed + Chart.pageShift) / Chart.pageDuration); }
    }

    public float CurrentPageUnfloored
    {
        get { return (TimeElapsed + Chart.pageShift) / Chart.pageDuration; }
    }
    
    public Chart Chart { get; private set; }
    public PlayData PlayData { get; private set; }
    public OrderedDictionary NoteViews = new OrderedDictionary();

    [SerializeField] private GameObject singleNotePrefab;
    [SerializeField] private GameObject holdNotePrefab;
    [SerializeField] private GameObject chainNotePrefab;
    [SerializeField] private GameObject scannerPrefab;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AlphaMask backgroundOverlayMask;
    [SerializeField] private AlphaMask sceneTransitionMask;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Text titleText;
    [SerializeField] private GameObject rankedIndicator;
    [SerializeField] private GameObject levelInfoIndicator;
    
    private GameObject background;
    private AudioClip clip;
    private int anaId = -1;

    [SerializeField]
    public bool IsLoaded { get; private set; }

    [SerializeField]
    public bool IsEnded { get; private set; }

    public bool IsRanked { get; private set; }
    
    public readonly RankedPlayData RankedPlayData = new RankedPlayData();

    protected override void Awake()
    {
        base.Awake();
        BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Pause();
        }
    }

    private IEnumerator Start()
    {
        IsRanked = PlayerPrefsExt.GetBool(PreferenceKeys.RankedMode());
        if (!LocalProfile.Exists()) IsRanked = false;

        if (!IsRanked)
        {
            levelInfoIndicator.transform.SetLocalX(rankedIndicator.transform.localPosition.x);
            rankedIndicator.SetActive(false);
        }
        
        if (PlayerPrefs.GetInt("autoplay") == 1 && !IsRanked) autoPlay = true;
        if (PlayerPrefsExt.GetBool("larger_hitboxes")) hitboxMultiplier = 1.33f;
        showEarlyLateIndicator = PlayerPrefsExt.GetBool("early_late_indicator");

        CytoidApplication.SetAutoRotation(false);

        SetAllowPause(false);

        OnScreenChainNotes = new List<NoteView>();
        OnScreenHoldNotes = new List<NoteView>();
        OnScreenRegularAndHoldNotes = new List<NoteView>();

#if UNITY_EDITOR
        autoPlay = true;
        if (!string.IsNullOrEmpty(editorLevelOverride))
        {
            CytoidApplication.CurrentLevel = CytoidApplication.Levels.Find(it =>
                string.Equals(it.title, editorLevelOverride, StringComparison.OrdinalIgnoreCase));
            CytoidApplication.CurrentChartType = editorChartTypeOverride;
        }
        // Still null? Fallback
        if (CytoidApplication.CurrentLevel == null)
        {
            CytoidApplication.CurrentLevel = CytoidApplication.Levels.Find(it =>
                string.Equals(it.title, editorLevelFallback, StringComparison.OrdinalIgnoreCase));
            CytoidApplication.CurrentChartType = editorChartTypeFallback;
        }
        if (Math.Abs(editorStartAtOverride - startAt) > 0.00001f)
        {
            startAt = editorStartAtOverride;
        }
#endif

        var level = CytoidApplication.CurrentLevel;

        if (!level.ChartsLoaded)
        {
            level.LoadCharts();
        }

        ThemeController.Instance.Init(level);
        DisplayDifficultyView.Instance.SetDifficulty(CytoidApplication.CurrentChartType,
            level.GetDifficulty(CytoidApplication.CurrentChartType));
        titleText.text = level.title;
        
        isInversed = PlayerPrefsExt.GetBool("inverse");
        
        // Override options?
        if (ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(level)))
        {
            isInversed = ZPlayerPrefs.GetBool(PreferenceKeys.WillInverse(level));
        }

        // Chart and background are already loaded

        if (CytoidApplication.CurrentChartType == null) CytoidApplication.CurrentChartType = level.charts[0].type;
        Chart = level.charts.Find(it => it.type == CytoidApplication.CurrentChartType).chart;
        PlayData = new PlayData(Chart, IsRanked);
        CytoidApplication.CurrentPlayData = PlayData;
        
        if (IsRanked) CytoidApplication.CurrentRankedPlayData = RankedPlayData;
        

        // Load audio clip
        if (level.is_internal || Application.platform != RuntimePlatform.Android)
        {
            var www = new WWW((level.is_internal && Application.platform == RuntimePlatform.Android ? "" : "file://") +
                              level.basePath + level.GetMusicPath(CytoidApplication.CurrentChartType));
            yield return www;
            clip = CytoidApplication.ReadAudioClipFromWWW(www);
            audioSource.clip = clip;
        }

        // Don't continue until faded in
        backgroundOverlayMask.willFadeIn = true;
        while (backgroundOverlayMask.IsFading) yield return null;
        yield return null;

        // Init notes
        NoteViews = new OrderedDictionary();
        foreach (var id in Chart.chronologicalIds)
        {
            var note = Chart.notes[id];
            // if (note.time <= startAt) continue;
            var prefab = singleNotePrefab;
            switch (note.type)
            {
                case NoteType.Hold:
                    prefab = holdNotePrefab;
                    break;
                case NoteType.Chain:
                    prefab = chainNotePrefab;
                    break;
            }
            var noteView = Instantiate(prefab, transform).GetComponent<NoteView>();
            noteView.Init(Chart, note);
            NoteViews.Add(id, noteView);
        }
        
        foreach (NoteView note in NoteViews.Values)
        {
            note.OnAllNotesInitialized();
        }

        // Register handlers
        LeanTouch.OnFingerDown += OnFingerDown;
        LeanTouch.OnFingerSet += OnFingerSet;
        LeanTouch.OnFingerUp += OnFingerUp;

        // Release unused assets
        Resources.UnloadUnusedAssets();

        // Init scanner
        Instantiate(scannerPrefab, transform);


        if (level.is_internal || Application.platform != RuntimePlatform.Android)
        {
            audioSource.time = startAt;
            var userOffset = PlayerPrefs.GetFloat("user_offset", 0.2f);

            // Override options?
            if (ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(level)))
            {
                userOffset = ZPlayerPrefs.GetFloat(PreferenceKeys.NoteDelay(level));
            }

            const float delay = 1f;
            audioSource.PlayDelayed(delay);

            StartTime = Time.time + Chart.offset + userOffset + delay;
        }
        else
        {
            anaId = ANAMusic.load(level.basePath + level.GetMusicPath(CytoidApplication.CurrentChartType), true, true,
                id =>
                {
                    StartCoroutine(OnAndroidPlayerLoaded());
                });
        }
        
        // Ranked
        if (!LocalProfile.Exists())
        {
            RankedPlayData.user = "local";
            RankedPlayData.password = "";
        }
        else
        {
            RankedPlayData.user = LocalProfile.Instance.username;
            RankedPlayData.password = LocalProfile.Instance.password;
        }
        RankedPlayData.start = TimeExt.Millis();
        RankedPlayData.id = level.id;
        RankedPlayData.type = CytoidApplication.CurrentChartType;
        RankedPlayData.mods = "";
        RankedPlayData.version = level.version;
        RankedPlayData.chart_checksum = Chart.checksum;
        RankedPlayData.device.width = Screen.width;
        RankedPlayData.device.height = Screen.height;
        RankedPlayData.device.dpi = (int) Screen.dpi;
        RankedPlayData.device.model = SystemInfo.deviceModel;
        // Ranked

/*#if UNITY_EDITOR
        StartCoroutine(EndGame());
#endif*/
    }

    private IEnumerator EndGame()
    {
        yield return new WaitForSeconds(30);
        End();
    }

    private IEnumerator OnAndroidPlayerLoaded()
    {
        if (Math.Abs(startAt) > 0.00001) ANAMusic.seekTo(anaId, (int) (startAt * 1000));
        yield return new WaitForSeconds(1);
        ANAMusic.play(anaId);

        var userOffset = PlayerPrefs.GetFloat("user_offset", 0.12f);
        
        // Override options?
        if (ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(CytoidApplication.CurrentLevel)))
        {
            userOffset = ZPlayerPrefs.GetFloat(PreferenceKeys.NoteDelay(CytoidApplication.CurrentLevel));
        }
        
        
        StartTime = Time.time + Chart.offset + userOffset;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            NotifyWillPause();
            if (CytoidApplication.UseDoozyUI)
            {
                UIManager.ShowUiElement("PauseBackground", "Game", false);
                UIManager.ShowUiElement("PauseRoot", "Game", false);
            }
        }
        
        TimeElapsed = IsLoaded ? Time.time - StartTime + startAt : 0;
        UpdateNoteViews();
        if (!IsLoaded && audioSource != null &&
            ((!CytoidApplication.CurrentLevel.is_internal && Application.platform == RuntimePlatform.Android && anaId != -1 && ANAMusic.isPlaying(anaId)) ||
             audioSource.time > 0.001f))
        {
            IsLoaded = true;
            SetAllowPause(true); // Enable pausing
        }
        if (willPause)
        {
            Pause();
        }
        /*if (IsLoaded && !IsPaused && (audioSource.clip.length <= audioSource.time || audioSource.clip.length - audioSource.time < 0.001f))
        {
            print("Mark end");
            End();
        }*/
        if (PlayData.NoteCleared == Chart.notes.Count)
        {
            End();
        }
    }

    /*
        Because notes use FixedUpdate() to update their states, we use FixedUpdate() to handle the pause.
        (Note that however, the scanner uses Update() for smoother transitions)
    */
    private void FixedUpdate()
    {
        if (IsPaused)
        {
            StartTime += Time.fixedDeltaTime;
        }
    }

    private void Pause()
    {
        if (CytoidApplication.CurrentLevel == null || IsEnded) return;
        RankedPlayData.pauses.Add(new RankedPlayData.Pause { start = TimeExt.Millis() });
        IsPaused = true;
        willPause = false;
        UnpauseCountdown = -1;
        if (CytoidApplication.UseDoozyUI)
        {
            UIManager.ShowUiElement("PauseBackground", "Game", true);
            UIManager.ShowUiElement("PauseRoot", "Game", true);
        }
        if (!CytoidApplication.CurrentLevel.is_internal && Application.platform == RuntimePlatform.Android) ANAMusic.pause(anaId);
        else audioSource.Pause();
        if (!CytoidApplication.CurrentLevel.is_internal && Application.platform == RuntimePlatform.Android) pausedAt = ANAMusic.getCurrentPosition(anaId) / 1000f;
        else pausedAt = audioSource.time;
        foreach (var fingerIndex in holdingNotes.Keys)
        {
            var holdNote = holdingNotes[fingerIndex];
            holdNote.StopHolding();
        }
        holdingNotes.Clear();
        SetAllowPause(false);
        if (unpauseCoroutine != null) StopCoroutine(unpauseCoroutine);
    }

    private void Unpause()
    {
        RankedPlayData.pauses.Last().end = TimeExt.Millis();
        unpauseCoroutine = UnpauseCoroutine();
        StartCoroutine(unpauseCoroutine);
    }

    private IEnumerator unpauseCoroutine;
    public int UnpauseCountdown { get; private set; }

    private IEnumerator UnpauseCoroutine()
    {
        // TODO: I actually love this shit code
        UnpauseCountdown = 3;
        yield return new WaitForSeconds(1f);
        UnpauseCountdown = 2;
        yield return new WaitForSeconds(1f);
        UnpauseCountdown = 1;
        yield return new WaitForSeconds(1f);
        IsPaused = false;
        if (!CytoidApplication.CurrentLevel.is_internal && Application.platform == RuntimePlatform.Android) ANAMusic.play(anaId);
        else audioSource.UnPause();
        SetAllowPause(true);
        pausedAt = 0;
    }

    public void End()
    {
        if (IsEnded) return;
        IsEnded = true;
        SetAllowPause(false);
        var result = new PlayResult
        {
            Ranked = IsRanked,
            Score = PlayData.Score,
            Tp = PlayData.Tp,
            MaxCombo = PlayData.MaxCombo,
            PerfectCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteGrading.Perfect),
            GreatCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteGrading.Great),
            GoodCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteGrading.Good),
            BadCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteGrading.Bad),
            MissCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteGrading.Miss)
        };
        CytoidApplication.LastPlayResult = result;
        StartCoroutine(EndCoroutine());
    }

    private IEnumerator EndCoroutine()
    {
        yield return new WaitForSeconds(6f);
        RankedPlayData.end = TimeExt.Millis();
        action = Action.Result;
        if (CytoidApplication.UseDoozyUI)
        {
            // Comment out this section and the import if DoozyUI not used
            UIManager.HideUiElement("ScoreText", "Game");
            UIManager.HideUiElement("ComboText", "Game");
            UIManager.HideUiElement("TpText", "Game");
            UIManager.HideUiElement("TitleText", "Game");
            UIManager.HideUiElement("Mask", "Game");
        }
        else
        {
            DoAction();
        }
    }

    public void DoAction()
    {
        if (!CytoidApplication.CurrentLevel.is_internal && Application.platform == RuntimePlatform.Android)
        {
            ANAMusic.release(anaId);
        }
        switch (action)
        {
            case Action.Back:
                print("Loading LevelSelection scene.");
                StartCoroutine(GoBackToLevelSelectionCoroutine());
                break;
            case Action.Retry:
                BackgroundCanvasHelper.PersistBackgroundCanvas();
                SceneManager.LoadScene("Game");
                break;
            case Action.Result:
                BackgroundCanvasHelper.PersistBackgroundCanvas();
                print("Loading GameResult scene.");
                SceneManager.LoadScene("GameResult");
                break;
        }
    }

    public void SetAction(string action)
    {
        this.action = action;
    }

    private IEnumerator GoBackToLevelSelectionCoroutine()
    {
        sceneTransitionMask.willFadeIn = true;
        sceneTransitionMask.GetComponent<Image>().raycastTarget = true; // Block button interactions
        while (sceneTransitionMask.IsFading) yield return null;
        foreach (DictionaryEntry entry in NoteViews)
        {
            var noteView = entry.Value as NoteView;
            if (noteView == null) continue; // Already cleared/destroyed
            Destroy(noteView.gameObject);
        }
        SceneManager.LoadScene("LevelSelection");
    }

    private List<NoteView> OnScreenRegularAndHoldNotes;
    private List<NoteView> OnScreenChainNotes;
    private List<NoteView> OnScreenHoldNotes;

    private void UpdateNoteViews()
    {
        OnScreenRegularAndHoldNotes.Clear();
        OnScreenChainNotes.Clear();
        OnScreenHoldNotes.Clear();
        foreach (DictionaryEntry entry in NoteViews)
        {
            var noteView = entry.Value as NoteView;
            if (noteView == null) continue;
            var timeUntil = noteView.note.time - TimeElapsed;
            noteView.TimeUntil = timeUntil;
            noteView.TimeDiff = Math.Abs(timeUntil);
            if (noteView.note.type != NoteType.Chain && noteView.displayed && !noteView.cleared)
            {
                OnScreenRegularAndHoldNotes.Add(noteView);
            }
            if (noteView.note.type == NoteType.Chain && noteView.displayed && !noteView.cleared)
            {
                OnScreenChainNotes.Add(noteView);
            }
            if (noteView.note.type == NoteType.Hold && !((HoldNoteView) noteView).isHolding && noteView.displayed &&
                !noteView.cleared)
            {
                OnScreenHoldNotes.Add(noteView);
            }
        }
    }

    private readonly Dictionary<int, HoldNoteView> holdingNotes = new Dictionary<int, HoldNoteView>();

    private void OnFingerDown(LeanFinger finger)
    {
        var pos = Camera.main.ScreenToWorldPoint(finger.ScreenPosition);

        var touchedChain = false;
        
        // Query chain notes
        foreach (var noteView in OnScreenChainNotes)
        {
            if (noteView == null) continue;
            if (noteView.OverlapPoint(pos))
            {
                noteView.Touch(finger.ScreenPosition);
                touchedChain = true;
                break; // Note that we want to query hold notes as well so break only
            }
        }
        
        foreach (var noteView in OnScreenRegularAndHoldNotes)
        {
            if (noteView == null) continue;
            if (noteView.OverlapPoint(pos))
            {
                if (touchedChain && noteView.TimeDiff > Chart.pageDuration / 8) continue;
                noteView.Touch(finger.ScreenPosition);
                return;
            }
        }

    }


    private void OnFingerSet(LeanFinger finger)
    {
        var pos = Camera.main.ScreenToWorldPoint(finger.ScreenPosition);
        
        // Query chain notes
        foreach (var noteView in OnScreenChainNotes)
        {
            if (noteView == null) continue;
            if (noteView.OverlapPoint(pos))
            {
                noteView.Touch(finger.ScreenPosition);
                break; // Note that we want to query hold notes as well so break only
            }
        }

        if (!holdingNotes.ContainsKey(finger.Index))
        {
            var newHold = false;
            // Query again, this time looking for (unholded) hold notes
            foreach (var noteView in OnScreenHoldNotes)
            {
                if (noteView == null) continue;
                if (noteView.OverlapPoint(pos))
                {
                    holdingNotes.Add(finger.Index, (HoldNoteView) noteView);
                    ((HoldNoteView) noteView).StartHoldBy(finger.Index);
                    newHold = true;
                    break;
                }
            }
            if (!newHold)
            {
                foreach (var entry in holdingNotes)
                {
                    var noteView = entry.Value;
                    if (noteView.OverlapPoint(pos))
                    {
                        holdingNotes.Add(finger.Index, noteView);
                        noteView.StartHoldBy(finger.Index);
                        break;
                    }
                }
            }
        }
        else
        {
            // Holding the note already
            var holdNote = holdingNotes[finger.Index];
            var released = false;

            // If note cleared
            if (holdNote.cleared)
            {
                holdingNotes.Remove(finger.Index);
                released = true;
            } else 
            // If holding elsewhere
            if (!holdNote.OverlapPoint(pos))
            {
                holdNote.StopHoldBy(finger.Index);
                holdingNotes.Remove(finger.Index);
                released = true;
            }

            if (released)
            {
                holdNote.rankedNoteData.release_time = TimeExt.Millis();
                holdNote.rankedNoteData.release_x = (int) finger.ScreenPosition.x;
                holdNote.rankedNoteData.release_y = (int) finger.ScreenPosition.y;
            }
        }
    }

    private void OnFingerUp(LeanFinger finger)
    {
        if (holdingNotes.ContainsKey(finger.Index))
        {
            var holdNote = holdingNotes[finger.Index];
            holdNote.StopHoldBy(finger.Index);
            holdingNotes.Remove(finger.Index);
        }
    }

    public void NotifyWillPause()
    {
        if (IsPaused) return;
        willPause = true;
    }

    public void NotifyWillUnpause()
    {
        if (!IsPaused) return;
        Unpause();
    }

    public void SetAllowPause(bool allowPause)
    {
        pauseButton.gameObject.SetActive(allowPause);
    }
    
}