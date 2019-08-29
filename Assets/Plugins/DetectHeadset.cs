using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class DetectHeadset {

	[DllImport ("__Internal")]
	private static extern bool _Detect();

	public static bool Detect() {
		try
		{
#if UNITY_IOS && !UNITY_EDITOR
			return _Detect();
#elif UNITY_ANDROID && !UNITY_EDITOR

			using (var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {

				using (var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {

					using (var androidPlugin =
 new AndroidJavaObject("com.davikingcode.DetectHeadset.DetectHeadset", currentActivity)) {

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
