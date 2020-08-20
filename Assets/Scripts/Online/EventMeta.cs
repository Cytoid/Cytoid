using System;
using JetBrains.Annotations;

[Serializable]
public class EventMeta
{
    public string uid;
    public string title;
    public string slogan;

    public bool locked;
    
    public DateTimeOffset? startDate;
    public DateTimeOffset? endDate;

    public int? levelId;
    public string collectionId;
    [CanBeNull] public OnlineImageAsset cover;
    [CanBeNull] public OnlineImageAsset logo;

    public string url;
}