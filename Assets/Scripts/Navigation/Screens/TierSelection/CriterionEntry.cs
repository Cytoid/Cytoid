using UnityEngine;
using UnityEngine.UI;

public class CriterionEntry : MonoBehaviour
{
    [GetComponent] public Text text;

    public void SetModel(string criterion)
    {
        text.text = criterion;
    }
}