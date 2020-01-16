using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RateLevelElement : MonoBehaviour
{
    public Text averageRatingText;
    public Text numRatingsText;
    public Text messageText;
    public InteractableMonoBehavior rateButton;

    private void Awake()
    {
        averageRatingText.text = "N/A";
        messageText.text = "";
        numRatingsText.text = "";
        rateButton.gameObject.SetActive(false);
    }

    public void SetModel(string levelId, LevelRating data)
    {
        if (data.total > 0)
        {
            averageRatingText.text = (data.average / 2.0).ToString("0.00");
            numRatingsText.text = $"{data.total} rating" + (data.total > 1 ? "s" : "");
        }
        else
        {
            averageRatingText.text = "N/A";
            numRatingsText.text = "0 ratings";
        }

        if (Context.OnlinePlayer.IsAuthenticated && Context.LevelManager.LoadedLocalLevels.Any(it => it.Meta.id == levelId))
        {
            messageText.text = data.rating > 0
                ? $"You rated {data.rating / 2.0:0.#}/5."
                : (data.total > 0 ? "You haven't rated yet." : "Be the first!");
            rateButton.gameObject.SetActive(true);
        }
        else
        {
            messageText.text = "";
            rateButton.gameObject.SetActive(false);
        }
        LayoutFixer.Fix(transform);
    }
}