using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UpperOverlay : MonoBehaviour
{
    public RectTransform contentRect;
    public float minAlpha = 0;
    public float maxAlpha = 0.9f;
    
    [GetComponent] public CanvasGroup canvasGroup;

    protected virtual void Update()
    {
        if (contentRect == null) return;
        var alpha = Math.Max(minAlpha, Math.Min(maxAlpha, contentRect.anchoredPosition.y / 360));
        canvasGroup.DOFade(alpha, 0.2f).SetEase(Ease.OutCubic);
    }
}