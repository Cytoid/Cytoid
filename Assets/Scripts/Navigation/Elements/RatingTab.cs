using System;
using Proyecto26;
using RSG;
using UnityEngine;
using UnityEngine.UI;

public class RatingTab : MonoBehaviour, ScreenInitializedListener
{
    public GameObject icon;
    public SpinnerElement spinner;
    
    public Text ratingText;
    public RateLevelElement rateLevelElement;

    private string levelId;
    
    public void OnScreenInitialized()
    {
        ratingText.text = "";
    }

    private string FormatRatingText(int rating)
    {
        if (rating > 10000)
        {
            return (rating / 1000f).ToString("0.0") + "k+";
        }
        if (rating > 0)
        {
            return rating.ToString("0");
        }
        else
        {
            return "N/A";
        }
    }

    private void OnLevelRatingUpdated(string id, LevelRating data)
    {
        if (levelId != id) return;
        ratingText.text = FormatRatingText(data.like);

        rateLevelElement.SetModel(id, data);
        rateLevelElement.rateButton.onPointerClick.SetListener(_ =>
        {
            var dialog = RateLevelDialog.Instantiate(id, data.rating ?? -1);
            dialog.onLevelRated.AddListener(rating =>
            {
                print("Updated with " + rating.rating);
                OnLevelRatingUpdated(id, rating);
            });
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
                Headers = Context.OnlinePlayer.GetRequestHeaders(),
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
