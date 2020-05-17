using System;

[Serializable]
public class RankingEntry
{
    public int rank;
    
    public int score;
    public double accuracy; // 0~1
    public Details details;
    public string[] mods;
    public OnlineUser owner;
    
    public DateTime date;
    
    [Serializable]
    public class Details
    {
        public int perfect;
        public int great;
        public int good;
        public int bad;
        public int miss;
        public int maxCombo;
    }
}