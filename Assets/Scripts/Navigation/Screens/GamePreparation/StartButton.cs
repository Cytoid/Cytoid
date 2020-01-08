using UnityEngine;

public class StartButton : MonoBehaviour, ScreenInitializedListener
{
    [GetComponent] public InteractableMonoBehavior interactableMonoBehavior;
    [GetComponent] public PulseElement pulseElement;
    [GetComponent] public ScheduledPulse scheduledPulse;
    
    public void OnScreenInitialized()
    {
        interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            pulseElement.Pulse();
            
            var parent = this.GetScreenParent<GamePreparationScreen>();
            if (parent.Level.IsLocal)
            {
                scheduledPulse.NextPulseTime = long.MaxValue;
            }
            parent.OnStartButton();
        });
    }
}