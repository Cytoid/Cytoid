using System;

[Serializable]
public class OnlineRecord
{
    public double accuracy;
    public int score;
    public DateTimeOffset date;
    public Chart chart;
    
    public OnlineUser owner;

    [Serializable]
    public class Chart
    {
        public string type;
        public string name; 
        public int difficulty;
        public int notesCount;
        public OnlineLevel level;
    }
}