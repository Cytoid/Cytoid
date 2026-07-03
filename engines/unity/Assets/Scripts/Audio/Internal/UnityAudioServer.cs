using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity-only audio server implementation. Manages a dedicated music AudioSource
/// and a round-robin SFX pool. The backend used when AudioServerType is Unity.
/// </summary>
public class UnityAudioServer : IAudioServer
{
    private readonly AudioSource musicSource;
    private readonly AudioSource[] sfxPool;
    private readonly Dictionary<string, UnitySoundEffect> sfx = new Dictionary<string, UnitySoundEffect>();
    private UnityMusicTrack currentMusic;
    private float musicVolume;
    private float sfxVolume;

    internal class RoundRobinIndex
    {
        public int Value;
        public int PoolSize;
        public int Next() => Value++ % PoolSize;
    }

    private readonly RoundRobinIndex sfxRoundRobin;

    public UnityAudioServer(AudioSource musicSource, AudioSource[] sfxPool)
    {
        this.musicSource = musicSource;
        this.sfxPool = sfxPool;
        this.sfxRoundRobin = new RoundRobinIndex { Value = 0, PoolSize = sfxPool.Length };
    }

    public void Initialize()
    {
        musicVolume = Context.Player.Settings.MusicVolume;
        sfxVolume = Context.Player.Settings.SoundEffectsVolume;
    }

    public void Dispose()
    {
        // Unload all non-preloaded SFX
        var sfxKeys = new List<string>(sfx.Keys);
        foreach (var id in sfxKeys)
        {
            var effect = sfx[id];
            if (!effect.IsPreloaded)
            {
                effect.Unload();
                sfx.Remove(id);
            }
        }

        // Unload music if loaded
        if (currentMusic != null)
        {
            currentMusic.Unload();
            currentMusic = null;
        }
    }

    public IMusicTrack LoadMusic(string id, AudioClip clip, bool isResource)
    {
        currentMusic = new UnityMusicTrack(musicSource, clip, isResource);
        return currentMusic;
    }

    public void UnloadMusic()
    {
        if (currentMusic != null)
        {
            currentMusic.Unload();
            currentMusic = null;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (currentMusic != null)
        {
            currentMusic.Volume = musicVolume;
        }
    }

    public ISoundEffect LoadSfx(string id, AudioClip clip, bool isResource, bool isPreloaded = false)
    {
        if (sfx.TryGetValue(id, out var existing))
            existing.Unload();
        var effect = new UnitySoundEffect(sfxPool, sfxRoundRobin, clip, isResource, isPreloaded);
        sfx[id] = effect;
        return effect;
    }

    public ISoundEffect GetSfx(string id)
    {
        return sfx.TryGetValue(id, out var effect) ? effect : null;
    }

    public void UnloadSfx(string id)
    {
        if (sfx.TryGetValue(id, out var effect))
        {
            effect.Unload();
            sfx.Remove(id);
        }
    }

    public bool IsSfxLoaded(string id) => sfx.ContainsKey(id);

    public void SetSfxVolume(float volume)
    {
        sfxVolume = volume;
        foreach (var effect in sfx.Values)
        {
            effect.Volume = sfxVolume;
        }
    }

    public void UpdateVolumes(float musicVolume, float sfxVolume)
    {
        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
    }

    public double AudioClockSeconds => AudioSettings.dspTime;
}