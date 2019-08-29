using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Lean.Touch
{
	/// <summary>This script will modify LeanFingerTrail to draw a straight line.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerLine")]
	public class LeanFingerLine : LeanFingerTrail
	{
		// Event signatures
		[System.Serializable] public class Vector3Vector3Event : UnityEvent<Vector3, Vector3> {}
		[System.Serializable] public class Vector3Event : UnityEvent<Vector3> {}

		[Tooltip("Should the line appear on the opposite side?")]
		public bool Inverse;

		[Tooltip("The thickness scale per unit (0 = no scaling)")]
		public float ThicknessScale;

		[Tooltip("Limit the length (0 = none)")]
		public float LengthMin;

		[Tooltip("Limit the length (0 = none)")]
		public float LengthMax;

		[Tooltip("Should the line originate from a target point?")]
		public Transform Target;

		public Vector3Vector3Event OnReleasedFromTo { get { if (onReleasedFromTo == null) onReleasedFromTo = new Vector3Vector3Event(); return onReleasedFromTo; } } [FormerlySerializedAs("OnReleasedFromTo")] [SerializeField] private Vector3Vector3Event onReleasedFromTo;

		public Vector3Event OnReleasedTo { get { if (onReleasedTo == null) onReleasedTo = new Vector3Event(); return onReleasedTo; } } [FormerlySerializedAs("OnReleasedTo")] [SerializeField] private Vector3Event onReleasedTo;

		protected override void LinkFingerUp(FingerData link)
		{
			// Calculate points
			var start = GetStartPoint(link.Finger);
			var end   = GetEndPoint(link.Finger, start);

			if (onReleasedFromTo != null)
			{
				onReleasedFromTo.Invoke(start, end);
			}

			if (onReleasedTo != null)
			{
				onReleasedTo.Invoke(end);
			}
		}

		protected override void WritePositions(LineRenderer line, LeanFinger finger)
		{
			// Calculate points
			var start = GetStartPoint(finger);
			var end   = GetEndPoint(finger, start);

			// Adjust thickness?
			if (ThicknessScale > 0.0f)
			{
				var thickness = Vector3.Distance(start, end) * ThicknessScale;

				line.startWidth = thickness;
				line.endWidth   = thickness;
			}

			// Write positions
			line.positionCount = 2;

			line.SetPosition(0, start);
			line.SetPosition(1, end);
		}

		private Vector3 GetStartPoint(LeanFinger finger)
		{
			// Use target position?
			if (Target != null)
			{
				return Target.position;
			}

			// Convert
			return ScreenDepth.Convert(finger.StartScreenPosition, gameObject);
		}

		private Vector3 GetEndPoint(LeanFinger finger, Vector3 start)
		{
			// Cauculate distance based on start position, because the Target point may override Distance
			var end      = ScreenDepth.Convert(finger.ScreenPosition, gameObject);
			var length   = Vector3.Distance(start, end);

			// Limit the length?
			if (LengthMin > 0.0f && length < LengthMin)
			{
				length = LengthMin;
			}

			if (LengthMax > 0.0f && length > LengthMax)
			{
				length = LengthMax;
			}

			// Recalculate end
			var delta = Vector3.Normalize(end - start) * length;

			return Inverse == true ? start - delta : start + delta;
		}
	}
}