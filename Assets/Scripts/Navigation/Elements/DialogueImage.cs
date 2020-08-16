using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

public class DialogueImage : MonoBehaviour
{
    [GetComponent] public RectTransform rectTransform;
    [GetComponent] public ProceduralImage image;
    [GetComponent] public UniformModifier modifier;

    private void Awake()
    {
        image.sprite = null;
        image.SetAlpha(0);
    }

    public void Clear()
    {
        if (image.sprite == null) return;
        image.DOKill();
        image.DOFade(0, 0.4f).OnComplete(() => image.sprite = null);
    }

    public void SetData(Sprite sprite, int width, int height, int cornerRadius)
    {
        image.DOKill();
        rectTransform.SetWidth(width);
        rectTransform.SetHeight(height);
        image.sprite = sprite;
        image.DOFade(1, 0.4f);
        modifier.Radius = cornerRadius;
    }
}