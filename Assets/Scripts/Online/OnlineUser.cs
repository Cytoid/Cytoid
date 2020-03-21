using System;
using Newtonsoft.Json;

[Serializable]
public class OnlineUser
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("uid")] public string Uid { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("avatarURL")] public string AvatarUrl { get; set; }
    [JsonProperty("avatar")] public OnlineAvatarImageAsset Avatar { get; set; }
}

[Serializable]
public class OnlineAvatarImageAsset
{
    [JsonProperty("original")] public string OriginalUrl { get; set; }
    [JsonProperty("small")] public string SmallUrl { get; set; }
    [JsonProperty("large")] public string LargeUrl { get; set; }
}