using System;

public sealed class TierPlaySession
{
    public string TierId { get; }
    public int StageIndex { get; }
    public int? StageCount { get; }
    public double MaxHealth { get; }
    public double InitialHealth { get; }
    public string IntroLabel { get; }

    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }

    public TierPlaySession(TierPlayLaunch launch)
    {
        launch.Validate();
        TierId = launch.tierId;
        StageIndex = launch.stageIndex;
        StageCount = launch.stageCount;
        MaxHealth = launch.maxHealth;
        InitialHealth = launch.ResolvedInitialHealth;
        IntroLabel = launch.introLabel;
        Combo = launch.ResolvedInitialCombo;
        MaxCombo = Combo;
    }

    public void OnMiss()
    {
        Combo = 0;
    }

    public void OnNonMissHit()
    {
        Combo++;
        if (Combo > MaxCombo)
        {
            MaxCombo = Combo;
        }
    }
}
