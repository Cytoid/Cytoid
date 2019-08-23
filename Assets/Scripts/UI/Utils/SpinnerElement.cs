using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class SpinnerElement : InteractableMonoBehavior
{
    public RectTransform defaultIcon;
    public RectTransform spinnerIcon;
    public float fullScale = 1f;
    private TweenerCore<Vector3, Vector3, VectorOptions> spinnerIconScaleTween;
    
    private bool isSpinning;
    public bool IsSpinning
    {
        get => isSpinning;
        set
        {
            if (value == isSpinning) return;
            if (value)
            {
                isSpinning = true;
                if (defaultIcon != null) defaultIcon.DOScale(0, 0.4f).SetEase(Ease.InBack);
                spinnerIconScaleTween = spinnerIcon.DOScale(fullScale, 0.4f).SetDelay(0.4f).SetEase(Ease.OutBack);
                spinnerIcon.localRotation = Quaternion.identity;
                spinnerIcon
                    .DOLocalRotate(new Vector3(0, 0, -360), 1f, RotateMode.FastBeyond360)
                    .SetRelative()
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Incremental);
            }
            else
            {
                spinnerIconScaleTween.Kill();
                spinnerIcon.DOScale(0, 0.4f).SetEase(Ease.InBack);
                if (defaultIcon != null)
                {
                    defaultIcon.DOKill();
                    defaultIcon.DOScale(fullScale, 0.4f).SetDelay(0.4f).SetEase(Ease.OutBack);
                }
                Run.After(0.4f, () => isSpinning = false);
            }
        }
    }
    
    protected virtual void Start()
    {
        if (defaultIcon != null) defaultIcon.gameObject.SetActive(true);
        spinnerIcon.gameObject.SetActive(true);
        spinnerIcon.localScale = Vector3.zero;
    }
}