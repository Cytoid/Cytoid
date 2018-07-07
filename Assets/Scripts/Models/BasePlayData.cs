using System.Collections.Generic;

public class BasePlayData
{
    
    public bool IsRanked;
        
    public Dictionary<int, NoteGrading> NoteRankings;
    public int NoteCount;
    public int NoteCleared;
        
    public double Score;
    public double Tp;
    public int Combo;
    public int MaxCombo;

    public BasePlayData(bool isRanked)
    {
        IsRanked = isRanked;
    }
    
}