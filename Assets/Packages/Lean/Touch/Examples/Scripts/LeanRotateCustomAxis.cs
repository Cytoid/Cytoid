using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component allows you to rotate the current GameObject around a specific local axis using finger twists.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanRotateCustomAxis")]
	public class LeanRotateCustomAxis : MonoBehaviour
	{
		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("Allows you to force rotation with a specific amount of fingers (0 = any)")]
		public int RequiredFingerCount;

		[Tooltip("Does rotation require an object to be selected?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("The axis of rotation")]
		public Vector3 Axis = Vector3.down;

		[Tooltip("Rotate locally or globally?")]
		public Space Space = Space.Self;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Start();
		}
#endif

		protected virtual void Start()
		{
			if (RequiredSelectable == null)
			{
				RequiredSelectable = GetComponent<LeanSelectable>();
			}
		}

		protected virtual void Update()
		{
			// Get the fingers we want to use
			var fingers = LeanSelectable.GetFingers(IgnoreStartedOverGui, IgnoreIsOverGui, RequiredFingerCount, RequiredSelectable);

			// Calculate the rotation values based on these fingers
			var twistDegrees = LeanGesture.GetTwistDegrees(fingers);

			// Perform rotation
			transform.Rotate(Axis, twistDegrees, Space);
		}
	}
}