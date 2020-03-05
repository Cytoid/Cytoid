using UnityEngine;
using UnityEngine.UI;

public class TierStageResultWidget : MonoBehaviour
{
    [GetComponentInChildren] public DifficultyBall difficultyBall;
    public Text titleText;
    [GetComponentInChildren] public PerformanceWidget performanceWidget;
}