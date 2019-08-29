using UnityEngine;
using UnityEngine.UI;

namespace Lean.Touch
{
	/// <summary>This script rotates the current GameObject based on a finger swipe angle.</summary>
	[ExecuteInEditMode]
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanCanvasArrow")]
	public class LeanCanvasArrow : MonoBehaviour
	{
		[Tooltip("The current angle")]
		public float Angle;

		public Text AngleText;

		public void RotateToDelta(Vector2 delta)
		{
			Angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;

			if (AngleText != null)
			{
				AngleText.text = string.Format("You swiped x = {0}, y = {1}\nangle = {2:0.}\u00B0, distance = {3:0.}", delta.x, delta.y, Angle, delta.magnitude);
			}
		}

		protected virtual void Update()
		{
			transform.rotation = Quaternion.Euler(0.0f, 0.0f, -Angle);
		}
	}
}