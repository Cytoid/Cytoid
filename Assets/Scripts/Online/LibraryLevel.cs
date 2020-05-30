using System;
using Newtonsoft.Json;

[Serializable]
public class LibraryLevel
{
    [JsonProperty("date")] public DateTimeOffset Date { get; set; }
    [JsonProperty("expiryDate")] public DateTimeOffset? ExpiryDate { get; set; }
    [JsonProperty("granted")] public bool Granted { get; set; }
    [JsonProperty("level")] public OnlineLevel Level { get; set; }
}