using System;
using System.Collections.Generic;
using Cytus2.Controllers;

namespace Cytus2.Models
{
    
    [Serializable]
    public class Play
    {
        
        public bool IsRanked;
        public HashSet<Mod> Mods = new HashSet<Mod>();
        
        public Dictionary<int, NoteGrading> NoteRankings;
        public int NoteCount;
        public int NoteCleared;
        
        public double Score;
        public double Tp;
        public int Combo;
        public int MaxCombo;
        public float Hp;
        public float MaxHp;
        
        public double ComboMultiplier = 1.0f;

        private bool isMillionMasterPossible = true;
        private double currentTp;
        private double magicNumber;

        public Play(bool isRanked)
        {
            IsRanked = isRanked;
        }

        public void Init(Chart chart)
        {
            NoteRankings = new Dictionary<int, NoteGrading>();
            foreach (var note in chart.Root.note_list)
            {
                NoteRankings[note.id] = NoteGrading.Undetermined;
            }
            NoteCount = chart.Root.note_list.Count;
            magicNumber = Math.Sqrt(NoteCount) / 3.0;
        }

        public void OnClear(int id, NoteGrading grading, double greatGradeWeight)
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
            
            // HP
            if (Mods.Contains(Mod.Hard) || Mods.Contains(Mod.ExHard))
            {
                var ex = Mods.Contains(Mod.ExHard);
                switch (grading)
                {
                    case NoteGrading.Perfect:
                        Hp += ex ? 1f : 2f;
                        break;
                    case NoteGrading.Great:
                        Hp -= (ex ? 0.04f : 0.01f) * MaxHp;
                        break;
                    case NoteGrading.Good:
                        Hp -= (ex ? 0.08f : 0.03f) * MaxHp;
                        break;
                    case NoteGrading.Bad:
                        Hp -= (ex ? 0.15f : 0.06f) * MaxHp;
                        break;
                    case NoteGrading.Miss:
                        Hp -= (ex ? 0.20f : 0.08f) * MaxHp;
                        break;
                }
                if (Hp > MaxHp) Hp = MaxHp;
                if (Hp < 0) Game.Instance.Fail();
            }

            if (
                (Mods.Contains(Mod.AP) && grading != NoteGrading.Perfect)
                ||
                (Mods.Contains(Mod.FC) && (grading == NoteGrading.Bad || grading == NoteGrading.Miss))
            )
            {
                Game.Instance.Fail();
            }
        }

    }
}