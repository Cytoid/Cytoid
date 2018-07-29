using System.Runtime.InteropServices;
using UnityEngine;

public class Headset
{
    [DllImport("__Internal")]
    private static extern bool DetectHeadset();

    public static bool Detect()
    {
        #if UNITY_EDITOR
        return false;
        #endif
#if UNITY_IOS
        return DetectHeadset();
#elif UNITY_ANDROID && !UNITY_EDITOR
		using (var javaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (var activityClass = javaClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                return activityClass.Call<bool>("DetectHeadset");
            }
        }	
#else
        return false;
#endif
    }
}