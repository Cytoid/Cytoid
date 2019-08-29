using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Touch
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanSelectable))]
	public class LeanSelectable_Inspector : Common.LeanInspector<LeanSelectable>
	{
		private bool showUnusedEvents;

		// Draw the whole inspector
		protected override void DrawInspector()
		{
			// isSelected modified?
			if (Draw("isSelected") == true)
			{
				// Grab the new value
				var isSelected = serializedObject.FindProperty("isSelected").boolValue;

				// Apply it directly to each instance before the SerializedObject applies it when this method returns
				Each(t => t.IsSelected = isSelected);
			}
			Draw("DeselectOnUp");
			Draw("HideWithFinger");
			Draw("IsolateSelectingFingers");

			EditorGUILayout.Separator();

			var usedA = Any(t => t.OnSelect.GetPersistentEventCount() > 0);
			var usedB = Any(t => t.OnSelectSet.GetPersistentEventCount() > 0);
			var usedC = Any(t => t.OnSelectUp.GetPersistentEventCount() > 0);
			var usedD = Any(t => t.OnDeselect.GetPersistentEventCount() > 0);

			showUnusedEvents = EditorGUILayout.Foldout(showUnusedEvents, "Show Unused Events");

			if (usedA == true || showUnusedEvents == true)
			{
				Draw("onSelect");
			}

			if (usedB == true || showUnusedEvents == true)
			{
				Draw("onSelectSet");
			}

			if (usedC == true || showUnusedEvents == true)
			{
				Draw("onSelectUp");
			}

			if (usedD == true || showUnusedEvents == true)
			{
				Draw("onDeselect");
			}
		}
	}
}
#endif

namespace Lean.Touch
{
	/// <summary>This component makes this GameObject selectable.
	/// If your game is 3D then make sure this GameObject or a child has a Collider component.
	/// If your game is 2D then make sure this GameObject or a child has a Collider2D component.
	/// If your game is UI based then make sure this GameObject or a child has a graphic with "Raycast Target" enabled.
	/// To then select it, you can add the LeanSelect and LeanFingerTap components to your scene. You can then link up the LeanFingerTap.OnTap event to LeanSelect.SelectScreenPosition.</summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanSelectable")]
	public class LeanSelectable : MonoBehaviour
	{
		// Event signature
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}

		public static List<LeanSelectable> Instances = new List<LeanSelectable>();

		public static System.Action<LeanSelectable, LeanFinger> OnSelectGlobal;

		public static System.Action<LeanSelectable, LeanFinger> OnSelectSetGlobal;

		public static System.Action<LeanSelectable, LeanFinger> OnSelectUpGlobal;

		public static System.Action<LeanSelectable> OnDeselectGlobal;

		[Tooltip("Should this get deselected when the selecting finger goes up?")]
		public bool DeselectOnUp;

		[Tooltip("Should IsSelected temporarily return false if the selecting finger is still being held? This is useful when selecting multiple objects using a complex gesture (e.g. RTS style selection box)")]
		public bool HideWithFinger;

		[Tooltip("If the selecting fingers are still active, only return those to RequiredSelectable queries?")]
		public bool IsolateSelectingFingers;

		/// <summary>This event is called when selection begins (finger = the finger that selected this).</summary>
		public LeanFingerEvent OnSelect { get { if (onSelect == null) onSelect = new LeanFingerEvent(); return onSelect; } } [FormerlySerializedAs("OnSelect")] [SerializeField] private LeanFingerEvent onSelect;

		/// <summary>This event is called every frame this selectable is selected with a finger (finger = the finger that selected this).</summary>
		public LeanFingerEvent OnSelectSet { get { if (onSelectSet == null) onSelectSet = new LeanFingerEvent(); return onSelectSet; } } [FormerlySerializedAs("OnSelectSet")] [SerializeField] private LeanFingerEvent onSelectSet;

		/// <summary>This event is called when the selecting finger goes up (finger = the finger that selected this).</summary>
		public LeanFingerEvent OnSelectUp { get { if (onSelectUp == null) onSelectUp = new LeanFingerEvent(); return onSelectUp; } } [FormerlySerializedAs("OnSelectUp")] [SerializeField] private LeanFingerEvent onSelectUp;

		/// <summary>This event is called when this is deselected, if OnSelectUp hasn't been called yet, it will get called first.</summary>
		public UnityEvent OnDeselect { get { if (onDeselect == null) onDeselect = new UnityEvent(); return onDeselect; } } [FormerlySerializedAs("OnDeselect")] [SerializeField] private UnityEvent onDeselect;

		/// <summary>If you want to change this, do it via the Select/Deselect methods (accessible from the context menu gear icon in editor)</summary>
		[Tooltip("If you want to change this, do it via the Select/Deselect methods (accessible from the context menu gear icon in editor)")]
		[SerializeField]
		private bool isSelected;

		// The fingers that were used to select this GameObject
		// If a finger goes up then it will be removed from this list
		[System.NonSerialized]
		private List<LeanFinger> selectingFingers = new List<LeanFinger>();

		/// <summary>Returns isSelected, or false if HideWithFinger is true and SelectingFinger is still set.</summary>
		public bool IsSelected
		{
			set
			{
				if (value == true)
				{
					Select();
				}
				else
				{
					Deselect();
				}
			}

			get
			{
				// Hide IsSelected?
				if (HideWithFinger == true && isSelected == true && selectingFingers.Count > 0)
				{
					return false;
				}

				return isSelected;
			}
		}

		/// <summary>Bypass HideWithFinger.</summary>
		public bool IsSelectedRaw
		{
			get
			{
				return isSelected;
			}
		}

		/// <summary>This tells you how many LeanSelectable objects in your scene are currently selected.</summary>
		public static int IsSelectedCount
		{
			get
			{
				var count = 0;

				for (var i = Instances.Count - 1; i >= 0; i--)
				{
					if (Instances[i].IsSelected == true)
					{
						count += 1;
					}
				}

				return count;
			}
		}

		/// <summary>This tells you the first or earliest still active finger that initiated selection of this object.
		/// NOTE: If the selecting finger went up then this may return null.</summary>
		public LeanFinger SelectingFinger
		{
			get
			{
				if (selectingFingers.Count > 0)
				{
					return selectingFingers[0];
				}

				return null;
			}
		}

		/// <summary>This tells you every currently active finger that selected this object.</summary>
		public List<LeanFinger> SelectingFingers
		{
			get
			{
				return selectingFingers;
			}
		}

		/// <summary>If requiredSelectable is set and not selected, the fingers list will be empty. If selected then the fingers list will only contain the selecting finger.</summary>
		public static List<LeanFinger> GetFingers(bool ignoreIfStartedOverGui, bool ignoreIfOverGui, int requiredFingerCount = 0, LeanSelectable requiredSelectable = null)
		{
			var fingers = LeanTouch.GetFingers(ignoreIfStartedOverGui, ignoreIfOverGui, requiredFingerCount);

			if (requiredSelectable != null)
			{
				if (requiredSelectable.IsSelected == false)
				{
					fingers.Clear();
				}

				if (requiredSelectable.IsolateSelectingFingers == true)
				{
					fingers.Clear();

					fingers.AddRange(requiredSelectable.selectingFingers);
				}
			}

			return fingers;
		}

		/// <summary>This allows you to limit how many objects can be selected in your scene.</summary>
		public static void Cull(int maxCount)
		{
			var count = 0;

			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				if (selectable.IsSelected == true)
				{
					count += 1;

					if (count > maxCount)
					{
						selectable.Deselect();
					}
				}
			}
		}

		/// <summary>If the specified finger selected an object, this will return the first one.</summary>
		public static LeanSelectable FindSelectable(LeanFinger finger)
		{
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				if (selectable.IsSelectedBy(finger) == true)
				{
					return selectable;
				}
			}

			return null;
		}

		/// <summary>This allows you to replace the currently selected objects with the ones in the specified list. This is useful if you're doing box selection or switching selection groups.</summary>
		public static void ReplaceSelection(LeanFinger finger, List<LeanSelectable> selectables)
		{
			var selectableCount = 0;

			// Deselect missing selectables
			if (selectables != null)
			{
				for (var i = Instances.Count - 1; i >= 0; i--)
				{
					var selectable = Instances[i];

					if (selectable.isSelected == true && selectables.Contains(selectable) == false)
					{
						selectable.Deselect();
					}
				}
			}

			// Add new selectables
			if (selectables != null)
			{
				for (var i = selectables.Count - 1; i >= 0; i--)
				{
					var selectable = selectables[i];

					if (selectable != null)
					{
						if (selectable.isSelected == false)
						{
							selectable.Select(finger);
						}

						selectableCount += 1;
					}
				}
			}

			// Nothing was selected?
			if (selectableCount == 0)
			{
				DeselectAll();
			}
		}

		/// <summary>This tells you if the current selectable was selected by the specified finger.</summary>
		public bool IsSelectedBy(LeanFinger finger)
		{
			for (var i = selectingFingers.Count - 1; i >= 0; i--)
			{
				if (selectingFingers[i] == finger)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>This tells you the IsSelected or IsSelectedRaw value.</summary>
		public bool GetIsSelected(bool raw)
		{
			return raw == true ? IsSelectedRaw : IsSelected;
		}

		/// <summary>This selects the current object.</summary>
		[ContextMenu("Select")]
		public void Select()
		{
			Select(null);
		}

		/// <summary>This selects the current object with the specified finger.</summary>
		public void Select(LeanFinger finger)
		{
			isSelected = true;

			if (finger != null)
			{
				if (IsSelectedBy(finger) == false)
				{
					selectingFingers.Add(finger);
				}
			}

			if (onSelect != null)
			{
				onSelect.Invoke(finger);
			}

			if (OnSelectGlobal != null)
			{
				OnSelectGlobal(this, finger);
			}

			// Make sure FingerUp is only registered once
			LeanTouch.OnFingerUp -= FingerUp;
			LeanTouch.OnFingerUp += FingerUp;

			// Make sure FingerSet is only registered once
			LeanTouch.OnFingerSet -= FingerSet;
			LeanTouch.OnFingerSet += FingerSet;
		}

		/// <summary>This deselects the current object.</summary>
		[ContextMenu("Deselect")]
		public void Deselect()
		{
			// Make sure we don't deselect multiple times
			if (isSelected == true)
			{
				isSelected = false;

				for (var i = selectingFingers.Count - 1; i >= 0; i--)
				{
					var selectingFinger = selectingFingers[i];

					if (selectingFinger != null)
					{
						if (onSelectUp != null)
						{
							onSelectUp.Invoke(selectingFinger);
						}

						if (OnSelectUpGlobal != null)
						{
							OnSelectUpGlobal(this, selectingFinger);
						}
					}
				}

				selectingFingers.Clear();

				if (onDeselect != null)
				{
					onDeselect.Invoke();
				}

				if (OnDeselectGlobal != null)
				{
					OnDeselectGlobal(this);
				}
			}
		}

		/// <summary>This deselects all objects in the scene.</summary>
		public static void DeselectAll()
		{
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				Instances[i].Deselect();
			}
		}

		protected virtual void OnEnable()
		{
			// Register instance
			Instances.Add(this);
		}

		protected virtual void OnDisable()
		{
			// Unregister instance
			Instances.Remove(this);

			if (isSelected == true)
			{
				Deselect();
			}
		}

		protected virtual void LateUpdate()
		{
			// Null the selecting finger?
			// NOTE: This is done in LateUpdate so certain OnFingerUp actions that require checking SelectingFinger can still work properly
			for (var i = selectingFingers.Count - 1; i >= 0; i--)
			{
				var selectingFinger = selectingFingers[i];

				if (selectingFinger.Set == false || isSelected == false)
				{
					selectingFingers.RemoveAt(i);
				}
			}
		}

		private static void FingerSet(LeanFinger finger)
		{
			// Loop through all selectables
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				// Was this selected with this finger?
				if (selectable.IsSelectedBy(finger) == true)
				{
					if (selectable.onSelectSet != null)
					{
						selectable.onSelectSet.Invoke(finger);
					}

					if (OnSelectSetGlobal != null)
					{
						OnSelectSetGlobal(selectable, finger);
					}
				}
			}
		}

		private static void FingerUp(LeanFinger finger)
		{
			// Loop through all selectables
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				// Was this selected with this finger?
				for (var j = selectable.selectingFingers.Count - 1; j >= 0; j--)
				{
					if (selectable.selectingFingers[j] == finger)
					{
						if (selectable.DeselectOnUp == true && selectable.IsSelected == true && selectable.selectingFingers.Count == 1)
						{
							selectable.Deselect();
						}
						// Deselection will call onSelectUp
						else
						{
							// Null the finger and call onSelectUp
							selectable.selectingFingers.RemoveAt(j);

							if (selectable.onSelectUp != null)
							{
								selectable.onSelectUp.Invoke(finger);
							}
						}
					}
				}
			}
		}
	}
}