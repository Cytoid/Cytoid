using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

public class RewardOverlay : SingletonMonoBehavior<RewardOverlay>
{
    [GetComponent] public Canvas canvas;
    [GetComponent] public CanvasGroup canvasGroup;
    public InteractableMonoBehavior detectionArea;
    public Text message;
    public CharacterDisplay characterDisplay;
    public Text characterName;
    public LevelCard levelCard;

    private void Start()
    {
        canvas.overrideSorting = true;
        canvas.sortingOrder = NavigationSortingOrder.RewardOverlay;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        message.text = "";
        characterName.text = "";
    }

    private async UniTask Enter()
    {
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
    }

    public static void Show(List<OnlinePlayerStateChange.Reward> rewards)
    {
        Instance.Apply(async it =>
        {
            var entered = false;
            foreach (var reward in rewards)
            {
                switch (reward.Type)
                {
                    case OnlinePlayerStateChange.Reward.RewardType.Level:
                        it.message.text = "REWARD_LEVEL_ADDED_TO_LIBRARY".Get();
                        it.levelCard.gameObject.SetActive(true);
                        it.levelCard.SetModel(reward.onlineLevelValue.Value.ToLevel(LevelType.Official));
                        break;
                    case OnlinePlayerStateChange.Reward.RewardType.Character:
                        it.message.text = "REWARD_CHARACTER_UNLOCKED".Get();
                        it.characterName.gameObject.SetActive(true);
                        it.characterName.text = reward.characterValue.Value.Name;
                        it.characterDisplay.gameObject.SetActive(true);
                        it.characterDisplay.Load(reward.characterValue.Value.TachieAssetId);
                        break;
                }

                if (!entered)
                {
                    await it.Enter();
                    entered = true;
                }

                var elements = new List<TransitionElement>{it.message.GetComponent<TransitionElement>()};
                switch (reward.Type)
                {
                    case OnlinePlayerStateChange.Reward.RewardType.Level:
                        elements.Add(it.levelCard.GetComponent<TransitionElement>());
                        break;
                    case OnlinePlayerStateChange.Reward.RewardType.Character:
                        elements.Add(it.characterDisplay.GetComponent<TransitionElement>());
                        elements.Add(it.characterName.GetComponent<TransitionElement>());
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
                    case OnlinePlayerStateChange.Reward.RewardType.Level:
                        it.levelCard.Unload();
                        break;
                    case OnlinePlayerStateChange.Reward.RewardType.Character:
                        it.characterDisplay.Unload();
                        break;
                }
            }
            await it.Leave();
            it.message.text = "";
            it.characterName.text = "";
        });
    }
    
}