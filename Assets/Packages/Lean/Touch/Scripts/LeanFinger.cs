using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	/// <summary>This class stores information about a single touch (or simulated touch).</summary>
	public class LeanFinger
	{
		/// <summary>This is the hardware ID of the finger.
		/// NOTE: Simulated fingers will use hardware ID -1 and -2.</summary>
		public int Index;

		/// <summary>This tells you how long this finger has been active (or inactive) in seconds.</summary>
		public float Age;

		/// <summary>Is this finger currently touching the screen?</summary>
		public bool Set;

		/// <summary>This tells you the 'Set' value of the last frame.</summary>
		public bool LastSet;

		/// <summary>Did this finger just tap the screen?</summary>
		public bool Tap;

		/// <summary>This tells you how many times this finger has been tapped.</summary>
		public int TapCount;

		/// <summary>Did this finger just swipe the screen?</summary>
		public bool Swipe;

		/// <summary>If this finger has been inactive for more than TapThreshold, this will be true.</summary>
		public bool Expired;

		/// <summary>This tells you the Pressure value last frame.</summary>
		public float LastPressure;

		/// <summary>This tells you the current pressure of this finger (NOTE: only some devices support this).</summary>
		public float Pressure;

		/// <summary>This tells you the 'ScreenPosition' value of this finger when it began touching the screen.</summary>
		public Vector2 StartScreenPosition;

		/// <summary>This tells you the last screen position of the finger.</summary>
		public Vector2 LastScreenPosition;

		/// <summary>This tells you the current screen position of the finger in pixels, where 0,0 = bottom left.</summary>
		public Vector2 ScreenPosition;

		/// <summary>This tells you if the current finger had 'IsOverGui' set to true when it began touching the screen.</summary>
		public bool StartedOverGui;

		/// <summary>Used to store position snapshots, enable RecordFingers in LeanTouch to use this.</summary>
		public List<LeanSnapshot> Snapshots = new List<LeanSnapshot>(1000);

		/// <summary>This will return true if this finger is currently touching the screen.</summary>
		public bool IsActive
		{
			get
			{
				return LeanTouch.Fingers.Contains(this);
			}
		}

		/// <summary>This will tell you how many seconds of snapshot footage is stored for this finger.</summary>
		public float SnapshotDuration
		{
			get
			{
				if (Snapshots.Count > 0)
				{
					return Age - Snapshots[0].Age;
				}

				return 0.0f;
			}
		}

		/// <summary>This will return true if the current finger is over any Unity GUI elements.</summary>
		public bool IsOverGui
		{
			get
			{
				return LeanTouch.PointOverGui(ScreenPosition);
			}
		}

		/// <summary>Did this finger begin touching the screen this frame?</summary>
		public bool Down
		{
			get
			{
				return Set == true && LastSet == false;
			}
		}

		/// <summary>Did the finger stop touching the screen this frame?</summary>
		public bool Up
		{
			get
			{
				return Set == false && LastSet == true;
			}
		}

		/// <summary>This will return how far in pixels the finger has moved since the last recorded snapshot.</summary>
		public Vector2 LastSnapshotScreenDelta
		{
			get
			{
				var snapshotCount = Snapshots.Count;

				if (snapshotCount > 0)
				{
					var snapshot = Snapshots[snapshotCount - 1];

					if (snapshot != null)
					{
						return ScreenPosition - snapshot.ScreenPosition;
					}
				}

				return Vector2.zero;
			}
		}

		/// <summary>This returns a resolution-independent 'LastSnapshotScreenDelta' value.</summary>
		public Vector2 LastSnapshotScaledDelta
		{
			get
			{
				return LastSnapshotScreenDelta * LeanTouch.ScalingFactor;
			}
		}

		/// <summary>This will return how far in pixels the finger has moved since the last frame.</summary>
		public Vector2 ScreenDelta
		{
			get
			{
				return ScreenPosition - LastScreenPosition;
			}
		}

		/// <summary>This returns a resolution-independent 'ScreenDelta' value.</summary>
		public Vector2 ScaledDelta
		{
			get
			{
				return ScreenDelta * LeanTouch.ScalingFactor;
			}
		}

		/// <summary>This tells you how far this finger has moved since it began touching the screen.</summary>
		public Vector2 SwipeScreenDelta
		{
			get
			{
				return ScreenPosition - StartScreenPosition;
			}
		}

		/// <summary>This returns a resolution-independent 'SwipeScreenDelta' value.</summary>
		public Vector2 SwipeScaledDelta
		{
			get
			{
				return SwipeScreenDelta * LeanTouch.ScalingFactor;
			}
		}

		/// <summary>This will return the ray of the finger's current position relative to the specified camera (none/null = Main Camera).</summary>
		public Ray GetRay(Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				return camera.ScreenPointToRay(ScreenPosition);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Ray);
		}

		/// <summary>This will return the ray of the finger's start position relative to the specified camera (none/null = Main Camera).</summary>
		public Ray GetStartRay(Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				return camera.ScreenPointToRay(StartScreenPosition);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Ray);
		}

		/// <summary>This will tell you how far the finger has moved in the past 'deltaTime' seconds.</summary>
		public Vector2 GetSnapshotScreenDelta(float deltaTime)
		{
			return ScreenPosition - GetSnapshotScreenPosition(Age - deltaTime);
		}

		/// <summary>This returns a resolution-independent 'GetSnapshotScreenDelta' value.</summary>
		public Vector2 GetSnapshotScaledDelta(float deltaTime)
		{
			return GetSnapshotScreenDelta(deltaTime) * LeanTouch.ScalingFactor;
		}

		/// <summary>This will return the recorded position of the current finger when it was at 'targetAge'.</summary>
		public Vector2 GetSnapshotScreenPosition(float targetAge)
		{
			var screenPosition = ScreenPosition;

			LeanSnapshot.TryGetScreenPosition(Snapshots, targetAge, ref screenPosition);

			return screenPosition;
		}

		/// <summary>This will return the recorded world of the current finger when it was at 'targetAge'.</summary>
		public Vector3 GetSnapshotWorldPosition(float targetAge, float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var screenPosition = GetSnapshotScreenPosition(targetAge);
				var point          = new Vector3(screenPosition.x, screenPosition.y, distance);

				return camera.ScreenToWorldPoint(point);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Vector3);
		}

		/// <summary>This will return the angle between the finger and the reference point, relative to the screen.</summary>
		public float GetRadians(Vector2 referencePoint)
		{
			return Mathf.Atan2(ScreenPosition.x - referencePoint.x, ScreenPosition.y - referencePoint.y);
		}

		/// <summary>This will return the angle between the finger and the reference point, relative to the screen.</summary>
		public float GetDegrees(Vector2 referencePoint)
		{
			return GetRadians(referencePoint) * Mathf.Rad2Deg;
		}

		/// <summary>This will return the angle between the last finger position and the reference point, relative to the screen.</summary>
		public float GetLastRadians(Vector2 referencePoint)
		{
			return Mathf.Atan2(LastScreenPosition.x - referencePoint.x, LastScreenPosition.y - referencePoint.y);
		}

		/// <summary>This will return the angle between the last finger position and the reference point, relative to the screen.</summary>
		public float GetLastDegrees(Vector2 referencePoint)
		{
			return GetLastRadians(referencePoint) * Mathf.Rad2Deg;
		}

		/// <summary>This will return the delta angle between the last and current finger position relative to the reference point.</summary>
		public float GetDeltaRadians(Vector2 referencePoint)
		{
			return GetDeltaRadians(referencePoint, referencePoint);
		}

		/// <summary>This will return the delta angle between the last and current finger position relative to the reference point and the last reference point.</summary>
		public float GetDeltaRadians(Vector2 referencePoint, Vector2 lastReferencePoint)
		{
			var a = GetLastRadians(lastReferencePoint);
			var b = GetRadians(referencePoint);
			var d = Mathf.Repeat(a - b, Mathf.PI * 2.0f);

			if (d > Mathf.PI)
			{
				d -= Mathf.PI * 2.0f;
			}

			return d;
		}

		/// <summary>This will return the delta angle between the last and current finger position relative to the reference point.</summary>
		public float GetDeltaDegrees(Vector2 referencePoint)
		{
			return GetDeltaRadians(referencePoint, referencePoint) * Mathf.Rad2Deg;
		}

		/// <summary>This will return the delta angle between the last and current finger position relative to the reference point and the last reference point.</summary>
		public float GetDeltaDegrees(Vector2 referencePoint, Vector2 lastReferencePoint)
		{
			return GetDeltaRadians(referencePoint, lastReferencePoint) * Mathf.Rad2Deg;
		}

		/// <summary>This will return the distance between the finger and the reference point.</summary>
		public float GetScreenDistance(Vector2 point)
		{
			return Vector2.Distance(ScreenPosition, point);
		}

		/// <summary>This returns a resolution-independent 'GetScreenDistance' value.</summary>
		public float GetScaledDistance(Vector2 point)
		{
			return GetScreenDistance(point) * LeanTouch.ScalingFactor;
		}

		/// <summary>This will return the distance between the last finger and the reference point.</summary>
		public float GetLastScreenDistance(Vector2 point)
		{
			return Vector2.Distance(LastScreenPosition, point);
		}

		/// <summary>This returns a resolution-independent 'GetLastScreenDistance' value.</summary>
		public float GetLastScaledDistance(Vector2 point)
		{
			return GetLastScreenDistance(point) * LeanTouch.ScalingFactor;
		}

		/// <summary>This will return the distance between the start finger and the reference point.</summary>
		public float GetStartScreenDistance(Vector2 point)
		{
			return Vector2.Distance(StartScreenPosition, point);
		}

		/// <summary>This returns a resolution-independent 'GetStartScreenDistance' value.</summary>
		public float GetStartScaledDistance(Vector2 point)
		{
			return GetStartScreenDistance(point) * LeanTouch.ScalingFactor;
		}

		/// <summary>This will return the start world position of this finger based on the distance from the camera.</summary>
		public Vector3 GetStartWorldPosition(float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var point = new Vector3(StartScreenPosition.x, StartScreenPosition.y, distance);

				return camera.ScreenToWorldPoint(point);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Vector3);
		}

		/// <summary>This will return the last world position of this finger based on the distance from the camera.</summary>
		public Vector3 GetLastWorldPosition(float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var point = new Vector3(LastScreenPosition.x, LastScreenPosition.y, distance);

				return camera.ScreenToWorldPoint(point);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Vector3);
		}

		/// <summary>This will return the world position of this finger based on the distance from the camera.</summary>
		public Vector3 GetWorldPosition(float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				var point = new Vector3(ScreenPosition.x, ScreenPosition.y, distance);

				return camera.ScreenToWorldPoint(point);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Vector3);
		}

		/// <summary>This will return the change in world position of this finger based on the distance from the camera.</summary>
		public Vector3 GetWorldDelta(float distance, Camera camera = null)
		{
			return GetWorldDelta(distance, distance, camera);
		}

		/// <summary>This will return the change in world position of this finger based on the last and current distance from the camera.</summary>
		public Vector3 GetWorldDelta(float lastDistance, float distance, Camera camera = null)
		{
			// Make sure the camera exists
			camera = LeanTouch.GetCamera(camera);

			if (camera != null)
			{
				return GetWorldPosition(distance, camera) - GetLastWorldPosition(lastDistance, camera);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.");
			}

			return default(Vector3);
		}

		/// <summary>This will clear all snapshots for this finger and pool them, count = -1 for all.</summary>
		public void ClearSnapshots(int count = -1)
		{
			// Clear old ones only?
			if (count > 0 && count <= Snapshots.Count)
			{
				for (var i = 0; i < count; i++)
				{
					LeanSnapshot.InactiveSnapshots.Add(Snapshots[i]);
				}

				Snapshots.RemoveRange(0, count);
			}
			// Clear all?
			else if (count < 0)
			{
				LeanSnapshot.InactiveSnapshots.AddRange(Snapshots);

				Snapshots.Clear();
			}
		}

		/// <summary>Calling this will instantly store a snapshot of the current finger position.</summary>
		public void RecordSnapshot()
		{
			// Get an unused snapshot and set it up
			var snapshot = LeanSnapshot.Pop();

			snapshot.Age            = Age;
			snapshot.ScreenPosition = ScreenPosition;

			// Add to list
			Snapshots.Add(snapshot);
		}
	}
}