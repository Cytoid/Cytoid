using System;
using E7.Native;
using UnityEngine;

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

        if (audioSources.Length != 7)
            throw new InvalidOperationException(
                $"Expected 7 AudioSources (1 music + 6 SFX), got {audioSources.Length}");

        var serverType = Context.Player.Settings.AudioServer;
        if (serverType == AudioServerType.Exceed7 && !NativeAudio.OnSupportedPlatform)
        {
            Debug.LogWarning("[Audio] Exceed7 not supported on this platform; falling back to Unity");
            serverType = AudioServerType.Unity;
        }

        var server = serverType switch
        {
            AudioServerType.Exceed7 => new Exceed7AudioServer(audioSources[0], audioSources[1..]),
            _ => new UnityAudioServer(audioSources[0], audioSources[1..]),
        };
        server.Initialize();

        foreach (var clip in preloadedAudioClips)
            server.LoadSfx(clip.name, clip, isResource: false, isPreloaded: true);

        _server = server;
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

    private IAudioServer Server =>
        _server ?? throw new InvalidOperationException("AudioManager not initialized");

    public IMusicTrack LoadMusic(string id, AudioClip clip, bool isResource)
        => Server.LoadMusic(id, clip, isResource);

    public ISoundEffect LoadSfx(string id, AudioClip clip, bool isResource, bool isPreloaded = false)
        => Server.LoadSfx(id, clip, isResource, isPreloaded);

    public ISoundEffect GetSfx(string id) => Server.GetSfx(id);

    public bool IsSfxLoaded(string id) => Server.IsSfxLoaded(id);

    public void UnloadMusic() => Server.UnloadMusic();

    public void UnloadSfx(string id) => Server.UnloadSfx(id);

    public double AudioClockSeconds => Server.AudioClockSeconds;

    public void UpdateVolumes()
    {
        if (!IsInitialized || _server == null) return;
        _server.UpdateVolumes(Context.Player.Settings.MusicVolume, Context.Player.Settings.SoundEffectsVolume);
    }
}
