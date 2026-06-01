using UnityEngine;

/// <summary>
/// Minimal Flutter-host entry scene. It initializes Context without loading Navigation;
/// GameLaunchBridge loads the Game scene after a bridge.play.start envelope arrives.
/// </summary>
public class CoreHostBootstrap : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 120;
        Debug.Log("[CoreHostBootstrap] Waiting for CytoidGameCore bridge.play.start.");
    }
}
