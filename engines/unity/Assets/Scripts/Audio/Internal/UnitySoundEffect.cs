using System;
using UnityEngine;

/// <summary>
/// Unity-only sound effect implementation. Uses round-robin on a pool of AudioSources.
/// </summary>
public class UnitySoundEffect : ISoundEffect
{
    private readonly AudioSource[] pool;
    private readonly Func<AudioSource> sourceSelector;
    private readonly AudioClip audioClip;
    private readonly bool isResource;
    public readonly bool IsPreloaded;

    private int lastIndex = -1;

    internal UnitySoundEffect(AudioSource[] pool, UnityAudioServer.RoundRobinIndex roundRobinIndex, AudioClip clip, bool isResource, bool isPreloaded)
    {
        this.pool = pool;
        this.audioClip = clip;
        this.isResource = isResource;
        this.IsPreloaded = isPreloaded;

        // Capture the round-robin counter reference via a delegate
        sourceSelector = () =>
        {
            var index = roundRobinIndex.Next();
            lastIndex = index;
            return pool[index];
        };

        Volume = Context.Player.Settings.SoundEffectsVolume;
    }

    public void Play(bool ignoreListenerPause = false)
    {
        var source = sourceSelector();
        source.ignoreListenerPause = ignoreListenerPause;
        source.clip = audioClip;
        source.volume = Volume;
        source.Play();
    }

    private float volume;
    public float Volume
    {
        get => volume;
        set => volume = value;
    }

    public bool IsPlaying => lastIndex >= 0 && pool[lastIndex].isPlaying;

    public void Unload()
    {
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