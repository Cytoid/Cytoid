using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public class OnlinePlayerStateChange
{
    public bool hasChanges;
    public List<Reward> rewards;

    [Serializable]
    public class Reward
    {
        public string type;
        public JObject value;
        public Lazy<OnlineLevel> onlineLevelValue;
        public Lazy<CharacterMeta> characterValue;

        public RewardType Type => type == "level" ? RewardType.Level : RewardType.Character;

        [JsonConstructor]
        public Reward()
        {
            onlineLevelValue = new Lazy<OnlineLevel>(() => value.ToObject<OnlineLevel>());
            characterValue = new Lazy<CharacterMeta>(() => value.ToObject<CharacterMeta>());
        }
        
        public enum RewardType
        {
            Level, Character
        }
    }
}