using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class OngoingQuest
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("rewards")] public List<Reward> Rewards { get; set; }
    [JsonProperty("objectives")] public List<Objective> Objectives { get; set; }
}

[Serializable]
public class Objective
{
    [JsonProperty("completed")] public bool Completed { get; set; }
    [JsonProperty("progress")] public float Progress { get; set; }
    [JsonProperty("completion")] public float Completion { get; set; }
    [JsonProperty("progressType")] public ProgressType ProgressType { get; set; } = ProgressType.Integer;
    [JsonProperty("description")] public string Description { get; set; }
}

[Serializable]
public class AdventureState
{
    [JsonProperty("ongoingQuests")] public List<OngoingQuest> OngoingQuests { get; set; }
    [JsonProperty("rewards")] public List<Reward> Rewards { get; set; } // For asynchronous rewards. Unused for now.
}

[Serializable]
public class SingleQuestState
{
    [JsonProperty("quest")] public OngoingQuest Quest { get; set; }
    [JsonProperty("rewards")] public List<Reward> Rewards { get; set; } // For asynchronous rewards. Unused for now.
}

[JsonConverter(typeof(DefaultUnknownEnumConverter), (int) Integer)]
public enum ProgressType
{
    Percentage, OneDecimal, TwoDecimal, Integer
}