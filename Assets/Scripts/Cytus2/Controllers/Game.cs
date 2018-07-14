using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cytoid.Storyboard;
using Cytus2.Models;
using Cytus2.Views;
using DG.Tweening;
using DoozyUI;
using Lean.Touch;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sprite = UnityEngine.Sprite;
using Text = UnityEngine.UI.Text;

namespace Cytus2.Controllers
{
    public class Game : SingletonMonoBehavior<Game>
    {
        public GameView View;

        public Level Level;
        public Chart Chart;
        public Dictionary<int, GameNote> GameNotes = new Dictionary<int, GameNote>();
        public Play Play;
        public RankedModeData RankData;
        
        [SerializeField] protected GameObject ClickNotePrefab;
        [SerializeField] protected GameObject HoldNotePrefab;
        [SerializeField] protected GameObject LongHoldNotePrefab;
        [SerializeField] protected GameObject DragHeadNotePrefab;
        [SerializeField] protected GameObject DragChildNotePrefab;
        [SerializeField] protected GameObject FlickNotePrefab;
        [SerializeField] protected GameObject DragLinePrefab;
        [SerializeField] protected Canvas InfoCanvas;
        [SerializeField] protected AudioSource AudioSource;
        [SerializeField] protected ScanlineView Scanline;

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
        public bool IsFailed { get; protected set; }

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
            
            Application.targetFrameRate = 120;

            IsLoaded = false;
            IsPlaying = false;
            IsCompleted = false;
            IsFailed = false;
            UnpauseCountdown = -1;
            PauseTime = -1;
            PauseDuration = 0;
            PauseAt = -1;
            
            // Play data
            var isRanked = PlayerPrefsExt.GetBool("ranked") && User.Exists();
            Play = new Play(isRanked);
            Play.Mods = new HashSet<Mod>(PlayerPrefsExt.GetStringArray("mods", new string[0]).ToList().ConvertAll(mod => (Mod) Enum.Parse(typeof(Mod), mod)));
            CytoidApplication.CurrentPlay = Play;

            View = new GameView(this);
            
            // Enable/disable FPS counter
            var fpsCounter = GameObject.FindGameObjectWithTag("FpsCounter");
            if (fpsCounter != null)
            {
                fpsCounter.SetActive(PlayerPrefsExt.GetBool("fps counter"));
            }
            
            BackgroundCanvasHelper.SetupBackgroundCanvas(gameObject.scene);
        }

        protected virtual IEnumerator Start()
        {
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
            if (CytoidApplication.CurrentChartType == null)
            {
                CytoidApplication.CurrentChartType = Level.charts[0].type;
            }
            
            var chartSection = Level.charts.Find(it => it.type == CytoidApplication.CurrentChartType);
            
            string chartText;
            if (Level.is_internal && Application.platform == RuntimePlatform.Android)
            {
                var www = new WWW(Level.BasePath + chartSection.path);
                while (!www.isDone)
                {
                }
                chartText = Encoding.UTF8.GetString(www.bytes);
            }
            else
            {
                chartText = File.ReadAllText(Level.BasePath + chartSection.path, Encoding.UTF8);
            }

            Chart = new Chart(
                chartText, 
                0.8f + (5 - (int) PlayerPrefs.GetFloat("horizontal margin", 3) - 1) * 0.025f, 
                (5.5f + (5 - (int) PlayerPrefs.GetFloat("vertical margin", 3)) * 0.5f) / 9.0f
            );
            
            // Load audio
            var audioPath = Level.BasePath + Level.GetMusicPath(CytoidApplication.CurrentChartType);

            if (GameOptions.Instance.UseAndroidNativeAudio)
            {
                nativeAudioId = ANAMusic.load(audioPath, true); // This would ensure the audio gets loaded
            }
            else
            {
                var www = new WWW((Level.is_internal && Application.platform == RuntimePlatform.Android) ? "" : "file://" + audioPath);
                yield return www;

                AudioSource.clip = www.GetAudioClip();

                www.Dispose();
            }

            // Touch handlers
            LeanTouch.OnFingerDown += OnFingerDown;
            LeanTouch.OnFingerSet += OnFingerSet;
            LeanTouch.OnFingerUp += OnFingerUp;
            
            // Game options
            var options = GameOptions.Instance;
            options.HitboxMultiplier = PlayerPrefsExt.GetBool("larger_hitboxes") ? 1.5555f : 1.3333f;
            options.ShowEarlyLateIndicator = PlayerPrefsExt.GetBool("early_late_indicator");
            options.ChartOffset = PlayerPrefs.GetFloat("user_offset", 0.08f);
            if (ZPlayerPrefs.GetBool(PreferenceKeys.WillOverrideOptions(Level)))
            {
                options.ChartOffset = ZPlayerPrefs.GetFloat(PreferenceKeys.NoteDelay(Level));
            }

            Play.Init(Chart);
            
            // Rank data
            if (Play.IsRanked)
            {
                RankData = new RankedModeData();
                RankData.user = User.Instance.username;
                RankData.password = User.Instance.password;
                RankData.start = TimeExt.Millis();
                RankData.id = Level.id;
                RankData.type = CytoidApplication.CurrentChartType;
                RankData.mods = string.Join(",", Array.ConvertAll(Play.Mods.ToArray(), mod => mod.ToString()));
                RankData.version = Level.version;
                RankData.chart_checksum = Chart.Checksum;
                print("Chart checksum: " + Chart.Checksum);
                RankData.device.width = Screen.width;
                RankData.device.height = Screen.height;
                RankData.device.dpi = (int) Screen.dpi;
                RankData.device.model = SystemInfo.deviceModel;
                CytoidApplication.CurrentRankedModeData = RankData;
            }
            else
            {
                CytoidApplication.CurrentRankedModeData = null;
            }

            yield return new WaitForSeconds(0.5f);
            
            View.OnStart();
            
            IsLoaded = true;

            EventKit.Broadcast("game start");

            StartGame();
        }

        protected void StartGame()
        {
            IsPlaying = true;

            lastNoteId = Chart.Root.note_list.Last().id;

            if (Mod.HideScanline.IsEnabled())
            {
                Scanline.GetComponent<LineRenderer>().enabled = false;
            }
            
            Scanline.PlayEnter();
            
            Play.MaxHp = Level.GetDifficulty(CytoidApplication.CurrentChartType) * 75;
            if (Play.MaxHp <= 0) Play.MaxHp = 1000;
            Play.Hp = Play.MaxHp;

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

            if (IsFailed)
            {
                AudioSource.volume -= 1f / 120f;
            }

            if (IsPlaying)
            {
                
                if (Play.NoteCleared >= Chart.Root.note_list.Count)
                {
                    Complete();
                }
                
                UpdateOnScreenNotes();

                if (GameOptions.Instance.UseAndroidNativeAudio)
                {
                    Time = UnityEngine.Time.time - StartTime + GameOptions.Instance.StartAt -
                           GameOptions.Instance.ChartOffset + Chart.MusicOffset - PauseDuration;
                    AudioPercentage = Time / ANAMusic.getDuration(nativeAudioId);
                }
                else
                {
                    Time = AudioSource.timeSamples * 1.0f / AudioSource.clip.frequency -
                           GameOptions.Instance.ChartOffset + Chart.MusicOffset;
                    AudioPercentage = Time / AudioSource.clip.length;
                }

                var chart = Chart.Root;
                var notes = chart.note_list;

                Scanline.transform.DOMoveY(Chart.GetScannerPosition(Time), UnityEngine.Time.deltaTime).SetEase(Ease.Linear);
                // Scanline.transform.position = new Vector3(0, Chart.GetScannerPosition(Time));
                Scanline.Direction = Chart.CurrentPageId < chart.page_list.Count
                    ? chart.page_list[Chart.CurrentPageId].scan_line_direction
                    : -chart.page_list[Chart.CurrentPageId - 1].scan_line_direction;

                while (currentEventId < chart.event_order_list.Count &&
                       chart.event_order_list[currentEventId].time < Time)
                {
                    // TODO: Speed up text
                    // TODO: Clean up this mess
                    if (chart.event_order_list[currentEventId].event_list[0].type == 0)
                    {
                        Scanline.PlaySpeedUp();
                    }
                    else
                    {
                        Scanline.PlaySpeedDown();
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
                            var id = currentNoteId;
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
            if (IsCompleted || IsFailed) return;
            
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
            if (IsCompleted || IsFailed) return;
            
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
                    holdNote.RankData.release_time = TimeExt.Millis();
                    holdNote.RankData.release_x = (int) finger.ScreenPosition.x;
                    holdNote.RankData.release_y = (int) finger.ScreenPosition.y;
                }
            }
        }

        protected virtual void OnFingerUp(LeanFinger finger)
        {
            if (IsCompleted || IsFailed) return;

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
            if (!IsLoaded || IsCompleted || IsFailed) return;
            if (!IsPlaying) return;
            
            IsPlaying = false;

            if (RankData != null)
            {
                RankData.pauses.Add(new RankedModeData.Pause { start = TimeExt.Millis() });
            }
            
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
            if (!IsLoaded || IsCompleted || IsFailed) return;
            if (IsPlaying) return;
            
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

            if (RankData != null)
            {
                RankData.pauses.Last().end = TimeExt.Millis();
            }

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

        public void Fail()
        {
            if (IsFailed) return;
            IsFailed = true;

            UIManager.ShowUiElement("FailBackground", "Game");
            UIManager.ShowUiElement("FailRoot", "Game");
        }

        public virtual void Complete()
        {
            if (IsCompleted || IsFailed) return;
            IsCompleted = true;
            
            Scanline.PlayExit();

            if (Mod.Auto.IsEnabled() || Mod.AutoDrag.IsEnabled() || Mod.AutoFlick.IsEnabled() ||
                Mod.AutoHold.IsEnabled())
            {
                action = Action.Back;
                StartCoroutine(BackCoroutine());
            }
            else
            {
                action = Action.Result;
                StartCoroutine(ProceedToResultCoroutine());
            }
        }
        
        private IEnumerator BackCoroutine()
        {
            yield return new WaitForSeconds(1f);
            DoAction();
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

            if (RankData != null)
            {
                RankData.end = TimeExt.Millis();
                
                RankData.score = (long) Play.Score;
                RankData.accuracy = (int) (Play.Tp * 1000000);
                RankData.max_combo = Play.MaxCombo;
                RankData.perfect = Play.NoteRankings.Values.Count(grading => grading == NoteGrading.Perfect);
                RankData.great = Play.NoteRankings.Values.Count(grading => grading == NoteGrading.Great);
                RankData.good = Play.NoteRankings.Values.Count(grading => grading == NoteGrading.Good);
                RankData.bad = Play.NoteRankings.Values.Count(grading => grading == NoteGrading.Bad);
                RankData.miss = Play.NoteRankings.Values.Count(grading => grading == NoteGrading.Miss);
                
                RankData.checksum = Checksum.From(RankData);
            }

            // Destroy all game notes
            foreach (var entry in GameNotes)
            {
                var note = entry.Value;
                if (note == null) continue;
                Destroy(note.gameObject);
            }
            
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
            
            // Generate note id holder

            if (this is StoryboardGame || PlayerPrefsExt.GetBool("note ids"))
            {
                var canvas = Instantiate(InfoCanvas, GameNotes[note.id].transform.Find("NoteFill"));
                canvas.GetComponentInChildren<Text>().text = "" + note.id;
            }
        }

        public void SpawnDragLine(ChartNote from, ChartNote to)
        {
            var dragLineView = Instantiate(DragLinePrefab, transform.parent).GetComponent<DragLineView>();
            dragLineView.FromNote = from;
            dragLineView.ToNote = to;
        }

        public void OnClear(GameNote note)
        {
            Play.OnClear(note, note.CalculateGrading(), note.GreatGradeWeight);
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