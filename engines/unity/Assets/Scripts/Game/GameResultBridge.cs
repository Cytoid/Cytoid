using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public static class GameResultBridge
{
    public static readonly UnityEvent<string> OnResultJson = new UnityEvent<string>();

    public static string LastResultJson { get; private set; } = "null";

    internal static bool? ActiveSessionRecordPlayEvents { get; set; }

    public static void Emit(GameState state, TierPlaySession tierPlaySession = null, string error = null, string sessionId = null)
    {
        var activeSessionId = ResolveSessionId(sessionId);
        var usedAutoMod = state != null && HasAutoMod(state);
        var telemetry = CaptureTelemetry(activeSessionId, usedAutoMod);
        var payload = BuildResultPayload(state, tierPlaySession, error, activeSessionId, telemetry);
        EmitResultEnvelope(activeSessionId, payload, logAsError: error != null);
    }

    public static void EmitTierRetry(TierPlaySession tierPlaySession)
    {
        if (tierPlaySession == null) throw new ArgumentNullException(nameof(tierPlaySession), "EmitTierRetry requires a non-null TierPlaySession.");
        var sessionId = ResolveSessionId(null);
        var payload = new SessionResultWirePayload
        {
            SessionId = sessionId,
            Mode = "tier",
            Mods = new List<string>(),
            Outcome = new OutcomeWirePayload
            {
                Kind = "tierRetry",
                TierId = tierPlaySession?.TierId,
                StageIndex = tierPlaySession?.StageIndex
            },
            Tier = BuildTierPayload(null, tierPlaySession),
            Flags = new FlagsWirePayload {UsedAutoMod = false},
            Telemetry = EmptyTelemetrySummary(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        EmitResultEnvelope(sessionId, payload, logAsError: false);
    }

    public static void EmitError(Exception exception)
    {
        EmitRejected(ResolveSessionId(null), "runtime_exception", exception.ToString());
    }

    public static void EmitRejected(string sessionId, string code, string message)
    {
        var activeSessionId = ResolveSessionId(sessionId);
        var payload = new SessionResultWirePayload
        {
            SessionId = activeSessionId,
            Mode = "ranked",
            Mods = new List<string>(),
            Outcome = new OutcomeWirePayload {Kind = "rejected"},
            Flags = new FlagsWirePayload {UsedAutoMod = false},
            Telemetry = EmptyTelemetrySummary(),
            Error = new ErrorWirePayload {Code = code, Message = message},
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        EmitResultEnvelope(activeSessionId, payload, logAsError: true);
    }

    public static void EmitCancelled(string sessionId, string reason)
    {
        var activeSessionId = ResolveSessionId(sessionId);
        var payload = new SessionResultWirePayload
        {
            SessionId = activeSessionId,
            Mode = Context.GameState != null ? ModeToWireName(Context.GameState.Mode) : "ranked",
            Mods = Context.GameState != null ? ModsToWireNames(Context.GameState.Mods) : new List<string>(),
            Outcome = new OutcomeWirePayload {Kind = "cancelled", Reason = string.IsNullOrEmpty(reason) ? "unknown" : reason},
            Level = Context.GameState != null ? BuildLevelPayload(Context.GameState) : null,
            Tier = Context.GameState?.Mode == GameMode.Tier ? BuildTierPayload(Context.GameState, Context.ActiveTierPlaySession) : null,
            Flags = new FlagsWirePayload {UsedAutoMod = Context.GameState != null && HasAutoMod(Context.GameState)},
            Telemetry = EmptyTelemetrySummary(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        EmitResultEnvelope(activeSessionId, payload, logAsError: false);
    }

    public static bool HasAutoMod(GameState state)
    {
        return state != null && (state.Mods.Contains(Mod.Auto) ||
                                state.Mods.Contains(Mod.AutoDrag) ||
                                state.Mods.Contains(Mod.AutoHold) ||
                                state.Mods.Contains(Mod.AutoFlick));
    }

    private static SessionResultWirePayload BuildResultPayload(
        GameState state,
        TierPlaySession tierPlaySession,
        string error,
        string sessionId,
        ResultTelemetryWirePayload telemetry)
    {
        var outcome = BuildOutcome(state, error);
        var payload = new SessionResultWirePayload
        {
            SessionId = sessionId,
            Mode = state != null ? ModeToWireName(state.Mode) : "ranked",
            Mods = state != null ? ModsToWireNames(state.Mods) : new List<string>(),
            Outcome = outcome,
            Level = ShouldIncludeLevel(state) ? BuildLevelPayload(state) : null,
            Calibration = outcome.Kind == "calibration" ? BuildCalibrationPayload(state) : null,
            Tier = state?.Mode == GameMode.Tier ? BuildTierPayload(state, tierPlaySession) : null,
            Flags = new FlagsWirePayload {UsedAutoMod = state != null && HasAutoMod(state)},
            Telemetry = telemetry,
            Error = (outcome.Kind == "rejected" || (outcome.Kind == "failed" && !string.IsNullOrEmpty(error)))
                ? new ErrorWirePayload {Code = "runtime_exception", Message = error}
                : null,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        if (state != null && state.IsCompleted && outcome.Kind == "completed")
        {
            payload.Score = BuildScorePayload(state, tierPlaySession);
        }

        return payload;
    }

    private static OutcomeWirePayload BuildOutcome(GameState state, string error)
    {
        if (!string.IsNullOrEmpty(error) && (state == null || !state.IsStarted))
        {
            return new OutcomeWirePayload {Kind = "rejected"};
        }
        if (state != null && (state.Mode == GameMode.Calibration || state.Mode == GameMode.GlobalCalibration))
        {
            return new OutcomeWirePayload {Kind = "calibration"};
        }
        if (state != null && state.IsFailed)
        {
            return new OutcomeWirePayload {Kind = "failed", Reason = state.Mode == GameMode.Tier ? "tierHpDepleted" : "hpDepleted"};
        }
        if (!string.IsNullOrEmpty(error))
        {
            return new OutcomeWirePayload {Kind = "failed", Reason = "unknown"};
        }
        return new OutcomeWirePayload {Kind = "completed"};
    }

    private static ResultTelemetryWirePayload CaptureTelemetry(string sessionId, bool usedAutoMod)
    {
        if (ActiveSessionRecordPlayEvents != true || usedAutoMod)
        {
            return EmptyTelemetrySummary();
        }

        var events = GamePlayEventRecorder.SnapshotAsWireObjects();
        var bytes = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(events));
        var telemetryPayload = new JObject
        {
            ["sessionId"] = sessionId,
            ["playEvents"] = new JObject
            {
                ["format"] = "json.v1",
                ["events"] = JArray.FromObject(events)
            }
        };
        var envelope = CytoidGameCoreEnvelope.Create(sessionId, WireMessageTypes.SessionTelemetry, telemetryPayload);
        GameBridge.OnTelemetryJson.Invoke(envelope.ToJsonString());
        return new ResultTelemetryWirePayload
        {
            Available = true,
            EventsRecorded = events.Length,
            Bytes = bytes
        };
    }

    private static ResultTelemetryWirePayload EmptyTelemetrySummary()
    {
        return new ResultTelemetryWirePayload
        {
            Available = false,
            EventsRecorded = 0,
            Bytes = 0
        };
    }

    private static bool ShouldIncludeLevel(GameState state)
    {
        return state != null && state.Mode != GameMode.Calibration && state.Mode != GameMode.GlobalCalibration;
    }

    private static LevelWirePayload BuildLevelPayload(GameState state)
    {
        return new LevelWirePayload
        {
            Id = state.Level.Id,
            Title = state.Level.Meta.title,
            Difficulty = state.Difficulty.Id,
            DifficultyLevel = state.DifficultyLevel
        };
    }

    private static ScoreWirePayload BuildScorePayload(GameState state, TierPlaySession tierPlaySession)
    {
        var lastPlay = LastPlayResult.FromGameState(state);
        return new ScoreWirePayload
        {
            Score = lastPlay.score,
            Accuracy = lastPlay.accuracy,
            MaxCombo = tierPlaySession?.MaxCombo ?? lastPlay.maxCombo,
            GradeCounts = LowerGradeCounts(lastPlay.gradeCounts),
            Early = lastPlay.early,
            Late = lastPlay.late,
            AverageTimingError = lastPlay.averageTimingError,
            StandardTimingError = lastPlay.standardTimingError
        };
    }

    private static Dictionary<string, int> LowerGradeCounts(Dictionary<string, int> source)
    {
        var result = new Dictionary<string, int>();
        if (source == null) return result;
        foreach (var pair in source)
        {
            result[LowerFirst(pair.Key)] = pair.Value;
        }
        return result;
    }

    private static CalibrationWirePayload BuildCalibrationPayload(GameState state)
    {
        return new CalibrationWirePayload
        {
            BaseNoteOffset = state?.Mode == GameMode.GlobalCalibration ? Context.Player.Settings.BaseNoteOffset : (float?) null,
            LevelNoteOffset = state?.Mode == GameMode.Calibration ? state.Level.Record.RelativeNoteOffset : (float?) null
        };
    }

    private static TierWirePayload BuildTierPayload(GameState state, TierPlaySession tierPlaySession)
    {
        if (tierPlaySession == null) return null;
        return new TierWirePayload
        {
            TierId = tierPlaySession.TierId,
            StageIndex = tierPlaySession.StageIndex,
            StageCount = tierPlaySession.StageCount,
            Health = state?.Health.Value,
            MaxHealth = tierPlaySession.MaxHealth,
            Combo = tierPlaySession.Combo
        };
    }

    private static List<string> ModsToWireNames(IEnumerable<Mod> mods)
    {
        var result = new List<string>();
        if (mods == null) return result;
        foreach (var mod in mods)
        {
            result.Add(ModToWireName(mod));
        }
        return result;
    }

    private static string ModToWireName(Mod mod)
    {
        switch (mod)
        {
            case Mod.FlipX: return "flipX";
            case Mod.FlipY: return "flipY";
            case Mod.FlipAll: return "flipAll";
            case Mod.Slow: return "slow";
            case Mod.Fast: return "fast";
            case Mod.FC: return "fc";
            case Mod.AP: return "ap";
            case Mod.Hard: return "hard";
            case Mod.ExHard: return "exHard";
            case Mod.HideScanline: return "hideScanline";
            case Mod.HideNotes: return "hideNotes";
            case Mod.AutoDrag: return "autoDrag";
            case Mod.AutoHold: return "autoHold";
            case Mod.AutoFlick: return "autoFlick";
            case Mod.Auto: return "auto";
            default: throw new ArgumentOutOfRangeException(nameof(mod), mod, null);
        }
    }

    private static string ModeToWireName(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Standard: return "ranked";
            case GameMode.Practice: return "practice";
            case GameMode.Calibration: return "calibration";
            case GameMode.GlobalCalibration: return "globalCalibration";
            case GameMode.Tier: return "tier";
            default: return "ranked";
        }
    }

    private static void EmitResultEnvelope(string sessionId, SessionResultWirePayload payload, bool logAsError)
    {
        var envelope = CytoidGameCoreEnvelope.Create(sessionId, WireMessageTypes.SessionResult, ToJObject(payload));
        LastResultJson = envelope.ToJsonString();
        if (logAsError)
        {
            Debug.LogError($"[GameResultBridge] {LastResultJson}");
        }
        else
        {
            Debug.Log($"[GameResultBridge] {LastResultJson}");
        }
        OnResultJson.Invoke(LastResultJson);
    }

    private static JObject ToJObject(object payload)
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
        return JObject.FromObject(payload, serializer);
    }

    private static string ResolveSessionId(string sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId)) return sessionId;
        return GameBridge.ActiveSessionId ?? "unknown-session";
    }

    private static string LowerFirst(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}

internal sealed class SessionResultWirePayload
{
    [JsonProperty("sessionId")]
    public string SessionId { get; set; }

    [JsonProperty("mode")]
    public string Mode { get; set; }

    [JsonProperty("mods")]
    public List<string> Mods { get; set; }

    [JsonProperty("outcome")]
    public OutcomeWirePayload Outcome { get; set; }

    [JsonProperty("level")]
    public LevelWirePayload Level { get; set; }

    [JsonProperty("score")]
    public ScoreWirePayload Score { get; set; }

    [JsonProperty("calibration")]
    public CalibrationWirePayload Calibration { get; set; }

    [JsonProperty("tier")]
    public TierWirePayload Tier { get; set; }

    [JsonProperty("flags")]
    public FlagsWirePayload Flags { get; set; }

    [JsonProperty("telemetry")]
    public ResultTelemetryWirePayload Telemetry { get; set; }

    [JsonProperty("error")]
    public ErrorWirePayload Error { get; set; }

    [JsonProperty("timestamp")]
    public long Timestamp { get; set; }
}

internal sealed class OutcomeWirePayload
{
    [JsonProperty("kind")]
    public string Kind { get; set; }

    [JsonProperty("reason")]
    public string Reason { get; set; }

    [JsonProperty("tierId")]
    public string TierId { get; set; }

    [JsonProperty("stageIndex")]
    public int? StageIndex { get; set; }
}

internal sealed class LevelWirePayload
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("difficulty")]
    public string Difficulty { get; set; }

    [JsonProperty("difficultyLevel")]
    public int DifficultyLevel { get; set; }
}

internal sealed class ScoreWirePayload
{
    [JsonProperty("score")]
    public int Score { get; set; }

    [JsonProperty("accuracy")]
    public double Accuracy { get; set; }

    [JsonProperty("maxCombo")]
    public int MaxCombo { get; set; }

    [JsonProperty("gradeCounts")]
    public Dictionary<string, int> GradeCounts { get; set; }

    [JsonProperty("early")]
    public int Early { get; set; }

    [JsonProperty("late")]
    public int Late { get; set; }

    [JsonProperty("averageTimingError")]
    public double AverageTimingError { get; set; }

    [JsonProperty("standardTimingError")]
    public double StandardTimingError { get; set; }
}

internal sealed class CalibrationWirePayload
{
    [JsonProperty("baseNoteOffset")]
    public float? BaseNoteOffset { get; set; }

    [JsonProperty("levelNoteOffset")]
    public float? LevelNoteOffset { get; set; }
}

internal sealed class TierWirePayload
{
    [JsonProperty("tierId")]
    public string TierId { get; set; }

    [JsonProperty("stageIndex")]
    public int StageIndex { get; set; }

    [JsonProperty("stageCount")]
    public int? StageCount { get; set; }

    [JsonProperty("health")]
    public double? Health { get; set; }

    [JsonProperty("maxHealth")]
    public double MaxHealth { get; set; }

    [JsonProperty("combo")]
    public int Combo { get; set; }
}

internal sealed class FlagsWirePayload
{
    [JsonProperty("usedAutoMod")]
    public bool UsedAutoMod { get; set; }
}

internal sealed class ResultTelemetryWirePayload
{
    [JsonProperty("available")]
    public bool Available { get; set; }

    [JsonProperty("eventsRecorded")]
    public int EventsRecorded { get; set; }

    [JsonProperty("bytes")]
    public int Bytes { get; set; }
}

internal sealed class ErrorWirePayload
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("details")]
    public JObject Details { get; set; }
}
