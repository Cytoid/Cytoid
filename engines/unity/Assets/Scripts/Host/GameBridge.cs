using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameBridge : MonoBehaviour
{
    private static GameBridge instance;
    private static Canvas handoffCanvas;
    private static CanvasGroup handoffCanvasGroup;

    public static GameBridge Instance => instance;
    public static readonly UnityEvent<string> OnTelemetryJson = new UnityEvent<string>();
    public static int Generation { get; private set; }
    internal static string ActiveSessionId => instance?.sessionState?.ActiveSessionId;

    private GamePlayState sessionState;
    private GameBridgeRouter router;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.name = "GameBridge";
        DontDestroyOnLoad(gameObject);

        sessionState = new GamePlayState();
        router = new GameBridgeRouter(sessionState);
        var logBridge = gameObject.AddComponent<GameLogBridge>();
        logBridge.Initialize(sessionState);

        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            Debug.Log(
                "[GameBridge] Standalone debug mode — CytoidGameCore protocol inactive. "
                + "Test bridge ↔ game messaging via engines/unity/flutter_plugin/example with plugin artifacts.");
            return;
        }

        Debug.Log("[GameBridge] Bridge-embedded mode active.");
        FlutterBridgeNavigationShell.Apply();
        OnTelemetryJson.AddListener(OnTelemetryJsonReceived);
        GameResultBridge.OnResultJson.AddListener(OnGameResultJson);
        Context.OnApplicationInitialized.AddListener(SendReadyToBridge);

        if (Context.IsInitialized)
        {
            SendReadyToBridge();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            return;
        }

        OnTelemetryJson.RemoveListener(OnTelemetryJsonReceived);
        GameResultBridge.OnResultJson.RemoveListener(OnGameResultJson);
        Context.OnApplicationInitialized.RemoveListener(SendReadyToBridge);
    }

    public void OnBridgeMessage(string json)
    {
        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            Debug.LogWarning("[GameBridge] Ignoring host message in standalone debug mode.");
            return;
        }

        router.Handle(json);
    }

    private void SendReadyToBridge()
    {
        if (sessionState.IsReadyForBridge)
        {
            Debug.Log("[CYTOID-DBG] SendReadyToBridge: skipped (already ready)");
            return;
        }

        sessionState.IsReadyForBridge = true;
        Generation++;
        Debug.Log("[CYTOID-DBG] SendReadyToBridge: sending engine.ready envelope");
        SendEngineReadyEnvelope();
    }

    internal void ReannounceReadyToBridge()
    {
        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            return;
        }

        Debug.Log("[CYTOID-DBG] ReannounceReadyToBridge: re-sending engine.ready without generation bump");
        SendEngineReadyEnvelope();
    }

    internal async UniTask EndActivePlayFromGame()
    {
        if (!GameEmbedMode.IsBridgeEmbedded || sessionState == null || !sessionState.HasActiveSession)
        {
            Debug.Log($"[CYTOID-DBG] EndActivePlayFromGame: skipped (HasActiveSession={sessionState?.HasActiveSession})");
            return;
        }

        Debug.Log($"[CYTOID-DBG] EndActivePlayFromGame ENTER: ActiveSessionId={sessionState.ActiveSessionId}");
        try
        {
            await ShowHandoffOverlay();
            var sessionId = sessionState.ActiveSessionId;
            sessionState.MarkPlayRouteEnded();
            Debug.Log($"[CYTOID-DBG] EndActivePlayFromGame: MarkPlayRouteEnded cleared, sessionId={sessionId}");
            // Engine-initiated abort has no v2 route-ended primitive; report a terminal cancelled result.
            GameResultBridge.EmitCancelled(sessionId, "unknown");
        }
        finally
        {
            GamePlayEventRecorder.End();
        }
    }

    private static void OnTelemetryJsonReceived(string telemetryJson)
    {
        NativeBridgeMessenger.Send(telemetryJson);
    }

    private async void OnGameResultJson(string resultJson)
    {
        try
        {
            Debug.Log($"[CYTOID-DBG] OnGameResultJson ENTER: HasActiveSession={sessionState.HasActiveSession} ActiveSessionId={sessionState.ActiveSessionId}");
            Debug.Log("[CYTOID-DBG] OnGameResultJson: BEFORE await ShowHandoffOverlay (0.2s window starts)");
            await ShowHandoffOverlay();
            Debug.Log($"[CYTOID-DBG] OnGameResultJson: AFTER await ShowHandoffOverlay — HasActiveSession still={sessionState.HasActiveSession} (race window ends)");
            NativeBridgeMessenger.Send(resultJson);
            Debug.Log("[CYTOID-DBG] OnGameResultJson: BEFORE ClearActiveSession");
            sessionState.ClearActiveSession();
            GameResultBridge.ActiveSessionRecordPlayEvents = null;
            Debug.Log($"[CYTOID-DBG] OnGameResultJson EXIT: HasActiveSession={sessionState.HasActiveSession}");
        }
        finally
        {
            GamePlayEventRecorder.End();
        }
    }

    private static void SendEngineReadyEnvelope()
    {
        SyncTargetFrameRateToDisplay();
        var payload = new JObject
        {
            ["engine"] = "unity",
            ["engineVersion"] = Application.unityVersion,
            ["generation"] = Generation,
            ["display"] = new JObject
            {
                ["targetFrameRate"] = Application.targetFrameRate,
                ["screenRefreshRate"] = GetScreenRefreshRateHz()
            }
        };

        var envelope = CytoidGameCoreEnvelope.Create($"ready-{Generation}", WireMessageTypes.EngineReady, payload);
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    internal static async UniTask ShowHandoffOverlay()
    {
        EnsureHandoffOverlay();
        Debug.Log($"[CYTOID-DBG] ShowHandoffOverlay: alpha before={handoffCanvasGroup.alpha} enabled={handoffCanvas.enabled}");
        handoffCanvas.enabled = true;
        handoffCanvasGroup.blocksRaycasts = true;
        handoffCanvasGroup.interactable = true;
        handoffCanvasGroup.DOKill();
        handoffCanvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        Debug.Log($"[CYTOID-DBG] ShowHandoffOverlay: alpha after={handoffCanvasGroup.alpha}");
    }

    internal static void HideHandoffOverlay()
    {
        EnsureHandoffOverlay();
        Debug.Log($"[CYTOID-DBG] HideHandoffOverlay: alpha before={handoffCanvasGroup.alpha} enabled={handoffCanvas.enabled}");
        handoffCanvasGroup.blocksRaycasts = false;
        handoffCanvasGroup.interactable = false;
        handoffCanvasGroup.DOKill();
        handoffCanvasGroup.DOFade(0, 0.25f).SetEase(Ease.OutCubic);
        Run.After(0.25f, () =>
        {
            if (handoffCanvasGroup != null && handoffCanvasGroup.alpha <= 0.01f)
            {
                handoffCanvas.enabled = false;
                Debug.Log("[CYTOID-DBG] HideHandoffOverlay: canvas disabled (faded out fully)");
            }
            else
            {
                Debug.Log($"[CYTOID-DBG] HideHandoffOverlay: deferred check alpha={handoffCanvasGroup?.alpha} (kept visible — race with ShowHandoffOverlay!)");
            }
        });
    }

    private static void EnsureHandoffOverlay()
    {
        if (handoffCanvasGroup != null)
        {
            return;
        }

        var overlayObject = new GameObject("GameBridgeHandoffOverlay");
        DontDestroyOnLoad(overlayObject);

        handoffCanvas = overlayObject.AddComponent<Canvas>();
        handoffCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        handoffCanvas.overrideSorting = true;
        handoffCanvas.sortingOrder = 32000;
        handoffCanvas.enabled = false;

        handoffCanvasGroup = overlayObject.AddComponent<CanvasGroup>();
        handoffCanvasGroup.alpha = 0;
        handoffCanvasGroup.blocksRaycasts = false;
        handoffCanvasGroup.interactable = false;

        var imageObject = new GameObject("Black");
        imageObject.transform.SetParent(overlayObject.transform, false);
        var image = imageObject.AddComponent<Image>();
        image.color = Color.black;

        var rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SyncTargetFrameRateToDisplay()
    {
        var refreshRate = Mathf.RoundToInt(GetScreenRefreshRateHz());
        if (refreshRate <= 0)
        {
            return;
        }

        Application.targetFrameRate = Mathf.Max(Application.targetFrameRate, refreshRate);
        Debug.Log($"[GameBridge] Synced targetFrameRate to {Application.targetFrameRate} (display {refreshRate}Hz).");
    }

    private static float GetScreenRefreshRateHz()
    {
        var refreshRateRatio = UnityEngine.Screen.currentResolution.refreshRateRatio;
        if (refreshRateRatio.denominator <= 0)
        {
            return 0f;
        }

        return (float)refreshRateRatio.numerator / refreshRateRatio.denominator;
    }
}
