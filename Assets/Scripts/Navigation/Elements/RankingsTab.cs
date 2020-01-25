using System;
using System.Collections.Generic;
using System.Linq;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.UI;

public class RankingsTab : MonoBehaviour, ScreenInitializedListener, ScreenBecameActiveListener
{
    public GameObject icon;
    public SpinnerElement spinner;
    
    public Text rankingText;
    public RankingContainer rankingContainer;
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
        if (!Context.LocalPlayer.PlayRanked && this.GetScreenParent() is ResultScreen)
        {
            icon.SetActive(false);
        }
    }
    
    private long updateRankingToken;
    
    public IPromise<System.Tuple<int, List<RankingEntry>>> UpdateRankings(string levelId, string chartType)
    {
        viewMoreButton.gameObject.SetActive(false);
        rankingText.text = "";
        rankingContainer.Clear();
        spinner.IsSpinning = true;
        rankingContainerStatusText.text = "Downloading level rankings...";
        updateRankingToken = DateTime.Now.ToFileTimeUtc();
        var token = updateRankingToken;
        return Context.OnlinePlayer.GetLevelRankings(levelId, chartType)
            .Then(ret =>
            {
                if (token != updateRankingToken) return null;
                var (rank, entries) = ret;
                rankingContainer.SetData(entries);
                if (rank > 0)
                {
                    if (rank > 99) rankingText.text = "#99+";
                    else rankingText.text = "#" + rank;
                }
                else rankingText.text = "N/A";

                rankingContainerStatusText.text = "";
                if (entries.Count == 0)
                {
                    rankingContainerStatusText.text = "No performances yet. Be the first!";
                }
                
                if (entries.Count > 0)
                {
                    viewMoreButton.gameObject.SetActive(true);
                    viewMoreButton.onPointerClick.RemoveAllListeners();
                    viewMoreButton.onPointerClick.AddListener(_ =>
                    {
                        Application.OpenURL(
                            $"https://cytoid.io/levels/{levelId}"); // TODO: Jump to selected difficulty?
                    });
                }

                return ret;
            })
            .Catch(error =>
            {
                if (token != updateRankingToken) return null;
                if (error is RequestException reqError)
                {
                    if (reqError.StatusCode != 404)
                    {
                        Debug.LogError(error);
                    }
                }
                else
                {
                    Debug.LogError(error);
                }
                rankingText.text = "N/A";
                rankingContainerStatusText.text = "Could not download level rankings.";
                throw error;
            })
            .Finally(() =>
            {
                if (token != updateRankingToken) return;
                spinner.IsSpinning = false;
            });
    }
    
}