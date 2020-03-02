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
public class UserTier
{
    [JsonIgnore] public bool isScrollRectFix;
    [JsonIgnore] public int index;
    public bool StagesDownloaded => tier.localStages.Count > 0 && tier.localStages.TrueForAll(it => it.IsLocal);
    public bool locked;
    public float completion;
    public OnlineTier tier;
}

[Serializable]
public class Season
{
    public string title;
    public List<UserTier> tiers;
}