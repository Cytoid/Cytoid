/// <summary>
/// A handle to a playing or loaded music track. Obtained from <see cref="IAudioServer.LoadMusic"/>.
/// </summary>
public interface IMusicTrack
{
    /// <summary>Schedules playback after a delay. Returns the scheduled start time on the audio clock.</summary>
    double SchedulePlay(double delaySeconds);

    /// <summary>The scheduled start time on the audio clock, from SchedulePlay's return value.</summary>
    double ScheduledStartTime { get; }

    /// <summary>Current playback position in seconds. For editor scrubbing and completion detection only — NOT the game clock.</summary>
    float SourceTimeSeconds { get; set; }

    float Length { get; }
    float Volume { get; set; }
    bool IsPlaying { get; }
    bool IsFinished { get; }

    void Pause();
    void Resume();
    void Stop();
    void Unload();
}