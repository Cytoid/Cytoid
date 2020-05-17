using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public sealed class GameState
{
    public GameMode Mode { get; }
    
    public Level Level { get; }
    
    public Difficulty Difficulty { get; }
    
    public int DifficultyLevel { get; }
    
    public bool IsStarted { get; set; }
    
    public bool IsPlaying { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public bool IsFailed { get; set; }
    public HashSet<Mod> Mods { get; }
    public double MaxHealth { get; }
    public int NoteCount { get; }

    public Dictionary<int, NoteJudgement> Judgements { get; private set; } = new Dictionary<int, NoteJudgement>();

    public int ClearCount { get; private set; }
    public bool ShouldFail { get; private set; }
    
    public double Score { get; private set; }
    public double Accuracy { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }
    public double Health { get; private set; }

    public double HealthPercentage => Health / MaxHealth;
    
    public bool UseHealthSystem { get; private set; }
    
    public double NoteScoreMultiplier { get; private set; } = 1.0;

    [AvailableOnComplete] public Dictionary<NoteGrade, int> GradeCounts => OnCompleteGuard(gradeCounts);

    [AvailableOnComplete]
    public int EarlyCount => OnCompleteGuard(
        Judgements.Values.Count(it => it.Grade != NoteGrade.Perfect && it.Grade != NoteGrade.Miss && it.Error < 0)
    );

    [AvailableOnComplete]
    public int LateCount => OnCompleteGuard(
        Judgements.Values.Count(it => it.Grade != NoteGrade.Perfect && it.Grade != NoteGrade.Miss && it.Error > 0)
    );

    [AvailableOnComplete]
    public double AverageTimingError => OnCompleteGuard(
        Judgements.Values.Sum(it => it.Error) / Judgements.Count
    );

    [AvailableOnComplete]
    public double StandardTimingError
    {
        get
        {
            OnCompleteGuard();
            var difference = 0.0;
            Judgements.Values.ForEach(it => difference += Math.Pow(AverageTimingError - it.Error, 2));
            return Math.Sqrt(difference / Judgements.Count);
        }
    }

    private bool isFullScorePossible = true;
    private readonly Dictionary<NoteGrade, int> gradeCounts = new Dictionary<NoteGrade, int>();
    private readonly double noteScoreMultiplierFactor;
    private double accumulatedAccuracy;

    private static readonly HashSet<Mod> AllowedTierMods = new HashSet<Mod>
    {
        Mod.Fast, Mod.Slow, Mod.HideScanline, Mod.HideNotes
    };
    private static readonly HashSet<Mod> DisallowedCalibrationMods = new HashSet<Mod>
    {
        Mod.Auto, Mod.AutoDrag, Mod.AutoFlick, Mod.AutoHold
    };
    
    public GameState(Game game, GameMode mode, HashSet<Mod> mods)
    {
        Level = game.Level;
        Difficulty = game.Difficulty;
        DifficultyLevel = Level.Meta.GetDifficultyLevel(Difficulty.Id);
        Mode = mode;
        Mods = new HashSet<Mod>(mods);
        
        NoteCount = game.Chart.Model.note_list.Count;
        game.Chart.Model.note_list.ForEach(it => Judgements[it.id] = new NoteJudgement());
        noteScoreMultiplierFactor = Math.Sqrt(NoteCount) / 3.0;
        
        UseHealthSystem = Mods.Contains(Mod.Hard) || Mods.Contains(Mod.ExHard) || mode == GameMode.Tier;
        MaxHealth = DifficultyLevel * 75;
        if (MaxHealth < 0) MaxHealth = 1000;
        Health = MaxHealth;
        
        switch (mode)
        {
            case GameMode.Tier:
            {
                Context.TierState.Stages[Context.TierState.CurrentStageIndex] = this;
                // Keep allowed mods only
                Mods.IntersectWith(AllowedTierMods);
                if (Application.isEditor && game.EditorForceAutoMod) Mods.Add(Mod.Auto);
                // Use max health from meta
                MaxHealth = Context.TierState.Tier.Meta.maxHealth;
                Health = MaxHealth;
                break;
            }
            case GameMode.Calibration:
                // Remove auto mods
                Mods.ExceptWith(DisallowedCalibrationMods);
                if (Application.isEditor && game.EditorForceAutoMod) Mods.Add(Mod.Auto);
                break;
        }
    }

    public GameState()
    {
        IsCompleted = true;
        Mods = new HashSet<Mod>();
        Score = 1000000;
        Accuracy = 1.000000;
        MaxCombo = 1;
        gradeCounts = new Dictionary<NoteGrade, int>
        {
            {NoteGrade.Perfect, 1},
            {NoteGrade.Great, 0},
            {NoteGrade.Good, 0},
            {NoteGrade.Bad, 0},
            {NoteGrade.Miss, 0}
        };
        Level = MockData.CommunityLevel;
        Difficulty = Difficulty.Parse(Level.Meta.charts[0].type);
    }
    
    public GameState(GameMode mode, Level level, Difficulty difficulty) : this()
    {
        Mode = mode;
        Level = level;
        Difficulty = difficulty;
    }

    public void FillTestData(int noteCount)
    {
        if (!Application.isEditor) throw new Exception();

        ClearCount = noteCount;
        for (var i = 0; i < noteCount; i++) Judgements[i] = new NoteJudgement
        {
            IsJudged = true,
            Grade = NoteGrade.Perfect,
            Error = 0,
        };
        Combo = MaxCombo = noteCount;
        Score = 1000000;
        Accuracy = 1.000000;
    }

    public void Judge(Note note, NoteGrade grade, double error, double greatGradeWeight)
    {
        if (IsCompleted || IsFailed)
        {
            return;
        }
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

        if (Mode == GameMode.Practice)
        {
            if (grade != NoteGrade.Perfect && grade != NoteGrade.Great) isFullScorePossible = false;
        }
        else
        {
            if (grade != NoteGrade.Perfect) isFullScorePossible = false;
        }

        // Combo
        var miss = grade == NoteGrade.Bad || grade == NoteGrade.Miss;
        
        if (miss) Combo = 0; else Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;

        if (Mode == GameMode.Tier)
        {
            if (miss) Context.TierState.Combo = 0; else Context.TierState.Combo++;
            if (Context.TierState.Combo > Context.TierState.MaxCombo) Context.TierState.MaxCombo = Context.TierState.Combo;
        }

        // Score multiplier
        if (Mode != GameMode.Practice)
        {
            switch (grade)
            {
                case NoteGrade.Perfect:
                    NoteScoreMultiplier += 0.004D * noteScoreMultiplierFactor;
                    break;
                case NoteGrade.Great:
                    NoteScoreMultiplier += 0.002D * noteScoreMultiplierFactor;
                    break;
                case NoteGrade.Good:
                    NoteScoreMultiplier += 0.001D * noteScoreMultiplierFactor;
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
        if (Mode == GameMode.Practice)
        {
            Score += 900000.0 / NoteCount * grade.GetScoreWeight(false) +
                     100000.0 / (NoteCount * (NoteCount + 1) / 2.0) * Combo;
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
            Score += noteScore;
        }
        if (Score > 999500)
        {
            if (ClearCount == NoteCount && isFullScorePossible)
            {
                Score = 1000000;
            }
        }
        if (Score > 1000000) Score = 1000000;
        if (Score == 1000000 && !isFullScorePossible) Score = 999999; // In case of double inaccuracy

        // Accuracy
        if (Mode == GameMode.Practice || grade != NoteGrade.Great)
        {
            accumulatedAccuracy += 1.0 * grade.GetAccuracyWeight();
        }
        else
        {
            accumulatedAccuracy += 1.0 * (NoteGrade.Great.GetAccuracyWeight() +
                                           (NoteGrade.Perfect.GetAccuracyWeight() -
                                            NoteGrade.Great.GetAccuracyWeight()) *
                                           greatGradeWeight);
        }
        Accuracy = accumulatedAccuracy / ClearCount;

        // Health mods
        if (UseHealthSystem)
        {
            var mods = Mods.Contains(Mod.ExHard) ? exHardHpMods : hardHpMods;
            if (Mode == GameMode.Tier) mods = tierHpMods;

            var mod = mods
                .Select[note.Type]
                .Select[Mode == GameMode.Practice ? unrankedGradingIndex[grade] : rankedGradingIndex[grade]];

            double change = 0;
            
            switch (mod.Type)
            {
                case HpModType.Absolute:
                    change = mod.Value;
                    break;
                case HpModType.Percentage:
                    change = mod.Value / 100f * MaxHealth;
                    break;
                case HpModType.DivideByNoteCount:
                    change = mod.Value / NoteCount / 100f * MaxHealth;
                    break;
            }

            if (change < 0 && mod.UseHealthBuffer)
            {
                double a;
                if (HealthPercentage > 0.3) a = 1;
                else a = 0.25 + 2.5 * HealthPercentage;
                change *= a;
            }

            Health += change;
            Health = Math.Min(Math.Max(Health, 0), MaxHealth);
            if (Health <= 0) ShouldFail = true;

            if (Mode == GameMode.Tier)
            {
                Context.TierState.Health = Health;
            }
        }

        if (
            Mods.Contains(Mod.AP) && grade != NoteGrade.Perfect
            ||
            Mods.Contains(Mod.FC) && (grade == NoteGrade.Bad || grade == NoteGrade.Miss)
        )
        {
            ShouldFail = true;
        }
    }

    public bool IsJudged(int noteId) => Judgements[noteId].IsJudged;

    public NoteJudgement GetJudgement(int noteId) => Judgements[noteId];

    public void OnComplete()
    {
        foreach (NoteGrade grade in Enum.GetValues(typeof(NoteGrade)))
        {
            if (grade == NoteGrade.None) continue;
            gradeCounts[grade] = Judgements.Count(it => it.Value.Grade == grade);
        }
    }

    public void OnFail()
    {
        if (Mode == GameMode.Tier)
        {
            Context.TierState.IsFailed = true;
        }
    }

    private void OnCompleteGuard()
    {
        if (!IsCompleted) throw new InvalidOperationException();
    }
    
    private T OnCompleteGuard<T>(T target)
    {
        if (!IsCompleted) throw new InvalidOperationException();
        return target;
    }

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
                new HpMod(0.5, HpModType.Absolute),
                new HpMod(-1, HpModType.Percentage),
                new HpMod(-3, HpModType.Percentage),
                new HpMod(-6, HpModType.Percentage),
                new HpMod(-8, HpModType.Percentage)
            })
        },
        {
            NoteType.Hold, new NoteHpMod(new List<HpMod>
            {
                new HpMod(0.5, HpModType.Absolute),
                new HpMod(0.25, HpModType.Absolute),
                new HpMod(-1.5, HpModType.Percentage),
                new HpMod(-4, HpModType.Percentage),
                new HpMod(-9, HpModType.Percentage),
                new HpMod(-12, HpModType.Percentage)
            })
        },
        {
            NoteType.LongHold, new NoteHpMod(new List<HpMod>
            {
                new HpMod(0.5, HpModType.Absolute),
                new HpMod(0.25, HpModType.Absolute),
                new HpMod(-1.5, HpModType.Percentage),
                new HpMod(-4, HpModType.Percentage),
                new HpMod(-9, HpModType.Percentage),
                new HpMod(-12, HpModType.Percentage)
            })
        },
        {
            NoteType.DragHead, new NoteHpMod(new List<HpMod>
            {
                new HpMod(0.2, HpModType.Absolute),
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
                new HpMod(0.1, HpModType.Absolute),
                new HpMod(0, HpModType.Absolute),
                new HpMod(0, HpModType.Absolute),
                new HpMod(0, HpModType.Absolute),
                new HpMod(0, HpModType.Absolute),
                new HpMod(-2.4, HpModType.Percentage)
            })
        },
        {
            NoteType.Flick, new NoteHpMod(new List<HpMod>
            {
                new HpMod(1, HpModType.Absolute),
                new HpMod(0.5, HpModType.Absolute),
                new HpMod(-0.75, HpModType.Percentage),
                new HpMod(-2.25, HpModType.Percentage),
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
                new HpMod(0.5, HpModType.Absolute),
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
                new HpMod(0.5, HpModType.Absolute),
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
                new HpMod(0.2, HpModType.Absolute),
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
                new HpMod(0.1, HpModType.Absolute),
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
    
    private static ModeHpMod tierHpMods = new ModeHpMod(new Dictionary<NoteType, NoteHpMod>
    {
        {
            NoteType.Click, new NoteHpMod(new List<HpMod>
            {
                new HpMod(1, HpModType.Absolute),
                new HpMod(0.25, HpModType.Absolute),
                new HpMod(-2, HpModType.Percentage, true),
                new HpMod(-4, HpModType.Percentage, true),
                new HpMod(-7, HpModType.Percentage, true),
                new HpMod(-10, HpModType.Percentage, true)
            })
        },
        {
            NoteType.Hold, new NoteHpMod(new List<HpMod>
            {
                new HpMod(0.5, HpModType.Absolute),
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
                new HpMod(0.5, HpModType.Absolute),
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
                new HpMod(0.2, HpModType.Absolute),
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
                new HpMod(0.1, HpModType.Absolute),
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
    public double Error;
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
    public double Value;
    public HpModType Type;
    public bool UseHealthBuffer;
    
    public HpMod(double value, HpModType type, bool useHealthBuffer = false)
    {
        Value = value;
        Type = type;
        UseHealthBuffer = useHealthBuffer;
    }
}

public enum HpModType
{
    Absolute,
    Percentage,
    DivideByNoteCount
}

public enum GameMode
{
    Unspecified = 0,
    Standard = 1,
    Practice = 2,
    Calibration = 3,
    Tier = 4,
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]  
public class AvailableOnComplete : Attribute
{
}