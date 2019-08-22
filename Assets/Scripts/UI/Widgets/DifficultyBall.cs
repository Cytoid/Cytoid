using UnityEngine;
using UnityEngine.UI;

public class DifficultyBall : MonoBehaviour
{ 
    public Text text;
    public GradientMeshEffect gradientMeshEffect;

    public void SetModel(Difficulty difficulty, int level)
    {
        text.text = Difficulty.ConvertToDisplayLevel(level);
        gradientMeshEffect.SetGradient(difficulty.Gradient);
    }
}