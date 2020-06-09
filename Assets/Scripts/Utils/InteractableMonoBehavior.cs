using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableMonoBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public PointerDataEvent onPointerEnter = new PointerDataEvent();
    [HideInInspector]  public PointerDataEvent onPointerExit = new PointerDataEvent();
    [HideInInspector] public PointerDataEvent onPointerDown = new PointerDataEvent();
    [HideInInspector] public PointerDataEvent onPointerUp = new PointerDataEvent();
    [HideInInspector] public PointerDataEvent onPointerClick = new PointerDataEvent();
    [HideInInspector] public Vector2Event onPointerMove = new Vector2Event();
    public bool IsPointerDown { get; protected set; }
    public virtual bool IsPointerEntered { get; protected set; }
    
    public bool scaleOnClick;
    public float scaleToOnClick = 0.9f;
    public bool pulseOnClick;
    public bool bypassOnClickHitboxCheck;
    
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
        if (IsPointerDown) OnPointerUp(eventData);
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        IsPointerDown = true;
        if (scaleOnClick) transform.DOScale(scaleToOnClick, 0.2f).SetEase(Ease.OutCubic);
        onPointerDown.Invoke(eventData);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (IsPointerDown && (bypassOnClickHitboxCheck || IsPointerEntered))
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
            if (localPointerPos == Vector2.positiveInfinity) continue;
            OnPointerMove(localPointerPos);
            yield return null;
        }
    }

    private Vector2 GetLocalPointerPos()
    {
        if (raycaster == null) raycaster = GetComponentInParent<GraphicRaycaster>();
        if (Input.mousePosition.x > 1000000000) return Vector2.positiveInfinity; // Seems like a random glitch in Unity to return infinite values for Input.mousePosition
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