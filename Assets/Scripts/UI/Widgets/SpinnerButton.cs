using DG.Tweening;
using UnityEngine.EventSystems;

public class SpinnerButton : SpinnerElement
{

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData); 
        transform.DOScale(0.9f, 0.2f).SetEase(Ease.OutCubic);
    }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (!IsSpinning)
        {
            IsSpinning = true; 
            OnClick();
        }
    }

    protected virtual void OnClick()
    {
        GetComponent<PulseElement>()?.Pulse();
    }

}