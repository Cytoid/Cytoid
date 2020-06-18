using System;
using System.Collections.Generic;
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

[Serializable]
public class FullProfile : Profile
{
    [JsonProperty("timeSeries")] public List<TimeSeriesData> TimeSeries { get; set; }
    [JsonProperty("lastActive")] public DateTimeOffset? LastActive { get; set; }
    [JsonProperty("levelCount")] public int LevelCount { get; set; }
    [JsonProperty("levels")] public List<OnlineLevel> Levels { get; set; }
    [JsonProperty("featuredLevelCount")] public int FeaturedLevelCount { get; set; }
    [JsonProperty("featuredLevels")] public List<OnlineLevel> FeaturedLevels { get; set; }
    [JsonProperty("collectionCount")] public int CollectionCount { get; set; }
    [JsonProperty("collections")] public List<CollectionMeta> Collections { get; set; }
    [JsonProperty("recentRecords")] public List<OnlineRecord> RecentRecords { get; set; }
    [JsonProperty("tier")] public TierMeta Tier { get; set; }
    [JsonProperty("character")] public CharacterMeta Character { get; set; }

    [Serializable]
    public class TimeSeriesData
    {
        [JsonProperty("count")] public int Count { get; set; }
        [JsonProperty("rating")] public double Rating { get; set; }
        [JsonProperty("accuracy")] public double Accuracy { get; set; }
        [JsonProperty("cumulativeRating")] public double CumulativeRating { get; set; }
        [JsonProperty("cumulativeAccuracy")] public double CumulativeAccuracy { get; set; }
        [JsonProperty("year")] public string Year { get; set; }
        [JsonProperty("week")] public string Week { get; set; }
    }

}