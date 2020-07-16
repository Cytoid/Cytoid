using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BeatPulseVisualizer : MonoBehaviour
{
    public Image image;

    public float Tempo { get; set; } = 120;

    public int BeatsPerMeasure { get; set; } = 4;

    private DateTime startTime = DateTime.MinValue;

    public void StartPulsing()
    {
        //startTime = DateTime.Now;

        image.DOKill();
        image.SetAlpha(0.3f);
        image.DOFade(0, 1f).SetEase(Ease.InCubic);
    }

    public void StopPulsing()
    {
        startTime = DateTime.MinValue;
    }

    private void Update()
    {
        if (startTime == DateTime.MinValue) return;

        var timeElapsed = (DateTime.Now - startTime).TotalSeconds;
        var timePerBeat = 60.0 / Tempo;
        var currentBeat = timeElapsed / timePerBeat + 0.5;
        var roundedBeat = (int) Math.Floor(currentBeat);
        var progress = currentBeat % 1;
        var halfProgress = progress % 0.5 * 2.0;
        var intensity = roundedBeat % BeatsPerMeasure == 0 ? 0.5f : 0.2f;
        var opacity = progress < 0.5
            ? EasingFunction.Linear(0.1f, intensity, (float) halfProgress)
            : EasingFunction.Linear(intensity, 0.1f, (float) halfProgress);
        image.DOFade(opacity, 0.2f);
    }
    
}