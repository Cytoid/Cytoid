using System;
using System.Collections.Generic;

[Serializable]
public class UploadTierRecord
{
    public double completion;
    public double health;
    public int score;
    public double averageAccuracy;
    public UploadRecord.Details details;
    public List<string> mods;
    public int maxCombo;
    public List<UploadRecord> records;
}