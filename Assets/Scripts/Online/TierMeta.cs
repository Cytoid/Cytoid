using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public class TierMeta
{
    public string name;
    public float completionPercentage;
    public ColorPalette colorPalette;
    public List<string> criteria;
    [JsonIgnore] public List<Criterion> Criteria => new List<Criterion>
    {
        new AverageAccuracyCriterion(0.975),
        new StageFullComboCriterion(0),
        new HealthPercentageCriterion(0.9f)
    };
    
    public List<OnlineLevel> stages;
    [JsonIgnore] public List<Level> localStages = new List<Level>();
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
public class Tier
{
    [JsonIgnore] public bool isScrollRectFix;
    [JsonIgnore] public int index;
    public bool StagesDownloaded => Meta.localStages.Count > 0 && Meta.localStages.TrueForAll(it => it.IsLocal);

    public bool locked;
    public double completion;
    [JsonProperty("tier")] public TierMeta Meta;
}

[Serializable]
public class Season
{
    public List<Tier> tiers;
}