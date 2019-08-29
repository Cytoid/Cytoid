using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This script allows you to zoom a camera in and out based on the pinch gesture
	/// This supports both perspective and orthographic cameras</summary>
	[ExecuteInEditMode]
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanCameraZoom")]
	public class LeanCameraZoom : MonoBehaviour
	{
		[Tooltip("The camera that will be zoomed (None = MainCamera)")]
		public Camera Camera;

		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("Allows you to force rotation with a specific amount of fingers (0 = any)")]
		public int RequiredFingerCount;

		[Tooltip("If RequiredSelectable.IsSelected is false, ignore?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("If you want the mouse wheel to simulate pinching then set the strength of it here")]
		[Range(-1.0f, 1.0f)]
		public float WheelSensitivity;

		[Tooltip("Should the scaling be performanced relative to the finger center?")]
		public bool Relative;

		[Tooltip("The current FOV/Size")]
		public float Zoom = 50.0f;

		[Tooltip("Limit the FOV/Size?")]
		public bool ZoomClamp;

		[Tooltip("The minimum FOV/Size we want to zoom to")]
		public float ZoomMin = 10.0f;

		[Tooltip("The maximum FOV/Size we want to zoom to")]
		public float ZoomMax = 60.0f;

		public void ContinuouslyZoom(float direction)
		{
			var factor = LeanTouch.GetDampenFactor(Mathf.Abs(direction), Time.deltaTime);

			if (direction > 0.0f)
			{
				Zoom = Mathf.Lerp(Zoom, ZoomMax, factor);
			}
			else if (direction <= 0.0f)
			{
				Zoom = Mathf.Lerp(Zoom, ZoomMin, factor);
			}
		}

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

		protected virtual void LateUpdate()
		{
			// Get the fingers we want to use
			var fingers = LeanSelectable.GetFingers(IgnoreStartedOverGui, IgnoreIsOverGui, RequiredFingerCount, RequiredSelectable);

			// Get the pinch ratio of these fingers
			var pinchRatio = LeanGesture.GetPinchRatio(fingers, WheelSensitivity);

			// Perform the translation if this is a relative scale
			if (Relative == true)
			{
				var pinchScreenCenter = LeanGesture.GetScreenCenter(fingers);

				Translate(pinchRatio, pinchScreenCenter);
			}

			// Modify the zoom value
			Zoom *= pinchRatio;

			if (ZoomClamp == true)
			{
				Zoom = Mathf.Clamp(Zoom, ZoomMin, ZoomMax);
			}

			// Set the new zoom
			SetZoom(Zoom);
		}

		protected virtual void Translate(float pinchScale, Vector2 screenCenter)
		{
			// Make sure the camera exists
			var camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				// Screen position of the transform
				var screenPosition = camera.WorldToScreenPoint(transform.position);

				// Push the screen position away from the reference point based on the scale
				screenPosition.x = screenCenter.x + (screenPosition.x - screenCenter.x) * pinchScale;
				screenPosition.y = screenCenter.y + (screenPosition.y - screenCenter.y) * pinchScale;

				// Convert back to world space
				transform.position = camera.ScreenToWorldPoint(screenPosition);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
			}
		}

		protected void SetZoom(float current)
		{
			// Make sure the camera exists
			var camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				if (camera.orthographic == true)
				{
					camera.orthographicSize = current;
				}
				else
				{
					camera.fieldOfView = current;
				}
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
			}
		}
	}
}