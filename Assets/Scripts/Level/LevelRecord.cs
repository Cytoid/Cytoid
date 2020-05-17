using System;
using System.Collections.Generic;
using LiteDB;
using Newtonsoft.Json;

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

    [JsonProperty("play_counts")]
    public Dictionary<string, int> PlayCounts { get; set; } = new Dictionary<string, int>();

    [JsonProperty("added_date")] public DateTimeOffset AddedDate { get; set; } = DateTimeOffset.MinValue;
    [JsonProperty("last_played_date")] public DateTimeOffset LastPlayedDate { get; set; } = DateTimeOffset.MinValue;

    [Serializable]
    public class Performance
    {
        [JsonProperty("score")] public int Score { get; set; }
        [JsonProperty("accuracy")] public double Accuracy { get; set; } // 0~1
    }
}