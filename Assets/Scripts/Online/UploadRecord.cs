using System.Collections.Generic;

public class UploadRecord
{
    public int score;
    public double accuracy;
    public Details details;
    public List<string> mods;
    public bool ranked;

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