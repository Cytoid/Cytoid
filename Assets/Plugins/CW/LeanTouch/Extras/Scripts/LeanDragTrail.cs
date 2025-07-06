using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>This component draws trails behind fingers.
	/// NOTE: This requires you to enable the LeanTouch.RecordFingers setting.</summary>
	[ExecuteInEditMode]
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanDragTrail")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Drag Trail")]
	public class LeanDragTrail : MonoBehaviour
	{
		// This class stores additional data for each LeanFinger
		[System.Serializable]
		public class FingerData : LeanFingerData
		{
			public LineRenderer Line;
			public float        Age;
			public float        Width;
		}

		/// <summary>The method used to find fingers to use with this component. See LeanFingerFilter documentation for more information.</summary>
		public LeanFingerFilter Use = new LeanFingerFilter(true);

		/// <summary>The method used to convert between screen coordinates, and world coordinates.</summary>
		public LeanScreenDepth ScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.FixedDistance, Physics.DefaultRaycastLayers, 10.0f);

		/// <summary>The line prefab that will be used to render the trails.</summary>
		public LineRenderer Prefab { set { prefab = value; } get { return prefab; } } [SerializeField] private LineRenderer prefab;

		/// <summary>The maximum amount of active trails.
		/// -1 = Unlimited.</summary>
		public int MaxTrails { set { maxTrails = value; } get { return maxTrails; } } [SerializeField] private int maxTrails = -1;

		/// <summary>How many seconds it takes for each trail to disappear after a finger is released.</summary>
		public float FadeTime { set { fadeTime = value; } get { return fadeTime; } } [SerializeField] protected float fadeTime = 1.0f;

		/// <summary>The color of the trail start.</summary>
		public Color StartColor { set { startColor = value; } get { return startColor; } } [SerializeField] protected Color startColor = Color.white;

		/// <summary>The color of the trail end.</summary>
		public Color EndColor { set { endColor = value; } get { return endColor; } } [SerializeField] protected Color endColor = Color.white;

		// This stores all the links between fingers and LineRenderer instances
		[SerializeField]
		protected List<FingerData> fingerDatas = new List<FingerData>();

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually add a finger.</summary>
		public void AddFinger(LeanFinger finger)
		{
			Use.AddFinger(finger);
		}

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually remove a finger.</summary>
		public void RemoveFinger(LeanFinger finger)
		{
			Use.RemoveFinger(finger);
		}

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually remove all fingers.</summary>
		public void RemoveAllFingers()
		{
			Use.RemoveAllFingers();
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}
#endif

		protected virtual void Awake()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}

		protected virtual void OnEnable()
		{
			LeanTouch.OnFingerUp += HandleFingerUp;
		}

		protected virtual void OnDisable()
		{
			LeanTouch.OnFingerUp -= HandleFingerUp;
		}

		protected virtual void Update()
		{
			// Get the fingers we want to use
			var fingers = Use.UpdateAndGetFingers(true);

			for (var i = 0; i < fingers.Count; i++)
			{
				var finger = fingers[i];

				if (LeanFingerData.Exists(fingerDatas, finger) == false)
				{
					// Too many active links?
					if (maxTrails >= 0 && LeanFingerData.Count(fingerDatas) >= maxTrails)
					{
						continue;
					}

					if (prefab != null)
					{
						// Spawn and activate
						var clone = Instantiate(prefab);

						clone.gameObject.SetActive(true);

						// Register with FingerData
						var fingerData = LeanFingerData.FindOrCreate(ref fingerDatas, finger);

						fingerData.Line  = clone;
						fingerData.Age   = 0.0f;
						fingerData.Width = prefab.widthMultiplier;
					}
				}
			}

			// Update all FingerData
			for (var i = fingerDatas.Count - 1; i >= 0; i--)
			{
				var fingerData = fingerDatas[i];

				if (fingerData.Line != null)
				{
					UpdateLine(fingerData, fingerData.Finger, fingerData.Line);

					if (fingerData.Age >= fadeTime)
					{
						Destroy(fingerData.Line.gameObject);

						fingerDatas.RemoveAt(i);
					}
				}
				else
				{
					fingerDatas.RemoveAt(i);
				}
			}
		}

		protected virtual void UpdateLine(FingerData fingerData, LeanFinger finger, LineRenderer line)
		{
			var color0 = startColor;
			var color1 =   endColor;

			if (finger != null)
			{
				// Reserve one point for each snapshot
				line.positionCount = finger.Snapshots.Count;

				// Loop through all snapshots
				for (var i = 0; i < finger.Snapshots.Count; i++)
				{
					var snapshot = finger.Snapshots[i];

					// Get the world position of this snapshot
					var worldPoint = ScreenDepth.Convert(snapshot.ScreenPosition, gameObject);

					// Write position
					line.SetPosition(i, worldPoint);
				}
			}
			else
			{
				fingerData.Age += Time.deltaTime;

				var alpha = Mathf.InverseLerp(fadeTime, 0.0f, fingerData.Age);

				color0.a *= alpha;
				color1.a *= alpha;
			}

			line.startColor = color0;
			line.endColor   = color1;
		}

		protected virtual void HandleFingerUp(LeanFinger finger)
		{
			var link = LeanFingerData.Find(fingerDatas, finger);

			if (link != null)
			{
				link.Finger = null; // The line will gradually fade out in Update
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanDragTrail;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET), true)]
	public class LeanDragTrail_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("Use");
			Draw("ScreenDepth");
			BeginError(Any(tgts, t => t.Prefab == null));
				Draw("prefab", "The line prefab that will be used to render the trails.");
			EndError();
			Draw("maxTrails", "The maximum amount of active trails.\n\n-1 = Unlimited.");

			Separator();

			Draw("fadeTime", "How many seconds it takes for each trail to disappear after a finger is released.");
			Draw("startColor", "The color of the trail start.");
			Draw("endColor", "The color of the trail end.");
		}
	}
}
#endif