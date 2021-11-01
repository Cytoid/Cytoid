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
    
    [JsonProperty("owned")] public bool Owned { get; set; }
    
    [JsonProperty("setId")] [CanBeNull] public string SetId { get; set; }
    [JsonProperty("setOrder")] public int SetOrder { get; set; }
    [JsonProperty("variantName")] [CanBeNull] public string VariantName { get; set; }
    [JsonProperty("variantParallaxAsset")] [CanBeNull] public string VariantParallaxAsset { get; set; }
    [JsonProperty("variantAudioAsset")] [CanBeNull] public string VariantAudioAsset { get; set; }

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
    [JsonProperty("questId")] public string QuestId { get; set; }
    
    [JsonProperty("exp")] [CanBeNull] public ExpData Exp { get; set; }
    
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