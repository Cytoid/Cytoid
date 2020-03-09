using UnityEngine;
using UnityEngine.UI;

public class CriterionEntry : MonoBehaviour
{
    [GetComponent] public Text text;
    [GetComponent] public Image icon;
    
    public Sprite undeterminedIcon;
    public Sprite passedIcon;
    public Sprite failedIcon;

    public void SetModel(string criterion, CriterionState state)
    {
        text.text = criterion;
        switch (state)
        {
            case CriterionState.Passed:
                icon.sprite = passedIcon;
                break;
            case CriterionState.Failed:
                icon.sprite = failedIcon;
                break;
            case CriterionState.Undetermined:
                icon.sprite = undeterminedIcon;
                break;
        }
    }
}