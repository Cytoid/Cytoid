using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ScheduledPulse : MonoBehaviour, ScreenBecameActiveListener, ScreenBecameInactiveListener, ScreenUpdateListener
{
    [GetComponent] public PulseElement pulseElement;

    public float initialDelay = 0.4f;
    public float interval = 2f;
    public bool isPulsing;
    public bool startPulsingOnScreenBecameActive = true;

    public long NextPulseTime { get; set; } = long.MaxValue;
    
    public void OnScreenBecameActive()
    {
        isPulsing = true;
        if (startPulsingOnScreenBecameActive)
        {
            var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var delay = (int) (initialDelay * 1000);
            NextPulseTime = start + delay;
        }
    }

    public void OnScreenBecameInactive()
    {
        isPulsing = false;
    }

    public void OnScreenUpdate()
    {
        if (isPulsing && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= NextPulseTime)
        {
            NextPulseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (int) (interval * 1000);
            pulseElement.Pulse();
        }
    }

    private void OnDestroy()
    {
        isPulsing = false;
    }
}