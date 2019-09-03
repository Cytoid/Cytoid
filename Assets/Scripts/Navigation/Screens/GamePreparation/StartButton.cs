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
            scheduledPulse.NextPulseTime = long.MaxValue;
            pulseElement.Pulse();
            
            this.GetScreenParent<GamePreparationScreen>().StartGame();
        });
    }
}