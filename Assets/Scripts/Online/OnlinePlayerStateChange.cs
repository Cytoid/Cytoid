using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public class OnlinePlayerStateChange
{
    public bool hasChanges;
    public List<Reward> rewards;
}