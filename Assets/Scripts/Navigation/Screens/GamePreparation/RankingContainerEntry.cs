using UnityEngine.UI;

public class RankingContainerEntry : ContainerEntry<RankingEntry>
{
    public Avatar avatar;
    public Text rank;
    public new Text name;
    public PerformanceWidget performance;
    
    public RankingEntry Model { get; protected set; }
    
    public override void SetModel(RankingEntry entry)
    {
        Model = entry;
        avatar.SetModel(entry.owner);
        rank.text = "#" + entry.rank;
        name.text = entry.owner.uid;
        performance.SetModel(entry);
    }

    public override RankingEntry GetModel() => Model;
}