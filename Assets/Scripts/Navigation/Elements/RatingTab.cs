using System;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.UI;

public class RatingTab : MonoBehaviour
{
    public GameObject icon;
    public SpinnerElement spinner;
    
    public Text ratingText;
    public RateLevelElement rateLevelElement;

    private string levelId;
    
    private void OnServerInitialized()
    {
        ratingText.text = "";
    }
    
    private void OnLevelRatingUpdated(string id, LevelRating data)
    {
        if (levelId != id) return;
        if (data.total > 0)
        {
            ratingText.text = ((data.average ?? 0) / 2.0).ToString("0.00");
        }
        else
        {
            ratingText.text = "N/A";
        }

        rateLevelElement.SetModel(id, data);
        rateLevelElement.rateButton.onPointerClick.RemoveAllListeners();
        rateLevelElement.rateButton.onPointerClick.AddListener(_ =>
        {
            var dialog = RateLevelDialog.Instantiate(id, data.rating ?? -1);
            dialog.onLevelRated.AddListener(rating => OnLevelRatingUpdated(id, rating));
            dialog.Open();
        });
    }
    
    public IPromise<LevelRating> UpdateLevelRating(string id)
    {
        levelId = id;
        spinner.IsSpinning = true;
        return RestClient.Get<LevelRating>(new RequestHelper
            {
                Uri = $"{Context.ApiUrl}/levels/{id}/ratings",
                Headers = Context.OnlinePlayer.GetAuthorizationHeaders(),
                EnableDebug = true
            })
            .Then(it =>
            {
                OnLevelRatingUpdated(id, it);
                return it;
            })
            .Catch(error =>
            {
                Debug.Log(error);
                ratingText.text = "N/A";
                throw error;
            }).Finally(() => spinner.IsSpinning = false);
    }
    
}