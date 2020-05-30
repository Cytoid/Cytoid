using System;
using Newtonsoft.Json;

[Serializable]
public class CharacterMeta
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("illustrator")] public IllustratorMeta Illustrator { get; set; }
    [JsonProperty("designer")] public CharacterDesignerMeta CharacterDesigner { get; set; }

    [Serializable]
    public class IllustratorMeta
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
    }

    [Serializable]
    public class CharacterDesignerMeta
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
    }

    [JsonProperty("level")] public OnlineLevel Level { get; set; }
    [JsonProperty("asset")] public string AssetId { get; set; }
    [JsonProperty("tachieAsset")] public string TachieAssetId { get; set; }

    public override string ToString() => JsonConvert.SerializeObject(this);

}