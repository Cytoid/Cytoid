using System;
using System.Collections.Generic;

public class PlayData
{

    public Dictionary<int, NoteGrading> NoteRankings;
    public int NoteCount;
    public double MagicNumber;
    public int NoteCleared;

    public bool ranked;
    public double multiplier = 1.0f;
    
    public double Score;
    public double Tp;
    private double tpNow;
    public int Combo;
    public int MaxCombo;
    public bool CanMM = true;

    public PlayData(Chart chart, bool ranked)
    {
        this.ranked = ranked;
        NoteRankings = new Dictionary<int, NoteGrading>();
        foreach (var note in chart.notes.Keys)
        {
            NoteRankings[note] = NoteGrading.Undetermined;
        }
        NoteCount = chart.notes.Count;
        MagicNumber = Math.Sqrt(NoteCount) / 3.0;
    }
    
    public virtual void ClearNote(NoteView noteView)
    {
        
        var grading = noteView.CalculateGrading();

        if (!ranked)
        {
            if (grading != NoteGrading.Perfect && grading != NoteGrading.Great) CanMM = false;
        }
        else
        {
            if (grading != NoteGrading.Perfect) CanMM = false;
        }

        if (NoteRankings[noteView.note.id] == NoteGrading.Undetermined) NoteCleared++;
        NoteRankings[noteView.note.id] = grading;
        
        // Combo
        if (grading == NoteGrading.Bad || grading == NoteGrading.Miss) Combo = 0;
        else
        {
            Combo++;
            if (MaxCombo < Combo) MaxCombo = Combo;
        }

        if (ranked)
        {
            switch (grading)
            {
                case NoteGrading.Perfect:
                    multiplier += 0.002D * MagicNumber;
                    break;
                case NoteGrading.Great:
                    multiplier += 0.0005D * MagicNumber;
                    break;
                case NoteGrading.Good:
                    multiplier -= -0.005D * MagicNumber;
                    break;
                case NoteGrading.Bad:
                    multiplier -= 0.025D * MagicNumber;
                    break;
                case NoteGrading.Miss:
                    multiplier -= 0.05D * MagicNumber;
                    break;
            }
            if (multiplier > 1) multiplier = 1;
            if (multiplier < 0) multiplier = 0;
        }
        
        // Score
        if (!ranked)
        {
            
            Score += 900000f / NoteCount * grading.ScoreWeight(ranked: false) +
                     100000f / (NoteCount * (double) (NoteCount + 1) / 2f) * Combo;
           
        }
        else
        {

            var maxNoteScore = 1000000.0 / NoteCount;

            double noteScore;

            if (grading == NoteGrading.Great)
            {
                noteScore = maxNoteScore * (NoteGrading.Great.ScoreWeight(ranked: true) +
                                            (NoteGrading.Perfect.ScoreWeight(ranked: true) -
                                             NoteGrading.Great.ScoreWeight(ranked: true)) * noteView.GreatGradeWeight);
            }
            else
            {
                noteScore = maxNoteScore * grading.ScoreWeight(ranked: true);
            }

            noteScore *= multiplier;

            Score += noteScore;

        }
        
        if (Score > 999500)
        {
            if (NoteCleared == NoteCount && CanMM)
            {
                Score = 1000000;
            }
        }
        if (Score > 1000000) Score = 1000000;
        
        // TP
        tpNow += 100f * grading.TpWeight();
        Tp = tpNow / NoteCleared;
    }
    
}