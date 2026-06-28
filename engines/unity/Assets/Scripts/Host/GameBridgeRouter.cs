using System;
using System.Collections.Generic;
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

        if (envelope.Schema != CytoidGameCoreEnvelope.CurrentSchema)
        {
            Debug.LogWarning($"[GameBridge] Unsupported envelope schema: {envelope.Schema}");
            return;
        }

        switch (envelope.Type)
        {
            case WireMessageTypes.HealthCheck:
                HandleHealthCheck(envelope);
                break;
            case WireMessageTypes.SessionStart:
                HandleSessionStart(envelope);
                break;
            case WireMessageTypes.SessionCancel:
                HandleSessionCancel(envelope);
                break;
            case WireMessageTypes.SettingsApply:
                HandleSettingsApply(envelope);
                break;
            default:
                Debug.LogWarning($"[GameBridge] Unhandled host message type: {envelope.Type}");
                break;
        }
    }

    private void HandleHealthCheck(CytoidGameCoreEnvelope envelope)
    {
        var payload = new JObject
        {
            ["engine"] = "unity",
            ["generation"] = GameBridge.Generation,
            ["state"] = sessionState.HasActiveSession ? "busy" : sessionState.IsReadyForBridge ? "ready" : "starting"
        };
        if (sessionState.HasActiveSession)
        {
            payload["activeSessionId"] = sessionState.ActiveSessionId;
        }

        var response = CytoidGameCoreEnvelope.Create(envelope.Id, WireMessageTypes.HealthOk, payload);
        NativeBridgeMessenger.Send(response.ToJsonString());
    }

    private void HandleSessionStart(CytoidGameCoreEnvelope envelope)
    {
        if (sessionState.HasActiveSession)
        {
            EmitRejectedSessionStart(envelope.Id, "overlapping_session", $"Session {sessionState.ActiveSessionId} is still active.");
            return;
        }

        if (!(envelope.Payload is JObject payload))
        {
            EmitRejectedSessionStart(envelope.Id, "invalid_payload", "session.start payload must be an object.");
            return;
        }

        IGameContentProvider provider;
        try
        {
            ApplyPendingSettings(payload);
            provider = GameLaunchBridge.PrepareLaunchProvider(payload.ToString());
        }
        catch (Exception e)
        {
            EmitRejectedSessionStart(envelope.Id, "invalid_payload", e.Message);
            return;
        }

        sessionState.SetActiveSession(envelope.Id);
        if (provider is ExternalGameContentProvider externalProvider)
        {
            GameResultBridge.ActiveSessionRecordPlayEvents = externalProvider.Payload.settings?.recordPlayEvents;
        }

        EmitSessionStarted(envelope.Id, payload["mode"]?.Value<string>() ?? "ranked");
        GameLaunchBridge.LoadGameScene(provider).Forget();
    }

    private async void HandleSessionCancel(CytoidGameCoreEnvelope envelope)
    {
        if (!sessionState.HasActiveSession)
        {
            EmitEngineError(envelope.Id, "not_active", "No active session to cancel.");
            return;
        }
        if (envelope.Id != sessionState.ActiveSessionId)
        {
            EmitEngineError(envelope.Id, "unknown_session", $"Active session is {sessionState.ActiveSessionId}.");
            return;
        }

        var reason = envelope.Payload?["reason"]?.Value<string>() ?? "unknown";
        await GameBridge.ShowHandoffOverlay();
        AbortCurrentExternalGame(suppressCalibrationResult: true);
        sessionState.MarkPlayRouteEnded();
        GameResultBridge.EmitCancelled(envelope.Id, reason);
    }

    private void HandleSettingsApply(CytoidGameCoreEnvelope envelope)
    {
        var appliedFields = new List<string>();
        var deferredFields = new List<string>();
        var rejectedFields = new List<string>();
        var errors = new JArray();

        try
        {
            if (!(envelope.Payload is JObject settingsObj))
            {
                throw new ArgumentException("settings.apply payload must be an object.");
            }

            var settings = ExternalGameContentProvider.FlattenSettingsPatch(
                settingsObj,
                out appliedFields,
                out deferredFields,
                out rejectedFields);

            if (sessionState.HasActiveSession)
            {
                MoveDeferredFieldsForActiveSession(appliedFields, deferredFields);
                pendingSettings = settings;
                ExternalGameContentProvider.ApplySettings(settings, realtimeVolumeOnly: true);
            }
            else
            {
                pendingSettings = settings;
                ExternalGameContentProvider.ApplySettings(settings);
            }
        }
        catch (Exception e)
        {
            rejectedFields.Add("payload");
            errors.Add(BuildError("invalid_payload", e.Message));
        }

        var responsePayload = new JObject
        {
            ["applied"] = rejectedFields.Count == 0,
            ["appliedFields"] = JArray.FromObject(appliedFields),
            ["deferredFields"] = JArray.FromObject(deferredFields),
            ["rejectedFields"] = JArray.FromObject(rejectedFields),
            ["errors"] = errors
        };
        var response = CytoidGameCoreEnvelope.Create(envelope.Id, WireMessageTypes.SettingsApplied, responsePayload);
        NativeBridgeMessenger.Send(response.ToJsonString());
    }

    private void ApplyPendingSettings(JObject payload)
    {
        if (pendingSettings == null) return;
        if (payload["settings"] == null || payload["settings"].Type == JTokenType.Null)
        {
            return;
        }
        ExternalGameContentProvider.ApplySettings(pendingSettings);
    }

    private static void MoveDeferredFieldsForActiveSession(List<string> appliedFields, List<string> deferredFields)
    {
        for (var i = appliedFields.Count - 1; i >= 0; i--)
        {
            var field = appliedFields[i];
            if (field == "runtime.musicVolume" || field == "runtime.soundEffectsVolume") continue;
            appliedFields.RemoveAt(i);
            deferredFields.Add(field);
        }
    }

    private static void AbortCurrentExternalGame(bool suppressCalibrationResult)
    {
        if (SceneManager.GetActiveScene().name != "Game") return;
        var game = UnityEngine.Object.FindObjectOfType<Game>();
        if (game == null || !game.UsesExternalContent) return;
        game.AbortExternalSession(!suppressCalibrationResult);
    }

    private static void EmitSessionStarted(string sessionId, string mode)
    {
        var envelope = CytoidGameCoreEnvelope.Create(
            sessionId,
            WireMessageTypes.SessionStarted,
            new JObject
            {
                ["sessionId"] = sessionId,
                ["mode"] = mode,
                ["generation"] = GameBridge.Generation
            });
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    private static void EmitRejectedSessionStart(string sessionId, string code, string message)
    {
        GameResultBridge.EmitRejected(sessionId, code, message);
    }

    private static void EmitEngineError(string id, string code, string message)
    {
        var envelope = CytoidGameCoreEnvelope.Create(
            id,
            WireMessageTypes.EngineError,
            new JObject { ["error"] = BuildError(code, message) });
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    private static JObject BuildError(string code, string message)
    {
        return new JObject
        {
            ["code"] = code,
            ["message"] = message
        };
    }
}
