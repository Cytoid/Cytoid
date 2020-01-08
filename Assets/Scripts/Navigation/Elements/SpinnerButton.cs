using DG.Tweening;
using UnityEngine.EventSystems;

public class SpinnerButton : SpinnerElement
{
    public bool spinOnClick = true;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        if (!IsSpinning)
        {
            if (spinOnClick) IsSpinning = true; 
            OnClick();
        }
    }

    protected virtual void OnClick()
    {
        GetComponent<PulseElement>()?.Pulse();
    }

}