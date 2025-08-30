using UnityEngine;
using UnityEngine.UI;

public class RateLevelElement : MonoBehaviour
{
    public Text numMainRatingsText;
    public Text numMinorRatingsText;
    public Text messageText;
    public InteractableMonoBehavior rateButton;

    private void Awake()
    {
        numMainRatingsText.text = "N/A";
        numMinorRatingsText.text = "";
        messageText.text = "";
        rateButton.gameObject.SetActive(false);
    }

    public void SetModel(string levelId, LevelRating data)
    {
        numMainRatingsText.text = data.like > 0 ? data.like.ToString("0") : "N/A";

        if (data.dislike > 0)
        {
            numMinorRatingsText.text = (data.dislike > 1 ? "GAME_PREP_RATINGS_RECV_DISLIKES" : "GAME_PREP_RATINGS_RECV_DISLIKE").Get(data.dislike);
        }
        else if (data.total > 0)
        {
            numMinorRatingsText.text = $"{data.total} " + (data.total > 1 ? "GAME_PREP_RATINGS_UNIT_RATINGS" : "GAME_PREP_RATINGS_UNIT_RATING").Get();
        }
        else
        {
            numMinorRatingsText.text = "0 " + "GAME_PREP_RATINGS_UNIT_RATINGS".Get();
        }

        if (Context.OnlinePlayer.IsAuthenticated && Context.LevelManager.LoadedLocalLevels.ContainsKey(levelId))
        {
            if (data.rating > 0)
            {
                messageText.text = "GAME_PREP_RATINGS_YOU_RATED_X_WITH_TYPE".Get(
                    $"{data.rating / 2.0:0.#}",
                    data.rating > 5
                        ? "GAME_PREP_RATINGS_LIKE".Get()
                        : "GAME_PREP_RATINGS_DISLIKE".Get()
                );
            }
            else
            {
                messageText.text = data.total > 0
                    ? "GAME_PREP_RATINGS_YOU_HAVENT_RATED".Get()
                    : "GAME_PREP_RATINGS_BE_THE_FIRST".Get();
            }

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
