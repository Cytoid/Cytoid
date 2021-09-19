using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RewardOverlay : SingletonMonoBehavior<RewardOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public InteractableMonoBehavior detectionArea;
    public Text topMessage;
    public CharacterDisplay characterDisplay;
    public Text bottomMessage;
    public LevelCard levelCard;
    public BadgeDisplay badgeDisplay;

    private void Start()
    {
        canvas.enabled = false;
        canvasGroup.enabled = false;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.RewardOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        topMessage.text = "";
        bottomMessage.text = "";
        badgeDisplay.gameObject.SetActive(false);
    }

    private async UniTask Enter()
    {
        canvas.enabled = true;
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.RewardOverlay;
        canvasGroup.enabled = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.DOKill();
        canvasGroup.DOFade(1, 0.4f).SetEase(Ease.OutCubic);
        Context.SetMajorCanvasBlockRaycasts(false);
        await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
    }
    
    private async UniTask Leave()
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.DOKill();
        canvasGroup.DOFade(0, 0.4f).SetEase(Ease.OutCubic);
        Context.SetMajorCanvasBlockRaycasts(true);
        await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
        canvas.enabled = false;
        canvasGroup.enabled = false;
    }

    public static void Show(List<Reward> rewards)
    {
        rewards = rewards.OrderBy(it => it.Type).ToList();
        Instance.Apply(async it =>
        {
            var entered = false;
            foreach (var reward in rewards)
            {
                if (reward.Type == Reward.RewardType.Badge)
                {
                    var badge = reward.badgeValue.Value;
                    if (badge.type != BadgeType.Event) continue; // Only support event badges at the moment
                }
                
                switch (reward.Type)
                {
                    case Reward.RewardType.Level:
                        it.topMessage.text = "REWARD_LEVEL_ADDED_TO_LIBRARY".Get();
                        it.levelCard.gameObject.SetActive(true);
                        it.levelCard.SetModel(reward.onlineLevelValue.Value.ToLevel(LevelType.User));
                        break;
                    case Reward.RewardType.Character:
                        it.topMessage.text = "REWARD_CHARACTER_UNLOCKED".Get();
                        it.bottomMessage.text = reward.characterValue.Value.Name;
                        it.characterDisplay.gameObject.SetActive(true);
                        it.characterDisplay.Load(CharacterAsset.GetTachieBundleId(reward.characterValue.Value.AssetId));
                        break;
                    case Reward.RewardType.Badge:
                        it.topMessage.text = "REWARD_BADGE_UNLOCKED".Get();
                        it.bottomMessage.text = reward.badgeValue.Value.title;
                        it.badgeDisplay.gameObject.SetActive(true);
                        it.badgeDisplay.SetModel(reward.badgeValue.Value);
                        break;
                }
                Context.AudioManager.Get("Unlock").Play();

                if (!entered)
                {
                    await it.Enter();
                    entered = true;
                }

                var elements = new List<TransitionElement>{it.topMessage.GetComponent<TransitionElement>()};
                switch (reward.Type)
                {
                    case Reward.RewardType.Level:
                        elements.Add(it.levelCard.GetComponent<TransitionElement>());
                        break;
                    case Reward.RewardType.Character:
                        elements.Add(it.characterDisplay.GetComponent<TransitionElement>());
                        elements.Add(it.bottomMessage.GetComponent<TransitionElement>());
                        break;
                    case Reward.RewardType.Badge:
                        elements.Add(it.badgeDisplay.GetComponent<TransitionElement>());
                        elements.Add(it.bottomMessage.GetComponent<TransitionElement>());
                        break;
                }
                elements.ForEach(x => x.Enter());
                await UniTask.Delay(TimeSpan.FromSeconds(2f));

                var clicked = false;
                it.detectionArea.onPointerClick.AddListener(_ =>
                {
                    clicked = true;
                });

                await UniTask.WaitUntil(() => clicked);
                
                it.detectionArea.onPointerClick.RemoveAllListeners();
                
                elements.ForEach(x => x.Leave());
                await UniTask.Delay(TimeSpan.FromSeconds(0.4f));
                
                switch (reward.Type)
                {
                    case Reward.RewardType.Level:
                        it.levelCard.Unload();
                        break;
                    case Reward.RewardType.Character:
                        it.characterDisplay.Unload();
                        break;
                    case Reward.RewardType.Badge:
                        it.badgeDisplay.Clear();
                        it.badgeDisplay.gameObject.SetActive(false);
                        break;
                }
                
                it.topMessage.text = "";
                it.bottomMessage.text = "";
            }
            await it.Leave();
        });
    }
    
}