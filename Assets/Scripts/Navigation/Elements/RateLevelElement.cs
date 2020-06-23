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
            averageRatingText.text = ((data.average ?? 0) / 2.0).ToString("0.00");
            numRatingsText.text = $"{data.total} " + (data.total > 1 ? "GAME_PREP_RATINGS_UNIT_RATINGS" : "GAME_PREP_RATINGS_UNIT_RATING").Get();
        }
        else
        {
            averageRatingText.text = "N/A";
            numRatingsText.text = "0 " + "GAME_PREP_RATINGS_UNIT_RATINGS".Get();
        }

        if (Context.OnlinePlayer.IsAuthenticated && Context.LevelManager.LoadedLocalLevels.ContainsKey(levelId))
        {
            messageText.text = data.rating > 0
                ? "GAME_PREP_RATINGS_YOU_RATED_X".Get($"{data.rating / 2.0:0.#}")
                : (data.total > 0 ? "GAME_PREP_RATINGS_YOU_HAVENT_RATED" : "GAME_PREP_RATINGS_BE_THE_FIRST").Get();
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