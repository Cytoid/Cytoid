using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : SerializedMonoBehaviour
{
    public Image backgroundImage;
    public Image progressImage;

    public float progress;

    private void Update()
    {
        if (progress == 0)
        {
            progressImage.SetAlpha(0);
        }
        else
        {
            progressImage.SetAlpha(1);
            progressImage.rectTransform.SetSize(new Vector2(backgroundImage.rectTransform.sizeDelta.x * progress, progressImage.rectTransform.sizeDelta.y));
        }
    }

    [Button][HideInPlayMode]
    public void Preview()
    {
        Update();
    }
}
