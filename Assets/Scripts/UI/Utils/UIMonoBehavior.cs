using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableMonoBehavior : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public Vector2Event onPointerMove;
    public bool IsPointerDown { get; protected set; }

    private GraphicRaycaster raycaster;
    
    private void Awake()
    {
        onPointerMove = new Vector2Event();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        StartCoroutine(nameof(OnPointerMove));
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopCoroutine(nameof(OnPointerMove));
    }
    
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        IsPointerDown = true;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        IsPointerDown = false;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
    }

    public IEnumerator OnPointerMove()
    {
        while (Application.isPlaying)
        {
            onPointerMove.Invoke(GetLocalPointerPos());
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