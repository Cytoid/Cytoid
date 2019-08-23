using System;
using UnityEngine;
using UnityEngine.UI;

public class UpperOverlay : MonoBehaviour
{
    public RectTransform contentRect;
    public float maxAlpha = 0.9f;
    
    [GetComponent] public CanvasGroup canvasGroup;

    protected virtual void Update()
    {
        var alpha = Math.Max(0, Math.Min(maxAlpha, contentRect.anchoredPosition.y / 360));
        canvasGroup.alpha = alpha;
    }
}