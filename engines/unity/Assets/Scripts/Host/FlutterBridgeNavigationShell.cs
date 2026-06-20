using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hides standalone debug Navigation UI while Flutter host initializes Context on the Navigation scene.
/// </summary>
public static class FlutterBridgeNavigationShell
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (!GameEmbedMode.IsBridgeEmbedded)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Navigation")
        {
            return;
        }

        Apply();
    }

    public static void Apply()
    {
        var canvas = GameObject.Find("Debug Navigation Canvas");
        if (canvas != null)
        {
            canvas.SetActive(false);
        }

        var camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }
    }
}
