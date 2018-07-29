using DG.Tweening;
using DoozyUI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class ProfileTexts : SingletonMonoBehavior<ProfileTexts>
    {
        private static bool firstFaded;
        private bool loaded;

        private CanvasGroup canvasGroup;
        private Text nameText;
        private TextMeshProUGUI summaryText;

        private int lastLevel;
        private float lastRating;
        private int lastExp;

        protected override void Awake()
        {
            base.Awake();
            
            canvasGroup = GetComponent<CanvasGroup>();
            nameText = GetComponentInChildren<Text>();
            summaryText = GetComponentInChildren<TextMeshProUGUI>();
            EventKit.Subscribe("profile update", OnProfileUpdate);

            if (!OnlinePlayer.Authenticated)
            {
                canvasGroup.alpha = 0;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventKit.Unsubscribe("profile update", OnProfileUpdate);
        }

        private void Update()
        {
            if (!loaded && OnlinePlayer.Authenticated)
            {
                loaded = true;

                lastLevel = OnlinePlayer.Level;
                lastRating = OnlinePlayer.Rating;
                lastExp = OnlinePlayer.Exp;
                
                UpdateTexts();

                if (!firstFaded)
                {
                    firstFaded = true;
                    canvasGroup.DOFade(1, 0.5f).SetEase(Ease.InQuad);
                }
                else
                {
                    canvasGroup.alpha = 1;
                }
            } 
            else if (loaded && !OnlinePlayer.Authenticated)
            {
                loaded = false;
                
                canvasGroup.alpha = 0;
                firstFaded = false;
            }
            
            if (SceneManager.GetActiveScene().name == "LevelSelection") UpdateTexts();
        }

        public void UpdateTexts()
        {
            nameText.text = string.Format("<b>Lv.{0}</b> {1}", lastLevel, OnlinePlayer.Name);
            summaryText.text = string.Format("<b>Rating</b> {0:0.00}", lastRating);

            var uiElement = UIManager.GetUiElements("ProfileRoot", "MusicSelection");
            if (uiElement.Count > 0 && uiElement[0].isVisible)
            {
                summaryText.text += string.Format("  <b>Exp</b> {0}/{1}", OnlinePlayer.Exp, OnlinePlayer.NextExp);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(summaryText.GetComponentInParent<RectTransform>());
        }

        private void OnProfileUpdate()
        {
            string levelChange;
            if (OnlinePlayer.Level > lastLevel)
            {
                levelChange = string.Format("<color=#9BC53D>(+{0} Lv.)</color>", OnlinePlayer.Level - lastLevel);
            }
            else
            {
                levelChange = "(+" + (OnlinePlayer.Exp - lastExp) + " exp)";
            }

            nameText.text = string.Format("<b>Lv.{0}</b> {1} {2}", OnlinePlayer.Level, OnlinePlayer.Name,
                levelChange);
            
            if (Mathf.Abs(OnlinePlayer.Rating - lastRating) >= 0.01)
            {
                string ratingChange;
                if (OnlinePlayer.Rating > lastRating)
                {
                    ratingChange = string.Format("<color=#9BC53D>(+{0:0.00})</color>",
                        OnlinePlayer.Rating - lastRating);
                }
                else
                {
                    ratingChange = string.Format("<color=#E55934>(-{0:0.00})</color>",
                        lastRating - OnlinePlayer.Rating);
                }
                summaryText.text = string.Format("<b>Rating</b> {0:0.00} {1}", OnlinePlayer.Rating, ratingChange);
            }
            else
            {
                summaryText.text = string.Format("<b>Rating</b> {0:0.00}", OnlinePlayer.Rating);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(summaryText.GetComponentInParent<RectTransform>());
        }
    }
}