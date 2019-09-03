using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameState
{
    
    public bool IsStarted { get; set; }
    
    public bool IsPlaying { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public bool IsFailed { get; set; }
    
    public bool IsRanked { get; }
    public HashSet<Mod> Mods { get; }
    public float MaxHealth { get; }
    public int NoteCount { get; }

    public Dictionary<int, NoteJudgement> Judgements { get; private set; } = new Dictionary<int, NoteJudgement>();
    public int ClearCount { get; private set; }
    public bool ShouldFail { get; private set; }
    
    public float Score { get; private set; }
    public float Accuracy { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public float Health { get; private set; }
    
    public double NoteScoreMultiplier { get; private set; } = 1.0;

    public int EarlyCount =>
        Judgements.Values.Count(it => it.Grade != NoteGrade.Perfect && it.Grade != NoteGrade.Miss && it.Error < 0);

    public int LateCount =>
        Judgements.Values.Count(it => it.Grade != NoteGrade.Perfect && it.Grade != NoteGrade.Miss && it.Error > 0);

    public float AverageTimingError => Judgements.Values.Sum(it => it.Error) / Judgements.Count;

    public float StandardTimingError
    {
        get
        {
            var difference = 0f;
            Judgements.Values.ForEach(it => difference += Mathf.Pow(AverageTimingError - it.Error, 2));
            return Mathf.Sqrt(difference / Judgements.Count);
        }
    }

    private bool isFullScorePossible = true;
    private float accumulatedAccuracy;
    private float noteScoreMultiplierFactor;

    public GameState(Game game, bool isRanked, IEnumerable<Mod> mods, float maxHealth)
    {
        IsRanked = isRanked;
        Mods = new HashSet<Mod>(mods);
        MaxHealth = maxHealth;
        Health = MaxHealth;
        NoteCount = game.Chart.Model.note_list.Count;
        game.Chart.Model.note_list.ForEach(it => Judgements[it.id] = new NoteJudgement());
        noteScoreMultiplierFactor = Mathf.Sqrt(NoteCount) / 3.0f;
    }

    public void Judge(Note note, NoteGrade grade, float error, float greatGradeWeight)
    {
        if (Judgements[note.Model.id].IsJudged)
        {
            Debug.LogWarning($"Trying to judge note {note.Model.id} which is already judged.");
        }

        ClearCount++;
        Judgements[note.Model.id].Apply(it =>
        {
            it.IsJudged = true;
            it.Grade = grade;
            it.Error = error;
        });

        if (!IsRanked)
        {
            if (grade != NoteGrade.Perfect && grade != NoteGrade.Great) isFullScorePossible = false;
        }
        else
        {
            if (grade != NoteGrade.Perfect) isFullScorePossible = false;
        }

        // Combo
        if (grade == NoteGrade.Bad || grade == NoteGrade.Miss) Combo = 0;
        else Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;

        // Score multiplier
        if (IsRanked)
        {
            switch (grade)
            {
                case NoteGrade.Perfect:
                    NoteScoreMultiplier += 0.002D * noteScoreMultiplierFactor;
                    break;
                case NoteGrade.Great:
                    NoteScoreMultiplier += 0.0005D * noteScoreMultiplierFactor;
                    break;
                case NoteGrade.Good:
                    NoteScoreMultiplier -= -0.005D * noteScoreMultiplierFactor;
                    break;
                case NoteGrade.Bad:
                    NoteScoreMultiplier -= 0.025D * noteScoreMultiplierFactor;
                    break;
                case NoteGrade.Miss:
                    NoteScoreMultiplier -= 0.05D * noteScoreMultiplierFactor;
                    break;
            }

            if (NoteScoreMultiplier > 1) NoteScoreMultiplier = 1;
            if (NoteScoreMultiplier < 0) NoteScoreMultiplier = 0;
        }

        // Score
        if (!IsRanked)
        {
            Score += 900000f / NoteCount * grade.GetScoreWeight(false) +
                     100000f / (NoteCount * (float) (NoteCount + 1) / 2f) * Combo;
        }
        else
        {
            var maxNoteScore = 1000000.0 / NoteCount;

            double noteScore;
            if (grade == NoteGrade.Great)
            {
                noteScore = maxNoteScore * (NoteGrade.Great.GetScoreWeight(true) +
                                            (NoteGrade.Perfect.GetScoreWeight(true) -
                                             NoteGrade.Great.GetScoreWeight(true)) *
                                            greatGradeWeight);
            }
            else
            {
                noteScore = maxNoteScore * grade.GetScoreWeight(true);
            }

            noteScore *= NoteScoreMultiplier;
            Score += (float) noteScore;
        }
        if (Score > 999500)
        {
            if (ClearCount == NoteCount && isFullScorePossible)
            {
                Score = 1000000;
            }
        }
        if (Score > 1000000) Score = 1000000;

        // Accuracy
        if (!IsRanked || grade != NoteGrade.Great)
        {
            accumulatedAccuracy += 100f * grade.GetAccuracyWeight();
        }
        else
        {
            accumulatedAccuracy += 100f * (NoteGrade.Great.GetAccuracyWeight() +
                                           (NoteGrade.Perfect.GetAccuracyWeight() -
                                            NoteGrade.Great.GetAccuracyWeight()) *
                                           greatGradeWeight);
        }
        Accuracy = accumulatedAccuracy / ClearCount;

        // Health mods
        if (Mods.Contains(Mod.Hard) || Mods.Contains(Mod.ExHard))
        {
            var mods = Mods.Contains(Mod.ExHard) ? exHardHpMods : hardHpMods;

            var mod = mods
                .Select[note.Type]
                .Select[IsRanked ? rankedGradingIndex[grade] : unrankedGradingIndex[grade]];

            switch (mod.Type)
            {
                case HpModType.Absolute:
                    Health += mod.Value;
                    break;
                case HpModType.Percentage:
                    Health += mod.Value / 100f * MaxHealth;
                    break;
                case HpModType.DivideByNoteCount:
                    Health += mod.Value / NoteCount / 100f * MaxHealth;
                    break;
            }

            Health = Mathf.Clamp(Health, 0, MaxHealth);
            if (Health < 0) ShouldFail = true;
        }

        if (
            Mods.Contains(Mod.AllPerfect) && grade != NoteGrade.Perfect
            ||
            Mods.Contains(Mod.FullCombo) && (grade == NoteGrade.Bad || grade == NoteGrade.Miss)
        )
        {
            ShouldFail = true;
        }
    }

    public bool IsJudged(int noteId) => Judgements[noteId].IsJudged;

    public NoteJudgement GetJudgement(int noteId) => Judgements[noteId];

    #region Health Mods

    private static Dictionary<NoteGrade, int> unrankedGradingIndex = new Dictionary<NoteGrade, int>
    {
        {NoteGrade.Perfect, 0},
        {NoteGrade.Great, 2},
        {NoteGrade.Good, 3},
        {NoteGrade.Bad, 4},
        {NoteGrade.Miss, 5}
    };

    private static Dictionary<NoteGrade, int> rankedGradingIndex = new Dictionary<NoteGrade, int>
    {
        {NoteGrade.Perfect, 0},
        {NoteGrade.Great, 1},
        {NoteGrade.Good, 2},
        {NoteGrade.Bad, 3},
        {NoteGrade.Miss, 5}
    };

    private static ModeHpMod hardHpMods = new ModeHpMod(new Dictionary<NoteType, NoteHpMod>
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

    private static ModeHpMod exHardHpMods = new ModeHpMod(new Dictionary<NoteType, NoteHpMod>
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

    #endregion
    
}

public class NoteJudgement
{
    public bool IsJudged;
    public NoteGrade Grade;
    public float Error;
}

public class ModeHpMod
{
    public readonly Dictionary<NoteType, NoteHpMod> Select;

    public ModeHpMod(Dictionary<NoteType, NoteHpMod> select)
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