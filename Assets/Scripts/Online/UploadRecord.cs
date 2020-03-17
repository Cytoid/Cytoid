using System;
using System.Collections.Generic;

[Serializable]
public class UploadRecord
{
    public int score;
    public double accuracy;
    public Details details;
    public List<string> mods;
    public bool ranked;
    public string hash;

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