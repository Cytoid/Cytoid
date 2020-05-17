using System;

[Serializable]
public class EventMeta
{
    public string id;
    public string logoUrl;
    public string coverUrl;
    public string postUrl;
    public DateTimeOffset startDate;
    public DateTimeOffset endDate;
    public string associatedLevelUid;
    public OnlineLevel associatedLevel;
    public string associatedCollectionId;
}