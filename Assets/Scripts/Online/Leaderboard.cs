using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class Leaderboard
{
    [Serializable]
    public class Entry : OnlineUser
    {
        [JsonProperty("rank")] public int Rank { get; set; }
        [JsonProperty("rating")] public double Rating { get; set; }
    }
}