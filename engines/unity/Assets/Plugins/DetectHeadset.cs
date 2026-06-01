using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class DetectHeadset {

#if UNITY_IOS && !UNITY_EDITOR
	[DllImport ("__Internal")]
	private static extern bool _Detect();
#endif

	public static bool Detect() {
		try
		{
#if UNITY_IOS && !UNITY_EDITOR
			return _Detect();
#elif UNITY_ANDROID && !UNITY_EDITOR

			using (var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {

				using (var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {

					using (var androidPlugin =
 new AndroidJavaObject("me.tigerhix.cytoid.DetectHeadset", currentActivity)) {

						return androidPlugin.Call<bool>("_Detect");
					}
				}
			}
#else
			return false;
#endif
		}
		catch (Exception e)
		{
			Debug.LogWarning("Could not detect headset");
			Debug.LogWarning(e);
			return false;
		}
	}
	
}
