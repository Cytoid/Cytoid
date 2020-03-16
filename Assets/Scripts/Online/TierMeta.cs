using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public class TierMeta
{
    public string name;
    public float completionPercentage;
    public ColorPalette colorPalette;
    public List<CriterionMeta> criteria;
    [JsonIgnore] public List<Criterion> parsedCriteria;
    
    public List<OnlineLevel> stages;
    [JsonIgnore] public List<Level> parsedStages = new List<Level>();
    public OnlineCharacter character;
    public double thresholdAccuracy;
    public double maxHealth;

    public class ColorPalette
    {
        public string background;
        public string[] stages;
    }
}

[Serializable]
public class CriterionMeta
{
    public string name;
    public JObject args;
    public string description;
}

[Serializable]
public class Tier
{
    [JsonIgnore] public bool isScrollRectFix;
    [JsonIgnore] public int index;
    public bool StagesDownloaded => Meta.parsedStages.Count > 0 && Meta.parsedStages.TrueForAll(it => it.IsLocal);

    public bool locked;
    public double completion;
    [JsonProperty("tier")] public TierMeta Meta;
}

[Serializable]
public class Season
{
    public string uid;
    public List<Tier> tiers;
}