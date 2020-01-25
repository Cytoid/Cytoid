using System;
using UnityEngine;
using UnityEngine.UI;

public class RankingContainerEntry : ContainerEntry<RankingEntry>
{
    public GameObject background;
    public Avatar avatar;
    public Text rank;
    public new Text name;
    public PerformanceWidget performance;
    
    public RankingEntry Model { get; protected set; }

    private void Awake()
    {
        background.SetActive(false);
    }

    public override void SetModel(RankingEntry entry)
    {
        Model = entry;
        background.SetActive(Context.OnlinePlayer.IsAuthenticated && entry.owner.uid == Context.OnlinePlayer.GetUid());
        avatar.SetModel(entry.owner);
        rank.text = "#" + entry.rank;
        name.text = entry.owner.uid;
        performance.SetModel(entry);
    }

    public override RankingEntry GetModel() => Model;
}