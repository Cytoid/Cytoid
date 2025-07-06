using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Lean.Common;

namespace Lean.Touch
{
	/// <summary>This component allows you to select LeanSelectable components.
	/// To use it, you can call the SelectScreenPosition method from somewhere (e.g. the LeanFingerTap.OnTap event).</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanSelectByFinger")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Select By Finger")]
	public class LeanSelectByFinger : LeanSelect
	{
		[System.Serializable] public class LeanSelectableLeanFingerEvent : UnityEvent<LeanSelectable, LeanFinger> {}

		public LeanScreenQuery ScreenQuery = new LeanScreenQuery(LeanScreenQuery.MethodType.Raycast);

		/// <summary>If you enable this then any selected object will automatically be deselected if the finger used to select it is no longer touching the screen.</summary>
		public bool DeselectWithFingers { set { deselectWithFingers = value; } get { return deselectWithFingers; } } [SerializeField] private bool deselectWithFingers;

		/// <summary>This is invoked when an object is selected.</summary>
		public LeanSelectableLeanFingerEvent OnSelectedFinger { get { if (onSelectedFinger == null) onSelectedFinger = new LeanSelectableLeanFingerEvent(); return onSelectedFinger; } } [SerializeField] private LeanSelectableLeanFingerEvent onSelectedFinger;

		public static event System.Action<LeanSelectByFinger, LeanSelectable, LeanFinger> OnAnySelectedFinger;

		/// <summary>This method allows you to initiate selection at the finger's <b>StartScreenPosition</b>.
		/// NOTE: This method be called from somewhere for this component to work (e.g. LeanFingerTap).</summary>
		public void SelectStartScreenPosition(LeanFinger finger)
		{
			SelectScreenPosition(finger, finger.StartScreenPosition);
		}

		/// <summary>This method allows you to initiate selection at the finger's current <b>ScreenPosition</b>.
		/// NOTE: This method be called from somewhere for this component to work (e.g. LeanFingerTap).</summary>
		public void SelectScreenPosition(LeanFinger finger)
		{
			SelectScreenPosition(finger, finger.ScreenPosition);
		}

		/// <summary>This method allows you to initiate selection of a finger at a custom screen position.
		/// NOTE: This method be called from a custom script for this component to work.</summary>
		public void SelectScreenPosition(LeanFinger finger, Vector2 screenPosition)
		{
			var result = ScreenQuery.Query<LeanSelectable>(gameObject, screenPosition);

			Select(result, finger);
		}

		/// <summary>This method allows you to manually select an object with the specified finger using this component's selection settings.</summary>
		public void Select(LeanSelectable selectable, LeanFinger finger)
		{
			var pair = new LeanSelectableByFinger.SelectedPair() { Finger = finger, Select = this };

			if (TrySelect(selectable) == true)
			{
				var selectableByFinger = selectable as LeanSelectableByFinger;

				if (selectableByFinger != null)
				{
					if (selectableByFinger.SelectingPairs.Contains(pair) == false)
					{
						selectableByFinger.SelectingPairs.Add(pair);
					}
					
					selectableByFinger.OnSelectedFinger.Invoke(finger);
					selectableByFinger.OnSelectedSelectFinger.Invoke(this, finger);

					LeanSelectableByFinger.InvokeAnySelectedFinger(this, selectableByFinger, finger);

					if (finger.Up == true)
					{
						selectableByFinger.OnSelectedFingerUp.Invoke(finger);
						selectableByFinger.OnSelectedSelectFingerUp.Invoke(this, finger);

						selectableByFinger.SelectingPairs.Remove(pair);
					}
				}

				if (onSelectedFinger != null) onSelectedFinger.Invoke(selectable, finger);

				if (OnAnySelectedFinger != null) OnAnySelectedFinger.Invoke(this, selectable, finger);
			}
			else
			{
				if (finger.Up == false)
				{
					var selectableByFinger = selectable as LeanSelectableByFinger;

					if (selectableByFinger != null)
					{
						if (selectableByFinger.SelectingPairs.Contains(pair) == false)
						{
							selectableByFinger.SelectingPairs.Add(pair);
						}
					}
				}
			}
		}

		protected virtual void Update()
		{
			if (deselectWithFingers == true && selectables != null)
			{
				for (var i = selectables.Count - 1; i >= 0; i--)
				{
					var selectable = selectables[i];

					if (ShouldRemoveSelectable(selectable) == true)
					{
						Deselect(selectable);
					}
				}
			}
		}

		private bool ShouldRemoveSelectable(LeanSelectable selectable)
		{
			var selectableByFinger = selectable as LeanSelectableByFinger;

			if (selectableByFinger != null)
			{
				foreach (var pair in selectableByFinger.SelectingPairs)
				{
					if (pair.Finger.Up == false)
					{
						return false;
					}
				}
			}

			return true;
		}

		/// <summary>This allows you to replace the currently selected objects with the ones in the specified list. This is useful if you're doing box selection or switching selection groups.</summary>
		public void ReplaceSelection(List<LeanSelectable> newSelectables, LeanFinger finger)
		{
			if (newSelectables != null)
			{
				// Deselect missing selectables
				if (selectables != null)
				{
					for (var i = selectables.Count - 1; i >= 0; i--)
					{
						var selectable = selectables[i];

						if (newSelectables.Contains(selectable) == false)
						{
							Deselect(selectable);
						}
					}
				}

				// Select new selectables
				foreach (var selectable in newSelectables)
				{
					if (selectables == null || selectables.Contains(selectable) == false)
					{
						var selectableByFinger = selectable as LeanSelectableByFinger;

						if (selectableByFinger != null)
						{
							Select(selectableByFinger, finger);
						}
						else
						{
							Select(selectable);
						}
					}
				}
			}
			else
			{
				DeselectAll();
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanSelectByFinger;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSelectByFinger_Editor : Common.Editor.LeanSelect_Editor
	{
		[System.NonSerialized] TARGET tgt; [System.NonSerialized] TARGET[] tgts;

		protected override void OnInspector()
		{
			GetTargets(out tgt, out tgts);

			Draw("ScreenQuery");
			Draw("deselectWithFingers", "If you enable this then any selected object will automatically be deselected if the finger used to select it is no longer touching the screen.");

			base.OnInspector();
		}

		protected override void DrawEvents(bool showUnusedEvents)
		{
			base.DrawEvents(showUnusedEvents);

			if (Any(tgts, t => t.OnSelectedFinger.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onSelectedFinger");
			}
		}
	}
}
#endif