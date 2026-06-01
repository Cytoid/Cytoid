using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class GameLogPayload
{
    public string level;
    public string message;
    public string stackTrace;
    public string timestamp;
    public string playId;

    public static GameLogPayload Create(string level, string message, string stackTrace, string playId)
    {
        return new GameLogPayload
        {
            level = level,
            message = message,
            stackTrace = string.IsNullOrEmpty(stackTrace) ? null : stackTrace,
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            playId = string.IsNullOrEmpty(playId) ? null : playId
        };
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

[Serializable]
public class GameLogBatchPayload
{
    public string reason;
    public string triggerLevel;
    public string timestamp;
    public bool truncated;
    public List<GameLogPayload> logs;

    public static GameLogBatchPayload Create(
        string reason,
        string triggerLevel,
        bool truncated,
        List<GameLogPayload> logs)
    {
        return new GameLogBatchPayload
        {
            reason = reason,
            triggerLevel = triggerLevel,
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            truncated = truncated,
            logs = logs ?? new List<GameLogPayload>()
        };
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}
