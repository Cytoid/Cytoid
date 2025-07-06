using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This struct stores information about and allows you to search the scene for a specific object on screen space.</summary>
	[System.Serializable]
	public struct LeanScreenQuery
	{
		public enum MethodType
		{
			Raycast
		}

		public enum SearchType
		{
			GetComponent,
			GetComponentInParent,
			GetComponentInChildren
		}

		/// <summary>The method used to search the scene based on a screen position.
		/// Raycast = 3D, 2D, and EventSystem raycast.</summary>
		public MethodType Method;

		/// <summary>The scene will be queried (e.g. Raycast) against these layers.</summary>
		public LayerMask Layers;

		/// <summary>When the query hits a GameObject, how should the desired component be searched for relative to it?</summary>
		public SearchType Search;

		/// <summary>The component found from the search must have this tag.</summary>
		public string RequiredTag;

		/// <summary>The camera used to perform the search.
		/// None = MainCamera.</summary>
		public Camera Camera;

		public float Distance;

		private static RaycastHit[] raycastHits = new RaycastHit[1024];

		private static RaycastHit2D[] raycastHit2Ds = new RaycastHit2D[1024];

		// Used to find if the GUI is in use
		private static List<RaycastResult> tempRaycastResults = new List<RaycastResult>(10);

		// Used by RaycastGui
		private static PointerEventData tempPointerEventData;

		// Used by RaycastGui
		private static EventSystem tempEventSystem;

		private static List<KeyValuePair<GameObject, int>> tempLayers = new List<KeyValuePair<GameObject, int>>();

		public LeanScreenQuery(MethodType newMethod) : this(newMethod, Physics.DefaultRaycastLayers)
		{
		}

		public LeanScreenQuery(MethodType newMethod, LayerMask layers)
		{
			Method      = newMethod;
			Search      = SearchType.GetComponentInParent;
			RequiredTag = null;
			Camera      = null;
			Layers      = layers;
			Distance    = 50.0f;
		}

		public static void ChangeLayers(GameObject root, bool ancestors, bool children)
		{
			tempLayers.Add(new KeyValuePair<GameObject, int>(root, root.layer));

			root.layer = 2; // Ignore raycast

			if (ancestors == true && root.transform.parent != null)
			{
				ChangeLayers(root.transform.parent.gameObject, true, false);
			}

			if (children == true)
			{
				for (var i = root.transform.childCount - 1; i >= 0; i--)
				{
					ChangeLayers(root.transform.GetChild(i).gameObject, false, true);
				}
			}
		}

		public static void RevertLayers()
		{
			foreach (var tempLayer in tempLayers)
			{
				if (tempLayer.Key != null)
				{
					tempLayer.Key.layer = tempLayer.Value;
				}
			}

			tempLayers.Clear();
		}

		public T Query<T>(GameObject gameObject, Vector2 screenPosition)
		{
			var result        = default(T);
			var root          = default(Component);
			var worldPosition = default(Vector3);

			if (TryQuery(gameObject, screenPosition, ref result, ref root, ref worldPosition) == true)
			{
				return result;
			}

			return default(T);
		}

		public bool TryQuery<T>(GameObject gameObject, Vector2 screenPosition, ref T result, ref Component root, ref Vector3 worldPosition)
		{
			var camera       = CwHelper.GetCamera(Camera, gameObject);
			var bestHit      = default(Component);
			var bestDistance = float.PositiveInfinity;
			var bestPosition = default(Vector3);

			if (camera != null)
			{
				if (camera.pixelRect.Contains(screenPosition) == true)
				{
					DoRaycast3D(camera, screenPosition, ref bestHit, ref bestDistance, ref bestPosition);
					DoRaycast2D(camera, screenPosition, ref bestHit, ref bestDistance, ref bestPosition);
					DoRaycastUI(screenPosition, ref bestHit, ref bestDistance, ref bestPosition);
				}
			}

			if (bestHit != null)
			{
				worldPosition = bestPosition;

				return TryResult(bestHit, ref result, ref root);
			}

			return false;
		}

		private bool TryResult<T>(Component hit, ref T result, ref Component component)
		{
			if (hit != null)
			{
				switch (Search)
				{
					case SearchType.GetComponent:           if (TryGetComponent          (hit, ref result, ref component) == false) return false; break;
					case SearchType.GetComponentInParent:   if (TryGetComponentInParent  (hit, ref result, ref component) == false) return false; break;
					case SearchType.GetComponentInChildren: if (TryGetComponentInChildren(hit, ref result, ref component) == false) return false; break;
				}

				// Discard if tag doesn't match
				if (result != null && string.IsNullOrEmpty(RequiredTag) == false && component.tag != RequiredTag)
				{
					return false;
				}

				return true;
			}

			return false;
		}

		private bool TryGetComponent<T>(Component hit, ref T result, ref Component component)
		{
			result = hit.GetComponent<T>();

			if (result != null)
			{
				component = hit;

				return true;
			}

			return false;
		}

		private bool TryGetComponentInParent<T>(Component hit, ref T result, ref Component component)
		{
			if (TryGetComponent(hit, ref result, ref component) == true)
			{
				return true;
			}

			if (hit.transform.parent != null)
			{
				return TryGetComponentInParent(hit.transform.parent, ref result, ref component);
			}

			return false;
		}

		private bool TryGetComponentInChildren<T>(Component hit, ref T result, ref Component component)
		{
			if (TryGetComponent(hit, ref result, ref component) == true)
			{
				return true;
			}

			for (var i = 0; i < hit.transform.childCount; i++)
			{
				if (TryGetComponentInParent(hit.transform.GetChild(i), ref result, ref component) == true)
				{
					return true;
				}
			}

			return false;
		}

		private static int GetClosestRaycastHitsIndex(int count)
		{
			var closestIndex    = -1;
			var closestDistance = float.PositiveInfinity;

			for (var i = 0; i < count; i++)
			{
				var distance = raycastHits[i].distance;

				if (distance < closestDistance)
				{
					closestIndex    = i;
					closestDistance = distance;
				}
			}

			return closestIndex;
		}

		private void DoRaycast3D(Camera camera, Vector2 screenPosition, ref Component bestResult, ref float bestDistance, ref Vector3 bestPosition)
		{
			var ray   = camera.ScreenPointToRay(screenPosition);
			var count = Physics.RaycastNonAlloc(ray, raycastHits, float.PositiveInfinity, Layers);

			if (count > 0)
			{
				var closestHit = raycastHits[GetClosestRaycastHitsIndex(count)];
				var distance   = closestHit.distance;

				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestResult   = closestHit.collider;
					bestPosition = closestHit.point;
				}
			}
		}

		private void DoRaycast2D(Camera camera, Vector2 screenPosition, ref Component bestResult, ref float bestDistance, ref Vector3 bestPosition)
		{
			var ray   = camera.ScreenPointToRay(screenPosition);
			var count = Physics2D.GetRayIntersectionNonAlloc(ray, raycastHit2Ds, float.PositiveInfinity, Layers);

			if (count > 0)
			{
				var closestHit = raycastHit2Ds[0];
				var distance   = closestHit.distance;
				
				if (distance < bestDistance)
				{
					bestDistance = distance;
					bestResult   = closestHit.transform;
					bestPosition = closestHit.point;
				}
			}
		}

		private void DoRaycastUI(Vector2 screenPosition, ref Component bestResult, ref float bestDistance, ref Vector3 bestPosition)
		{
			var currentEventSystem = EventSystem.current;

			if (currentEventSystem == null)
			{
				currentEventSystem = CwHelper.FindAnyObjectByType<EventSystem>();
			}

			if (currentEventSystem != null)
			{
				// Create point event data for this event system?
				if (currentEventSystem != tempEventSystem)
				{
					tempEventSystem = currentEventSystem;

					if (tempPointerEventData == null)
					{
						tempPointerEventData = new PointerEventData(tempEventSystem);
					}
					else
					{
						tempPointerEventData.Reset();
					}
				}

				// Raycast event system at the specified point
				tempPointerEventData.position = screenPosition;

				currentEventSystem.RaycastAll(tempPointerEventData, tempRaycastResults);

				foreach (var result in tempRaycastResults)
				{
					var resultLayer = 1 << result.gameObject.layer;

					if ((resultLayer & Layers) != 0)
					{
						var distance = result.distance;

						if (distance < bestDistance)
						{
							bestDistance = distance;
							bestResult   = result.gameObject.transform;
							bestPosition = result.worldPosition;
						}

						break;
					}
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;

	[CustomPropertyDrawer(typeof(LeanScreenQuery))]
	public class LeanScreenQuery_Drawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var method = (LeanScreenQuery.MethodType)property.FindPropertyRelative("Method").enumValueIndex;
			var height = base.GetPropertyHeight(property, label);
			var step   = height + 2;

			switch (method)
			{
				case LeanScreenQuery.MethodType.Raycast: height += step * 4; break;
			}

			return height;
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
		{
			var method = (LeanScreenQuery.MethodType)property.FindPropertyRelative("Method").enumValueIndex;
			var height     = base.GetPropertyHeight(property, label);

			rect.height = height;

			DrawProperty(ref rect, property, label, "Method", label.text, "The method used to search the scene based on a screen position.\n\nRaycast = 3D, 2D, and EventSystem raycast.");

			EditorGUI.indentLevel++;
			{
				DrawProperty(ref rect, property, label, "Camera", null, "The camera the depth calculations will be done using.\n\nNone = MainCamera.");

				switch (method)
				{
					case LeanScreenQuery.MethodType.Raycast:
					{
						//LeanEditor.BeginError(property.FindPropertyRelative("Distance").floatValue == 0.0f);
						//	DrawProperty(ref rect, property, label, "Distance", "Distance", "The world space distance from the camera the point will be placed. This should be greater than 0.");
						//LeanEditor.EndError();
						CwEditor.BeginError(property.FindPropertyRelative("Layers").intValue == 0.0f);
							DrawProperty(ref rect, property, label, "Layers", "Layers", "The scene will be queried (e.g. Raycast) against these layers.");
						CwEditor.EndError();
						DrawProperty(ref rect, property, label, "Search", "Search", "When the query hits a GameObject, how should the desired component be searched for relative to it?");
						DrawProperty(ref rect, property, label, "RequiredTag", "RequiredTag", "The component found from the search must have this tag.");
					}
					break;
				}
			}
			EditorGUI.indentLevel--;
		}

		private void DrawObjectProperty<T>(ref Rect rect, SerializedProperty property, string title, string tooltip)
			where T : Object
		{
			var propertyObject = property.FindPropertyRelative("Object");
			var oldValue       = propertyObject.objectReferenceValue as T;

			CwEditor.BeginError(oldValue == null);
				var mixed = EditorGUI.showMixedValue; EditorGUI.showMixedValue = propertyObject.hasMultipleDifferentValues;
					var newValue = EditorGUI.ObjectField(rect, new GUIContent(title, tooltip), oldValue, typeof(T), true);
				EditorGUI.showMixedValue = mixed;
			CwEditor.EndError();

			if (oldValue != newValue)
			{
				propertyObject.objectReferenceValue = newValue;
			}

			rect.y += rect.height;
		}

		private void DrawProperty(ref Rect rect, SerializedProperty property, GUIContent label, string childName, string overrideName = null, string overrideTooltip = null)
		{
			var childProperty = property.FindPropertyRelative(childName);

			label.text = string.IsNullOrEmpty(overrideName) == false ? overrideName : childProperty.displayName;

			label.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : childProperty.tooltip;

			EditorGUI.PropertyField(rect, childProperty, label);

			rect.y += rect.height + 2;
		}
	}
}
#endif