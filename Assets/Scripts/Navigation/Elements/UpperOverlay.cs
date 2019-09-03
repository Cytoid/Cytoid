using System;
using DG.Tweening;
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
        canvasGroup.DOFade(alpha, 0.2f).SetEase(Ease.OutCubic);
    }
}