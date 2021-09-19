using UnityEngine;
using UnityEngine.UI;

public class ObjectiveCardView : MonoBehaviour
{
    public Transform completedIcon;
    public Text descriptionText;
    public Text progressText;
    public ProgressBar progressBar;

    public void SetModel(Objective objective)
    {
        completedIcon.gameObject.SetActive(objective.IsCompleted);
        LayoutFixer.Fix(completedIcon.parent);
        descriptionText.text = objective.Description;
        progressText.text = $"{objective.CurrentProgress}/{objective.MaxProgress}";
        progressBar.progress = objective.CurrentProgress * 1.0f / objective.MaxProgress;
    }
}