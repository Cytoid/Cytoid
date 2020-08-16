using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class Badge
{
    public string uid;
    public string title;
    public string description;
    public bool listed;
    public DateTimeOffset date;

    [JsonConverter(typeof(StringEnumConverter))]
    public BadgeType type;

    public Dictionary<string, object> metadata;

    public string GetImageUrl()
    {
        if (type != BadgeType.Event) throw new ArgumentOutOfRangeException();
        return (string) metadata["imageUrl"];
    }
    
    public List<string> GetBadgeOverrides()
    {
        if (metadata == null || !metadata.ContainsKey("overrides")) return new List<string>();
        return ((JArray) metadata["overrides"]).Select(it => (string) it).ToList();
    }
    
}

public enum BadgeType
{
    Achievement, Event
}