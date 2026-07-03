/// <summary>
/// A handle to a playing or loaded music track. Obtained from <see cref="IAudioServer.LoadMusic"/>.
/// </summary>
public interface IMusicTrack
{
    /// <summary>Schedules playback after a delay. Returns the scheduled start time on the audio clock.</summary>
    double SchedulePlay(double delaySeconds);

    /// <summary>
    /// Stop, seek to <paramref name="sourceTimeSeconds"/>, and resume playback.
    /// Equivalent to Stop + SourceTimeSeconds = value + SchedulePlay, but atomic.
    /// Returns the effective audio clock start time (scheduledStart - sourceTimeSeconds),
    /// so that <c>AudioSettings.dspTime - returnValue</c> yields correct logical elapsed time.
    /// Used by Lab timeline seek/resync.
    /// </summary>
    double PlayFrom(float sourceTimeSeconds, double delaySeconds = 0);

    /// <summary>
    /// The logical music start time on the audio clock, from SchedulePlay or PlayFrom's return value.
    /// For PlayFrom, this is <c>scheduledStart - sourceTimeSeconds</c> so that
    /// <c>AudioSettings.dspTime - ScheduledStartTime</c> yields correct logical elapsed time.
    /// </summary>
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