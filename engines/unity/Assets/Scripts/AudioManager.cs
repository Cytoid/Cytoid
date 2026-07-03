using System;
using E7.Native;
using UnityEngine;
using UnityEngine.Assertions;

public class AudioManager : SingletonMonoBehavior<AudioManager>
{
    public AudioSource[] audioSources;
    public AudioClip[] preloadedAudioClips;

    public bool IsInitialized { get; private set; }

    private IAudioServer _server;

    protected override void Awake()
    {
        base.Awake();
        Context.AudioManager = this;
    }

    public void Initialize()
    {
        if (IsInitialized) return;

        // Partition assertion FIRST — fail clearly on bad Inspector wiring before indexing.
        Assert.AreEqual(7, audioSources.Length, "Expected 1 music + 6 SFX AudioSources");

        var serverType = Context.Player.Settings.AudioServer;
        if (serverType == AudioServerType.Exceed7 && !NativeAudio.OnSupportedPlatform)
        {
            Debug.LogWarning("[Audio] Exceed7 requested but Native Audio not supported; falling back to Unity");
            serverType = AudioServerType.Unity;
        }
        _server = serverType switch
        {
            AudioServerType.Unity   => new UnityAudioServer(audioSources[0], audioSources[1..]),
            AudioServerType.Exceed7 => new Exceed7AudioServer(audioSources[0], audioSources[1..]),
            _ => throw new NotSupportedException($"AudioServer {serverType} not supported"),
        };
        _server.Initialize();

        // Preload SFX
        foreach (var clip in preloadedAudioClips)
            _server.LoadSfx(clip.name, clip, isResource: false, isPreloaded: true);

        IsInitialized = true;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispose();
    }

    public void Dispose()
    {
        if (!IsInitialized) return;
        IsInitialized = false;
        try { _server?.Dispose(); }
        catch (Exception e) { Debug.LogError($"Error disposing audio server: {e}"); }
        _server = null;
    }

    public IMusicTrack LoadMusic(string id, AudioClip clip, bool isResource)
        => _server.LoadMusic(id, clip, isResource);

    public ISoundEffect LoadSfx(string id, AudioClip clip, bool isResource, bool isPreloaded = false)
        => _server.LoadSfx(id, clip, isResource, isPreloaded);

    public ISoundEffect GetSfx(string id) => _server.GetSfx(id);

    public bool IsSfxLoaded(string id) => _server.IsSfxLoaded(id);

    public void UnloadMusic() => _server.UnloadMusic();

    public void UnloadSfx(string id) => _server.UnloadSfx(id);

    public double AudioClockSeconds => _server.AudioClockSeconds;

    public void UpdateVolumes()
    {
        if (!IsInitialized || _server == null) return;
        _server.UpdateVolumes(Context.Player.Settings.MusicVolume, Context.Player.Settings.SoundEffectsVolume);
    }
}
