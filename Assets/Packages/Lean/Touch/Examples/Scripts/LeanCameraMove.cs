using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This script allows you to track & pedestal this GameObject (e.g. Camera) based on finger drags.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanCameraMove")]
	public class LeanCameraMove : MonoBehaviour
	{
		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("Ignore fingers if the finger count doesn't match? (0 = any)")]
		public int RequiredFingerCount;

		[Tooltip("The sensitivity of the movement, use -1 to invert")]
		public float Sensitivity = 1.0f;

		public LeanScreenDepth ScreenDepth;

		public virtual void SnapToSelection()
		{
			var center = default(Vector3);
			var count  = 0;

			for (var i = 0; i < LeanSelectable.Instances.Count; i++)
			{
				var selectable = LeanSelectable.Instances[i];

				if (selectable.IsSelected == true)
				{
					center += selectable.transform.position;
					count  += 1;
				}
			}

			if (count > 0)
			{
				transform.position = center / count;
			}
		}

		protected virtual void LateUpdate()
		{
			// Get the fingers we want to use
			var fingers = LeanTouch.GetFingers(IgnoreStartedOverGui, IgnoreIsOverGui, RequiredFingerCount);

			// Get the last and current screen point of all fingers
			var lastScreenPoint = LeanGesture.GetLastScreenCenter(fingers);
			var screenPoint     = LeanGesture.GetScreenCenter(fingers);

			// Get the world delta of them after conversion
			var worldDelta = ScreenDepth.ConvertDelta(lastScreenPoint, screenPoint, gameObject);

			// Pan the camera based on the world delta
			transform.position -= worldDelta * Sensitivity;
		}
	}
}