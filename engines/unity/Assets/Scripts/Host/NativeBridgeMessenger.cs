using System.Runtime.InteropServices;
using UnityEngine;

public static class NativeBridgeMessenger
{
    private const string AndroidCallbackClass =
        "org.cytoid.gamecore.UnityHostCallback";

    private static AndroidJavaClass androidCallbackClass;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CytoidHostNative_SendMessage(string json);
#endif

    public static void Send(string json)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            androidCallbackClass ??= new AndroidJavaClass(AndroidCallbackClass);
            androidCallbackClass.CallStatic("onMessage", json);
        }
        catch (AndroidJavaException exception)
        {
            Debug.LogError($"[NativeBridgeMessenger] Failed to forward message to Flutter: {exception.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        try
        {
            CytoidHostNative_SendMessage(json);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[NativeBridgeMessenger] Failed to forward message to Flutter: {exception.Message}");
        }
#else
        Debug.Log($"[NativeBridgeMessenger] {json}");
#endif
    }
}
