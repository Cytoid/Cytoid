using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class GameLogPayload
{
    public string level;
    public string message;
    public string stackTrace;
    public long timestamp;
    public string sessionId;

    public static GameLogPayload Create(string level, string message, string stackTrace, string sessionId)
    {
        return new GameLogPayload
        {
            level = level,
            message = message,
            stackTrace = string.IsNullOrEmpty(stackTrace) ? null : stackTrace,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            sessionId = string.IsNullOrEmpty(sessionId) ? null : sessionId
        };
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
    }
}

[Serializable]
public class GameLogBatchPayload
{
    public string reason;
    public string triggerLevel;
    public long timestamp;
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
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            truncated = truncated,
            logs = logs ?? new List<GameLogPayload>()
        };
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
    }
}
