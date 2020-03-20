using System;
using UnityEngine;
using UnityEngine.UI;

public class CircleButton : MonoBehaviour, ScreenInitializedListener, ScreenBecameInactiveListener
{
    [GetComponentInChildren] public GradientMeshEffect gradient;
    [GetComponentInChildren] public Text text;
    
    [GetComponent] public InteractableMonoBehavior interactableMonoBehavior;
    [GetComponent] public PulseElement pulseElement;
    public ScheduledPulse scheduledPulse;
    
    public void OnScreenInitialized()
    {
        interactableMonoBehavior.onPointerClick.AddListener(_ =>
        {
            pulseElement.Pulse();
        });
        StopPulsing();
    }
    
    public void StartPulsing()
    {
        if (scheduledPulse != null)
        {
            scheduledPulse.NextPulseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long) (scheduledPulse.initialDelay * 1000L);
        }
    }

    public void StopPulsing()
    {
        if (scheduledPulse != null)
        {
            scheduledPulse.NextPulseTime = long.MaxValue;
        }
    }

    public CircleButtonState State
    {
        set
        {
            switch (value)
            {
                case CircleButtonState.Start:
                    gradient.SetGradient(new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45));
                    text.text = "GAME_PREP_START".Get();
                    break;
                case CircleButtonState.Practice:
                    gradient.SetGradient(new ColorGradient("#F953C6".ToColor(), "#B91D73".ToColor(), 135));
                    text.text = "GAME_PREP_PRACTICE".Get();
                    break;
                case CircleButtonState.Download:
                    gradient.SetGradient(new ColorGradient("#476ADC".ToColor(), "#9CAFEC".ToColor(), -45));
                    text.text = "GAME_PREP_DOWNLOAD".Get();
                    break;
                case CircleButtonState.Retry:
                    gradient.SetGradient(new ColorGradient("#DD5E89".ToColor(), "#F7BB97".ToColor(), -45));
                    text.text = "RESULT_RETRY".Get();
                    break;
                case CircleButtonState.Next:
                    gradient.SetGradient(new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45));
                    text.text = "RESULT_NEXT".Get();
                    break;
                case CircleButtonState.NextStage:
                    gradient.SetGradient(new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45));
                    text.text = "TIER_NEXT_STAGE".Get();
                    break;
                case CircleButtonState.Finish:
                    gradient.SetGradient(new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45));
                    text.text = "TIER_FINISH".Get();
                    break;
                case CircleButtonState.GoBack:
                    gradient.SetGradient(new ColorGradient("#12D8FA".ToColor(), "#A6FFCB".ToColor(), -45));
                    text.text = "TIER_GO_BACK".Get();
                    break;
            }
        }
    }

    public void OnScreenBecameInactive()
    {
        if (scheduledPulse != null)
        {
            scheduledPulse.NextPulseTime = long.MaxValue;
        }
    }
}

public enum CircleButtonState
{
    Start, Practice, Download, Retry, Next, NextStage, Finish, GoBack
}

