using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	/// <summary>This component tells you when a finger finishes touching the screen. The finger must begin touching the screen with the specified the specified conditions for it to be considered.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerUp")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Finger Up")]
	public class LeanFingerUp : LeanFingerDown
	{
		/// <summary>Ignore fingers with OverGui?</summary>
		public bool IgnoreIsOverGui { set { ignoreIsOverGui = value; } get { return ignoreIsOverGui; } } [SerializeField] private bool ignoreIsOverGui;

		private List<LeanFinger> fingers = new List<LeanFinger>();

		protected override void OnEnable()
		{
			LeanTouch.OnFingerDown += HandleFingerDown;
			LeanTouch.OnFingerUp   += HandleFingerUp;
		}

		protected override void OnDisable()
		{
			LeanTouch.OnFingerDown -= HandleFingerDown;
			LeanTouch.OnFingerUp   -= HandleFingerUp;
		}

		protected override bool UseFinger(LeanFinger finger)
		{
			if (ignoreIsOverGui == true && finger.IsOverGui == true)
			{
				return false;
			}

			return base.UseFinger(finger);
		}

		protected override void HandleFingerDown(LeanFinger finger)
		{
			if (UseFinger(finger) == true)
			{
				fingers.Add(finger);
			}
		}

		protected virtual void HandleFingerUp(LeanFinger finger)
		{
			if (fingers.Remove(finger) == true)
			{
				InvokeFinger(finger);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanFingerUp;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanFingerUp_Editor : LeanFingerDown_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();
		}
	}
}
#endif