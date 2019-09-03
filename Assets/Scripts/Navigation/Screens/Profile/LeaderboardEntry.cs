using System;
using DG.Tweening;
using Proyecto26;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardEntry : ContainerEntry<Leaderboard.Entry>
{
    public Avatar avatar;
    public Text rank;
    public new Text name;
    public Text rating;

    public override void SetModel(Leaderboard.Entry entry)
    {
        avatar.SetModel(entry.owner);
        rank.text = entry.rank + ".";
        name.text = entry.owner.uid;
        rating.text = "Rating " + entry.rating.ToString("N2");
    }

}