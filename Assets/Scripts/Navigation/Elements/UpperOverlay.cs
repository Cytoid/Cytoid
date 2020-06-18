using System;
using DG.Tweening;
using UnityEngine;

public class UpperOverlay : MonoBehaviour, ScreenBecameActiveListener, ScreenUpdateListener
{
    public RectTransform contentRect;
    public float minAlpha = 0;
    public float maxAlpha = 0.9f;
    
    [GetComponent] public CanvasGroup canvasGroup;

    private DateTime enterTime;
    private float multiplier;

    protected virtual void Update()
    {
        if (contentRect == null) return;
        var alpha = Math.Max(minAlpha, Math.Min(maxAlpha, contentRect.anchoredPosition.y / 360));
        canvasGroup.DOFade(alpha * multiplier, 0.2f).SetEase(Ease.OutCubic);
    }

    public void OnScreenBecameActive()
    {
        multiplier = 0;
        enterTime = DateTime.Now + TimeSpan.FromSeconds(0.2f);
    }

    public void OnScreenUpdate()
    {
        if (DateTime.Now < enterTime || multiplier >= 1) return;
        multiplier += 1f / 60 * (1 / 0.2f);
    }
    
}