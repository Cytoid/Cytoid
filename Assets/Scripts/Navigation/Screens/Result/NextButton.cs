using UnityEngine;

public class NextButton : MonoBehaviour, ScreenInitializedListener
{
    [GetComponent] public InteractableMonoBehavior interactableMonoBehavior;
    [GetComponent] public PulseElement pulseElement;
    [GetComponent] public ScheduledPulse scheduledPulse;
    
    public void OnScreenInitialized()
    {
        interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            scheduledPulse.NextPulseTime = long.MaxValue;
            pulseElement.Pulse();
            
            this.GetScreenParent<ResultScreen>().Done();
        });
    }
}