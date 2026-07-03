/// <summary>
/// A handle to a loaded sound effect. Obtained from <see cref="IAudioServer.LoadSfx"/> or <see cref="IAudioServer.GetSfx"/>.
/// </summary>
public interface ISoundEffect
{
    /// <param name="ignoreListenerPause">If true, plays even when AudioListener.pause is true (for UI sounds in paused menus).</param>
    void Play(bool ignoreListenerPause = false);

    float Volume { get; set; }
    bool IsPlaying { get; }
    void Unload();
}