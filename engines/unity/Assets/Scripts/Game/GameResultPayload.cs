using System;
using Newtonsoft.Json;

[Serializable]
public class GameResultPayload : LastPlayResult
{
    public bool completed;
    public bool failed;
    public bool usedAutoMod;
    public string error;
    public string gameMode;
    public float? calibratedBaseNoteOffset;
    public float? calibratedLevelNoteOffset;
    public TierPlayResult tierPlay;
    public string tierRetry;
    public GamePlayEvent[] playEvents;

    public static GameResultPayload FromGameState(
        GameState state,
        TierPlaySession tierPlaySession = null,
        string error = null,
        GamePlayEvent[] playEvents = null)
    {
        var result = new GameResultPayload
        {
            completed = state != null && state.IsCompleted,
            failed = state != null && state.IsFailed,
            usedAutoMod = state != null && GameResultBridge.HasAutoMod(state),
            error = error,
            gameMode = state?.Mode.ToString(),
            playEvents = playEvents ?? Array.Empty<GamePlayEvent>()
        };

        if (state == null)
        {
            result.timestamp = DateTimeOffset.UtcNow.ToString("o");
            return result;
        }

        if (state.Mode == GameMode.GlobalCalibration)
        {
            result.calibratedBaseNoteOffset = Context.Player.Settings.BaseNoteOffset;
        }
        else if (state.Mode == GameMode.Calibration)
        {
            result.calibratedLevelNoteOffset = state.Level.Record.RelativeNoteOffset;
        }

        if (state.Mode == GameMode.Tier && tierPlaySession != null)
        {
            result.tierPlay = TierPlayResult.FromSession(tierPlaySession, state);
            result.timestamp = DateTimeOffset.UtcNow.ToString("o");
            result.levelId = state.Level.Id;
            result.title = state.Level.Meta.title;
            result.difficulty = state.Difficulty.Id;
            result.difficultyLevel = state.DifficultyLevel;
            if (state.IsCompleted)
            {
                var lastPlay = LastPlayResult.FromGameState(state);
                result.score = lastPlay.score;
                result.accuracy = lastPlay.accuracy;
                result.maxCombo = tierPlaySession.MaxCombo;
                result.gradeCounts = lastPlay.gradeCounts;
                result.early = lastPlay.early;
                result.late = lastPlay.late;
                result.averageTimingError = lastPlay.averageTimingError;
                result.standardTimingError = lastPlay.standardTimingError;
            }

            return result;
        }

        if (state.Mode == GameMode.Calibration || state.Mode == GameMode.GlobalCalibration)
        {
            result.timestamp = DateTimeOffset.UtcNow.ToString("o");
            result.levelId = state.Level.Id;
            result.title = state.Level.Meta.title;
            result.difficulty = state.Difficulty.Id;
            result.difficultyLevel = state.DifficultyLevel;
            return result;
        }

        if (!state.IsCompleted)
        {
            result.timestamp = DateTimeOffset.UtcNow.ToString("o");
            return result;
        }

        var standardPlay = LastPlayResult.FromGameState(state);
        result.timestamp = standardPlay.timestamp;
        result.levelId = standardPlay.levelId;
        result.title = standardPlay.title;
        result.difficulty = standardPlay.difficulty;
        result.difficultyLevel = standardPlay.difficultyLevel;
        result.score = standardPlay.score;
        result.accuracy = standardPlay.accuracy;
        result.maxCombo = standardPlay.maxCombo;
        result.gradeCounts = standardPlay.gradeCounts;
        result.early = standardPlay.early;
        result.late = standardPlay.late;
        result.averageTimingError = standardPlay.averageTimingError;
        result.standardTimingError = standardPlay.standardTimingError;
        return result;
    }

    public new string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
