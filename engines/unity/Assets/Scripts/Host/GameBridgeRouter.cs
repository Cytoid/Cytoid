using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBridgeRouter
{
    private readonly GamePlayState sessionState;
    private GameLaunchSettings pendingSettings;

    public GameBridgeRouter(GamePlayState sessionState)
    {
        this.sessionState = sessionState;
    }

    public void Handle(string json)
    {
        CytoidGameCoreEnvelope envelope;
        try
        {
            envelope = CytoidGameCoreEnvelope.FromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameBridge] Failed to parse host envelope: {e.Message}");
            return;
        }

        if (envelope == null || string.IsNullOrEmpty(envelope.Type))
        {
            Debug.LogWarning("[GameBridge] Received host envelope without a type.");
            return;
        }

        if (envelope.Version != 1)
        {
            Debug.LogWarning($"[GameBridge] Unsupported envelope version: {envelope.Version}");
            return;
        }

        switch (envelope.Type)
        {
            case WireMessageTypes.BridgeStatus:
                HandleHostStatus(envelope);
                break;
            case WireMessageTypes.BridgePing:
                HandleHostPing(envelope);
                break;
            case WireMessageTypes.BridgePlayStart:
                HandleGameStart(envelope);
                break;
            case WireMessageTypes.BridgeSettingsUpdate:
                HandleSettingsUpdate(envelope);
                break;
            case WireMessageTypes.BridgePlayEnd:
                HandleSessionEnd(envelope);
                break;
            default:
                Debug.LogWarning($"[GameBridge] Unhandled host message type: {envelope.Type}");
                break;
        }
    }

    private void HandleHostPing(CytoidGameCoreEnvelope envelope)
    {
        // During gameplay, heartbeat pings must only receive game.pong — not another game.ready.
        if (!sessionState.HasActivePlay)
        {
            GameBridge.Instance?.ReannounceReadyToBridge();
        }

        var response = CytoidGameCoreEnvelope.Create(
            envelope.Id,
            WireMessageTypes.GamePong,
            envelope.Payload ?? new JObject());
        NativeBridgeMessenger.Send(response.ToJsonString());
    }

    private void HandleGameStart(CytoidGameCoreEnvelope envelope)
    {
        if (sessionState.HasActivePlay)
        {
            EmitRejectedGameStart(envelope.Id,
                $"Overlapping {WireMessageTypes.BridgePlayStart}: session {sessionState.ActivePlayId} is still active.");
            return;
        }

        if (envelope.Payload == null || envelope.Payload.Type == JTokenType.Null)
        {
            EmitRejectedGameStart(envelope.Id, $"{WireMessageTypes.BridgePlayStart} payload is required.");
            return;
        }

        var payload = envelope.Payload as JObject;
        if (payload == null)
        {
            EmitRejectedGameStart(envelope.Id, $"{WireMessageTypes.BridgePlayStart} payload must be an object.");
            return;
        }

        sessionState.SetActivePlay(envelope.Id);
        ApplyPendingSettings(payload);
        var launchJson = payload.ToString();
        Debug.Log($"[GameBridge] Starting play {envelope.Id}");
        GameLaunchBridge.StartGame(launchJson);
    }

    private async void HandleSessionEnd(CytoidGameCoreEnvelope envelope)
    {
        Debug.Log($"[GameBridge] {WireMessageTypes.BridgePlayEnd} received (id={envelope.Id}).");
        await GameBridge.ShowHandoffOverlay();
        var emittedCalibrationResult = AbortCurrentExternalGame();
        if (emittedCalibrationResult)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25));
        }
        sessionState.MarkPlayRouteEnded();
        EmitPlayRouteEnded(envelope.Id);
    }

    private void HandleSettingsUpdate(CytoidGameCoreEnvelope envelope)
    {
        var settings = envelope.Payload?.ToObject<GameLaunchSettings>();
        if (settings != null)
        {
            if (sessionState.HasActivePlay)
            {
                pendingSettings = settings;
                var calibrationSession = Context.GameState != null &&
                                         (Context.GameState.Mode == GameMode.Calibration ||
                                          Context.GameState.Mode == GameMode.GlobalCalibration);
                ExternalGameContentProvider.ApplySettings(settings, realtimeVolumeOnly: !calibrationSession);
            }
            else
            {
                pendingSettings = settings;
                ExternalGameContentProvider.ApplySettings(settings);
            }
        }

        var response = CytoidGameCoreEnvelope.Create(
            envelope.Id,
            WireMessageTypes.GameSettingsUpdated,
            new JObject {["applied"] = true});
        NativeBridgeMessenger.Send(response.ToJsonString());
    }

    private void HandleHostStatus(CytoidGameCoreEnvelope envelope)
    {
        var state = sessionState.HasActivePlay ? "busy" : sessionState.IsReadyForBridge ? "ready" : "starting";
        var payload = new JObject
        {
            ["state"] = state,
            ["engine"] = "unity"
        };
        if (sessionState.HasActivePlay)
        {
            payload["activePlayId"] = sessionState.ActivePlayId;
        }

        var response = CytoidGameCoreEnvelope.Create(envelope.Id, WireMessageTypes.GameStatus, payload);
        NativeBridgeMessenger.Send(response.ToJsonString());
    }

    private void ApplyPendingSettings(JObject payload)
    {
        if (pendingSettings == null) return;
        if (payload["settings"] == null || payload["settings"].Type == JTokenType.Null)
        {
            payload["settings"] = JObject.FromObject(pendingSettings);
        }
        ExternalGameContentProvider.ApplySettings(pendingSettings);
    }

    private static bool AbortCurrentExternalGame()
    {
        if (SceneManager.GetActiveScene().name != "Game") return false;
        var game = UnityEngine.Object.FindObjectOfType<Game>();
        if (game == null || !game.UsesExternalContent) return false;
        var emittedCalibrationResult = Context.GameState != null &&
                                       (Context.GameState.Mode == GameMode.Calibration ||
                                        Context.GameState.Mode == GameMode.GlobalCalibration);
        game.AbortExternalSession();
        return emittedCalibrationResult;
    }

    private static void EmitRejectedGameStart(string playId, string error)
    {
        Debug.LogWarning($"[GameBridge] Rejecting {WireMessageTypes.BridgePlayStart}: {error}");

        var payload = new GameResultPayload
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            completed = false,
            failed = true,
            error = error
        };

        var envelope = CytoidGameCoreEnvelope.Create(
            playId,
            WireMessageTypes.GamePlayResult,
            JObject.Parse(payload.ToJson()));
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    private static void EmitPlayRouteEnded(string playId)
    {
        var envelope = CytoidGameCoreEnvelope.Create(
            playId,
            WireMessageTypes.GamePlayEnded,
            new JObject {["ended"] = true});
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }
}
