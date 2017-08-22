using System.Collections.Generic;

public class PlayData
{

    public Dictionary<int, NoteRanking> NoteRankings;
    public int NoteCount;
    public int NoteCleared;

    public float Score;
    public float Tp;
    private float tpNow;
    public int Combo;
    public int MaxCombo;

    public PlayData(Chart chart)
    {
        NoteRankings = new Dictionary<int, NoteRanking>();
        foreach (var note in chart.notes.Keys)
        {
            NoteRankings[note] = NoteRanking.Undetermined;
        }
        NoteCount = chart.notes.Count;
    }
    
    public void ClearNote(int id, NoteRanking ranking)
    {
        if (NoteRankings[id] == NoteRanking.Undetermined) NoteCleared++;
        NoteRankings[id] = ranking;
        if (ranking == NoteRanking.Bad || ranking == NoteRanking.Miss) Combo = 0;
        else
        {
            Combo++;
            if (MaxCombo < Combo) MaxCombo = Combo;
        }
        Score += 900000f / NoteCount * ranking.ScoreWeight() + 100000f / (NoteCount * (float) (NoteCount + 1) / 2f) * Combo;
        if (Score > 1000000 || 1000000 - Score < 0.00001f) Score = 1000000f;
        tpNow += 100f * ranking.TpWeight();
        Tp = tpNow / NoteCleared;
    }
    
}