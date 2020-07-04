using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ChibiDisplay : MonoBehaviour
{
    public Image image;

    private readonly List<Sequence> tweenSequences = new List<Sequence>();

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            image.sprite = null;
            return;
        }

        image.sprite = sprite;
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