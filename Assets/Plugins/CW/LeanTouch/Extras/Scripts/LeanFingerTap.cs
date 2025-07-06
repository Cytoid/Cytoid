using UnityEngine;
using UnityEngine.Events;
using Lean.Common;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>This component calls the OnFingerTap event when a finger taps the screen.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerTap")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Finger Tap")]
	public class LeanFingerTap : MonoBehaviour
	{
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}
		[System.Serializable] public class Vector3Event : UnityEvent<Vector3> {}
		[System.Serializable] public class Vector2Event : UnityEvent<Vector2> {}
		[System.Serializable] public class IntEvent : UnityEvent<int> {}

		/// <summary>Ignore fingers with StartedOverGui?</summary>
		public bool IgnoreStartedOverGui { set { ignoreStartedOverGui = value; } get { return ignoreStartedOverGui; } } [SerializeField] private bool ignoreStartedOverGui = true;

		/// <summary>Ignore fingers with OverGui?</summary>
		public bool IgnoreIsOverGui { set { ignoreIsOverGui = value; } get { return ignoreIsOverGui; } } [SerializeField] private bool ignoreIsOverGui;

		/// <summary>If the specified object is set and isn't selected, then this component will do nothing.</summary>
		public LeanSelectable RequiredSelectable { set { requiredSelectable = value; } get { return requiredSelectable; } } [SerializeField] private LeanSelectable requiredSelectable;

		/// <summary>How many times must this finger tap before OnTap gets called?
		/// 0 = Every time (keep in mind OnTap will only be called once if you use this).</summary>
		public int RequiredTapCount { set { requiredTapCount = value; } get { return requiredTapCount; } } [SerializeField] private int requiredTapCount;

		/// <summary>How many times repeating must this finger tap before OnTap gets called?
		/// 0 = Every time (e.g. a setting of 2 means OnTap will get called when you tap 2 times, 4 times, 6, 8, 10, etc).</summary>
		public int RequiredTapInterval { set { requiredTapInterval = value; } get { return requiredTapInterval; } } [SerializeField] private int requiredTapInterval;

		/// <summary>This event will be called if the above conditions are met when you tap the screen.</summary>
		public LeanFingerEvent OnFinger { get { if (onFinger == null) onFinger = new LeanFingerEvent(); return onFinger; } } [SerializeField] private LeanFingerEvent onFinger;

		/// <summary>This event will be called if the above conditions are met when you tap the screen.
		/// Int = The finger tap count.</summary>
		public IntEvent OnCount { get { if (onCount == null) onCount = new IntEvent(); return onCount; } } [SerializeField] private IntEvent onCount;

		/// <summary>The method used to find world coordinates from a finger. See LeanScreenDepth documentation for more information.</summary>
		public LeanScreenDepth ScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.DepthIntercept);

		/// <summary>This event will be called if the above conditions are met when you tap the screen.
		/// Vector3 = Finger position in world space.</summary>
		public Vector3Event OnWorld { get { if (onWorld == null) onWorld = new Vector3Event(); return onWorld; } } [SerializeField] private Vector3Event onWorld;

		/// <summary>This event will be called if the above conditions are met when you tap the screen.
		/// Vector2 = Finger position in screen space.</summary>
		public Vector2Event OnScreen { get { if (onScreen == null) onScreen = new Vector2Event(); return onScreen; } } [SerializeField] private Vector2Event onScreen;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			requiredSelectable = GetComponentInParent<LeanSelectable>();
		}
#endif

		protected virtual void Start()
		{
			if (requiredSelectable == null)
			{
				requiredSelectable = GetComponentInParent<LeanSelectable>();
			}
		}

		protected virtual void OnEnable()
		{
			LeanTouch.OnFingerTap += HandleFingerTap;
		}

		protected virtual void OnDisable()
		{
			LeanTouch.OnFingerTap -= HandleFingerTap;
		}

		protected void HandleFingerTap(LeanFinger finger)
		{
			// Ignore?
			if (ignoreStartedOverGui == true && finger.StartedOverGui == true)
			{
				return;
			}

			if (ignoreIsOverGui == true && finger.IsOverGui == true)
			{
				return;
			}

			if (requiredTapCount > 0 && finger.TapCount != requiredTapCount)
			{
				return;
			}

			if (requiredTapInterval > 0 && (finger.TapCount % requiredTapInterval) != 0)
			{
				return;
			}

			if (requiredSelectable != null && requiredSelectable.IsSelected == false)
			{
				return;
			}

			if (onFinger != null)
			{
				onFinger.Invoke(finger);
			}

			if (onCount != null)
			{
				onCount.Invoke(finger.TapCount);
			}

			if (onWorld != null)
			{
				var position = ScreenDepth.Convert(finger.ScreenPosition, gameObject);

				onWorld.Invoke(position);
			}

			if (onScreen != null)
			{
				onScreen.Invoke(finger.ScreenPosition);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanFingerTap;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanFingerTap_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("ignoreStartedOverGui", "Ignore fingers with StartedOverGui?");
			Draw("ignoreIsOverGui", "Ignore fingers with OverGui?");
			Draw("requiredSelectable", "If the specified object is set and isn't selected, then this component will do nothing.");
			Draw("requiredTapCount", "How many times must this finger tap before OnTap gets called?\n\n0 = Every time (keep in mind OnTap will only be called once if you use this).");
			Draw("requiredTapInterval", "How many times repeating must this finger tap before OnTap gets called?\n\n0 = Every time (e.g. a setting of 2 means OnTap will get called when you tap 2 times, 4 times, 6, 8, 10, etc).");

			Separator();

			var showUnusedEvents = DrawFoldout("Show Unused Events", "Show all events?");

			if (Any(tgts, t => t.OnFinger.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onFinger");
			}

			if (Any(tgts, t => t.OnCount.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onCount");
			}

			if (Any(tgts, t => t.OnWorld.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("ScreenDepth");
				Draw("onWorld");
			}

			if (Any(tgts, t => t.OnScreen.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onScreen");
			}
		}
	}
}
#endif