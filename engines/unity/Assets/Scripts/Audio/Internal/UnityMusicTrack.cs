using System;
using UnityEngine;

/// <summary>
/// Unity-only music track implementation. Uses a dedicated AudioSource.
/// </summary>
public class UnityMusicTrack : IMusicTrack
{
    private readonly AudioSource source;
    private readonly AudioClip audioClip;
    private readonly bool isResource;
    private double scheduledStartTime;

    public UnityMusicTrack(AudioSource source, AudioClip clip, bool isResource)
    {
        this.source = source;
        this.audioClip = clip;
        this.isResource = isResource;
        this.scheduledStartTime = 0;
        Volume = Context.Player.Settings.MusicVolume;
    }

    public double SchedulePlay(double delaySeconds)
    {
        var time = AudioSettings.dspTime + delaySeconds;
        source.PlayScheduled(time);
        scheduledStartTime = time;
        return time;
    }

    public double ScheduledStartTime => scheduledStartTime;

    public float SourceTimeSeconds
    {
        get => source.timeSamples * 1f / source.clip.frequency;
        set => source.time = value;
    }

    public float Length => audioClip.length;

    private float volume;
    public float Volume
    {
        get => volume;
        set
        {
            volume = value;
            source.volume = volume;
        }
    }

    public bool IsPlaying => source.isPlaying;

    public bool IsFinished => !source.isPlaying;

    public void Pause()
    {
        source.Pause();
    }

    public void Resume()
    {
        source.UnPause();
    }

    public void Stop()
    {
        source.Stop();
    }

    public void Unload()
    {
        Stop();
        source.clip = null;

        if (audioClip != null)
        {
            try
            {
                audioClip.UnloadAudioData();
            }
            catch (MissingReferenceException)
            {
            }
            catch (NullReferenceException)
            {
            }

            if (!isResource)
            {
                UnityEngine.Object.Destroy(audioClip);
            }
            else
            {
                Resources.UnloadAsset(audioClip);
            }
        }
    }
}