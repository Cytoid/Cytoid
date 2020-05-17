using System;
using UnityEngine;
using UnityEngine.UI;

public class TierRankingContainerEntry : ContainerEntry<TierRankingEntry>
{
    public GameObject background;
    public Avatar avatar;
    public Text rank;
    public new Text name;
    public TierPerformanceWidget performance;
    
    public TierRankingEntry Model { get; protected set; }

    private void Awake()
    {
        background.SetActive(false);
    }

    public override void SetModel(TierRankingEntry entry)
    {
        Model = entry;
        background.SetActive(Context.OnlinePlayer.IsAuthenticated && entry.owner.Uid == Context.Player.Id);
        avatar.SetModel(entry.owner);
        rank.text = "#" + entry.rank;
        name.text = entry.owner.Uid;
        performance.SetModel(entry);
    }

    public override TierRankingEntry GetModel() => Model;
}