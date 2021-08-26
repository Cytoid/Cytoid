using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

[Serializable]
public class CharacterMeta
{
    [JsonProperty("date")] public DateTimeOffset? Date { get; set; }
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("illustrator")] public IllustratorMeta Illustrator { get; set; }
    [JsonProperty("designer")] public CharacterDesignerMeta CharacterDesigner { get; set; }
    
    [JsonProperty("standard_id")] [CanBeNull] public string StandardId { get; set; }
    [JsonProperty("variant_description")] [CanBeNull] public string VariantDescription { get; set; }

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

    public override string ToString() => JsonConvert.SerializeObject(this);

}