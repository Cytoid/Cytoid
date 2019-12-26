using System;
using System.Collections.Generic;

[Serializable]
public class GameResult
{

    public int Score;
    public double Accuracy;
    public int MaxCombo;
    public List<Mod> Mods;

    public Dictionary<NoteGrade, int> GradeCounts;

    public int EarlyCount;
    public int LateCount;
    public double AverageTimingError;
    public double StandardTimingError;
    
    public string LevelId;
    public int LevelVersion;
    public Difficulty ChartType;

}