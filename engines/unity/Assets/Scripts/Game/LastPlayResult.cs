using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class LastPlayResult
{
    private static LastPlayResult cached;

    public string timestamp;
    public string levelId;
    public string title;
    public string difficulty;
    public int difficultyLevel;
    public int score;
    public double accuracy;
    public int maxCombo;
    public Dictionary<string, int> gradeCounts;
    public int early;
    public int late;
    public double averageTimingError;
    public double standardTimingError;

    public static LastPlayResult FromGameState(GameState state)
    {
        var result = new LastPlayResult
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            levelId = state.Level.Id,
            title = state.Level.Meta.title,
            difficulty = state.Difficulty.Id,
            difficultyLevel = state.DifficultyLevel,
            score = (int) state.Score,
            accuracy = state.Accuracy,
            maxCombo = state.MaxCombo,
            gradeCounts = new Dictionary<string, int>(),
            early = state.EarlyCount,
            late = state.LateCount,
            averageTimingError = state.AverageTimingError,
            standardTimingError = state.StandardTimingError
        };

        foreach (var pair in state.GradeCounts)
        {
            result.gradeCounts[pair.Key.ToString()] = pair.Value;
        }

        return result;
    }

    public static void Save(LastPlayResult result)
    {
        cached = result;
    }

    public static string LoadJson()
    {
        return cached?.ToJson() ?? "null";
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
