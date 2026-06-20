using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GameLogBridge : MonoBehaviour
{
    private const int MaxMessageLength = 4096;
    private const int MaxBufferedLogs = 500;
    private const int MaxBatchEntries = 120;
    private const int BatchSizeThreshold = 50;
    private const float BatchIntervalSeconds = 2f;

    private static readonly HashSet<string> IgnoredPrefixes = new HashSet<string>
    {
        "[NativeBridgeMessenger]",
        "[GameLogBridge]"
    };

    private GamePlayState sessionState;
    private readonly Queue<GameLogPayload> bufferedLogs = new Queue<GameLogPayload>();
    private readonly object queueLock = new object();
    private float nextIntervalFlushTime = -1f;
    private bool batchTruncated;

    public void Initialize(GamePlayState state)
    {
        sessionState = state;
    }

    private void OnEnable()
    {
        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            return;
        }

        if (ShouldIgnoreMessage(message))
        {
            return;
        }

        var bufferedCount = BufferLog(message, stackTrace, type);

        if (ShouldFlushImmediately(type))
        {
            EmitLogBatch("trigger", type);
            return;
        }

        if (bufferedCount >= BatchSizeThreshold)
        {
            EmitLogBatch("count", type);
        }
    }

    private void LateUpdate()
    {
        if (nextIntervalFlushTime < 0f || Time.unscaledTime < nextIntervalFlushTime)
        {
            return;
        }

        EmitLogBatch("interval", LogType.Log);
    }

    private static bool ShouldIgnoreMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return true;
        }

        foreach (var prefix in IgnoredPrefixes)
        {
            if (message.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private int BufferLog(string message, string stackTrace, LogType type)
    {
        var payload = CreatePayload(message, stackTrace, type);
        lock (queueLock)
        {
            if (bufferedLogs.Count == 0)
            {
                nextIntervalFlushTime = Time.unscaledTime + BatchIntervalSeconds;
            }

            bufferedLogs.Enqueue(payload);
            while (bufferedLogs.Count > MaxBufferedLogs)
            {
                bufferedLogs.Dequeue();
                batchTruncated = true;
            }
            return bufferedLogs.Count;
        }
    }

    private static bool ShouldFlushImmediately(LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                return true;
            default:
                return false;
        }
    }

    private void EmitLogBatch(string reason, LogType triggerType)
    {
        List<GameLogPayload> snapshot;
        bool truncated;
        lock (queueLock)
        {
            snapshot = new List<GameLogPayload>(bufferedLogs);
            bufferedLogs.Clear();
            nextIntervalFlushTime = -1f;
            truncated = batchTruncated || snapshot.Count > MaxBatchEntries;
            batchTruncated = false;
            if (truncated)
            {
                snapshot = snapshot.GetRange(snapshot.Count - MaxBatchEntries, MaxBatchEntries);
            }
        }

        if (snapshot.Count == 0)
        {
            return;
        }

        var payload = GameLogBatchPayload.Create(reason, MapLevel(triggerType), truncated, snapshot);
        var envelope = CytoidGameCoreEnvelope.Create(
            Guid.NewGuid().ToString(),
            WireMessageTypes.GameLogsBatch,
            JObject.Parse(payload.ToJson()));
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    private GameLogPayload CreatePayload(string message, string stackTrace, LogType type)
    {
        if (message != null && message.Length > MaxMessageLength)
        {
            message = message.Substring(0, MaxMessageLength);
        }

        if (stackTrace != null && stackTrace.Length > MaxMessageLength)
        {
            stackTrace = stackTrace.Substring(0, MaxMessageLength);
        }

        var playId = sessionState != null && sessionState.HasActivePlay
            ? sessionState.ActivePlayId
            : null;

        return GameLogPayload.Create(MapLevel(type), message, stackTrace, playId);
    }

    private static string MapLevel(LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
                return "warning";
            case LogType.Error:
            case LogType.Assert:
                return "error";
            case LogType.Exception:
                return "exception";
            default:
                return "log";
        }
    }
}
