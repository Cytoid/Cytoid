using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using E7.Native;
using UnityEngine;

internal class NativeSfxBackend
{
    private readonly Dictionary<string, NativeSoundEffect> sfx = new();
    private int _nextNativeSource;
    private float sfxVolume;

    public void Initialize()
    {
        var options = new NativeAudio.InitializationOptions
        {
            androidAudioTrackCount = 2,
            androidBufferSize = -1
        };
        NativeAudio.Initialize(options);
        Debug.Log($"Native Audio initialized with {NativeAudio.GetNativeSourceCount()} sources");
        _nextNativeSource = 0;
        sfxVolume = Context.Player.Settings.SoundEffectsVolume;
    }

    public void Dispose()
    {
        if (NativeAudio.Initialized)
        {
            // Stop ALL native sources first — ensures no play head is reading any pointer's memory
            var sourceCount = NativeAudio.GetNativeSourceCount();
            for (var i = 0; i < sourceCount; i++)
                NativeAudio.GetNativeSource(i).Stop();
        }

        foreach (var entry in sfx)
            entry.Value.UnloadPointerSync();

        try { NativeAudio.Dispose(); }
        catch (Exception e) { Debug.LogError($"Error disposing native audio: {e}"); }

        sfx.Clear();
    }

    public ISoundEffect LoadSfx(string id, AudioClip clip, bool isResource, bool isPreloaded)
    {
        if (sfx.TryGetValue(id, out var existing))
            existing.Unload();
        var effect = new NativeSoundEffect(this, clip, isPreloaded);
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

    public bool IsSfxLoaded(string id)
    {
        return sfx.ContainsKey(id);
    }

    public void SetSfxVolume(float vol)
    {
        sfxVolume = vol;
        foreach (var entry in sfx)
        {
            entry.Value.Volume = vol;
        }
    }

    internal int GetNextNativeSourceIndex()
    {
        var index = _nextNativeSource;
        _nextNativeSource = (_nextNativeSource + 1) % NativeAudio.GetNativeSourceCount();
        return index;
    }

    internal class NativeSoundEffect : ISoundEffect
    {
        private readonly NativeSfxBackend parent;
        private NativeSource source;
        private NativeAudioPointer pointer;
        private readonly float length;
        private float volume;
        private bool isPlaying;

        internal bool IsPreloaded { get; }

        public NativeSoundEffect(NativeSfxBackend parent, AudioClip clip, bool isPreloaded)
        {
            this.parent = parent;
            pointer = NativeAudio.Load(clip, NativeAudio.LoadOptions.defaultOptions);
            length = clip.length;
            volume = Context.Player.Settings.SoundEffectsVolume;
            IsPreloaded = isPreloaded;
        }

        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }

        public bool IsPlaying => isPlaying;

        public void Play(bool ignoreListenerPause = false)
        {
            var sourceIndex = parent.GetNextNativeSourceIndex();
            source = NativeAudio.GetNativeSource(sourceIndex);
            source.Play(pointer);
            source.SetVolume(volume);
            isPlaying = true;
        }

        public void Unload()
        {
            Stop();
            UniTask.Void(async () =>
            {
                await UniTask.DelayFrame(10);
                pointer.Unload();
            });
        }

        internal void UnloadPointerSync()
        {
            Stop();
            pointer.Unload();
        }

        public void Stop()
        {
            isPlaying = false;
            if (source.IsValid)
            {
                source.Stop();
            }
        }
    }
}