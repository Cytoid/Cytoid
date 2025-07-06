using UnityEngine;
using Lean.Common;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>This struct handles the conversion between screen coordinates, and world coordinates.
	/// This conversion is required for many touch interactions, and there are numerous ways it can be performed.</summary>
	[System.Serializable]
	public struct LeanScreenDepth
	{
		public enum ConversionType
		{
			FixedDistance,
			DepthIntercept,
			PhysicsRaycast,
			PlaneIntercept,
			PathClosest,
			AutoDistance,
			HeightIntercept
		}

		/// <summary>The method used to convert between screen coordinates, and world coordinates.
		/// FixedDistance = A point will be projected out from the camera.
		/// DepthIntercept = A point will be intercepted out from the camera on a surface lying flat on the XY plane.
		/// PhysicsRaycast = A ray will be cast from the camera.
		/// PlaneIntercept = A point will be intercepted out from the camera to the closest point on the specified plane.
		/// PathClosest = A point will be intercepted out from the camera to the closest point on the specified path.
		/// AutoDistance = A point will be projected out from the camera based on the current Transform depth.
		/// HeightIntercept = A point will be intercepted out from the camera on a surface lying flat on the XZ plane.</summary>
		public ConversionType Conversion;

		/// <summary>The camera the depth calculations will be done using.
		/// None = MainCamera.</summary>
		public Camera Camera;

		/// <summary>The plane/path/etc that will be intercepted.</summary>
		public Object Object;

		/// <summary>The layers used in the raycast.</summary>
		public LayerMask Layers;

		/// <summary>Tooltips are modified at runtime based on Conversion setting.</summary>
		public float Distance;

		/// <summary>When performing a ScreenDepth conversion, the converted point can have a normal associated with it. This stores that.</summary>
		public static Vector3 LastWorldNormal = Vector3.forward;

		private static readonly RaycastHit[] hits = new RaycastHit[128];

		public LeanScreenDepth(ConversionType newConversion, int newLayers = Physics.DefaultRaycastLayers, float newDistance = 0.0f)
		{
			Conversion = newConversion;
			Camera     = null;
			Object     = null;
			Layers     = newLayers;
			Distance   = newDistance;
		}

		// This will do the actual conversion
		public Vector3 Convert(Vector2 screenPoint, GameObject gameObject = null, Transform ignore = null)
		{
			var position = default(Vector3);

			TryConvert(ref position, screenPoint, gameObject, ignore);

			return position;
		}

		// This will return the delta between two converted screenPoints
		public Vector3 ConvertDelta(Vector2 lastScreenPoint, Vector2 screenPoint, GameObject gameObject = null, Transform ignore = null)
		{
			var lastWorldPoint = Convert(lastScreenPoint, gameObject, ignore);
			var     worldPoint = Convert(    screenPoint, gameObject, ignore);

			return worldPoint - lastWorldPoint;
		}

		// This will do the actual conversion
		public bool TryConvert(ref Vector3 position, Vector2 screenPoint, GameObject gameObject = null, Transform ignore = null)
		{
			var camera = CwHelper.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				switch (Conversion)
				{
					case ConversionType.FixedDistance:
					{
						var screenPoint3 = new Vector3(screenPoint.x, screenPoint.y, Distance);

						position = camera.ScreenToWorldPoint(screenPoint3);

						LastWorldNormal = -camera.transform.forward;

						return true;
					}

					case ConversionType.DepthIntercept:
					{
						var ray   = camera.ScreenPointToRay(screenPoint);
						var slope = -ray.direction.z;

						if (slope != 0.0f)
						{
							var scale = (ray.origin.z - Distance) / slope;

							position = ray.GetPoint(scale);

							LastWorldNormal = Vector3.back;

							return true;
						}
					}
					break;

					case ConversionType.PhysicsRaycast:
					{
						var ray       = camera.ScreenPointToRay(screenPoint);
						var hitCount  = Physics.RaycastNonAlloc(ray, hits, float.PositiveInfinity, Layers);
						var bestPoint = default(Vector3);
						var bestDist  = float.PositiveInfinity;

						for (var i = hitCount - 1; i >= 0; i--)
						{
							var hit         = hits[i];
							var hitDistance = hit.distance;

							if (hitDistance < bestDist && IsChildOf(hit.transform, ignore) == false)
							{
								bestPoint = hit.point + hit.normal * Distance;
								bestDist  = hitDistance;

								LastWorldNormal = hit.normal;
							}
						}

						if (bestDist < float.PositiveInfinity)
						{
							position = bestPoint;

							return true;
						}
					}
					break;

					case ConversionType.PlaneIntercept:
					{
						var plane = default(LeanPlane);

						if (Exists(gameObject, ref plane) == true)
						{
							var ray = camera.ScreenPointToRay(screenPoint);
							var hit = default(Vector3);

							if (plane.TryRaycast(ray, ref hit, Distance) == true)
							{
								position = hit;

								LastWorldNormal = plane.transform.forward;

								return true;
							}
						}
					}
					break;

					case ConversionType.PathClosest:
					{
						var path = default(LeanPath);

						if (Exists(gameObject, ref path) == true)
						{
							var ray = camera.ScreenPointToRay(screenPoint);

							if (path.TryGetClosest(ray, ref position, -1, Distance * Time.deltaTime) == true)
							{
								LastWorldNormal = LeanPath.LastWorldNormal;

								return true;
							}
						}
					}
					break;

					case ConversionType.AutoDistance:
					{
						if (gameObject != null)
						{
							var depth        = camera.WorldToScreenPoint(gameObject.transform.position).z;
							var screenPoint3 = new Vector3(screenPoint.x, screenPoint.y, depth + Distance);

							position = camera.ScreenToWorldPoint(screenPoint3);

							LastWorldNormal = -camera.transform.forward;

							return true;
						}
					}
					break;

					case ConversionType.HeightIntercept:
					{
						var ray   = camera.ScreenPointToRay(screenPoint);
						var slope = -ray.direction.y;

						if (slope != 0.0f)
						{
							var scale = (ray.origin.y - Distance) / slope;

							position = ray.GetPoint(scale);

							LastWorldNormal = Vector3.down;

							return true;
						}
					}
					break;
				}
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", gameObject);
			}

			return false;
		}

		// If the specified object doesn't exist, try and find it in the scene
		private bool Exists<T>(GameObject gameObject, ref T instance)
			where T : Object
		{
			instance = Object as T;

			// Already exists?
			if (instance != null)
			{
				return true;
			}

			// Exists in ancestor?
			Object = instance = gameObject.GetComponentInParent<T>();

			if (instance != null)
			{
				return true;
			}

			// Exists in scene?
			Object = instance = CwHelper.FindAnyObjectByType<T>();

			if (instance != null)
			{
				return true;
			}

			// Doesn't exist
			return false;
		}
		
		// This will return true if current or one of its parents matches the specified gameObject's Transform (current must be non-null)
		private static bool IsChildOf(Transform current, Transform target)
		{
			if (target != null)
			{
				while (true)
				{
					if (current == target)
					{
						return true;
					}

					current = current.parent;

					if (current == null)
					{
						break;
					}
				}
			}

			return false;
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;

	[CustomPropertyDrawer(typeof(LeanScreenDepth))]
	public class LeanScreenDepth_Drawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var conversion = (LeanScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
			var height     = base.GetPropertyHeight(property, label);
			var step       = height + 2;

			switch (conversion)
			{
				case LeanScreenDepth.ConversionType.FixedDistance:   height += step * 2; break;
				case LeanScreenDepth.ConversionType.DepthIntercept:  height += step * 2; break;
				case LeanScreenDepth.ConversionType.PhysicsRaycast:  height += step * 3; break;
				case LeanScreenDepth.ConversionType.PlaneIntercept:  height += step * 3; break;
				case LeanScreenDepth.ConversionType.PathClosest:     height += step * 3; break;
				case LeanScreenDepth.ConversionType.AutoDistance:    height += step * 2; break;
				case LeanScreenDepth.ConversionType.HeightIntercept: height += step * 2; break;
			}

			return height;
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
		{
			var conversion = (LeanScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
			var height     = base.GetPropertyHeight(property, label);

			rect.height = height;

			DrawProperty(ref rect, property, label, "Conversion", label.text, "The method used to convert between screen coordinates, and world coordinates.\n\nFixedDistance = A point will be projected out from the camera.\n\nDepthIntercept = A point will be intercepted out from the camera on a surface lying flat on the XY plane.\n\nPhysicsRaycast = A ray will be cast from the camera.\n\nPlaneIntercept = A point will be intercepted out from the camera to the closest point on the specified plane.\n\nPathClosest = A point will be intercepted out from the camera to the closest point on the specified path.\n\nAutoDistance = A point will be projected out from the camera based on the current Transform depth.\n\nHeightIntercept = A point will be intercepted out from the camera on a surface lying flat on the XZ plane.");

			EditorGUI.indentLevel++;
			{
				DrawProperty(ref rect, property, label, "Camera", null, "The camera the depth calculations will be done using.\n\nNone = MainCamera.");

				switch (conversion)
				{
					case LeanScreenDepth.ConversionType.FixedDistance:
					{
						CwEditor.BeginError(property.FindPropertyRelative("Distance").floatValue == 0.0f);
						DrawProperty(ref rect, property, label, "Distance", "Distance", "The world space distance from the camera the point will be placed. This should be greater than 0.");
						CwEditor.EndError();
					}
					break;

					case LeanScreenDepth.ConversionType.DepthIntercept:
					{
						DrawProperty(ref rect, property, label, "Distance", "Z =", "The world space point along the Z axis the plane will be placed. For normal 2D scenes this should be 0.");
					}
					break;

					case LeanScreenDepth.ConversionType.PhysicsRaycast:
					{
						CwEditor.BeginError(property.FindPropertyRelative("Layers").intValue == 0);
							DrawProperty(ref rect, property, label, "Layers", "Layers", "The layers used in the raycast.");
						CwEditor.EndError();
						DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the raycast hit point.");
					}
					break;

					case LeanScreenDepth.ConversionType.PlaneIntercept:
					{
						DrawObjectProperty<LeanPlane>(ref rect, property, "Plane", "The plane that will be intercepted.");
						DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the intercept hit point.");
					}
					break;

					case LeanScreenDepth.ConversionType.PathClosest:
					{
						DrawObjectProperty<LeanPath>(ref rect, property, "Path", "The path that will be intercepted.");
						DrawProperty(ref rect, property, label, "Distance", "Max Delta", "The maximum amount of segments that can be moved between.");
					}
					break;

					case LeanScreenDepth.ConversionType.AutoDistance:
					{
						DrawProperty(ref rect, property, label, "Distance", "Offset", "The depth offset from the calculated point.");
					}
					break;

					case LeanScreenDepth.ConversionType.HeightIntercept:
					{
						DrawProperty(ref rect, property, label, "Distance", "Y =", "The world space point along the Y axis the plane will be placed. For normal top down scenes this should be 0.");
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

			var color = GUI.color; if (oldValue == null) GUI.color = Color.red;
				var mixed = EditorGUI.showMixedValue; EditorGUI.showMixedValue = propertyObject.hasMultipleDifferentValues;
					var newValue = EditorGUI.ObjectField(rect, new GUIContent(title, tooltip), oldValue, typeof(T), true);
				EditorGUI.showMixedValue = mixed;
			GUI.color = color;

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