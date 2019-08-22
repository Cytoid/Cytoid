using System;
using UniRx.Async;
using UnityEngine;

public class ScheduledPulse : MonoBehaviour, ScreenBecameActiveListener, ScreenBecameInactiveListener
{
    [GetComponent] public PulseElement pulseElement;
    public float delay;
    public float interval = 2f;
    public bool isPulsing;

    public async void OnScreenBecameActive()
    {
        isPulsing = true;
        if (delay > 0) await UniTask.Delay(TimeSpan.FromSeconds(delay));
        while (isPulsing)
        {
            pulseElement.Pulse();
            await UniTask.Delay(TimeSpan.FromSeconds(interval));
        }
    }

    public void OnScreenBecameInactive()
    {
        isPulsing = false;
    }

    private void OnDestroy()
    {
        isPulsing = false;
    }
}