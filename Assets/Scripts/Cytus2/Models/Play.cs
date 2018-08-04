using System;
using System.Collections.Generic;
using System.Linq;
using Cytus2.Controllers;
using UnityEngine;

namespace Cytus2.Models
{
    [Serializable]
    public class Play
    {
        public bool IsRanked;
        public HashSet<Mod> Mods = new HashSet<Mod>();

        public Dictionary<int, NoteGrade> NoteRankings;
        public int NoteCount;
        public int NoteCleared;
        public int Early;
        public int Late;
        private readonly List<float> timeOffs = new List<float>();

        public float AvgTimeOff
        {
            get { return timeOffs.Sum() / timeOffs.Count; }
        }

        public float StandardTimeOff
        {
            get
            {
                var difference = 0f;
                timeOffs.ForEach(it => difference += Mathf.Pow(AvgTimeOff - it, 2));
                return Mathf.Sqrt(difference / timeOffs.Count);
            }
        }

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
            NoteRankings = new Dictionary<int, NoteGrade>();
            foreach (var note in chart.Root.note_list)
            {
                NoteRankings[note.id] = NoteGrade.Undetermined;
            }

            NoteCount = chart.Root.note_list.Count;
            magicNumber = Math.Sqrt(NoteCount) / 3.0;
        }

        public void OnClear(GameNote note, NoteGrade grade, float timeUntilEnd, double greatGradeWeight)
        {
            if (grade == NoteGrade.Undetermined) return;
            if (NoteRankings[note.Note.id] != NoteGrade.Undetermined) return;

            if (!IsRanked)
            {
                if (grade != NoteGrade.Perfect && grade != NoteGrade.Great) isMillionMasterPossible = false;
            }
            else
            {
                if (grade != NoteGrade.Perfect) isMillionMasterPossible = false;
            }

            NoteCleared++;
            NoteRankings[note.Note.id] = grade;

            if (grade != NoteGrade.Perfect && grade != NoteGrade.Miss)
            {
                if (timeUntilEnd > 0) Early++;
                else Late++;
            }
            
            timeOffs.Add(timeUntilEnd);

            // Combo
            if (grade == NoteGrade.Bad || grade == NoteGrade.Miss) Combo = 0;
            else
            {
                Combo++;
                if (MaxCombo < Combo) MaxCombo = Combo;
            }

            if (IsRanked)
            {
                switch (grade)
                {
                    case NoteGrade.Perfect:
                        ComboMultiplier += 0.002D * magicNumber;
                        break;
                    case NoteGrade.Great:
                        ComboMultiplier += 0.0005D * magicNumber;
                        break;
                    case NoteGrade.Good:
                        ComboMultiplier -= -0.005D * magicNumber;
                        break;
                    case NoteGrade.Bad:
                        ComboMultiplier -= 0.025D * magicNumber;
                        break;
                    case NoteGrade.Miss:
                        ComboMultiplier -= 0.05D * magicNumber;
                        break;
                }

                if (ComboMultiplier > 1) ComboMultiplier = 1;
                if (ComboMultiplier < 0) ComboMultiplier = 0;
            }

            // Score
            if (!IsRanked)
            {
                Score += 900000f / NoteCount * grade.ScoreWeight(ranked: false) +
                         100000f / (NoteCount * (double) (NoteCount + 1) / 2f) * Combo;
            }
            else
            {
                var maxNoteScore = 1000000.0 / NoteCount;

                double noteScore;

                if (grade == NoteGrade.Great)
                {
                    noteScore = maxNoteScore * (NoteGrade.Great.ScoreWeight(ranked: true) +
                                                (NoteGrade.Perfect.ScoreWeight(ranked: true) -
                                                 NoteGrade.Great.ScoreWeight(ranked: true)) *
                                                greatGradeWeight);
                }
                else
                {
                    noteScore = maxNoteScore * grade.ScoreWeight(ranked: true);
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
            if (!IsRanked || grade != NoteGrade.Great)
            {
                currentTp += 100f * grade.TpWeight();
            }
            else
            {
                currentTp += 100f * (NoteGrade.Great.TpWeight() +
                             (NoteGrade.Perfect.TpWeight() - NoteGrade.Great.TpWeight()) * greatGradeWeight);
            }
            Tp = currentTp / NoteCleared;

            // HP
            if (Mods.Contains(Mod.Hard) || Mods.Contains(Mod.ExHard))
            {
                var mods = Mods.Contains(Mod.ExHard) ? ExHardHpMods : HardHpMods;

                var mod = mods.Select[note.Note.type]
                    .Select[IsRanked ? RankedGradingIndices[grade] : UnrankedGradingIndices[grade]];

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
                (Mods.Contains(Mod.AP) && grade != NoteGrade.Perfect)
                ||
                (Mods.Contains(Mod.FC) && (grade == NoteGrade.Bad || grade == NoteGrade.Miss))
            )
            {
                Game.Instance.Fail();
            }
        }

        public static Dictionary<NoteGrade, int> UnrankedGradingIndices = new Dictionary<NoteGrade, int>
        {
            {NoteGrade.Perfect, 0},
            {NoteGrade.Great, 2},
            {NoteGrade.Good, 3},
            {NoteGrade.Bad, 4},
            {NoteGrade.Miss, 5}
        };

        public static Dictionary<NoteGrade, int> RankedGradingIndices = new Dictionary<NoteGrade, int>
        {
            {NoteGrade.Perfect, 0},
            {NoteGrade.Great, 1},
            {NoteGrade.Good, 2},
            {NoteGrade.Bad, 3},
            {NoteGrade.Miss, 5}
        };

        public static ModeHpMod HardHpMods = new ModeHpMod(new Dictionary<int, NoteHpMod>
        {
            {
                NoteType.Click, new NoteHpMod(new List<HpMod>
                {
                    new HpMod(1, HpModType.Absolute),
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
                    new HpMod(0.5f, HpModType.Absolute),
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
                    new HpMod(0.5f, HpModType.Absolute),
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
                    new HpMod(0.2f, HpModType.Absolute),
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
                    new HpMod(0.1f, HpModType.Absolute),
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
                    new HpMod(1, HpModType.Absolute),
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
            Absolute,
            Percentage,
            DivideByNoteCount
        }
    }
}