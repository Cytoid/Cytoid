using System;
using UniRx.Async;
using UnityEngine;

public class ScheduledPulse : MonoBehaviour, ScreenBecameActiveListener, ScreenBecameInactiveListener, ScreenUpdateListener
{
    [GetComponent] public PulseElement pulseElement;
    public float delay;
    public float interval = 2f;
    public bool isPulsing;

    private long nextPulseTime;
    
    public void OnScreenBecameActive()
    {
        isPulsing = true;
        nextPulseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (int) (delay * 1000);
    }

    public void OnScreenBecameInactive()
    {
        isPulsing = false;
    }

    public void OnScreenUpdate()
    {
        if (isPulsing && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= nextPulseTime)
        {
            nextPulseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (int) (interval * 1000);
            pulseElement.Pulse();
        }
    }

    private void OnDestroy()
    {
        isPulsing = false;
    }
}