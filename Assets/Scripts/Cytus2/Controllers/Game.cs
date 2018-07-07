using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cytus2.Models;
using Cytus2.Views;
using Lean.Touch;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cytus2.Controllers
{
    public class Game : SingletonMonoBehavior<Game>
    {
        public GameView View;

        public Level Level;
        public Chart Chart;
        public Dictionary<int, GameNote> GameNotes = new Dictionary<int, GameNote>();
        public PlayData PlayData;
        
        [SerializeField] protected GameObject ClickNotePrefab;
        [SerializeField] protected GameObject HoldNotePrefab;
        [SerializeField] protected GameObject LongHoldNotePrefab;
        [SerializeField] protected GameObject DragHeadNotePrefab;
        [SerializeField] protected GameObject DragChildNotePrefab;
        [SerializeField] protected GameObject FlickNotePrefab;
        [SerializeField] protected GameObject DragLinePrefab;
        [SerializeField] protected AudioSource AudioSource;
        [SerializeField] protected ScannerView Scanner;

        public float Time { get; protected set; }
        public float StartTime { get; protected set; }
        public float PauseTime { get; protected set; }
        public float PauseDuration { get; protected set; } // Accumulated
        public float PauseAt { get; protected set; }
        public float AudioPercentage { get; protected set; }

        public int CurrentPageId
        {
            get { return Chart.CurrentPageId; }
        }

        public bool IsLoaded { get; protected set; }
        public bool IsPlaying { get; protected set; }
        public bool IsCompleted { get; protected set; }

        public int UnpauseCountdown;

        private int currentNoteId;
        private int currentEventId;
        private int currentAnimId;
        private int currentPageId;

        private int lastNoteId;

        private readonly Dictionary<int, HoldNote> holdingNotes = new Dictionary<int, HoldNote>();
        private readonly Dictionary<int, FlickNote> flickingNotes = new Dictionary<int, FlickNote>();

        private readonly List<GameNote> touchableNormalNotes = new List<GameNote>(); // Click, Hold, Long hold
        private readonly List<GameNote> touchableDragNotes = new List<GameNote>(); // Drag head, Drag child
        private readonly List<HoldNote> touchableHoldNotes = new List<HoldNote>(); // Hold, Long hold

        private Coroutine unpauseCoroutine;

        protected override void Awake()
        {
            base.Awake();
            
            Application.targetFrameRate = 60;

            IsLoaded = false;
            IsPlaying = false;
            IsCompleted = false;
            UnpauseCountdown = -1;
            PauseTime = -1;
            PauseDuration = 0;
            PauseAt = -1;

            View = new GameView(this);
            View.OnAwake();
        }

        protected virtual IEnumerator Start()
        {
            BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);

            // Load level
            if (CytoidApplication.CurrentLevel != null)
            {
                Level = CytoidApplication.CurrentLevel;
            }
            else
            {
                
                Level = JsonConvert.DeserializeObject<Level>(File.ReadAllText(Application.persistentDataPath + "/player/level.json"));
                Level.BasePath = Application.persistentDataPath + "/player/";
                CytoidApplication.CurrentChartType = Level.charts[0].type;
                
                var www = new WWW("file://" + Level.BasePath + Level.background.path);
                yield return www;
                yield return null; // Wait an extra frame
        
                www.LoadImageIntoTexture(CytoidApplication.BackgroundTexture);

                var backgroundSprite =
                    Sprite.Create(CytoidApplication.BackgroundTexture,
                        new Rect(0, 0, CytoidApplication.BackgroundTexture.width, CytoidApplication.BackgroundTexture.height),
                        new Vector2(0, 0));
                var background = GameObject.FindGameObjectWithTag("Background");
                background.GetComponent<Image>().sprite = backgroundSprite;

                www.Dispose();
                Resources.UnloadUnusedAssets();

                // Fill the screen by adapting to the aspect ratio
                background.GetComponent<AspectRatioFitter>().aspectRatio =
                    (float) CytoidApplication.BackgroundTexture.width / CytoidApplication.BackgroundTexture.height;
                yield return null; // Wait an extra frame

                CytoidApplication.CurrentLevel = Level;
            }
            
            
            // System settings
            CytoidApplication.SetAutoRotation(false);
            if (Application.platform == RuntimePlatform.Android && !Level.is_internal)
            {
                GameOptions.Instance.UseAndroidNativeAudio = true;
            }
            
            // Load chart
            if (!Level.IsLoadedIntoMemory)
            {
                Level.LoadChartsIntoMemory();
            }

            if (CytoidApplication.CurrentChartType == null)
            {
                CytoidApplication.CurrentChartType = Level.charts[0].type;
            }
            
            Chart = (Chart) Level.charts.Find(it => it.type == CytoidApplication.CurrentChartType).chart;
            
            // Load audio
            var audioPath = Level.BasePath + Level.GetMusicPath(CytoidApplication.CurrentChartType);

            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                nativeAudioId = ANAMusic.load(audioPath, true); // This would ensure the audio gets loaded
            }
            else
            {
                var www = new WWW("file://" + audioPath);
                yield return www;

                AudioSource.clip = www.GetAudioClip();

                www.Dispose();
            }

            // Touch handlers
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerSet += OnFingerSet;
            LeanTouch.OnFingerUp += OnFingerUp;
            
            // Game options
            // TODO: Inverse
            var options = GameOptions.Instance;
#if UNITY_EDITOR
            options.WillAutoPlay = true;
#endif
            options.IsRanked = false; // TODO
            options.HitboxMultiplier = PlayerPrefsExt.GetBool("larger_hitboxes") ? 1.5555f : 1.3333f;
            options.ShowEarlyLateIndicator = PlayerPrefsExt.GetBool("early_late_indicator");
            options.ChartOffset = PlayerPrefs.GetFloat("user_offset", 0.08f);
            if (ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(Level)))
            {
                options.ChartOffset = ZPlayerPrefs.GetFloat(PreferenceKeys.NoteDelay(Level));
            }
            
            // Play data
            PlayData = new PlayData(false, Chart); // TODO
            CytoidApplication.CurrentPlayData = PlayData;
            
            yield return null;
            
            View.OnStart();
            
            // TODO: Rank data
            
            IsLoaded = true;

            EventKit.Broadcast("game start");

            StartGame();
        }

        protected void StartGame()
        {
            IsPlaying = true;

            lastNoteId = Chart.Root.note_list.Last().id;
            
            Scanner.PlayEnter();

            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                if (GameOptions.Instance.StartAt > 0.00001)
                {
                    ANAMusic.seekTo(nativeAudioId, (int) (GameOptions.Instance.StartAt * 1000f));
                }
                ANAMusic.play(nativeAudioId);
            }
            else
            {
                AudioSource.time = GameOptions.Instance.StartAt;
                AudioSource.Play();
            }
            
            StartTime = UnityEngine.Time.time;
        }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Pause();
                return;
            }

            if (IsCompleted)
            {
                Time += UnityEngine.Time.deltaTime;
                return;
            }

            if (IsPlaying)
            {
                
                if (PlayData.NoteCleared >= Chart.Root.note_list.Count)
                {
                    Complete();
                }
                
                UpdateOnScreenNotes();

                if (GameOptions.Instance.UseAndroidNativeAudio)
                {
                    Time = UnityEngine.Time.time - StartTime + GameOptions.Instance.StartAt - 
                           GameOptions.Instance.ChartOffset - PauseDuration;
                    AudioPercentage = Time / ANAMusic.getDuration(nativeAudioId);
                }
                else
                {
                    Time = AudioSource.timeSamples * 1.0f / AudioSource.clip.frequency -
                           GameOptions.Instance.ChartOffset;
                    AudioPercentage = Time / AudioSource.clip.length;
                }

                var chart = Chart.Root;
                var notes = chart.note_list;

                Scanner.transform.position = new Vector3(0, Chart.GetScannerPosition(Time));
                Scanner.Direction = Chart.CurrentPageId < chart.page_list.Count
                    ? chart.page_list[Chart.CurrentPageId].scan_line_direction
                    : -chart.page_list[Chart.CurrentPageId - 1].scan_line_direction;

                while (currentEventId < chart.event_order_list.Count &&
                       chart.event_order_list[currentEventId].time < Time)
                {
                    // TODO: Speed up text
                    // TODO: Clean up this mess
                    if (chart.event_order_list[currentEventId].event_list[0].type == 0)
                    {
                        Scanner.PlaySpeedUp();
                    }
                    else
                    {
                        Scanner.PlaySpeedDown();
                    }

                    currentEventId++;
                }

                while (currentAnimId < chart.animation_list.Count &&
                       chart.animation_list[currentAnimId].time < Time)
                {
                    // TODO: Subtitle animation
                    currentAnimId++;
                }

                while (currentPageId < chart.page_list.Count && chart.page_list[currentPageId].end_time <= Time)
                {
                    // TODO: Boundary animations
                    currentPageId++;
                }

                while (currentNoteId < notes.Count && notes[currentNoteId].start_time - 2.0f < Time)
                {
                    switch (notes[currentNoteId].type)
                    {
                        case NoteType.DragHead:
                            int id = currentNoteId;
                            while (notes[id].next_id > 0)
                            {
                                SpawnDragLine(notes[id], notes[notes[id].next_id]);
                                id = notes[id].next_id;
                            }
                            SpawnNote(notes[currentNoteId]);
                            currentNoteId++;
                            break;
                        default:
                            SpawnNote(notes[currentNoteId]);
                            currentNoteId++;
                            break;
                    }

                    // TODO: Complete
                }
            }
        }

        protected virtual void UpdateOnScreenNotes()
        {
            touchableNormalNotes.Clear();
            touchableDragNotes.Clear();
            touchableHoldNotes.Clear();
            for (var id = 0; id <= lastNoteId; id++)
            {
                if (!GameNotes.ContainsKey(id)) continue;
                
                var note = GameNotes[id];
                if (!note.HasEmerged || note.IsCleared) continue;

                if (note.Note.type != NoteType.DragHead && note.Note.type != NoteType.DragChild)
                {
                    touchableNormalNotes.Add(note);
                }
                
                if (note.Note.type == NoteType.DragHead || note.Note.type == NoteType.DragChild)
                {
                    touchableDragNotes.Add(note);
                }
                
                if ((note.Note.type == NoteType.Hold || note.Note.type == NoteType.LongHold) && !((HoldNote) note).IsHolding)
                {
                    touchableHoldNotes.Add((HoldNote) note);
                }
            }
        }

        protected virtual void OnFingerDown(LeanFinger finger)
        {
            var pos = Camera.main.orthographic ? Camera.main.ScreenToWorldPoint(finger.ScreenPosition) : Camera.main.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));

            var touchedDrag = false;
            
            // Query drag notes first
            foreach (var note in touchableDragNotes)
            {
                if (note == null) continue;
                if (note.DoesCollide(pos))
                {
                    note.Touch(finger.ScreenPosition);
                    touchedDrag = true;
                    break; // Query other notes too!
                }
            }

            foreach (var note in touchableNormalNotes)
            {
                if (note == null) continue;
                if (note.DoesCollide(pos))
                {
                    if (touchedDrag && Math.Abs(note.TimeUntilStart) > note.Page.Duration / 8f) continue;
                    if (note is FlickNote)
                    {
                        if (flickingNotes.ContainsKey(finger.Index) || flickingNotes.ContainsValue((FlickNote) note)) continue;
                        flickingNotes.Add(finger.Index, (FlickNote) note);
                        ((FlickNote) note).StartFlicking(pos);
                    }
                    else
                    {
                        note.Touch(finger.ScreenPosition);
                    }

                    return;
                }
            }
        }

        protected virtual void OnFingerSet(LeanFinger finger)
        {
            var pos = Camera.main.orthographic ? Camera.main.ScreenToWorldPoint(finger.ScreenPosition) : Camera.main.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 10));
            
            // Query flick note
            if (flickingNotes.ContainsKey(finger.Index))
            {
                var flickingNote = flickingNotes[finger.Index];
                var cleared = flickingNote.UpdateFingerPosition(pos);
                if (cleared)
                {
                    flickingNotes.Remove(finger.Index);
                }
            }
            
            // Query drag notes
            foreach (var note in touchableDragNotes)
            {
                if (note == null) continue;
                if (note.DoesCollide(pos))
                {
                    note.Touch(finger.ScreenPosition);
                    break; // Query other notes too!
                }
            }

            // If this is a new finger
            if (!holdingNotes.ContainsKey(finger.Index))
            {
                
                var heldNew = false; // If the finger holds a new note
                
                // Query unheld hold notes
                foreach (var note in touchableHoldNotes)
                {
                    if (note == null) continue;
                    if (note.DoesCollide(pos))
                    {
                        holdingNotes.Add(finger.Index, note);
                        note.StartHoldingBy(finger.Index);
                        heldNew = true;
                        break;
                    }
                }

                // Query held hold notes (i.e. multiple fingers on the same hold note)
                if (!heldNew)
                {
                    foreach (var holdNote in holdingNotes.Values)
                    {
                        if (holdNote.DoesCollide(pos))
                        {
                            holdingNotes.Add(finger.Index, holdNote);
                            holdNote.StartHoldingBy(finger.Index);
                            break;
                        }
                    }
                }
            }
            else // The finger is already holding a note
            {
                var holdNote = holdingNotes[finger.Index];
                var released = false;

                if (holdNote.IsCleared) // If cleared
                {
                    holdingNotes.Remove(finger.Index);
                    released = true;
                }
                else if (!holdNote.DoesCollide(pos)) // If holding elsewhere
                {
                    holdNote.StopHoldingBy(finger.Index);
                    holdingNotes.Remove(finger.Index);
                    released = true;
                }

                if (released)
                {
                    // TODO: Rank data
                }
            }
        }

        protected virtual void OnFingerUp(LeanFinger finger)
        {
            if (holdingNotes.ContainsKey(finger.Index))
            {
                var holdNote = holdingNotes[finger.Index];
                holdNote.StopHoldingBy(finger.Index);
                holdingNotes.Remove(finger.Index);
            }
        }

        protected virtual void OnApplicationPause(bool willPause)
        {
            if (willPause) Pause();
        }

        public void Pause()
        {
            if (!IsLoaded || IsCompleted) return;
            if (!IsPlaying) return;
            // TODO: Rank data
            IsPlaying = false;

            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                ANAMusic.pause(nativeAudioId);
                PauseAt = ANAMusic.getCurrentPosition(nativeAudioId) / 1000f;
            }
            else
            {
                AudioSource.Pause();
            }
            
            PauseTime = UnityEngine.Time.time;
            holdingNotes.Values.ToList().ForEach(note => note.StopHolding());
            holdingNotes.Clear();

            View.OnPause();

            if (unpauseCoroutine != null)
            {
                StopCoroutine(unpauseCoroutine);
                unpauseCoroutine = null;
            }
        }

        public void Unpause()
        {
            if (!IsLoaded || IsCompleted) return;
            if (IsPlaying) return;

            // TODO: Rank data

            View.OnUnpause();
            unpauseCoroutine = StartCoroutine(UnpauseCoroutine());
        }

        protected virtual IEnumerator UnpauseCoroutine()
        {
            UnpauseCountdown = 3;
            while (UnpauseCountdown > 0)
            {
                yield return new WaitForSeconds(1f);
                UnpauseCountdown--;
            }

            UnpauseCountdown = -1;
            UnpauseImmediately();
        }

        public virtual void UnpauseImmediately()
        {
            IsPlaying = true;

            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                ANAMusic.seekTo(nativeAudioId, (int) (PauseAt * 1000f));
                ANAMusic.play(nativeAudioId);
            }
            else
            {
                AudioSource.UnPause();
            }
            
            PauseDuration += UnityEngine.Time.time - PauseTime;
            PauseTime = -1;
            PauseAt = -1;
        }

        public virtual void Complete()
        {
            if (IsCompleted) return;
            IsCompleted = true;
            
            Scanner.PlayExit();

            var result = new PlayResult
            {
                Ranked = false, // TODO
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

            action = Action.Result;
            StartCoroutine(ProceedToResultCoroutine());
        }

        private IEnumerator ProceedToResultCoroutine()
        {
            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                while (ANAMusic.isPlaying(nativeAudioId))
                {
                    yield return null;
                }
            }
            else
            {
                while (AudioSource.isPlaying)
                {
                    yield return null;
                }
            }
            
            // Destroy all game notes
            foreach (var entry in GameNotes)
            {
                var note = entry.Value;
                if (note == null) continue;
                Destroy(note.gameObject);
            }

            // TODO: Rank data
            View.OnProceedToResult();
        }

        public virtual void SpawnNote(ChartNote note)
        {
            GameNote gameNote;
            switch (note.type)
            {
                case NoteType.Click:
                    gameNote = Instantiate(ClickNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
                case NoteType.Hold:
                    gameNote = Instantiate(HoldNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
                case NoteType.LongHold:
                    gameNote = Instantiate(LongHoldNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
                case NoteType.DragHead:
                    gameNote = Instantiate(DragHeadNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
                case NoteType.DragChild:
                    gameNote = Instantiate(DragChildNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
                case NoteType.Flick:
                    gameNote = Instantiate(FlickNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
                default:
                    gameNote = Instantiate(ClickNotePrefab, transform.parent).GetComponent<GameNote>();
                    break;
            }

            gameNote.Init(Chart.Root, note);
            GameNotes.Add(note.id, gameNote);
        }

        public void SpawnDragLine(ChartNote from, ChartNote to)
        {
            var dragLineView = Instantiate(DragLinePrefab, transform.parent).GetComponent<DragLineView>();
            dragLineView.FromNote = from;
            dragLineView.ToNote = to;
        }

        public void Clear(GameNote note)
        {
            PlayData.Clear(note.Note.id, note.CalculateGrading(), note.GreatGradeWeight);
        }
        
        // TODO: Get away, Doozy UI
        
        private string action = Action.Result;

        public static class Action
        {
            public const string Result = "Result";
            public const string Back = "Back";
            public const string Retry = "Retry";
        }
        
        public void SetAction(string action)
        {
            this.action = action;
        }
        
        public void DoAction()
        {
            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                ANAMusic.release(nativeAudioId);
            }
            switch (action)
            {
                case Action.Back:
                    print("Loading LevelSelection scene.");
                    StartCoroutine(View.ReturnToLevelSelectionCoroutine());
                    break;
                case Action.Retry:
                    BackgroundCanvasHelper.PersistBackgroundCanvas();
                    SceneManager.LoadScene("CytusGame");
                    break;
                case Action.Result:
                    BackgroundCanvasHelper.PersistBackgroundCanvas();
                    print("Loading GameResult scene.");
                    SceneManager.LoadScene("GameResult");
                    break;
            }
        }
        
        // SECTION: Android Native Audio

        protected int nativeAudioId;

    }
}