using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Touch
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanSelect))]
	public class LeanSelect_Inspector : Lean.Common.LeanInspector<LeanSelect>
	{
	}
}
#endif

namespace Lean.Touch
{
	/// <summary>This component allows you to select LeanSelectable components.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanSelect")]
	public class LeanSelect : MonoBehaviour
	{
		public enum SelectType
		{
			Manually = -1,
			Raycast3D,
			Overlap2D,
			CanvasUI
		}

		public enum SearchType
		{
			GetComponent,
			GetComponentInParent,
			GetComponentInChildren
		}

		public enum ReselectType
		{
			KeepSelected,
			Deselect,
			DeselectAndSelect,
			SelectAgain
		}

		/// <summary>This stores all active and enabled LeanSelect instances in the scene.</summary>
		public static List<LeanSelect> Instances = new List<LeanSelect>();

		public SelectType SelectUsing;

		[Tooltip("The layers you want the raycast/overlap to hit")]
		public LayerMask LayerMask = Physics.DefaultRaycastLayers;

		[Tooltip("The camera used to calculate the ray (None = MainCamera)")]
		public Camera Camera;

		[Tooltip("The maximum number of selectables that can be selected at the same time (0 = Unlimited)")]
		public int MaxSelectables;

		[Tooltip("How should the candidate GameObjects be searched for the LeanSelectable component?")]
		public SearchType Search = SearchType.GetComponentInParent;

		[Tooltip("If you select an already selected selectable, what should happen?")]
		public ReselectType Reselect = ReselectType.SelectAgain;

		[Tooltip("Automatically deselect everything if nothing was selected?")]
		public bool AutoDeselect;

		// NOTE: This must be called from somewhere
		public void SelectStartScreenPosition(LeanFinger finger)
		{
			SelectScreenPosition(finger, finger.StartScreenPosition);
		}

		// NOTE: This must be called from somewhere
		public void SelectScreenPosition(LeanFinger finger)
		{
			SelectScreenPosition(finger, finger.ScreenPosition);
		}

		// NOTE: This must be called from somewhere
		public virtual void SelectScreenPosition(LeanFinger finger, Vector2 screenPosition)
		{
			// Stores the component we hit (Collider or Collider2D)
			var component = default(Component);

			TryGetComponent(SelectUsing, screenPosition, ref component);

			Select(finger, component);
		}

		protected void TryGetComponent(SelectType selectUsing, Vector2 screenPosition, ref Component component)
		{
			switch (selectUsing)
			{
				case SelectType.Raycast3D:
				{
					// Make sure the camera exists
					var camera = LeanTouch.GetCamera(Camera, gameObject);

					if (camera != null)
					{
						var ray = camera.ScreenPointToRay(screenPosition);
						var hit = default(RaycastHit);

						if (Physics.Raycast(ray, out hit, float.PositiveInfinity, LayerMask) == true)
						{
							component = hit.collider;
						}
					}
					else
					{
						Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
					}
				}
				break;

				case SelectType.Overlap2D:
				{
					// Make sure the camera exists
					var camera = LeanTouch.GetCamera(Camera, gameObject);

					if (camera != null)
					{
						var point = camera.ScreenToWorldPoint(screenPosition);

						component = Physics2D.OverlapPoint(point, LayerMask);
					}
					else
					{
						Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
					}
				}
				break;

				case SelectType.CanvasUI:
				{
					var results = LeanTouch.RaycastGui(screenPosition, LayerMask);

					if (results != null && results.Count > 0)
					{
						component = results[0].gameObject.transform;
					}
				}
				break;
			}
		}

		public void Select(LeanFinger finger, Component component)
		{
			// Stores the selectable we will search for
			var selectable = default(LeanSelectable);

			// Was a collider found?
			if (component != null)
			{
				switch (Search)
				{
					case SearchType.GetComponent:           selectable = component.GetComponent          <LeanSelectable>(); break;
					case SearchType.GetComponentInParent:   selectable = component.GetComponentInParent  <LeanSelectable>(); break;
					case SearchType.GetComponentInChildren: selectable = component.GetComponentInChildren<LeanSelectable>(); break;
				}
			}

			// Select the selectable
			Select(finger, selectable);
		}

		public void Select(LeanFinger finger, LeanSelectable selectable)
		{
			// Something was selected?
			if (selectable != null && selectable.isActiveAndEnabled == true)
			{
				if (selectable.HideWithFinger == true)
				{
					for (var i = LeanSelectable.Instances.Count - 1; i >= 0; i--)
					{
						var instance = LeanSelectable.Instances[i];

						if (instance.HideWithFinger == true && instance.IsSelected == true)
						{
							return;
						}
					}
				}

				// Did we select a new LeanSelectable?
				if (selectable.IsSelected == false)
				{
					// Deselect some if we have too many
					if (MaxSelectables > 0)
					{
						LeanSelectable.Cull(MaxSelectables - 1);
					}

					// Select
					selectable.Select(finger);
				}
				// Did we reselect the current LeanSelectable?
				else
				{
					switch (Reselect)
					{
						case ReselectType.Deselect:
						{
							selectable.Deselect();
						}
						break;

						case ReselectType.DeselectAndSelect:
						{
							selectable.Deselect();
							selectable.Select(finger);
						}
						break;

						case ReselectType.SelectAgain:
						{
							selectable.Select(finger);
						}
						break;
					}
				}
			}
			// Nothing was selected?
			else
			{
				// Deselect?
				if (AutoDeselect == true)
				{
					DeselectAll();
				}
			}
		}

		[ContextMenu("Deselect All")]
		public void DeselectAll()
		{
			LeanSelectable.DeselectAll();
		}

		protected virtual void OnEnable()
		{
			if (Instances.Count > 0)
			{
				Debug.LogWarning("Your scene already contains a LeanSelect component, using more than once at once may cause selection issues", Instances[0]);
			}

			Instances.Add(this);
		}

		protected virtual void OnDisable()
		{
			Instances.Remove(this);
		}
	}
}