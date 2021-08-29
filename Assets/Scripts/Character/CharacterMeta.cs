using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

[Serializable]
public class CharacterMeta
{
    [JsonProperty("date")] public string Date { get; set; }
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("illustrator")] public IllustratorMeta Illustrator { get; set; }
    [JsonProperty("designer")] public CharacterDesignerMeta CharacterDesigner { get; set; }
    
    [JsonProperty("locked")] public bool Locked { get; set; }
    
    [JsonProperty("set_id")] [CanBeNull] public string SetId { get; set; }
    [JsonProperty("variant_name")] [CanBeNull] public string VariantName { get; set; }
    [JsonProperty("variant_parallax_asset")] [CanBeNull] public string VariantParallaxAsset { get; set; }
    [JsonProperty("variant_audio_asset")] [CanBeNull] public string VariantAudioAsset { get; set; }

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

    [JsonProperty("level")] [CanBeNull] public OnlineLevel Level { get; set; }
    [JsonProperty("asset")] public string AssetId { get; set; }
    
    [JsonProperty("exp")] public ExpData Exp { get; set; }
    
    [Serializable]
    public class ExpData
    {
        [JsonProperty("currentLevel")] public int CurrentLevel { get; set; }
        [JsonProperty("totalExp")] public float TotalExp { get; set; }
        [JsonProperty("currentLevelExp")] public float CurrentLevelExp { get; set; }
        [JsonProperty("nextLevelExp")] public float NextLevelExp { get; set; }
    }

    public override string ToString() => JsonConvert.SerializeObject(this);

}