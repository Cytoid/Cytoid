using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	/// <summary>This component will hook into every LeanTouch event, and spam the console with the information.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanTouchEvents")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Touch Events")]
	public class LeanTouchEvents : MonoBehaviour
	{
		protected virtual void OnEnable()
		{
			// Hook into the events we need
			LeanTouch.OnFingerDown   += HandleFingerDown;
			LeanTouch.OnFingerUpdate += HandleFingerUpdate;
			LeanTouch.OnFingerUp     += HandleFingerUp;
			LeanTouch.OnFingerTap    += HandleFingerTap;
			LeanTouch.OnFingerSwipe  += HandleFingerSwipe;
			LeanTouch.OnGesture      += HandleGesture;
		}

		protected virtual void OnDisable()
		{
			// Unhook the events
			LeanTouch.OnFingerDown   -= HandleFingerDown;
			LeanTouch.OnFingerUpdate -= HandleFingerUpdate;
			LeanTouch.OnFingerUp     -= HandleFingerUp;
			LeanTouch.OnFingerTap    -= HandleFingerTap;
			LeanTouch.OnFingerSwipe  -= HandleFingerSwipe;
			LeanTouch.OnGesture      -= HandleGesture;
		}

		public void HandleFingerDown(LeanFinger finger)
		{
			Debug.Log("Finger " + finger.Index + " began touching the screen");
		}

		public void HandleFingerUpdate(LeanFinger finger)
		{
			Debug.Log("Finger " + finger.Index + " is still touching the screen");
		}

		public void HandleFingerUp(LeanFinger finger)
		{
			Debug.Log("Finger " + finger.Index + " finished touching the screen");
		}

		public void HandleFingerTap(LeanFinger finger)
		{
			Debug.Log("Finger " + finger.Index + " tapped the screen");
		}

		public void HandleFingerSwipe(LeanFinger finger)
		{
			Debug.Log("Finger " + finger.Index + " swiped the screen");
		}

		public void HandleGesture(List<LeanFinger> fingers)
		{
			Debug.Log("Gesture with " + fingers.Count + " finger(s)");
			Debug.Log("    pinch scale: " + LeanGesture.GetPinchScale(fingers));
			Debug.Log("    twist degrees: " + LeanGesture.GetTwistDegrees(fingers));
			Debug.Log("    twist radians: " + LeanGesture.GetTwistRadians(fingers));
			Debug.Log("    screen delta: " + LeanGesture.GetScreenDelta(fingers));
		}
	}
}