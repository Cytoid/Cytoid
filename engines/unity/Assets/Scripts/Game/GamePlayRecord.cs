using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// v2 record events are heterogeneous, self-describing JSON objects. A field keeps
/// the same meaning wherever it appears: s = sequence, t = game time (µs), p = event
/// kind, finger = attribution, note = chart note id, x/y = judgement-world coordinates
/// scaled by <see cref="GamePlayRecordSnapshot.CoordinateScale"/>. File order is
/// authoritative; t may repeat or move if the gameplay clock does.
/// </summary>
public abstract class GamePlayRecordEvent
{
    public long s;
    public long t;
    public string p;

    public void Write(TextWriter writer)
    {
        writer.Write("{\"s\":");
        writer.Write(s.ToString(CultureInfo.InvariantCulture));
        writer.Write(",\"t\":");
        writer.Write(t.ToString(CultureInfo.InvariantCulture));
        writer.Write(",\"p\":");
        writer.Write(JsonConvert.ToString(p));
        WriteFields(writer);
        writer.Write("}");
    }

    protected abstract void WriteFields(TextWriter writer);
}

public sealed class GamePlayTouchEvent : GamePlayRecordEvent
{
    public int finger;
    public int x;
    public int y;

    protected override void WriteFields(TextWriter writer)
    {
        writer.Write(",\"finger\":");
        writer.Write(finger.ToString(CultureInfo.InvariantCulture));
        writer.Write(",\"x\":");
        writer.Write(x.ToString(CultureInfo.InvariantCulture));
        writer.Write(",\"y\":");
        writer.Write(y.ToString(CultureInfo.InvariantCulture));
    }
}

public sealed class GamePlayControlEvent : GamePlayRecordEvent
{
    protected override void WriteFields(TextWriter writer)
    {
    }
}

/// <summary>Input-layer contact recording; does not imply settlement.</summary>
public sealed class GamePlayArmEvent : GamePlayRecordEvent
{
    public int note;
    public int finger;
    public long input;
    public bool hasInput;

    protected override void WriteFields(TextWriter writer)
    {
        writer.Write(",\"note\":");
        writer.Write(note.ToString(CultureInfo.InvariantCulture));
        writer.Write(",\"finger\":");
        writer.Write(finger.ToString(CultureInfo.InvariantCulture));
        if (!hasInput) return;
        writer.Write(",\"input\":");
        writer.Write(input.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>Settlement with grade, immediate cause, and finger attribution (null = system).</summary>
public sealed class GamePlayJudgeEvent : GamePlayRecordEvent
{
    public int note;
    public int? finger;
    public string grade;
    public string cause;
    public long input;
    public bool hasInput;

    protected override void WriteFields(TextWriter writer)
    {
        writer.Write(",\"note\":");
        writer.Write(note.ToString(CultureInfo.InvariantCulture));
        writer.Write(",\"finger\":");
        if (finger.HasValue) writer.Write(finger.Value.ToString(CultureInfo.InvariantCulture));
        else writer.Write("null");
        writer.Write(",\"grade\":");
        writer.Write(JsonConvert.ToString(grade));
        writer.Write(",\"cause\":");
        writer.Write(JsonConvert.ToString(cause));
        if (!hasInput) return;
        writer.Write(",\"input\":");
        writer.Write(input.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Stable wire strings for grades and judgment causes. Mapped through explicit
/// switches so the wire contract survives C# enum reordering; unrecognized enum
/// values map to "unknown" and log a diagnostic instead of silently re-becoming
/// an existing value.
/// </summary>
public static class GamePlayRecordWire
{
    public const string FormatId = "cytoid.play-record";

    public const string CausePress = "press";
    public const string CauseContact = "contact";
    public const string CauseRelease = "release";
    public const string CauseTriggerTick = "trigger_tick";
    public const string CauseMiss = "miss";
    public const string CauseAutoplay = "autoplay";
    public const string CauseUnknown = "unknown";

    public static string ToWireGrade(NoteGrade grade)
    {
        switch (grade)
        {
            case NoteGrade.Miss: return "miss";
            case NoteGrade.Bad: return "bad";
            case NoteGrade.Good: return "good";
            case NoteGrade.Great: return "great";
            case NoteGrade.Perfect: return "perfect";
            case NoteGrade.None: return "none";
            default:
                Debug.LogWarning($"Play record: unmapped NoteGrade {grade} serialized as unknown");
                return "unknown";
        }
    }
}

[Serializable]
public sealed class GamePlayRecordSnapshot
{
    public const int CoordinateScale = 1000000;

    public string ContentHash;
    public string ClientVersion;
    public string UnityVersion;
    public string Mode;
    public string[] Mods;
    public double TimeBase;
    public int ScreenWidth;
    public int ScreenHeight;
    public int SafeAreaX;
    public int SafeAreaY;
    public int SafeAreaWidth;
    public int SafeAreaHeight;
    public int CameraOrthographicSize;
    public int PlayfieldLeft;
    public int PlayfieldRight;
    public int PlayfieldBottom;
    public int PlayfieldTop;
    public int HorizontalMargin;
    public int VerticalMargin;
    public bool RestrictPlayAreaAspectRatio;
    public bool DisplayBoundaries;
    public bool FlipX;
    public bool FlipY;
    public long JudgmentOffsetUs;
    public int NoteSize;
    public int GlobalNoteSizeMultiplier;
    public Dictionary<string, int> HitboxSettings;
    public Dictionary<string, int> HitboxSizes;
    public bool ExperimentalNoteAr;
    public bool ExperimentalNoteDesign;
    public GamePlayRecordEvent[] Events;

    public int EventCount => Events?.Length ?? 0;

    public void Release()
    {
        Events = null;
        Mods = null;
        HitboxSettings = null;
        HitboxSizes = null;
    }
}

public sealed class GamePlayRecordRecorder
{
    private readonly Game game;
    private readonly List<GamePlayRecordEvent> events = new List<GamePlayRecordEvent>(4096);
    private readonly Stack<JudgmentAttribution> attributionStack = new Stack<JudgmentAttribution>();
    private long nextSequence;
    private bool active = true;

    public GamePlayRecordRecorder(Game game)
    {
        this.game = game;
        game.onNoteJudged.AddListener(OnNoteJudged);
    }

    /// <summary>
    /// Immediate settlement attribution captured at the causal operation. The scoped
    /// stack handles nesting; every judgment inside the scope inherits the same
    /// attribution (a press can settle a whole batch). A null finger means the
    /// settlement had no directly triggering finger (timer/sweep). Returns null when
    /// recording is inactive; <c>using (null)</c> is a valid no-op.
    /// </summary>
    public IDisposable PushJudgmentContext(string cause, int? finger, long? inputSequence)
    {
        if (!active || !game.State.IsPlaying) return null;
        attributionStack.Push(new JudgmentAttribution
        {
            Cause = cause,
            Finger = finger,
            InputSequence = inputSequence
        });
        return new ContextScope(this);
    }

    /// <summary>
    /// Judgment trace: every settlement (press, contact, release, trigger tick, miss
    /// sweep, autoplay) with grade, immediate cause, and finger attribution read from
    /// the scoped context. Settlements outside any scope record cause "unknown" --
    /// a logging gap, never silently re-attributed to a system cause.
    /// </summary>
    private void OnNoteJudged(Game _, Note note, JudgeData data)
    {
        if (!active || !game.State.IsPlaying || note == null) return;
        JudgmentAttribution attribution;
        attribution = attributionStack.Count > 0 ? attributionStack.Peek() : JudgmentAttribution.Unknown;
        events.Add(new GamePlayJudgeEvent
        {
            s = nextSequence++,
            t = ToFixed(game.Time),
            p = "judge",
            note = note.Model != null ? note.Model.id : -1,
            finger = attribution.Finger,
            grade = GamePlayRecordWire.ToWireGrade(data?.Grade ?? NoteGrade.None),
            cause = attribution.Cause,
            input = attribution.InputSequence ?? -1,
            hasInput = attribution.InputSequence.HasValue
        });
    }

    /// <summary>Touch event; returns its sequence number for later input back-references.</summary>
    public long Record(int finger, string phase, Vector3 worldPosition)
    {
        if (!active || !game.State.IsPlaying) return -1;
        var sequence = nextSequence++;
        events.Add(new GamePlayTouchEvent
        {
            s = sequence,
            t = ToFixed(game.Time),
            p = phase,
            finger = finger,
            x = ToFixedInt(worldPosition.x),
            y = ToFixedInt(worldPosition.y)
        });
        return sequence;
    }

    public long RecordControl(string phase)
    {
        if (!active) return -1;
        var sequence = nextSequence++;
        events.Add(new GamePlayControlEvent
        {
            s = sequence,
            t = ToFixed(game.Time),
            p = phase
        });
        return sequence;
    }

    /// <summary>Input-layer contact recording (drag arm); does not imply settlement.</summary>
    public long RecordArm(int fingerIndex, Note note, long? inputSequence)
    {
        if (!active || !game.State.IsPlaying || note == null) return -1;
        var sequence = nextSequence++;
        events.Add(new GamePlayArmEvent
        {
            s = sequence,
            t = ToFixed(game.Time),
            p = "arm",
            note = note.Model != null ? note.Model.id : -1,
            finger = fingerIndex,
            input = inputSequence ?? -1,
            hasInput = inputSequence.HasValue
        });
        return sequence;
    }

    private struct JudgmentAttribution
    {
        public string Cause;
        public int? Finger;
        public long? InputSequence;

        public static JudgmentAttribution Unknown => new JudgmentAttribution
        {
            Cause = GamePlayRecordWire.CauseUnknown,
            Finger = null,
            InputSequence = null
        };
    }

    private sealed class ContextScope : IDisposable
    {
        private GamePlayRecordRecorder recorder;

        public ContextScope(GamePlayRecordRecorder recorder)
        {
            this.recorder = recorder;
        }

        public void Dispose()
        {
            if (recorder == null) return;
            recorder.attributionStack.Pop();
            recorder = null;
        }
    }

    /// <summary>
    /// Inspection access to the accumulated events without finalizing the snapshot;
    /// used by tests to assert attribution on a live recording.
    /// </summary>
    public IReadOnlyList<GamePlayRecordEvent> RecordedEvents => events;

    public static bool ShouldCollect(bool consent, GameMode mode)
    {
        return consent && (mode == GameMode.Standard || mode == GameMode.Practice);
    }

    public GamePlayRecordSnapshot Complete()
    {
        if (!active) return null;
        active = false;
        game.onNoteJudged.RemoveListener(OnNoteJudged);

        var safeArea = UnityEngine.Screen.safeArea;
        var left = game.Chart.ConvertChartXToScreenX(0);
        var right = game.Chart.ConvertChartXToScreenX(1);
        var bottom = game.Chart.GetBoundaryPosition(true);
        var top = game.Chart.GetBoundaryPosition(false);
        var settings = Context.Player.Settings;

        return new GamePlayRecordSnapshot
        {
            // Upstream does not track a chart checksum yet; the field keeps the wire shape.
            ContentHash = null,
            ClientVersion = Application.version,
            UnityVersion = Application.unityVersion,
            Mode = game.State.Mode.ToString().ToLowerInvariant(),
            Mods = game.State.Mods.Select(it => it.ToString()).OrderBy(it => it).ToArray(),
            TimeBase = game.Chart.Model.time_base,
            ScreenWidth = UnityEngine.Screen.width,
            ScreenHeight = UnityEngine.Screen.height,
            SafeAreaX = Mathf.RoundToInt(safeArea.x),
            SafeAreaY = Mathf.RoundToInt(safeArea.y),
            SafeAreaWidth = Mathf.RoundToInt(safeArea.width),
            SafeAreaHeight = Mathf.RoundToInt(safeArea.height),
            CameraOrthographicSize = ToFixedInt(game.camera.orthographicSize),
            PlayfieldLeft = ToFixedInt(Math.Min(left, right)),
            PlayfieldRight = ToFixedInt(Math.Max(left, right)),
            PlayfieldBottom = ToFixedInt(Math.Min(bottom, top)),
            PlayfieldTop = ToFixedInt(Math.Max(bottom, top)),
            HorizontalMargin = game.Chart.HorizontalMargin,
            VerticalMargin = game.Chart.VerticalMargin,
            RestrictPlayAreaAspectRatio = game.Chart.RestrictPlayAreaAspectRatio,
            DisplayBoundaries = game.Chart.DisplayBoundaries,
            FlipX = game.Chart.IsHorizontallyInverted,
            FlipY = game.Chart.IsVerticallyInverted,
            JudgmentOffsetUs = ToFixed(settings.JudgmentOffset),
            NoteSize = ToFixedInt(settings.NoteSize),
            GlobalNoteSizeMultiplier = ToFixedInt(game.Config.GlobalNoteSizeMultiplier),
            HitboxSettings = settings.HitboxSizes.ToDictionary(it => it.Key.ToString(), it => it.Value),
            HitboxSizes = game.Config.NoteHitboxSizes.ToDictionary(
                it => it.Key.ToString(), it => ToFixedInt(it.Value)),
            ExperimentalNoteAr = settings.UseExperimentalNoteAr,
            ExperimentalNoteDesign = settings.UseExperimentalNoteDesign,
            Events = events.ToArray()
        };
    }

    public void Discard()
    {
        active = false;
        game.onNoteJudged.RemoveListener(OnNoteJudged);
        events.Clear();
    }

    public static long ToFixed(double value)
    {
        return (long) Math.Round(value * GamePlayRecordSnapshot.CoordinateScale,
            MidpointRounding.AwayFromZero);
    }

    public static int ToFixedInt(double value)
    {
        return checked((int) ToFixed(value));
    }
}

public static class GamePlayRecordCodec
{
    public static byte[] EncodeGzip(GamePlayRecordSnapshot snapshot, long chartId, long recordId)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
            using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
            {
                writer.NewLine = "\n";
                writer.WriteLine(CreateMetadata(snapshot, chartId, recordId).ToString(Formatting.None));
                if (snapshot.Events != null)
                {
                    foreach (var inputEvent in snapshot.Events)
                    {
                        inputEvent.Write(writer);
                        writer.WriteLine();
                    }
                }
            }
            return output.ToArray();
        }
    }

    public static JObject CreateMetadata(GamePlayRecordSnapshot snapshot, long chartId, long recordId)
    {
        return new JObject
        {
            ["type"] = "metadata",
            ["format"] = GamePlayRecordWire.FormatId,
            ["version"] = 2,
            ["recordId"] = recordId,
            ["chartId"] = chartId,
            ["contentHash"] = snapshot.ContentHash,
            ["client"] = new JObject
            {
                ["version"] = snapshot.ClientVersion,
                ["unityVersion"] = snapshot.UnityVersion
            },
            ["mode"] = snapshot.Mode,
            ["mods"] = new JArray(snapshot.Mods ?? Array.Empty<string>()),
            ["clock"] = new JObject
            {
                ["unit"] = "us",
                ["source"] = "game_time",
                ["origin"] = "chart_tick_0",
                ["chartTimeBase"] = snapshot.TimeBase
            },
            ["coordinates"] = new JObject
            {
                ["space"] = "judgement_world",
                ["scale"] = GamePlayRecordSnapshot.CoordinateScale
            },
            ["viewport"] = new JObject
            {
                ["width"] = snapshot.ScreenWidth,
                ["height"] = snapshot.ScreenHeight,
                ["safeArea"] = new JObject
                {
                    ["x"] = snapshot.SafeAreaX,
                    ["y"] = snapshot.SafeAreaY,
                    ["width"] = snapshot.SafeAreaWidth,
                    ["height"] = snapshot.SafeAreaHeight
                }
            },
            ["camera"] = new JObject
            {
                ["orthographicSize"] = snapshot.CameraOrthographicSize,
                ["scale"] = GamePlayRecordSnapshot.CoordinateScale
            },
            ["playfield"] = new JObject
            {
                ["left"] = snapshot.PlayfieldLeft,
                ["right"] = snapshot.PlayfieldRight,
                ["bottom"] = snapshot.PlayfieldBottom,
                ["top"] = snapshot.PlayfieldTop,
                ["scale"] = GamePlayRecordSnapshot.CoordinateScale,
                ["horizontalMargin"] = snapshot.HorizontalMargin,
                ["verticalMargin"] = snapshot.VerticalMargin,
                ["restrictAspectRatio"] = snapshot.RestrictPlayAreaAspectRatio,
                ["displayBoundaries"] = snapshot.DisplayBoundaries,
                ["flipX"] = snapshot.FlipX,
                ["flipY"] = snapshot.FlipY
            },
            ["judgement"] = new JObject
            {
                ["offsetUs"] = snapshot.JudgmentOffsetUs,
                ["noteSize"] = snapshot.NoteSize,
                ["globalNoteSizeMultiplier"] = snapshot.GlobalNoteSizeMultiplier,
                ["fixedScale"] = GamePlayRecordSnapshot.CoordinateScale,
                ["hitboxSettings"] = JObject.FromObject(
                    snapshot.HitboxSettings ?? new Dictionary<string, int>()),
                ["hitboxSizes"] = JObject.FromObject(snapshot.HitboxSizes ?? new Dictionary<string, int>()),
                ["experimentalNoteAr"] = snapshot.ExperimentalNoteAr,
                ["experimentalNoteDesign"] = snapshot.ExperimentalNoteDesign
            },
            ["eventCount"] = snapshot.EventCount
        };
    }
}
