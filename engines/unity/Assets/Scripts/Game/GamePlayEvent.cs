using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GamePlayEvent
{
    public int t;
    public int f;
    public string p;
    public int x;
    public int y;
}

public static class GamePlayEventRecorder
{
    private const float MoveSampleIntervalSeconds = 1f / 60f;
    private const int MoveSampleDistance = 96;

    private static readonly List<GamePlayEvent> events = new List<GamePlayEvent>(4096);
    private static readonly Dictionary<int, GamePlayEvent> lastEventsByFinger = new Dictionary<int, GamePlayEvent>();

    private static Game game;
    private static bool isSubscribed;
    private static bool isRecording;

    public static void Begin(Game currentGame)
    {
        game = currentGame;
        events.Clear();
        lastEventsByFinger.Clear();
        isRecording = true;
        Subscribe();
    }

    /// <summary>
    /// Temporarily stops sampling events (e.g. while the game is paused)
    /// without releasing the recorder. Finger tracking state is cleared so
    /// the next down/move after resume is not compared against a stale
    /// sample taken before the pause.
    /// </summary>
    public static void Suspend()
    {
        if (!isRecording) return;
        isRecording = false;
        lastEventsByFinger.Clear();
    }

    /// <summary>Resumes sampling after <see cref="Suspend"/>.</summary>
    public static void Resume()
    {
        if (game == null) return;
        lastEventsByFinger.Clear();
        isRecording = true;
    }

    public static GamePlayEvent[] Snapshot()
    {
        return events.ToArray();
    }

    public static GamePlayEvent[] SnapshotAsWireObjects()
    {
        return Snapshot();
    }

    public static void End()
    {
        isRecording = false;
        game = null;
        events.Clear();
        lastEventsByFinger.Clear();
        Unsubscribe();
    }

    private static void Subscribe()
    {
        if (isSubscribed) return;
        GameTouchInput.FingerDown += OnFingerDown;
        GameTouchInput.FingerUpdate += OnFingerUpdate;
        GameTouchInput.FingerUp += OnFingerUp;
        isSubscribed = true;
    }

    private static void Unsubscribe()
    {
        if (!isSubscribed) return;
        GameTouchInput.FingerDown -= OnFingerDown;
        GameTouchInput.FingerUpdate -= OnFingerUpdate;
        GameTouchInput.FingerUp -= OnFingerUp;
        isSubscribed = false;
    }

    private static void OnFingerDown(GameFinger finger)
    {
        Record(finger, "down", true);
    }

    private static void OnFingerUpdate(GameFinger finger)
    {
        Record(finger, "move", false);
    }

    private static void OnFingerUp(GameFinger finger)
    {
        Record(finger, "up", true);
        lastEventsByFinger.Remove(finger.Index);
    }

    private static void Record(GameFinger finger, string phase, bool force)
    {
        if (!isRecording || game == null || game.State == null || !game.State.IsStarted) return;

        var next = new GamePlayEvent
        {
            t = Mathf.Max(0, Mathf.RoundToInt(game.Time * 1000f)),
            f = finger.Index,
            p = phase,
            x = Normalize(finger.ScreenPosition.x, UnityEngine.Screen.width),
            y = Normalize(finger.ScreenPosition.y, UnityEngine.Screen.height)
        };

        if (!force && !ShouldSampleMove(next)) return;

        events.Add(next);
        lastEventsByFinger[finger.Index] = next;
    }

    private static bool ShouldSampleMove(GamePlayEvent next)
    {
        if (!lastEventsByFinger.TryGetValue(next.f, out var last)) return true;
        if (next.t - last.t >= MoveSampleIntervalSeconds * 1000f) return true;

        var dx = next.x - last.x;
        var dy = next.y - last.y;
        return dx * dx + dy * dy >= MoveSampleDistance * MoveSampleDistance;
    }

    private static int Normalize(float value, int max)
    {
        if (max <= 0) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(value / max * 65535f), 0, 65535);
    }
}
