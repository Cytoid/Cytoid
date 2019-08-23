using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FatScrollbar : Scrollbar
{
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        (transform as RectTransform).DOWidth(32, 0.2f);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        (transform as RectTransform).DOWidth(16, 0.2f);
    }
}