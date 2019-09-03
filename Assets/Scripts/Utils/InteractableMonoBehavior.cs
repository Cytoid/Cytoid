using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableMonoBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public PointerDataEvent onPointerEnter = new PointerDataEvent();
    public PointerDataEvent onPointerExit = new PointerDataEvent();
    public PointerDataEvent onPointerDown = new PointerDataEvent();
    public PointerDataEvent onPointerUp = new PointerDataEvent();
    public PointerDataEvent onPointerClick = new PointerDataEvent();
    public Vector2Event onPointerMove = new Vector2Event();
    public bool IsPointerDown { get; protected set; }
    public virtual bool IsPointerEntered { get; protected set; }
    
    public bool scaleOnClick;
    public float scaleToOnClick = 0.9f;
    public bool pulseOnClick;
    
    protected GraphicRaycaster raycaster;

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerEntered = true;
        StartCoroutine(nameof(OnPointerMoveIEnumerator));
        onPointerEnter.Invoke(eventData);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        IsPointerEntered = false;
        StopCoroutine(nameof(OnPointerMoveIEnumerator));
        onPointerExit.Invoke(eventData);
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        IsPointerDown = true;
        if (scaleOnClick) transform.DOScale(scaleToOnClick, 0.2f).SetEase(Ease.OutCubic);
        onPointerDown.Invoke(eventData);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (IsPointerDown && IsPointerEntered)
        {
            OnPointerClick(eventData);
        }
        IsPointerDown = false;
        if (scaleOnClick) transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
        onPointerUp.Invoke(eventData);
    }

    // NOTE: This is not a Unity interface. Unity's OnPointerClick doesn't fire correctly at all times,
    // so this method is fired with another method. See OnPointerUp.
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        onPointerClick.Invoke(eventData);
        if (pulseOnClick) GetComponent<PulseElement>()?.Pulse();
    }
    
    public virtual void OnPointerMove(Vector2 localPointerPos)
    {
        onPointerMove.Invoke(localPointerPos);
    }

    public IEnumerator OnPointerMoveIEnumerator()
    {
        while (Application.isPlaying) {
            var localPointerPos = GetLocalPointerPos();
            OnPointerMove(localPointerPos);
            yield return null;
        }
    }

    public Vector2 GetLocalPointerPos()
    {
        if (raycaster == null) raycaster = GetComponentInParent<GraphicRaycaster>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, Input.mousePosition,
            raycaster.eventCamera, out var localPos);
        return localPos;
    }

}

[Serializable]
public class Vector2Event : UnityEvent<Vector2>
{
}

[Serializable]
public class PointerDataEvent : UnityEvent<PointerEventData>
{
}