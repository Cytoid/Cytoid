using DG.Tweening;
using UnityEngine.EventSystems;

public class SpinnerButton : SpinnerElement
{

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