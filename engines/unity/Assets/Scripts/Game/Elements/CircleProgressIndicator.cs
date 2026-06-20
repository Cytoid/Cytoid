using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CircleProgressIndicator : MonoBehaviour
{
    public Image image;
    public Text text;

    public float Progress
    {
        get => progress;
        set
        {
            progress = value;
            UpdateProgress();
        }
    }

    public string Text
    {
        get => text.text;
        set => text.text = value;
    }

    private float progress;
    private Sequence tweenSequence;

    private void OnValidate()
    {
        this.AutoFill(ref image);
        this.AutoFillInChildren(ref text);
    }

    private void Awake()
    {
        this.AutoFill(ref image);
        this.AutoFillInChildren(ref text);
        Text = "";
        image.fillAmount = 0;
    }

    private void UpdateProgress()
    {
        image.DOFillAmount(progress, 0.4f);

        tweenSequence?.Kill();
        tweenSequence = DOTween.Sequence()
            .Append(transform.DOScale(0.93f, 0.2f))
            .Append(transform.DOScale(1f, 0.2f));
    }

}