using System;

[Serializable]
public class TierRankingEntry
{
    public int rank;
    
    public double completion;
    public double averageAccuracy;
    public double health;
    public int maxCombo;
    public OnlineUser owner;
    
    public DateTime date;
}