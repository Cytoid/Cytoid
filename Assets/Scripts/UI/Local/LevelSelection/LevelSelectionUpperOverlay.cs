using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUpperOverlay : MonoBehaviour
{
    public LoopScrollRect loopScrollRect;
    public RectTransform contentRect;
    public float maxAlpha = 0.9f;
    
    [GetComponent] public CanvasGroup canvasGroup;

    private void Update()
    {
        if (loopScrollRect.StartItemIndex > 0)
        {
            canvasGroup.alpha = maxAlpha;
        }
        else
        {
            var alpha = Math.Max(0, Math.Min(maxAlpha, contentRect.anchoredPosition.y / 360));
            canvasGroup.alpha = alpha;
        }
    }
}