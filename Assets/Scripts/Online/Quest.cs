using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class Quest
{
    [JsonProperty("title")] public string Description { get; set; }
    [JsonProperty("objectives")] public List<Objective> Objectives { get; set; }
    [JsonProperty("rewards")] public List<Reward> Rewards { get; set; }
}

[Serializable]
public class Objective
{
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("isCompleted")] public bool IsCompleted { get; set; }
    [JsonProperty("currentProgress")] public int CurrentProgress { get; set; }
    [JsonProperty("maxProgress")] public int MaxProgress { get; set; }
}