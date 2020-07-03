using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class ChibiDisplay : MonoBehaviour
{
    public Image image;
    public List<string> keys;
    public List<Sprite> sprites;

    private List<Sequence> tweenSequences = new List<Sequence>();
    
    private void OnEnable()
    {
        Assert.IsTrue(keys.Count == sprites.Count);
        // Start breathing animation
    }

    public void SetSprite(string sprite)
    {
        if (sprite == null)
        {
            image.sprite = null;
            return;
        }
        var index = keys.FindIndex(it => it == sprite);
        if (index < 0) index = 0;
        image.sprite = sprites[index];
        tweenSequences.ForEach(it => it.Kill());
        tweenSequences.Clear();
        image.rectTransform.DOKill();
        tweenSequences.Add(DOTween.Sequence()
            .Append(image.rectTransform.DOScaleY(1.08f, 0.3f))
            .Append(image.rectTransform.DOScaleY(0.98f, 0.3f))
            .OnComplete(() =>
            {
                tweenSequences.Add(DOTween.Sequence()
                    .Append(image.rectTransform.DOScaleY(1.04f, 1.2f))
                    .Append(image.rectTransform.DOScaleY(0.98f, 1.2f))
                    .SetLoops(-1));
            }));
    }
}