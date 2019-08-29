using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Lean.Touch
{
	/// <summary>This script fires events if a finger has been held for a certain amount of time without moving.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerSwipe")]
	public class LeanFingerSwipe : MonoBehaviour
	{
		public enum ClampType
		{
			None,
			Normalize,
			Direction4,
			ScaledDelta
		}

		// Event signature
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}
		[System.Serializable] public class Vector2Event : UnityEvent<Vector2> {}

		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("Do nothing if this LeanSelectable isn't selected?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("Must the swipe be in a specific direction?")]
		public bool CheckAngle;

		[Tooltip("The required angle of the swipe in degrees, where 0 is up, and 90 is right")]
		public float Angle;

		[Tooltip("The left/right tolerance of the swipe angle in degrees")]
		public float AngleThreshold = 90.0f;

		[Tooltip("Should the swipe delta be modified before use?")]
		public ClampType Clamp;

		[Tooltip("The swipe delta multiplier, useful if you're using a Clamp mode")]
		public float Multiplier = 1.0f;

		// Called on the first frame the conditions are met
		public LeanFingerEvent OnSwipe { get { if (onSwipe == null) onSwipe = new LeanFingerEvent(); return onSwipe; } } [FormerlySerializedAs("OnSwipe")] [SerializeField] private LeanFingerEvent onSwipe;

		public Vector2Event OnSwipeDelta { get { if (onSwipeDelta == null) onSwipeDelta = new Vector2Event(); return onSwipeDelta; } } [FormerlySerializedAs("OnSwipeDelta")] [SerializeField] private Vector2Event onSwipeDelta;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Start();
		}
#endif

		protected bool CheckSwipe(LeanFinger finger, Vector2 swipeDelta)
		{
			// Invalid angle?
			if (CheckAngle == true)
			{
				var angle = Mathf.Atan2(swipeDelta.x, swipeDelta.y) * Mathf.Rad2Deg;
				var delta = Mathf.DeltaAngle(angle, Angle);

				if (delta < AngleThreshold * -0.5f || delta >= AngleThreshold * 0.5f)
				{
					return false;
				}
			}

			// Clamp delta?
			switch (Clamp)
			{
				case ClampType.Normalize:
				{
					swipeDelta = swipeDelta.normalized;
				}
				break;

				case ClampType.Direction4:
				{
					if (swipeDelta.x < -Mathf.Abs(swipeDelta.y)) swipeDelta = -Vector2.right;
					if (swipeDelta.x >  Mathf.Abs(swipeDelta.y)) swipeDelta =  Vector2.right;
					if (swipeDelta.y < -Mathf.Abs(swipeDelta.x)) swipeDelta = -Vector2.up;
					if (swipeDelta.y >  Mathf.Abs(swipeDelta.x)) swipeDelta =  Vector2.up;
				}
				break;

				case ClampType.ScaledDelta:
				{
					swipeDelta *= LeanTouch.ScalingFactor;
				}
				break;
			}

			// Call event
			if (onSwipe != null)
			{
				onSwipe.Invoke(finger);
			}

			if (onSwipeDelta != null)
			{
				onSwipeDelta.Invoke(swipeDelta * Multiplier);
			}

			return true;
		}

		protected virtual void OnEnable()
		{
			// Hook events
			LeanTouch.OnFingerSwipe += FingerSwipe;
		}

		protected virtual void Start()
		{
			if (RequiredSelectable == null)
			{
				RequiredSelectable = GetComponent<LeanSelectable>();
			}
		}

		protected virtual void OnDisable()
		{
			// Unhook events
			LeanTouch.OnFingerSwipe -= FingerSwipe;
		}

		private void FingerSwipe(LeanFinger finger)
		{
			// Ignore?
			if (IgnoreStartedOverGui == true && finger.StartedOverGui == true)
			{
				return;
			}

			if (IgnoreIsOverGui == true && finger.IsOverGui == true)
			{
				return;
			}

			if (RequiredSelectable != null && RequiredSelectable.IsSelectedBy(finger) == false)
			{
				return;
			}

			// Perform final swipe check and fire event
			CheckSwipe(finger, finger.SwipeScreenDelta);
		}
	}
}