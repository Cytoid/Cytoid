using System;
using System.Collections.Generic;
using LiteDB;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class LevelRecord
{
    
    public int Id { get; set; }
    
    [JsonProperty("level_id")] public string LevelId { get; set; }

    [JsonProperty("relative_note_offset")] public float RelativeNoteOffset { get; set; } = 0;

    [JsonProperty("best_performances")]
    public Dictionary<string, Performance> BestPerformances { get; set; } = new Dictionary<string, Performance>();

    [JsonProperty("best_practice_performances")]
    public Dictionary<string, Performance> BestPracticePerformances { get; set; } =
        new Dictionary<string, Performance>();

    public string TrySaveBestPerformance(GameMode mode, Difficulty difficulty, int score, double accuracy)
    {
        var bestPerformances = mode == GameMode.Standard ? BestPerformances : BestPracticePerformances;
        var performance = new Performance {Score = score, Accuracy = accuracy};

        if (!bestPerformances.ContainsKey(difficulty.Id))
        {
            bestPerformances[difficulty.Id] = performance;
            return "RESULT_NEW".Get();
        }

        var historicBest = bestPerformances[difficulty.Id];
        if (performance.Score > historicBest.Score)
        {
            bestPerformances[difficulty.Id] = performance;
            return $"+{performance.Score - historicBest.Score}";
        }
        if (performance.Score == historicBest.Score && performance.Accuracy > historicBest.Accuracy)
        {
            bestPerformances[difficulty.Id] = performance;
            return $"+{(Mathf.FloorToInt((float) (performance.Accuracy - historicBest.Accuracy) * 100 * 100) / 100f):0.00}%";
        }
        return "";
    }

    [JsonProperty("play_counts")]
    public Dictionary<string, int> PlayCounts { get; set; } = new Dictionary<string, int>();

    public void IncrementPlayCountByOne(Difficulty difficulty)
    {
        if (PlayCounts.ContainsKey(difficulty.Id))
        {
            PlayCounts[difficulty.Id] += 1;
        }
        else
        {
            PlayCounts[difficulty.Id] = 1;
        }
    }

    [JsonProperty("added_date")] public DateTimeOffset AddedDate { get; set; } = DateTimeOffset.MinValue;
    [JsonProperty("last_played_date")] public DateTimeOffset LastPlayedDate { get; set; } = DateTimeOffset.MinValue;

    [Serializable]
    public class Performance
    {
        [JsonProperty("score")] public int Score { get; set; }
        [JsonProperty("accuracy")] public double Accuracy { get; set; } // 0~1
    }
}