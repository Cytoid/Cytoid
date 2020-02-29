using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class OnlineTier
{
    public string name;
    public float completionPercentage;
    public ColorPalette colorPalette;
    public List<string> criteria;
    public List<OnlineLevel> stages;
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