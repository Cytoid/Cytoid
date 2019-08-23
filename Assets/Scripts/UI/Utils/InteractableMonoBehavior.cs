using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableMonoBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public PointerDataEvent onPointerEnter;
    public PointerDataEvent onPointerExit;
    public PointerDataEvent onPointerDown;
    public PointerDataEvent onPointerUp;
    public PointerDataEvent onPointerClick;
    public Vector2Event onPointerMove;
    public bool IsPointerDown { get; protected set; }
    
    public bool scaleOnClick;
    public float scaleToOnClick = 0.9f;
    public bool pulseOnClick;

    protected GraphicRaycaster raycaster;
    
    protected virtual void Awake()
    {
        onPointerEnter = new PointerDataEvent();
        onPointerExit = new PointerDataEvent();
        onPointerDown = new PointerDataEvent();
        onPointerUp = new PointerDataEvent();
        onPointerClick = new PointerDataEvent();
        onPointerMove = new Vector2Event();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        StartCoroutine(nameof(OnPointerMoveIEnumerator));
        onPointerEnter.Invoke(eventData);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopCoroutine(nameof(OnPointerMoveIEnumerator));
        onPointerEnter.Invoke(eventData);
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        IsPointerDown = true;
        if (scaleOnClick) transform.DOScale(scaleToOnClick, 0.2f).SetEase(Ease.OutCubic);
        onPointerEnter.Invoke(eventData);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        IsPointerDown = false;
        if (scaleOnClick) transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
        onPointerEnter.Invoke(eventData);
    }

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