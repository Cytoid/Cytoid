﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DoozyUI;
using Lean.Touch;
 using LunarConsolePluginInternal;
 using MoreLinq;
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
    }

    public string editorLevelOverride;
    public string editorChartTypeOverride;
    public string editorLevelFallback;
    public string editorChartTypeFallback;
    public bool autoPlay;
    
    public float startAt;
    public float editorStartAtOverride;

    public bool willPause;
    public float pausedAt;

    public bool IsPaused { get; private set; }

    public float StartTime { get; private set; }

    [SerializeField]
    public float TimeElapsed { get; private set; }

    [SerializeField]
    public int CurrentPage
    {
        get { return (int) ((TimeElapsed + Chart.pageShift) / Chart.pageDuration); }
    }

    public Chart Chart { get; private set; }
    public PlayData PlayData { get; private set; }
    public NoteView[] NoteViews;

    [SerializeField] private GameObject singleNotePrefab;
    [SerializeField] private GameObject holdNotePrefab;
    [SerializeField] private GameObject chainNotePrefab;
    [SerializeField] private GameObject scannerPrefab;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AlphaMask backgroundOverlayMask;
    [SerializeField] private AlphaMask sceneTransitionMask;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Text titleText;
    private GameObject background;
    private AudioClip clip;

    [SerializeField] public bool IsLoaded { get; private set; }
    [SerializeField] public bool IsEnded { get; private set; }

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
            if (CytoidApplication.UseDoozyUI)
            {
                UIManager.ShowUiElement("PauseBackground", "Game", true);
                UIManager.ShowUiElement("PauseRoot", "Game", true);
            }
        }
    }

    private IEnumerator Start()
    {
        if (PlayerPrefs.GetInt("autoplay") == 1) autoPlay = true;
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

        ThemeController.Instance.Init(level);
        titleText.text = level.title;

        // Chart and background are already loaded
        Chart = level.charts.Find(it => it.type == CytoidApplication.CurrentChartType).chart;
        PlayData = new PlayData(Chart);
        CytoidApplication.CurrentPlayData = PlayData;
        
        // Load audio clip
        var www = new WWW((level.isInternal && Application.platform == RuntimePlatform.Android ? "" : "file://") + level.basePath + level.GetMusicPath(CytoidApplication.CurrentChartType));
        yield return www;
        clip = CytoidApplication.ReadAudioClipFromWWW(www);
        audioSource.clip = clip;
        
        // Don't continue until faded in
        backgroundOverlayMask.willFadeIn = true;
        while (backgroundOverlayMask.IsFading) yield return null;
        yield return null;
        
        // Init notes
        NoteViews = new NoteView[Chart.notes.Count];
        foreach (var entry in Chart.notes)
        {
            var id = entry.Key;
            var note = entry.Value;
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
            NoteViews[id] = noteView;
        }
        
        // Register handlers
        LeanTouch.OnFingerDown += OnFingerDown;
        LeanTouch.OnFingerSet += OnFingerSet;
        LeanTouch.OnFingerUp += OnFingerUp;
        
        audioSource.time = startAt;
        var defaultOffset = 0.12f;
        if (Application.platform == RuntimePlatform.Android)
        {
            defaultOffset = 0.24f;
        }
        var userOffset = PlayerPrefs.GetFloat("user_offset", defaultOffset);
        
        // Release unused assets
        Resources.UnloadUnusedAssets();
        
        // Init scanner
        Instantiate(scannerPrefab, transform);

        const float delay = 1f;
        audioSource.PlayDelayed(delay);
        
        StartTime = Time.time + Chart.offset + userOffset + delay;
    }
    

    private void Update()
    {
        TimeElapsed = IsLoaded ? Time.time - StartTime + startAt : 0;
        UpdateNoteViews();
        if (!IsLoaded && audioSource != null && audioSource.time > 0.001f)
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
        IsPaused = true;
        willPause = false;
        UnpauseCountdown = -1;
        audioSource.Pause();
        pausedAt = audioSource.time;
        SetAllowPause(false);
        if (unpauseCoroutine != null) StopCoroutine(unpauseCoroutine);
    }

    private void Unpause()
    {
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
        audioSource.UnPause();
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
            Score = Calculations.Score(Chart, PlayData),
            Tp = Calculations.Tp(Chart, PlayData),
            MaxCombo = PlayData.MaxCombo,
            PerfectCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteRanking.Perfect),
            ExcellentCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteRanking.Excellent),
            GoodCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteRanking.Good),
            BadCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteRanking.Bad),
            MissCount = PlayData.NoteRankings.Values.Count(ranking => ranking == NoteRanking.Miss),
        };
        CytoidApplication.LastPlayResult = result;
        StartCoroutine(EndCoroutine());
    }

    private IEnumerator EndCoroutine()
    {
        yield return new WaitForSeconds(6f);
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
        switch (action)
        {
            case Action.Back:
                print("Loading LevelSelection scene.");
                StartCoroutine(GoBackToLevelSelectionCoroutine());
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
        foreach (var noteView in NoteViews)
        {
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
        foreach (var noteView in NoteViews)
        {
            if (noteView == null) continue;
            var timeUntil = noteView.note.time - TimeElapsed;
            noteView.TimeUntil = timeUntil;
            noteView.TimeDiff = Math.Abs(timeUntil);
            if (noteView.note.type != NoteType.Hold && noteView.displayed && !noteView.cleared)
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

        foreach (var noteView in OnScreenRegularAndHoldNotes)
        {
            if (noteView == null) continue;
            if (noteView.OverlapPoint(pos))
            {
                //TODO: Verify is minBy() needed?
                noteView.Touch();
                return;
            }
        }
        return;
        
        var onScreenNotes = (from noteView
                in NoteViews
            where noteView != null && noteView.note.type != NoteType.Hold && (noteView.displayed && !noteView.cleared && noteView.OverlapPoint(pos))
            select noteView).ToList();

        if (!onScreenNotes.Any()) return;

        var first = onScreenNotes.MinBy(noteView => noteView.note.time);

        if (first != null)
        {
            first.Touch();
        }
    }
    

    private void OnFingerSet(LeanFinger finger)
    {
        var pos = Camera.main.ScreenToWorldPoint(finger.ScreenPosition);

        if (!holdingNotes.ContainsKey(finger.Index))
        {

            // Query chain notes
            foreach (var noteView in OnScreenChainNotes)
            {
                if (noteView == null) continue;
                if (noteView.OverlapPoint(pos))
                {
                    noteView.Touch();
                    break; // Note that we want to query hold notes as well so break only
                }
            }

            // Query again, this time looking for (unholded) hold notes
            foreach (var noteView in OnScreenHoldNotes)
            {
                if (noteView == null) continue;
                if (noteView.OverlapPoint(pos))
                {
                    holdingNotes.Add(finger.Index, (HoldNoteView) noteView);
                    ((HoldNoteView) noteView).StartHolding();
                    break;
                }
            }

        }
        else
        {
            // Holding the note already
            var holdNote = holdingNotes[finger.Index];
            
            // If note cleared
            if (holdNote.cleared)
            {
                holdingNotes.Remove(finger.Index);
                return;
            }
            
            // If holding elsewhere
            if (!holdNote.OverlapPoint(pos))
            {
                holdNote.StopHolding();
                holdingNotes.Remove(finger.Index);
            }
        }

    }

    private void OnFingerUp(LeanFinger finger)
    {
        if (holdingNotes.ContainsKey(finger.Index))
        {
            var holdNote = holdingNotes[finger.Index];
            holdNote.StopHolding();
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