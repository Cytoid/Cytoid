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

        public void OnClear(GameNote note, NoteGrading grading, double greatGradeWeight)
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

            if (NoteRankings[note.Note.id] == NoteGrading.Undetermined) NoteCleared++;
            NoteRankings[note.Note.id] = grading;

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
                var mods = Mods.Contains(Mod.ExHard) ? ExHardHpMods : HardHpMods;

                var mod = mods.Select[note.Note.type]
                    .Select[IsRanked ? RankedGradingIndices[grading] : UnrankedGradingIndices[grading]];

                switch (mod.Type)
                {
                    case HpModType.Absolute:
                        Hp += mod.Value;
                        break;
                    case HpModType.Percentage:
                        Hp += mod.Value / 100f * MaxHp;
                        break;
                    case HpModType.DivideByNoteCount:
                        Hp += (mod.Value / NoteCount) / 100f * MaxHp;
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
        
        public static Dictionary<NoteGrading, int> UnrankedGradingIndices = new Dictionary<NoteGrading, int>
        {
            { NoteGrading.Perfect, 0 },
            { NoteGrading.Great, 2 },
            { NoteGrading.Good, 3 },
            { NoteGrading.Bad, 4 },
            { NoteGrading.Miss, 5 }
        };
        
        public static Dictionary<NoteGrading, int> RankedGradingIndices = new Dictionary<NoteGrading, int>
        {
            { NoteGrading.Perfect, 0 },
            { NoteGrading.Great, 1 },
            { NoteGrading.Good, 2 },
            { NoteGrading.Bad, 3 },
            { NoteGrading.Miss, 5 }
        };
        
        public static ModeHpMod HardHpMods = new ModeHpMod(new Dictionary<int, NoteHpMod>
        {
            {
                NoteType.Click, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(2, HpModType.Absolute),
                    new HpMod(0.5f, HpModType.Absolute),
                    new HpMod(-1, HpModType.Percentage),
                    new HpMod(-3, HpModType.Percentage),
                    new HpMod(-6, HpModType.Percentage),
                    new HpMod(-8, HpModType.Percentage)
                })
            },
            {
                NoteType.Hold, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(1, HpModType.Absolute),
                    new HpMod(0.25f, HpModType.Absolute),
                    new HpMod(-1.5f, HpModType.Percentage),
                    new HpMod(-4, HpModType.Percentage),
                    new HpMod(-9, HpModType.Percentage),
                    new HpMod(-12, HpModType.Percentage)
                })
            },
            {
                NoteType.LongHold, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(1, HpModType.Absolute),
                    new HpMod(0.25f, HpModType.Absolute),
                    new HpMod(-1.5f, HpModType.Percentage),
                    new HpMod(-4, HpModType.Percentage),
                    new HpMod(-9, HpModType.Percentage),
                    new HpMod(-12, HpModType.Percentage)
                })
            },
            {
                NoteType.DragHead, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(0.4f, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-8, HpModType.Percentage)
                })
            },
            {
                NoteType.DragChild, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(0.2f, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-2.4f, HpModType.Percentage)
                })
            },
            {
                NoteType.Flick, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(2, HpModType.Absolute),
                    new HpMod(0.5f, HpModType.Absolute),
                    new HpMod(-0.75f, HpModType.Percentage),
                    new HpMod(-2.25f, HpModType.Percentage),
                    new HpMod(-4, HpModType.Percentage),
                    new HpMod(-6, HpModType.Percentage)
                })
            }
        });
        
        public static ModeHpMod ExHardHpMods = new ModeHpMod(new Dictionary<int, NoteHpMod>
        {
            {
                NoteType.Click, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(1, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-4, HpModType.Percentage),
                    new HpMod(-8, HpModType.Percentage),
                    new HpMod(-15, HpModType.Percentage),
                    new HpMod(-20, HpModType.Percentage)
                })
            },
            {
                NoteType.Hold, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(0.5f, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-6, HpModType.Percentage),
                    new HpMod(-12, HpModType.Percentage),
                    new HpMod(-20, HpModType.Percentage),
                    new HpMod(-25, HpModType.Percentage)
                })
            },
            {
                NoteType.LongHold, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(0.5f, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-6, HpModType.Percentage),
                    new HpMod(-12, HpModType.Percentage),
                    new HpMod(-20, HpModType.Percentage),
                    new HpMod(-25, HpModType.Percentage)
                })
            },
            {
                NoteType.DragHead, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(0.2f, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-20, HpModType.Percentage)
                })
            },
            {
                NoteType.DragChild, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(0.1f, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-6, HpModType.Percentage)
                })
            },
            {
                NoteType.Flick, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(1, HpModType.Absolute),
                    new HpMod(0, HpModType.Absolute),
                    new HpMod(-3, HpModType.Percentage),
                    new HpMod(-6, HpModType.Percentage),
                    new HpMod(-12, HpModType.Percentage),
                    new HpMod(-15, HpModType.Percentage)
                })
            }
        });

        public class ModeHpMod
        {
            public readonly Dictionary<int, NoteHpMod> Select;

            public ModeHpMod(Dictionary<int, NoteHpMod> select)
            {
                Select = select;
            }
        }

        public class NoteHpMod
        {
            public readonly List<HpMod> Select;

            public NoteHpMod(List<HpMod> select)
            {
                Select = select;
            }
        }

        public class HpMod
        {
            public float Value;
            public HpModType Type;

            public HpMod(float value, HpModType type)
            {
                Value = value;
                Type = type;
            }
            
        }
        
        public enum HpModType
        {
            Absolute, Percentage, DivideByNoteCount
        }

    }
}