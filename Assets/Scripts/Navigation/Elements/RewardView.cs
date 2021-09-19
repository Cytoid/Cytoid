using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardView : MonoBehaviour
{
    public Text rewardTypeText;
    public Text rewardDescriptionText;

    public void SetModel(Reward reward)
    {
        switch (reward.Type)
        {
            case Reward.RewardType.Character:
                rewardTypeText.text = "REWARD_CHARACTER".Get();
                rewardDescriptionText.text = reward.characterValue.Value.Name;
                break;
            case Reward.RewardType.Level:
                rewardTypeText.text = "REWARD_LEVEL".Get();
                rewardDescriptionText.text = reward.onlineLevelValue.Value.Title;
                break;
            case Reward.RewardType.Badge:
                rewardTypeText.text = "REWARD_BADGE".Get();
                rewardDescriptionText.text = reward.badgeValue.Value.title;
                break;
            default:
                rewardTypeText.text = "REWARD";
                rewardDescriptionText.text = "Unknown";
                break;
        }
    }
}