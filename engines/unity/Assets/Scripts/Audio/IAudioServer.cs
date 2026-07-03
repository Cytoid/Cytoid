using UnityEngine;

/// <summary>
/// Complete audio backend abstraction. Each implementation is a user-selectable
/// audio server that handles both music and sound effects.
/// </summary>
public interface IAudioServer
{
    void Initialize();
    void Dispose();

    // Music
    IMusicTrack LoadMusic(string id, AudioClip clip, bool isResource);
    void UnloadMusic();
    void SetMusicVolume(float volume);

    // Sound effects
    ISoundEffect LoadSfx(string id, AudioClip clip, bool isResource, bool isPreloaded = false);
    ISoundEffect GetSfx(string id);
    void UnloadSfx(string id);
    bool IsSfxLoaded(string id);
    void SetSfxVolume(float volume);

    // Both
    void UpdateVolumes(float musicVolume, float sfxVolume);

    /// <summary>The audio engine's internal clock in seconds, for scheduling alignment.</summary>
    double AudioClockSeconds { get; }
}