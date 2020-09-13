using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.UI;

public class RankingsTab : MonoBehaviour, ScreenInitializedListener, ScreenBecameActiveListener, ScreenBecameInactiveListener
{
    public GameObject icon;
    public SpinnerElement spinner;
    
    public Text rankingText;
    public RankingContainer rankingContainer;
    public TierRankingContainer tierRankingContainer;
    
    public Text rankingContainerStatusText;
    public InteractableMonoBehavior viewMoreButton;

    public void OnScreenInitialized()
    {
        rankingText.text = "";
        rankingContainerStatusText.text = "";
        viewMoreButton.gameObject.SetActive(false);
    }

    public void OnScreenBecameActive()
    {
        if (!Context.Player.Settings.PlayRanked && this.GetScreenParent() is ResultScreen)
        {
            icon.SetActive(false);
        }
    }

    public void OnScreenBecameInactive()
    {
        updateRankingToken = DateTime.Now;
    }

    private DateTime updateRankingToken;
    
    public IPromise<(int, List<RankingEntry>)> UpdateRankings(string levelId, string chartType)
    {
        viewMoreButton.gameObject.SetActive(false);
        rankingText.text = "";
        spinner.IsSpinning = true;
        
        rankingContainer.Clear();
        rankingContainerStatusText.text = "GAME_PREP_RANKINGS_DOWNLOADING".Get();
        var token = updateRankingToken = DateTime.Now;
        return Context.OnlinePlayer.GetLevelRankings(levelId, chartType)
            .Then(ret =>
            {
                if (token != updateRankingToken) return (-1, null);
                var (rank, entries) = ret;
                SetRanking(rank);
                
                rankingContainer.SetData(entries);
                rankingContainerStatusText.text = "";
                if (entries.Count == 0)
                {
                    rankingContainerStatusText.text = "GAME_PREP_RANKINGS_BE_THE_FIRST".Get();
                }
                
                if (entries.Count > 0)
                {
                    viewMoreButton.gameObject.SetActive(true);
                }
                viewMoreButton.onPointerClick.SetListener(_ =>
                {
                    Application.OpenURL(
                        $"{Context.WebsiteUrl}/levels/{levelId}"); // TODO: Jump to selected difficulty?
                });
                return ret;
            })
            .CatchRequestError(error =>
            {
                if (token != updateRankingToken) return (-1, null);
                if (error.IsHttpError) {
                    if (error.StatusCode != 404)
                    {
                        throw error;
                    }
                }
                rankingText.text = "N/A";
                rankingContainerStatusText.text = "GAME_PREP_RANKINGS_COULD_NOT_DOWNLOAD".Get();
                return (-1, null);
            })
            .Finally(() =>
            {
                if (token != updateRankingToken) return;
                spinner.IsSpinning = false;
            });
    }

    public void SetRanking(int rank)
    {
        if (this == null) return;
        if (rank > 0)
        {
            if (rank > 99) rankingText.text = "#99+";
            else rankingText.text = "#" + rank;
        }
        else rankingText.text = "N/A";
    }

    private DateTime updateTierRankingToken;
    
    public IPromise<(int, List<TierRankingEntry>)> UpdateTierRankings(string tierId)
    {
        viewMoreButton.gameObject.SetActive(false);
        rankingText.text = "";
        spinner.IsSpinning = true;
        
        tierRankingContainer.Clear();
        rankingContainerStatusText.text = "TIER_RANKINGS_DOWNLOADING".Get();
        var token = updateTierRankingToken = DateTime.Now;
        
        return Context.OnlinePlayer.GetTierRankings(tierId)
            .Then(ret =>
            {
                if (token != updateTierRankingToken) return (-1, null);
                var (rank, entries) = ret;
                
                print($"Rank: {rank}");
                print($"Entries: {entries.ToList()}");
                tierRankingContainer.SetData(entries);
                SetRanking(rank);

                rankingContainerStatusText.text = "";
                if (entries.Count == 0)
                {
                    rankingContainerStatusText.text = "TIER_RANKINGS_BE_THE_FIRST".Get();
                }
                
                // TODO: Show view more button
                /*if (entries.Count > 0)
                {
                    viewMoreButton.gameObject.SetActive(true);
                    viewMoreButton.onPointerClick.SetListener(_ =>
                    {
                        Application.OpenURL(
                            $"https://cytoid.io/levels/{levelId}"); // TODO: Jump to selected difficulty?
                    });
                }*/

                return ret;
            })
            .CatchRequestError(error =>
            {
                if (token != updateTierRankingToken) return (-1, null);
                if (error.IsHttpError)
                {
                    if (error.StatusCode != 404)
                    {
                        throw error;
                    }
                }
                rankingText.text = "N/A";
                rankingContainerStatusText.text = "TIER_RANKINGS_COULD_NOT_DOWNLOAD".Get();
                return (-1, null);
            })
            .Finally(() =>
            {
                if (token != updateTierRankingToken) return;
                spinner.IsSpinning = false;
            });
    }

}