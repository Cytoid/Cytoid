using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameBridge : MonoBehaviour
{
    private static GameBridge instance;
    private static Canvas handoffCanvas;
    private static CanvasGroup handoffCanvasGroup;

    public static GameBridge Instance => instance;

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
            return;
        }

        sessionState.IsReadyForBridge = true;

        SyncTargetFrameRateToDisplay();

        var payload = new JObject
        {
            ["initialized"] = true,
            ["engine"] = "unity",
            ["engineVersion"] = Application.unityVersion,
            ["targetFrameRate"] = Application.targetFrameRate,
            ["screenRefreshRate"] = GetScreenRefreshRateHz()
        };

        var envelope = CytoidGameCoreEnvelope.Create(Guid.NewGuid().ToString(), WireMessageTypes.GameReady, payload);
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    internal void ReannounceReadyToBridge()
    {
        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            return;
        }

        SyncTargetFrameRateToDisplay();

        var payload = new JObject
        {
            ["initialized"] = true,
            ["engine"] = "unity",
            ["engineVersion"] = Application.unityVersion,
            ["targetFrameRate"] = Application.targetFrameRate,
            ["screenRefreshRate"] = GetScreenRefreshRateHz()
        };

        var envelope = CytoidGameCoreEnvelope.Create(Guid.NewGuid().ToString(), WireMessageTypes.GameReady, payload);
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    internal async UniTask EndActivePlayFromGame()
    {
        if (!GameEmbedMode.IsBridgeEmbedded || sessionState == null || !sessionState.HasActivePlay)
        {
            return;
        }

        await ShowHandoffOverlay();
        var playId = sessionState.ActivePlayId;
        sessionState.MarkPlayRouteEnded();
        var envelope = CytoidGameCoreEnvelope.Create(
            playId,
            WireMessageTypes.GamePlayEnded,
            new JObject {["ended"] = true});
        NativeBridgeMessenger.Send(envelope.ToJsonString());
    }

    private async void OnGameResultJson(string resultJson)
    {
        JObject payload;
        try
        {
            payload = JObject.Parse(resultJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameBridge] Failed to parse game result JSON: {e.Message}");
            return;
        }

        await ShowHandoffOverlay();
        var playId = sessionState.ActivePlayId ?? Guid.NewGuid().ToString();
        var envelope = CytoidGameCoreEnvelope.Create(playId, WireMessageTypes.GamePlayResult, payload);
        NativeBridgeMessenger.Send(envelope.ToJsonString());
        sessionState.ClearActivePlay();
    }

    internal static async UniTask ShowHandoffOverlay()
    {
        EnsureHandoffOverlay();
        handoffCanvas.enabled = true;
        handoffCanvasGroup.blocksRaycasts = true;
        handoffCanvasGroup.interactable = true;
        handoffCanvasGroup.DOKill();
        handoffCanvasGroup.DOFade(1, 0.2f).SetEase(Ease.OutCubic);
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
    }

    internal static void HideHandoffOverlay()
    {
        EnsureHandoffOverlay();
        handoffCanvasGroup.blocksRaycasts = false;
        handoffCanvasGroup.interactable = false;
        handoffCanvasGroup.DOKill();
        handoffCanvasGroup.DOFade(0, 0.25f).SetEase(Ease.OutCubic);
        Run.After(0.25f, () =>
        {
            if (handoffCanvasGroup != null && handoffCanvasGroup.alpha <= 0.01f)
            {
                handoffCanvas.enabled = false;
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
