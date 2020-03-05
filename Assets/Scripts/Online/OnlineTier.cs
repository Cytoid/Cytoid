using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public class OnlineTier
{
    public string name;
    public float completionPercentage;
    public ColorPalette colorPalette;
    public List<string> criteria;
    public List<OnlineLevel> stages;
    [JsonIgnore] public List<Level> localStages = new List<Level>();
    public OnlineCharacter character;

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
    public bool StagesDownloaded => data.localStages.Count > 0 && data.localStages.TrueForAll(it => it.IsLocal);
    public bool locked;
    public float completion;
    [JsonProperty("tier")] public OnlineTier data;
}

[Serializable]
public class Season
{
    public string title;
    public List<Tier> tiers;
}