using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHitZone : MonoBehaviour
{
    public float width;
    public float height;

    public class EmptyGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }

    private void OnValidate()
    {
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            width = Mathf.Max(width, rectTransform.sizeDelta.x);
            height = Mathf.Max(height, rectTransform.sizeDelta.y);
        }
    }

    private void Awake()
    {
        CreateHitZone();
    }

    private void CreateHitZone()
    {
        // create child object
        var gobj = new GameObject("Button Hit Zone");
        var hitzoneRectTransform = gobj.AddComponent<RectTransform>();
        hitzoneRectTransform.SetParent(transform);
        hitzoneRectTransform.localPosition = Vector3.zero;
        hitzoneRectTransform.localScale = Vector3.one;
        hitzoneRectTransform.sizeDelta = new Vector2(width, height);

        // create transparent graphic
        // Image image = gobj.AddComponent<Image>();
        // image.color = new Color(0, 0, 0, 0);
        
        // Add CanvasRenderer first, then EmptyGraphic
        gobj.AddComponent<CanvasRenderer>();
        gobj.AddComponent<EmptyGraphic>();

        // delegate events
        var eventTrigger = gobj.AddComponent<EventTrigger>();
        // pointer up
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerDown,
            data =>
            {
                ExecuteEvents.Execute(gameObject, data,
                    ExecuteEvents.pointerDownHandler);
            });
        // pointer down
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerUp,
            data =>
            {
                ExecuteEvents.Execute(gameObject, data,
                    ExecuteEvents.pointerUpHandler);
            });
        // pointer click
        AddEventTriggerListener(eventTrigger, EventTriggerType.PointerClick,
            data =>
            {
                ExecuteEvents.Execute(gameObject, data,
                    ExecuteEvents.pointerClickHandler);
            });
    }

    private static void AddEventTriggerListener(EventTrigger trigger, EventTriggerType eventType,
        System.Action<BaseEventData> method)
    {
        var entry = new EventTrigger.Entry {eventID = eventType, callback = new EventTrigger.TriggerEvent()};
        entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(method));
        trigger.triggers.Add(entry);
    }
}
