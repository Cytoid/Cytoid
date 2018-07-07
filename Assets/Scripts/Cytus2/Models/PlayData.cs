using System;
using System.Collections.Generic;

namespace Cytus2.Models
{
    public class PlayData : BasePlayData
    {
        
        public double ComboMultiplier = 1.0f;

        private bool isMillionMasterPossible = true;
        private double currentTp;
        private readonly double magicNumber;

        public PlayData(bool isRanked, Chart chart) : base(isRanked)
        {
            NoteRankings = new Dictionary<int, NoteGrading>();
            foreach (var note in chart.Root.note_list)
            {
                NoteRankings[note.id] = NoteGrading.Undetermined;
            }
            NoteCount = chart.Root.note_list.Count;
            magicNumber = Math.Sqrt(NoteCount) / 3.0;
        }

        public void Clear(int id, NoteGrading grading, double greatGradeWeight)
        {
            
            if (grading == NoteGrading.Undetermined) throw new InvalidOperationException("Note grading undetermined");

            if (!IsRanked)
            {
                if (grading != NoteGrading.Perfect && grading != NoteGrading.Great) isMillionMasterPossible = false;
            }
            else
            {
                if (grading != NoteGrading.Perfect) isMillionMasterPossible = false;
            }

            if (NoteRankings[id] == NoteGrading.Undetermined) NoteCleared++;
            NoteRankings[id] = grading;

            // Combo
            if (grading == NoteGrading.Bad || grading == NoteGrading.Miss) Combo = 0;
            else
            {
                Combo++;
                if (MaxCombo < Combo) MaxCombo = Combo;
            }

            if (IsRanked)
            {
                switch (grading)
                {
                    case NoteGrading.Perfect:
                        ComboMultiplier += 0.002D * magicNumber;
                        break;
                    case NoteGrading.Great:
                        ComboMultiplier += 0.0005D * magicNumber;
                        break;
                    case NoteGrading.Good:
                        ComboMultiplier -= -0.005D * magicNumber;
                        break;
                    case NoteGrading.Bad:
                        ComboMultiplier -= 0.025D * magicNumber;
                        break;
                    case NoteGrading.Miss:
                        ComboMultiplier -= 0.05D * magicNumber;
                        break;
                }

                if (ComboMultiplier > 1) ComboMultiplier = 1;
                if (ComboMultiplier < 0) ComboMultiplier = 0;
            }

            // Score
            if (!IsRanked)
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
                                                 NoteGrading.Great.ScoreWeight(ranked: true)) *
                                                greatGradeWeight);
                }
                else
                {
                    noteScore = maxNoteScore * grading.ScoreWeight(ranked: true);
                }

                noteScore *= ComboMultiplier;

                Score += noteScore;

            }

            if (Score > 999500)
            {
                if (NoteCleared == NoteCount && isMillionMasterPossible)
                {
                    Score = 1000000;
                }
            }

            if (Score > 1000000) Score = 1000000;

            // TP
            currentTp += 100f * grading.TpWeight();
            Tp = currentTp / NoteCleared;
        }

    }
}