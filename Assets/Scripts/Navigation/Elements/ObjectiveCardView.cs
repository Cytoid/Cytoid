using System;
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
        completedIcon.gameObject.SetActive(objective.Completed);
        LayoutFixer.Fix(completedIcon.parent);
        descriptionText.text = objective.Description;
        switch (objective.ProgressType)
        {
            case ProgressType.Percentage:
                progressText.text = $"{objective.Progress * 100:F2}%/{objective.Completion * 100:F2}%";
                break;
            case ProgressType.OneDecimal:
                progressText.text = $"{objective.Progress:F1}/{objective.Completion:F1}";
                break;
            case ProgressType.TwoDecimal:
                progressText.text = $"{objective.Progress:F2}/{objective.Completion:F2}";
                break;
            default:
                progressText.text = $"{Math.Round(objective.Progress)}/{Math.Round(objective.Completion)}";
                break;
        }
        progressBar.progress = Mathf.Clamp01(objective.Progress / objective.Completion);
    }
}