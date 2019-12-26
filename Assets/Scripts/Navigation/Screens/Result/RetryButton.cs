using UnityEngine;

public class RetryButton : MonoBehaviour, ScreenInitializedListener
{
    [GetComponent] public InteractableMonoBehavior interactableMonoBehavior;
    [GetComponent] public PulseElement pulseElement;
    
    public void OnScreenInitialized()
    {
        interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            pulseElement.Pulse();
            
            this.GetScreenParent<ResultScreen>().RetryGame();
        });
    }
}