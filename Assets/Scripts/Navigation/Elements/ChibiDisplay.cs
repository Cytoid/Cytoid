using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ChibiDisplay : MonoBehaviour
{
    public Image image;
    public Animator animator;

    private readonly List<Sequence> tweenSequences = new List<Sequence>();

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            image.sprite = null;
            return;
        }

        SetAnimation(null);

        image.sprite = sprite;
        PopTween();
    }

    public void SetAnimation(RuntimeAnimatorController controller, string animationName = "Default")
    {
        if (controller == null)
        {
            animator.runtimeAnimatorController = null;
            return;
        }

        SetSprite(null);

        animator.runtimeAnimatorController = controller;
        animator.Play(animationName);
        PopTween();
    }

    private void PopTween()
    {
        tweenSequences.ForEach(it => it.Kill());
        tweenSequences.Clear();
        image.rectTransform.DOKill();
        tweenSequences.Add(DOTween.Sequence()
            .Append(image.rectTransform.DOScaleY(1.03f, 0.3f))
            .Append(image.rectTransform.DOScaleY(1f, 0.3f))
            .OnComplete(BreatheTween));
    }

    private void BreatheTween()
    {
        tweenSequences.Add(DOTween.Sequence()
            .Append(image.rectTransform.DOScaleY(1.01f, 1.6f))
            .Append(image.rectTransform.DOScaleY(1f, 1.6f))
            .OnComplete(BreatheTween));
    }
    
}