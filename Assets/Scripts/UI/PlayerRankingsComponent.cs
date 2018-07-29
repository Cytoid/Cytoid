using System;
using System.Collections;
using DG.Tweening;
using QuickEngine.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.UI
{
    public class PlayerRankingsComponent : MonoBehaviour
    {
        public enum PlayerRankingType
        {
            TopRating, PersonalRating
        }
        
        public GameObject LoadingTransform;
        public RectTransform ContentTransform;
        public ScrollRect ScrollRect;
        public Text MessageText;
        public GameObject Wheel;
       
        public Transform EntryHolder;
        public GameObject EntryPrefab;

        public Button SwitchRankingButton;
        public Text RankingName;

        private PlayerRankingType type = PlayerRankingType.TopRating;
        private new bool enabled;
        private Coroutine reloadCoroutine;

        private void Awake()
        {
            EventKit.Subscribe("reload player rankings", ReloadPlayerRankings);
            SwitchRankingButton.onClick.AddListener(OnSwitchButtonPressed);
        }

        private void OnDestroy()
        {
            EventKit.Unsubscribe("reload player rankings", ReloadPlayerRankings);
        }
        
        private void Update()
        {
            Wheel.transform.eulerAngles = new Vector3(0, 0, Wheel.transform.eulerAngles.z - 135 * Time.deltaTime);
        }

        private void OnSwitchButtonPressed()
        {
            type = type.Next();
            ReloadPlayerRankings();
        }

        private void ReloadPlayerRankings()
        {
            if (!OnlinePlayer.Authenticated) return;
            
            switch (type)
            {
                case PlayerRankingType.TopRating:
                    RankingName.text = "Top Rating";
                    break;
                case PlayerRankingType.PersonalRating:
                    RankingName.text = "My Rating";
                    break;
            }

            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
            reloadCoroutine = StartCoroutine(ReloadRankingsCoroutine());
        }

        private IEnumerator ReloadRankingsCoroutine()
        {
            LoadingTransform.SetActive(true);
            
            foreach (var entry in EntryHolder.GetComponentsInChildren<PlayerRankingEntryComponent>())
            {
                entry.Destroy();
            }

            EntryHolder.gameObject.SetActive(false);
            MessageText.gameObject.SetActive(true);
            Wheel.SetActive(true);
            MessageText.text = "Loading player rankings...";

            var query = "";
            switch (type)
            {
                case PlayerRankingType.TopRating:
                    query = "top_rating";
                    break;
                case PlayerRankingType.PersonalRating:
                    query = "personal_rating";
                    break;
            }
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

            yield return OnlinePlayer.QueryPlayerRankings(query);

            var result = OnlinePlayer.LastPlayerRankingQueryResult;

            if (result.status == -1)
            {
                MessageText.text = "Could not fetch player rankings.";
                Wheel.SetActive(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
                yield break;
            }
            
            Wheel.SetActive(false);
            EntryHolder.gameObject.SetActive(true);

            var toY = 0;

            foreach (var ranking in result.rankings)
            {
                var entry = Instantiate(EntryPrefab, EntryHolder).GetComponent<PlayerRankingEntryComponent>();

                entry.Ranking = ranking;
                entry.Load();

                if (entry.Ranking.player == OnlinePlayer.Name)
                {
                    toY = -50 + ranking.rank * 50;
                    print(toY);
                }
            }
           
            MessageText.gameObject.SetActive(false);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(ContentTransform.GetComponent<RectTransform>());

            ContentTransform.GetComponent<VerticalLayoutGroup>().enabled = false;

            yield return null;

            ContentTransform.GetComponent<VerticalLayoutGroup>().enabled = true;

            ScrollRect.enabled = false;

            while (Math.Abs(ContentTransform.anchoredPosition.y - toY) > 0.01f)
            {
                ContentTransform.DOAnchorPosY(toY, 0.5f);
                yield return null;
            }

            ScrollRect.enabled = true;
        }
    }
}