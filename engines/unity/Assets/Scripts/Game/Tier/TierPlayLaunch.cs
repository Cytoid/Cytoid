using System;

[Serializable]
public class TierPlayLaunch
{
    public string tierId;
    public int stageIndex;
    public int? stageCount;
    public double maxHealth;
    public double? initialHealth;
    public int? initialCombo;
    public string introLabel;

    public void Validate()
    {
        if (maxHealth <= 0)
        {
            throw new ArgumentException("tierPlay.maxHealth must be positive");
        }
    }

    public double ResolvedInitialHealth => initialHealth ?? maxHealth;

    public int ResolvedInitialCombo => initialCombo ?? 0;
}
