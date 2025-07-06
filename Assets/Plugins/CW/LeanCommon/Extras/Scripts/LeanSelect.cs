using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This is the base class for all object selectors.</summary>
	[HelpURL(LeanCommon.HelpUrlPrefix + "LeanSelect")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Select")]
	public class LeanSelect : MonoBehaviour
	{
		[System.Serializable] public class LeanSelectableEvent : UnityEvent<LeanSelectable> {}

		public enum ReselectType
		{
			KeepSelected,
			Deselect,
			DeselectAndSelect,
			SelectAgain
		}

		public enum LimitType
		{
			Unlimited,
			StopAtMax,
			DeselectFirst
		}

		public static LinkedList<LeanSelect> Instances = new LinkedList<LeanSelect>(); [System.NonSerialized] private LinkedListNode<LeanSelect> instancesNode;

		/// <summary>If you attempt to select a point that has no objects underneath, should all currently selected objects be deselected?</summary>
		public bool DeselectWithNothing { set { deselectWithNothing = value; } get { return deselectWithNothing; } } [SerializeField] private bool deselectWithNothing;

		/// <summary>If you have selected the maximum number of objects, what should happen?
		/// Unlimited = Always allow selection.
		/// StopAtMax = Allow selection up to the <b>MaxSelectables</b> count, then do nothing.
		/// DeselectFirst = Always allow selection, but deselect the first object when the <b>MaxSelectables</b> count is reached.</summary>
		public LimitType Limit { set { limit = value; } get { return limit; } } [SerializeField] private LimitType limit;

		/// <summary>The maximum number of selectables that can be selected at the same time.
		/// 0 = Unlimited.</summary>
		public int MaxSelectables { set { maxSelectables = value; } get { return maxSelectables; } } [SerializeField] private int maxSelectables = 5;

		/// <summary>If you select an already selected selectable, what should happen?</summary>
		public ReselectType Reselect { set { reselect = value; } get { return reselect; } } [SerializeField] private ReselectType reselect = ReselectType.SelectAgain;

		/// <summary>This stores all objects selected by this component.</summary>
		public List<LeanSelectable> Selectables { get { if (selectables == null) selectables = new List<LeanSelectable>(); return selectables; } } [SerializeField] protected List<LeanSelectable> selectables;

		/// <summary>This is invoked when an object is selected.</summary>
		public LeanSelectableEvent OnSelected { get { if (onSelected == null) onSelected = new LeanSelectableEvent(); return onSelected; } } [SerializeField] private LeanSelectableEvent onSelected;

		/// <summary>This is invoked when an object is deselected.</summary>
		public LeanSelectableEvent OnDeselected { get { if (onDeselected == null) onDeselected = new LeanSelectableEvent(); return onDeselected; } } [SerializeField] private LeanSelectableEvent onDeselected;

		/// <summary>This is invoked when you try to select, but nothing is found.</summary>
		public UnityEvent OnNothing { get { if (onNothing == null) onNothing = new UnityEvent(); return onNothing; } } [SerializeField] private UnityEvent onNothing;

		public static event System.Action<LeanSelect, LeanSelectable> OnAnySelected;

		public static event System.Action<LeanSelect, LeanSelectable> OnAnyDeselected;

		public bool IsSelected(LeanSelectable selectable)
		{
			return selectables != null && selectables.Contains(selectable);
		}

		/// <summary>This will select the specified object and add it to this component's <b>Selectables</b> list, if it isn't already there.</summary>
		public void Select(LeanSelectable selectable)
		{
			TrySelect(selectable);
		}

		/// <summary>This remove the specified object from this component's <b>Selectables</b> list if present, and deselect it.</summary>
		public void Deselect(LeanSelectable selectable)
		{
			if (selectable != null && selectables != null)
			{
				TryDeselect(selectable);
			}
		}

		protected bool TrySelect(LeanSelectable selectable)
		{
			if (CwHelper.Enabled(selectable) == true)
			{
				if (TryReselect(selectable) == true) 
				{
					if (Selectables.Contains(selectable) == false) // NOTE: Property
					{
						switch (limit)
						{
							case LimitType.Unlimited:
							{
							}
							break;

							case LimitType.StopAtMax:
							{
								if (selectables.Count >= maxSelectables)
								{
									return false;
								}
							}
							break;

							case LimitType.DeselectFirst:
							{
								if (selectables.Count > 0 && selectables.Count >= maxSelectables)
								{
									TryDeselect(selectables[0]);
								}
							}
							break;
						}
					}

					selectables.Add(selectable);

					if (onSelected != null) onSelected.Invoke(selectable);

					if (OnAnySelected != null) OnAnySelected.Invoke(this, selectable);

					selectable.InvokeOnSelected(this);

					return true;
				}
			}
			// Nothing was selected?
			else
			{
				if (onNothing != null) onNothing.Invoke();

				if (deselectWithNothing == true)
				{
					DeselectAll();
				}
			}

			return false;
		}

		private bool TryReselect(LeanSelectable selectable)
		{
			switch (reselect)
			{
				case ReselectType.KeepSelected:
				{
					if (Selectables.Contains(selectable) == false) // NOTE: Property
					{
						return true;
					}
				}
				break;

				case ReselectType.Deselect:
				{
					if (Selectables.Contains(selectable) == false) // NOTE: Property
					{
						return true;
					}
					else
					{
						Deselect(selectable);
					}
				}
				break;

				case ReselectType.DeselectAndSelect:
				{
					if (Selectables.Contains(selectable) == true) // NOTE: Property
					{
						Deselect(selectable);
					}
				}
				return true;

				case ReselectType.SelectAgain:
				{
				}
				return true;
			}

			return false;
		}

		protected bool TryDeselect(LeanSelectable selectable)
		{
			if (selectables != null)
			{
				var index = selectables.IndexOf(selectable);

				if (index >= 0)
				{
					return TryDeselect(index);
				}
			}

			return false;
		}

		protected bool TryDeselect(int index)
		{
			var success = false;

			if (selectables != null && index >= 0 && index < selectables.Count)
			{
				var selectable = selectables[index];

				selectables.RemoveAt(index);

				if (selectable != null)
				{
					selectable.InvokeOnDeslected(this);

					if (onDeselected != null)
					{
						onDeselected.Invoke(selectable);
					}

					if (OnAnyDeselected != null)
					{
						OnAnyDeselected.Invoke(this, selectable);
					}
				}

				success = true;
			}

			return success;
		}

		/// <summary>This will deselect all objects that were selected by this component.</summary>
		[ContextMenu("Deselect All")]
		public void DeselectAll()
		{
			if (selectables != null)
			{
				while (selectables.Count > 0)
				{
					var index      = selectables.Count - 1;
					var selectable = selectables[index];

					selectables.RemoveAt(index);

					selectable.InvokeOnDeslected(this);
				}
			}
		}

		/// <summary>This will deselect objects in chronological order until the selected object count reaches the specified amount.</summary>
		public void Cull(int maxCount)
		{
			if (selectables != null)
			{
				while (selectables.Count > 0 && selectables.Count > maxCount)
				{
					var selectable = selectables[0];

					selectables.RemoveAt(0);

					if (selectable != null)
					{
						if (selectable != null)
						{
							Deselect(selectable);
						}
					}
				}
			}
		}

		protected virtual void OnEnable()
		{
			instancesNode = Instances.AddLast(this);
		}

		protected virtual void OnDisable()
		{
			Instances.Remove(instancesNode); instancesNode = null;
		}

		protected virtual void OnDestroy()
		{
			DeselectAll();
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanSelect;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSelect_Editor : CwEditor
	{
		[System.NonSerialized] TARGET tgt; [System.NonSerialized] TARGET[] tgts;

		protected override void OnInspector()
		{
			GetTargets(out tgt, out tgts);

			Draw("deselectWithNothing", "If you attempt to select a point that has no objects underneath, should all currently selected objects be deselected?");
			Draw("limit", "If you have selected the maximum number of objects, what should happen?\n\nUnlimited = Always allow selection.\n\nStopAtMax = Allow selection up to the <b>MaxSelectables</b> count, then do nothing.\n\nDeselectFirst = Always allow selection, but deselect the first object when the <b>MaxSelectables</b> count is reached.");
			if (Any(tgts, t => t.Limit != LeanSelect.LimitType.Unlimited))
			{
				BeginIndent();
					Draw("maxSelectables", "The maximum number of selectables that can be selected at the same time.\n\n0 = Unlimited.");
				EndIndent();
			}
			Draw("reselect", "If you select an already selected selectable, what should happen?");

			Separator();

			var select   = (LeanSelectable)UnityEditor.EditorGUILayout.ObjectField(new GUIContent("Select", "Drop a selectable object here to select it."), null, typeof(LeanSelectable), true);
			var deselect = (LeanSelectable)UnityEditor.EditorGUILayout.ObjectField(new GUIContent("Deselect", "Drop a selectable object here to deselect it."), null, typeof(LeanSelectable), true);

			BeginDisabled();
				Draw("selectables", "This stores all objects selected by this component.");
			EndDisabled();

			Separator();

			var showUnusedEvents = DrawFoldout("Show Unused Events", "Show all events?");

			DrawEvents(showUnusedEvents);

			if (select != null)
			{
				Each(tgts, t => t.Select(select), true);
			}

			if (deselect != null)
			{
				Each(tgts, t => t.Deselect(deselect), true);
			}
		}

		protected virtual void DrawEvents(bool showUnusedEvents)
		{
			if (Any(tgts, t => t.OnSelected.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onSelected");
			}

			if (Any(tgts, t => t.OnDeselected.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onDeselected");
			}

			if (Any(tgts, t => t.OnNothing.GetPersistentEventCount() > 0) == true || showUnusedEvents == true)
			{
				Draw("onNothing");
			}
		}
	}
}
#endif