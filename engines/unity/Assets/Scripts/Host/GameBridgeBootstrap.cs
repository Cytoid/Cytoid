using UnityEngine;

public static class GameBridgeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureHostBridge()
    {
        if (UnityEngine.Object.FindObjectOfType<GameBridge>() != null)
        {
            return;
        }

        var hostBridgeObject = new GameObject("GameBridge");
        hostBridgeObject.AddComponent<GameBridge>();
    }
}
