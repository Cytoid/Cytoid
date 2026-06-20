using System;

[Serializable]
public class TierPlayResult
{
    public string tierId;
    public int stageIndex;
    public double finalHealth;
    public double maxHealth;
    public int endingCombo;

    public static TierPlayResult FromSession(TierPlaySession session, GameState state)
    {
        return new TierPlayResult
        {
            tierId = session.TierId,
            stageIndex = session.StageIndex,
            finalHealth = state.Health,
            maxHealth = session.MaxHealth,
            endingCombo = session.Combo
        };
    }
}
