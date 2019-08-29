using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	/// <summary>This script will draw the path each finger has taken since it started being pressed.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerTrail")]
	public class LeanFingerTrail : MonoBehaviour
	{
		// This class will store an association between a Finger and a LineRenderer instance
		[System.Serializable]
		public class FingerData : LeanFingerData
		{
			public LineRenderer Line; // The LineRenderer instance associated with this link
		}

		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("Must RequiredSelectable.IsSelected be true?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("The line prefab")]
		public LineRenderer LinePrefab;

		[Tooltip("The conversion method used to find a world point from a screen point")]
		public LeanScreenDepth ScreenDepth;

		[Tooltip("The maximum amount of fingers used")]
		public int MaxLines;

		// This stores all the links
		private List<FingerData> fingerDatas = new List<FingerData>();

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

		protected virtual void OnEnable()
		{
			// Hook events
			LeanTouch.OnFingerSet += FingerSet;
			LeanTouch.OnFingerUp  += FingerUp;
		}

		protected virtual void OnDisable()
		{
			// Unhook events
			LeanTouch.OnFingerSet -= FingerSet;
			LeanTouch.OnFingerUp  -= FingerUp;
		}

		// Override the WritePositions method from LeanDragLine
		protected virtual void WritePositions(LineRenderer line, LeanFinger finger)
		{
			// Reserve one vertex for each snapshot
			line.positionCount = finger.Snapshots.Count;

			// Loop through all snapshots
			for (var i = 0; i < finger.Snapshots.Count; i++)
			{
				var snapshot = finger.Snapshots[i];

				// Get the world postion of this snapshot
				var worldPoint = ScreenDepth.Convert(snapshot.ScreenPosition, gameObject);

				// Write position
				line.SetPosition(i, worldPoint);
			}
		}

		private void FingerSet(LeanFinger finger)
		{
			// ignore?
			if (MaxLines > 0 && fingerDatas.Count >= MaxLines)
			{
				return;
			}

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

			// Get data for this finger
			var fingerData = LeanFingerData.FindOrCreate(ref fingerDatas, finger);

			// Create line?
			if (fingerData.Line == null)
			{
				fingerData.Line = Instantiate(LinePrefab);
			}

			// Write line data
			WritePositions(fingerData.Line, fingerData.Finger);
		}

		private void FingerUp(LeanFinger finger)
		{
			// Find link for this finger, and clear it
			var fingerData = LeanFingerData.Find(ref fingerDatas, finger);

			if (fingerData != null)
			{
				fingerDatas.Remove(fingerData);

				LinkFingerUp(fingerData);

				if (fingerData.Line != null)
				{
					Destroy(fingerData.Line.gameObject);
				}
			}
		}

		protected virtual void LinkFingerUp(FingerData link)
		{
		}
	}
}