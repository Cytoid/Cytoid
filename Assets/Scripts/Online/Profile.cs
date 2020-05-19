using System;
using Newtonsoft.Json;

[Serializable]
public class Profile
{
    [JsonProperty("user")] public OnlineUser User { get; set; }
    [JsonProperty("rating")] public float Rating { get; set; }
    [JsonProperty("exp")] public ExpData Exp { get; set; }
    [JsonProperty("grade")] public GradeData Grades { get; set; }
    [JsonProperty("activities")] public ActivitiesData Activities { get; set; }

    [Serializable]
    public class ExpData
    {
        [JsonProperty("currentLevel")] public int CurrentLevel { get; set; }
        [JsonProperty("totalExp")] public float TotalExp { get; set; }
        [JsonProperty("currentLevelExp")] public float CurrentLevelExp { get; set; }
        [JsonProperty("nextLevelExp")] public float NextLevelExp { get; set; }
    }

    [Serializable]
    public class GradeData
    {
        [JsonProperty("MAX")] public int MAX { get; set; }
        [JsonProperty("SSS")] public int SSS { get; set; }
        [JsonProperty("SS")] public int SS { get; set; }
        [JsonProperty("S")] public int S { get; set; }
        [JsonProperty("A")] public int A { get; set; }
        [JsonProperty("B")] public int B { get; set; }
        [JsonProperty("C")] public int C { get; set; }
        [JsonProperty("D")] public int D { get; set; }
        [JsonProperty("F")] public int F { get; set; }
    }

    [Serializable]
    public class ActivitiesData
    {
        [JsonProperty("totalRankedPlays")] public int TotalRankedPlays { get; set; }
        [JsonProperty("clearedNotes")] public long ClearedNotes { get; set; }
        [JsonProperty("maxCombo")] public int MaxCombo { get; set; }
        [JsonProperty("averageRankedAccuracy")] public double? AverageRankedAccuracy { get; set; }
        [JsonProperty("totalRankedScore")] public long? TotalRankedScore { get; set; }
        [JsonProperty("totalPlayTime")] public long TotalPlayTime { get; set; }
    }
}