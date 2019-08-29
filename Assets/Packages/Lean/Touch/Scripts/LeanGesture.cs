using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	/// <summary>This class calculates gesture information (e.g. pinch) based on all fingers touching the screen, or a specified list of fingers.
	/// NOTE: This isn't a component, so it can only be used directly from C#. Many example components make use of this class so you don't have to use code though.</summary>
	public static class LeanGesture
	{
		/// <summary>Gets the average ScreenPosition of all.</summary>
		public static Vector2 GetScreenCenter()
		{
			return GetScreenCenter(LeanTouch.Fingers);
		}

		/// <summary>Gets the average ScreenPosition of the specified fingers.</summary>
		public static Vector2 GetScreenCenter(List<LeanFinger> fingers)
		{
			var center = default(Vector2); TryGetScreenCenter(fingers, ref center); return center;
		}

		/// <summary>Gets the average ScreenPosition of the specified fingers, if at least one exists.</summary>
		public static bool TryGetScreenCenter(List<LeanFinger> fingers, ref Vector2 center)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.ScreenPosition;
						count += 1;
					}
				}

				if (count > 0)
				{
					center = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the last average ScreenPosition of all fingers.</summary>
		public static Vector2 GetLastScreenCenter()
		{
			return GetLastScreenCenter(LeanTouch.Fingers);
		}

		/// <summary>Gets the last average ScreenPosition of the specified fingers.</summary>
		public static Vector2 GetLastScreenCenter(List<LeanFinger> fingers)
		{
			var center = default(Vector2); TryGetLastScreenCenter(fingers, ref center); return center;
		}

		/// <summary>Gets the last average ScreenPosition of the specified fingers, if at least one exists.</summary>
		public static bool TryGetLastScreenCenter(List<LeanFinger> fingers, ref Vector2 center)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.LastScreenPosition;
						count += 1;
					}
				}

				if (count > 0)
				{
					center = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the start average ScreenPosition of all fingers.</summary>
		public static Vector2 GetStartScreenCenter()
		{
			return GetStartScreenCenter(LeanTouch.Fingers);
		}

		/// <summary>Gets the start average ScreenPosition of the specified fingers.</summary>
		public static Vector2 GetStartScreenCenter(List<LeanFinger> fingers)
		{
			var center = default(Vector2); TryGetStartScreenCenter(fingers, ref center); return center;
		}

		/// <summary>Gets the start average ScreenPosition of the specified fingers, if at least one exists.</summary>
		public static bool TryGetStartScreenCenter(List<LeanFinger> fingers, ref Vector2 center)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.StartScreenPosition;
						count += 1;
					}
				}

				if (count > 0)
				{
					center = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the average ScreenDelta of all fingers.</summary>
		public static Vector2 GetScreenDelta()
		{
			return GetScreenDelta(LeanTouch.Fingers);
		}

		/// <summary>Gets the average ScreenDelta of the specified fingers.</summary>
		public static Vector2 GetScreenDelta(List<LeanFinger> fingers)
		{
			var delta = default(Vector2); TryGetScreenDelta(fingers, ref delta); return delta;
		}

		/// <summary>Gets the average ScreenDelta of the specified fingers, if at least one exists.</summary>
		public static bool TryGetScreenDelta(List<LeanFinger> fingers, ref Vector2 delta)
		{
			if (fingers != null)
			{
				var total = Vector2.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.ScreenDelta;
						count += 1;
					}
				}

				if (count > 0)
				{
					delta = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>This returns a resolution-independent 'GetScreenDelta' value.</summary>
		public static Vector2 GetScaledDelta()
		{
			return GetScreenDelta() * LeanTouch.ScalingFactor;
		}

		/// <summary>This returns a resolution-independent 'GetScreenDelta' value.</summary>
		public static Vector2 GetScaledDelta(List<LeanFinger> fingers)
		{
			return GetScreenDelta(fingers) * LeanTouch.ScalingFactor;
		}

		/// <summary>This returns a resolution-independent 'TryGetScreenDelta' value.</summary>
		public static bool TryGetScaledDelta(List<LeanFinger> fingers, ref Vector2 delta)
		{
			if (TryGetScreenDelta(fingers, ref delta) == true)
			{
				delta *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}

		/// <summary>Gets the average WorldDelta of all fingers.</summary>
		public static Vector3 GetWorldDelta(float distance, Camera camera = null)
		{
			return GetWorldDelta(LeanTouch.Fingers, distance, camera);
		}

		/// <summary>Gets the average WorldDelta of the specified fingers.</summary>
		public static Vector3 GetWorldDelta(List<LeanFinger> fingers, float distance, Camera camera = null)
		{
			var delta = default(Vector3); TryGetWorldDelta(fingers, distance, ref delta, camera); return delta;
		}

		/// <summary>Gets the average WorldDelta of the specified fingers, if at least one exists.</summary>
		public static bool TryGetWorldDelta(List<LeanFinger> fingers, float distance, ref Vector3 delta, Camera camera = null)
		{
			if (fingers != null)
			{
				var total = Vector3.zero;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetWorldDelta(distance, camera);
						count += 1;
					}
				}

				if (count > 0)
				{
					delta = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the average ScreenPosition distance between the fingers.</summary>
		public static float GetScreenDistance()
		{
			return GetScreenDistance(LeanTouch.Fingers);
		}

		/// <summary>Gets the average ScreenPosition distance between the fingers.</summary>
		public static float GetScreenDistance(List<LeanFinger> fingers)
		{
			var distance = default(float);
			var center   = default(Vector2);

			if (TryGetScreenCenter(fingers, ref center) == true)
			{
				TryGetScreenDistance(fingers, center, ref distance);
			}

			return distance;
		}

		/// <summary>Gets the average ScreenPosition distance between the fingers.</summary>
		public static float GetScreenDistance(List<LeanFinger> fingers, Vector2 center)
		{
			var distance = default(float); TryGetScreenDistance(fingers, center, ref distance); return distance;
		}

		/// <summary>Gets the average ScreenPosition distance between the fingers.</summary>
		public static bool TryGetScreenDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetScreenDistance(center);
						count += 1;
					}
				}

				if (count > 0)
				{
					distance = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the average ScreenPosition distance * LeanTouch.ScalingFactor between the fingers.</summary>
		public static float GetScaledDistance()
		{
			return GetScreenDistance() * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the average ScreenPosition distance * LeanTouch.ScalingFactor between the fingers.</summary>
		public static float GetScaledDistance(List<LeanFinger> fingers)
		{
			return GetScreenDistance(fingers) * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the average ScreenPosition distance * LeanTouch.ScalingFactor between the fingers.</summary>
		public static float GetScaledDistance(List<LeanFinger> fingers, Vector2 center)
		{
			return GetScreenDistance(fingers, center) * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the average ScreenPosition distance * LeanTouch.ScalingFactor between the fingers.</summary>
		public static bool TryGetScaledDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (TryGetScreenDistance(fingers, center, ref distance) == true)
			{
				distance *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}

		/// <summary>Gets the last average ScreenPosition distance between all fingers.</summary>
		public static float GetLastScreenDistance()
		{
			return GetLastScreenDistance(LeanTouch.Fingers);
		}

		/// <summary>Gets the last average ScreenPosition distance between all fingers.</summary>
		public static float GetLastScreenDistance(List<LeanFinger> fingers)
		{
			var distance = default(float);
			var center   = default(Vector2);

			if (TryGetLastScreenCenter(fingers, ref center) == true)
			{
				TryGetLastScreenDistance(fingers, center, ref distance);
			}

			return distance;
		}

		/// <summary>Gets the last average ScreenPosition distance between all fingers.</summary>
		public static float GetLastScreenDistance(List<LeanFinger> fingers, Vector2 center)
		{
			var distance = default(float); TryGetLastScreenDistance(fingers, center, ref distance); return distance;
		}

		/// <summary>Gets the last average ScreenPosition distance between all fingers.</summary>
		public static bool TryGetLastScreenDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetLastScreenDistance(center);
						count += 1;
					}
				}

				if (count > 0)
				{
					distance = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the last average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static float GetLastScaledDistance()
		{
			return GetLastScreenDistance() * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the last average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static float GetLastScaledDistance(List<LeanFinger> fingers)
		{
			return GetLastScreenDistance(fingers) * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the last average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static float GetLastScaledDistance(List<LeanFinger> fingers, Vector2 center)
		{
			return GetLastScreenDistance(fingers, center) * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the last average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static bool TryGetLastScaledDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (TryGetLastScreenDistance(fingers, center, ref distance) == true)
			{
				distance *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}

		/// <summary>Gets the start average ScreenPosition distance between all fingers.</summary>
		public static float GetStartScreenDistance()
		{
			return GetStartScreenDistance(LeanTouch.Fingers);
		}

		/// <summary>Gets the start average ScreenPosition distance between all fingers.</summary>
		public static float GetStartScreenDistance(List<LeanFinger> fingers)
		{
			var distance = default(float);
			var center   = default(Vector2);

			if (TryGetStartScreenCenter(fingers, ref center) == true)
			{
				TryGetStartScreenDistance(fingers, center, ref distance);
			}

			return distance;
		}

		/// <summary>Gets the start average ScreenPosition distance between all fingers.</summary>
		public static float GetStartScreenDistance(List<LeanFinger> fingers, Vector2 center)
		{
			var distance = default(float); TryGetStartScreenDistance(fingers, center, ref distance); return distance;
		}

		/// <summary>Gets the start average ScreenPosition distance between all fingers.</summary>
		public static bool TryGetStartScreenDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetStartScreenDistance(center);
						count += 1;
					}
				}

				if (count > 0)
				{
					distance = total / count; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the start average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static float GetStartScaledDistance()
		{
			return GetStartScreenDistance() * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the start average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static float GetStartScaledDistance(List<LeanFinger> fingers)
		{
			return GetStartScreenDistance(fingers) * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the start average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static float GetStartScaledDistance(List<LeanFinger> fingers, Vector2 center)
		{
			return GetStartScreenDistance(fingers, center) * LeanTouch.ScalingFactor;
		}

		/// <summary>Gets the start average ScreenPosition distance * LeanTouch.ScalingFactor between all fingers.</summary>
		public static bool TryGetStartScaledDistance(List<LeanFinger> fingers, Vector2 center, ref float distance)
		{
			if (TryGetStartScreenDistance(fingers, center, ref distance) == true)
			{
				distance *= LeanTouch.ScalingFactor; return true;
			}

			return false;
		}

		/// <summary>Gets the pinch scale of the fingers.</summary>
		public static float GetPinchScale(float wheelSensitivity = 0.0f)
		{
			return GetPinchScale(LeanTouch.Fingers, wheelSensitivity);
		}

		/// <summary>Gets the pinch scale of the fingers.</summary>
		public static float GetPinchScale(List<LeanFinger> fingers, float wheelSensitivity = 0.0f)
		{
			var scale      = 1.0f;
			var center     = GetScreenCenter(fingers);
			var lastCenter = GetLastScreenCenter(fingers);

			TryGetPinchScale(fingers, center, lastCenter, ref scale, wheelSensitivity);

			return scale;
		}

		/// <summary>Gets the pinch scale of the fingers.</summary>
		public static bool TryGetPinchScale(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float scale, float wheelSensitivity = 0.0f)
		{
			var distance     = GetScreenDistance(fingers, center);
			var lastDistance = GetLastScreenDistance(fingers, lastCenter);

			if (lastDistance > 0.0f)
			{
				scale = distance / lastDistance; return true;
			}

			if (wheelSensitivity != 0.0f)
			{
				var scroll = Input.mouseScrollDelta.y;

				if (scroll > 0.0f)
				{
					scale = 1.0f - wheelSensitivity; return true;
				}

				if (scroll < 0.0f)
				{
					scale = 1.0f + wheelSensitivity; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the pinch ratio of the fingers (reciprocal of pinch scale).</summary>
		public static float GetPinchRatio(float wheelSensitivity = 0.0f)
		{
			return GetPinchRatio(LeanTouch.Fingers, wheelSensitivity);
		}

		/// <summary>Gets the pinch ratio of the fingers (reciprocal of pinch scale).</summary>
		public static float GetPinchRatio(List<LeanFinger> fingers, float wheelSensitivity = 0.0f)
		{
			var ratio      = 1.0f;
			var center     = GetScreenCenter(fingers);
			var lastCenter = GetLastScreenCenter(fingers);

			TryGetPinchRatio(fingers, center, lastCenter, ref ratio, wheelSensitivity);

			return ratio;
		}

		/// <summary>Gets the pinch ratio of the fingers (reciprocal of pinch scale).</summary>
		public static bool TryGetPinchRatio(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float ratio, float wheelSensitivity = 0.0f)
		{
			var distance     = GetScreenDistance(fingers, center);
			var lastDistance = GetLastScreenDistance(fingers, lastCenter);

			if (distance > 0.0f)
			{
				ratio = lastDistance / distance;

				return true;
			}

			if (wheelSensitivity != 0.0f)
			{
				var scroll = Input.mouseScrollDelta.y;

				if (scroll > 0.0f)
				{
					ratio = 1.0f + wheelSensitivity; return true;
				}

				if (scroll < 0.0f)
				{
					ratio = 1.0f - wheelSensitivity; return true;
				}
			}

			return false;
		}

		/// <summary>Gets the average twist of the fingers in degrees.</summary>
		public static float GetTwistDegrees()
		{
			return GetTwistDegrees(LeanTouch.Fingers);
		}

		/// <summary>Gets the average twist of the fingers in degrees.</summary>
		public static float GetTwistDegrees(List<LeanFinger> fingers)
		{
			return GetTwistRadians(fingers) * Mathf.Rad2Deg;
		}

		/// <summary>Gets the average twist of the fingers in degrees.</summary>
		public static float GetTwistDegrees(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter)
		{
			return GetTwistRadians(fingers, center, lastCenter) * Mathf.Rad2Deg;
		}

		/// <summary>Gets the average twist of the fingers in degrees.</summary>
		public static bool TryGetTwistDegrees(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float degrees)
		{
			if (TryGetTwistRadians(fingers, center, lastCenter, ref degrees) == true)
			{
				degrees *= Mathf.Rad2Deg;

				return true;
			}

			return false;
		}

		/// <summary>Gets the average twist of the fingers in radians.</summary>
		public static float GetTwistRadians()
		{
			return GetTwistRadians(LeanTouch.Fingers);
		}

		/// <summary>Gets the average twist of the fingers in radians.</summary>
		public static float GetTwistRadians(List<LeanFinger> fingers)
		{
			var center     = LeanGesture.GetScreenCenter(fingers);
			var lastCenter = LeanGesture.GetLastScreenCenter(fingers);

			return GetTwistRadians(fingers, center, lastCenter);
		}

		/// <summary>Gets the average twist of the fingers in radians.</summary>
		public static float GetTwistRadians(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter)
		{
			var radians = default(float); TryGetTwistRadians(fingers, center, lastCenter, ref radians); return radians;
		}

		/// <summary>Gets the average twist of the fingers in radians.</summary>
		public static bool TryGetTwistRadians(List<LeanFinger> fingers, Vector2 center, Vector2 lastCenter, ref float radians)
		{
			if (fingers != null)
			{
				var total = 0.0f;
				var count = 0;

				for (var i = fingers.Count - 1; i >= 0; i--)
				{
					var finger = fingers[i];

					if (finger != null)
					{
						total += finger.GetDeltaRadians(center, lastCenter);
						count += 1;
					}
				}

				if (count > 0)
				{
					radians = total / count; return true;
				}
			}

			return false;
		}
	}
}