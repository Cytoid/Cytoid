using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class QuestView : MonoBehaviour
{
    public ObjectiveCardView objectiveViewPrefab;
    public RewardView rewardViewPrefab;
    public Text descriptionText;
    public GameObject objectiveRoot;
    public GameObject rewardRoot;
    
    public void SetModel(Quest quest)
    {
        descriptionText.text = quest.Description;
        foreach (Transform child in objectiveRoot.transform) Destroy(child.gameObject);
        quest.Objectives.ForEach(it => Instantiate(objectiveViewPrefab, objectiveRoot.transform).SetModel(it));
        foreach (Transform child in rewardRoot.transform) Destroy(child.gameObject);
        quest.Rewards.ForEach(it => Instantiate(rewardViewPrefab, rewardRoot.transform).SetModel(it));
        LayoutFixer.Fix(objectiveRoot.transform);
        LayoutFixer.Fix(rewardRoot.transform);
    }
}
