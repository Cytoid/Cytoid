using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class DeepLinkListener : SingletonMonoBehavior<DeepLinkListener>
{
        
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void DeepLinkReceiverIsAlive();
#endif
    
    public DeepLinkReceivedEvent OnDeepLinkReceived { get; } = new DeepLinkReceivedEvent();

    private void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        DeepLinkReceiverIsAlive(); // Let the App Controller know it's ok to call URLOpened now.
#endif
        if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
        {
            using (var javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (var activityClass = javaClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    var deepLink = activityClass.Call<string>("consumeDeepLink");
                    if (deepLink != null)
                    {
                        URLOpened(deepLink);
                    }
                }
            }
        }
    }

    public void URLOpened(string url)
    {
        if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
        {
            using (var javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (var activityClass = javaClass.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activityClass.Call<string>("consumeDeepLink");
                }
            }
        }
        Debug.Log($"URL received: {url}");
        OnDeepLinkReceived.Invoke(url);
    }
    
}

public class DeepLinkReceivedEvent : UnityEvent<string>
{
}