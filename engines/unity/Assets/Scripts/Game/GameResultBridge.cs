using System;
using UnityEngine;
using UnityEngine.Events;

public static class GameResultBridge
{
    public static readonly UnityEvent<string> OnResultJson = new UnityEvent<string>();

    public static string LastResultJson { get; private set; } = "null";

    public static void Emit(GameState state, TierPlaySession tierPlaySession = null, string error = null)
    {
        var payload = GameResultPayload.FromGameState(state, tierPlaySession, error);
        LastResultJson = payload.ToJson();
        Debug.Log($"[GameResultBridge] {LastResultJson}");
        OnResultJson.Invoke(LastResultJson);
    }

    public static void EmitTierRetry(TierPlaySession tierPlaySession)
    {
        var payload = new GameResultPayload
        {
            completed = false,
            failed = false,
            usedAutoMod = false,
            gameMode = GameMode.Tier.ToString(),
            tierRetry = tierPlaySession?.TierId,
            timestamp = DateTimeOffset.UtcNow.ToString("o")
        };
        LastResultJson = payload.ToJson();
        Debug.Log($"[GameResultBridge] {LastResultJson}");
        OnResultJson.Invoke(LastResultJson);
    }

    public static void EmitError(Exception exception)
    {
        var payload = new GameResultPayload
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            completed = false,
            failed = true,
            error = exception.ToString()
        };
        LastResultJson = payload.ToJson();
        Debug.LogError($"[GameResultBridge] {LastResultJson}");
        OnResultJson.Invoke(LastResultJson);
    }

    public static bool HasAutoMod(GameState state)
    {
        return state.Mods.Contains(Mod.Auto) ||
               state.Mods.Contains(Mod.AutoDrag) ||
               state.Mods.Contains(Mod.AutoHold) ||
               state.Mods.Contains(Mod.AutoFlick);
    }
}
