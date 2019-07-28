using System;
using UnityEngine;
using UnityEngine.UI;

public class UpperOverlay : MonoBehaviour
{
    public LoopScrollRect loopScrollRect;
    public RectTransform contentRect;

    [GetComponent] public Image image;

    private void Update()
    {
        if (loopScrollRect.StartItemIndex > 0)
        {
            image.SetAlpha(1);
        }
        else
        {
            var alpha = Math.Max(0, Math.Min(1, contentRect.anchoredPosition.y / 360));
            image.SetAlpha(alpha);
        }
    }
}