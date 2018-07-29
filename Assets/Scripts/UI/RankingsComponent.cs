using System.Collections;
using DoozyUI;
using QuickEngine.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class RankingsComponent : MonoBehaviour
    {
        public Transform RootTransform;
       
        public Button Button;
        public Text MessageText;
        public Button ViewMoreButton;
        public Transform EntryHolder;
        public GameObject EntryPrefab;
        public GameObject Wheel;
        public Text PlayerRankText;
        public RectTransform ButtonTrueWidthTransform;
        
        private new bool enabled;
        private bool reloadingRankings;
        private RectTransform rootRectTransform;
        private Coroutine reloadCoroutine;
        private bool failed;

        private void Awake()
        {
            EventKit.Subscribe("reload rankings", ReloadRankings);
            Button.onClick.AddListener(OnButtonPressed);

            enabled = OnlinePlayer.Authenticated && PlayerPrefsExt.GetBool("ranked");

            if (!enabled)
            {
                RootTransform.gameObject.SetActive(false);
            }

            rootRectTransform = RootTransform.GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            EventKit.Unsubscribe("reload rankings", ReloadRankings);
        }
        
        private void Update()
        {
            if (!enabled && PlayerPrefsExt.GetBool("ranked") && OnlinePlayer.Authenticated)
            {
                enabled = true;
                RootTransform.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            } 
            else if (enabled && (!PlayerPrefsExt.GetBool("ranked") || !OnlinePlayer.Authenticated))
            {
                enabled = false;
                RootTransform.gameObject.SetActive(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            }
            Wheel.transform.eulerAngles = new Vector3(0, 0, Wheel.transform.eulerAngles.z - 135 * Time.deltaTime);

            rootRectTransform.SetWidth(ButtonTrueWidthTransform.GetWidth());

            LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
        }

        private void OnButtonPressed()
        {
            var uiElement = UIManager.GetUiElements("RankingsWindow")[0];
            if (uiElement.isVisible)
            {
                uiElement.Hide(false);
            }
            else
            {
                uiElement.Show(false);
                var elements = UIManager.GetUiElements("RateWindow");
                if (elements.Count > 0) elements[0].Hide(false);
                
                if (failed) ReloadRankings();
            }
        }

        private void ReloadRankings()
        {
            if (!OnlinePlayer.Authenticated || !PlayerPrefsExt.GetBool("ranked")) return;

            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
            reloadCoroutine = StartCoroutine(ReloadRankingsCoroutine());
        }

        private IEnumerator ReloadRankingsCoroutine()
        {
            reloadingRankings = true;

            Wheel.SetActive(true);
            PlayerRankText.gameObject.SetActive(false);
            
            foreach (var entry in EntryHolder.GetComponentsInChildren<RankingEntryComponent>())
            {
                entry.Destroy();
            }

            EntryHolder.gameObject.SetActive(false);
            MessageText.gameObject.SetActive(true);
            MessageText.text = "Loading rankings...";
            ViewMoreButton.gameObject.SetActive(false);

            if (SceneManager.GetActiveScene().name == "GameResult")
            {
                while (GameResultController.Instance.IsUploading)
                {
                    yield return null;
                }
            }
            
            yield return OnlinePlayer.QueryRankings(CytoidApplication.CurrentLevel.id,
                CytoidApplication.CurrentChartType);

            if (!PlayerPrefsExt.GetBool("ranked"))
            {
                yield break;
            }

            var result = OnlinePlayer.LastRankingQueryResult;

            if (result.status == -1)
            {
                MessageText.text = "Could not fetch rankings.";
                PlayerRankText.text = "N/A";
                Wheel.SetActive(false);
                PlayerRankText.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                failed = true;
                yield break;
            }

            if (result.rankings.Length == 0)
            {
                MessageText.text = "No ranked plays yet.\nBe the first!";
                PlayerRankText.text = "N/A";
                Wheel.SetActive(false);
                PlayerRankText.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                failed = true;
                yield break;
            } 
            
            Wheel.SetActive(false);
            PlayerRankText.text = result.player_rank == -1 ? "N/A" : "#" + result.player_rank;
            PlayerRankText.gameObject.SetActive(true);
            EntryHolder.gameObject.SetActive(true);

            foreach (var ranking in result.rankings)
            {
                var entry = Instantiate(EntryPrefab, EntryHolder).GetComponent<RankingEntryComponent>();

                entry.Ranking = ranking;
                entry.Load();
            }
           
            MessageText.gameObject.SetActive(false);
            ViewMoreButton.gameObject.SetActive(true);
            ViewMoreButton.transform.SetAsLastSibling();

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            reloadingRankings = false;
        }
    }
}