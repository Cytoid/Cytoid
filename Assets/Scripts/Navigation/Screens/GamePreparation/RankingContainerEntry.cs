using UnityEngine.UI;

public class RankingContainerEntry : ContainerEntry<RankingEntry>
{
    public Avatar avatar;
    public Text rank;
    public new Text name;
    public PerformanceWidget performance;
    
    public override void SetModel(RankingEntry entry)
    {
        avatar.SetModel(entry.owner);
        rank.text = entry.rank + ".";
        name.text = entry.owner.uid;
        performance.SetModel(entry);
    }
}