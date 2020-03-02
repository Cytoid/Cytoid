using System;
using UnityEngine;
using UnityEngine.UI;

public class StartButton : MonoBehaviour, ScreenInitializedListener
{
    public GradientMeshEffect gradient;
    public Text text;
    
    [GetComponent] public InteractableMonoBehavior interactableMonoBehavior;
    [GetComponent] public PulseElement pulseElement;
    [GetComponent] public ScheduledPulse scheduledPulse;
    
    public void OnScreenInitialized()
    {
        interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            pulseElement.Pulse();
        });
    }

    public void StopPulsing()
    {
        scheduledPulse.NextPulseTime = long.MaxValue;
    }

    public void SetState(State state)
    {
        switch (state)
        {
            case State.Start:
                gradient.SetGradient(new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45));
                text.text = "GAME_PREP_START".Get();
                break;
            case State.Practice:
                gradient.SetGradient(new ColorGradient("#F953C6".ToColor(), "#B91D73".ToColor(), 135));
                text.text = "GAME_PREP_PRACTICE".Get();
                break;
            case State.Download:
                gradient.SetGradient(new ColorGradient("#476ADC".ToColor(), "#9CAFEC".ToColor(), -45));
                text.text = "GAME_PREP_DOWNLOAD".Get();
                break;
        }
    }
    
    public enum State
    {
        Start, Practice, Download
    }
}

