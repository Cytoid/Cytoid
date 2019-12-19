using UnityEngine;
using UnityEngine.EventSystems;

public class PassThrough : InteractableMonoBehavior, IMoveHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    public InteractableMonoBehavior target;
    public GameObject unityTarget;
    
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (target) target.OnPointerEnter(eventData);
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.pointerEnterHandler);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (target) target.OnPointerExit(eventData);
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.pointerExitHandler);
    }        
    
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (target) target.OnPointerDown(eventData);
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.pointerDownHandler);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (target)
        {
            target.bypassOnClickHitboxCheck = true;
            target.OnPointerUp(eventData);
            target.bypassOnClickHitboxCheck = false;
        }
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.pointerUpHandler);
    }
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (target) target.OnPointerClick(eventData);
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.pointerClickHandler);
    }
    
    public override void OnPointerMove(Vector2 localPointerPos)
    {
        if (target) target.OnPointerMove(localPointerPos);
    }

    public void OnMove(AxisEventData eventData)
    {
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.moveHandler);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.dragHandler);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.endDragHandler);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (unityTarget) ExecuteEvents.Execute(unityTarget, eventData, ExecuteEvents.initializePotentialDrag);
    }
}