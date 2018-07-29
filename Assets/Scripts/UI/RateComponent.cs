using System.Collections;
using System.Linq;
using DoozyUI;
using QuickEngine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class RateComponent : MonoBehaviour
    {
        public Transform RootTransform;
       
        public Button Button;
        public Text MessageText;
        public StarRatingComponent Stars;
        public Button RateButton;
        public GameObject Wheel;
        public Text LevelRatingText;
        public RectTransform ButtonTrueWidthTransform;
        
        private new bool enabled;
        private RectTransform rootRectTransform;
        private Coroutine submitCoroutine;
        private bool failed;

        private void Awake()
        {
            Button.onClick.AddListener(OnButtonPressed);
            RateButton.onClick.AddListener(OnRateButtonPressed);

            enabled = OnlinePlayer.Authenticated;

            if (!enabled)
            {
                RootTransform.gameObject.SetActive(false);
            }

            rootRectTransform = RootTransform.GetComponent<RectTransform>();

            LoadRating(false);
        }
        
        private void Update()
        {
            Wheel.transform.eulerAngles = new Vector3(0, 0, Wheel.transform.eulerAngles.z - 135 * Time.deltaTime);
            rootRectTransform.SetWidth(ButtonTrueWidthTransform.GetWidth());
        }

        private void OnButtonPressed()
        {
            var uiElement = UIManager.GetUiElements("RateWindow")[0];
            if (uiElement.isVisible)
            {
                uiElement.Hide(false);
            }
            else
            {
                uiElement.Show(false);
                var elements = UIManager.GetUiElements("RankingsWindow");
                if (elements.Count > 0) elements[0].Hide(false);

                if (failed) LoadRating(false);
            }
        }
        
        private void OnRateButtonPressed()
        {
            LoadRating(true);
        }

        private void LoadRating(bool submit)
        {
            if (!OnlinePlayer.Authenticated) return;

            if (submitCoroutine != null) StopCoroutine(submitCoroutine);
            submitCoroutine = StartCoroutine(LoadRatingCoroutine(submit));
        }

        private IEnumerator LoadRatingCoroutine(bool submit)
        {
            yield return null;
            
            Wheel.SetActive(true);
            LevelRatingText.gameObject.SetActive(false);
            
            MessageText.gameObject.SetActive(true);
            MessageText.text = submit ? "Updating level rating..." : "Loading level rating...";
            Stars.gameObject.SetActive(false);
            RateButton.gameObject.GetComponent<UIButton>().enabled = false;
            RateButton.gameObject.SetActive(false);

            yield return OnlinePlayer.Rate(new RateData
            {
                user = OnlinePlayer.Name,
                password = OnlinePlayer.Password,
                level = CytoidApplication.CurrentLevel.id,
                player_rating = submit ? Stars.Rating : -1
            });

            var result = OnlinePlayer.LastRateResult;

            if (result.status == -1)
            {
                MessageText.text = "Could not fetch level ratings.";
                LevelRatingText.text = "N/A";
                Wheel.SetActive(false);
                LevelRatingText.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                failed = true;
                yield break;
            }

            Wheel.SetActive(false);
            LevelRatingText.gameObject.SetActive(true);
            
            if (result.total == 0)
            {
                LevelRatingText.text = "N/A";
            }
            else
            {
                LevelRatingText.text = string.Format("{0:0.00} ({1})", result.average_rating, result.total);
            }
           
            Stars.gameObject.SetActive(true);

            if (result.player_rating != -1)
            {
                Stars.Rating = result.player_rating;
                MessageText.text = string.Format("You rated {0:0.#}/5.", Mathf.FloorToInt(result.player_rating * 2) / 2f);
            }
            else
            {
                if (result.total == 0)
                {
                    MessageText.text = "Not rated yet.\nBe the first!";
                }
                else
                {
                    MessageText.text = "Rate this level!";
                }
                
                if (ScoreGrades.From((float) CytoidApplication.CurrentPlay.Score) >= ScoreGrade.A)
                {
                    UIManager.GetUiElements("RateWindow")[0].Show(false);
                }
            }
           
            RateButton.gameObject.SetActive(true);
            RateButton.gameObject.GetComponent<UIButton>().enabled = true;
            RateButton.transform.SetAsLastSibling();

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}