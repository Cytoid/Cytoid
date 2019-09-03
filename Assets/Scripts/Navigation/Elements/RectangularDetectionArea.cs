using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RectangularDetectionArea : InteractableMonoBehavior
{

    private bool detectionEnabled = true;
    public bool DetectionEnabled
    {
        get => detectionEnabled;
        set
        {
            detectionEnabled = value;
            image.raycastTarget = value;
        }
    }

    public Action onClick;

    [GetComponent] public Image image;

    private void Awake()
    {
        DetectionEnabled = DetectionEnabled;
    }
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        onClick();
    }
    
}