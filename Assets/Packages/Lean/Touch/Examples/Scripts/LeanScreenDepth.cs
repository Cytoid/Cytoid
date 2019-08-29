using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Touch
{
	[CustomPropertyDrawer(typeof(LeanScreenDepth))]
	public class LeanScreenDepth_Drawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var conversion = (LeanScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
			var height     = base.GetPropertyHeight(property, label);

			switch (conversion)
			{
				case LeanScreenDepth.ConversionType.CameraDistance: return height * 3;
				case LeanScreenDepth.ConversionType.DepthIntercept: return height * 3;
				case LeanScreenDepth.ConversionType.PhysicsRaycast: return height * 4;
				case LeanScreenDepth.ConversionType.PlaneIntercept: return height * 4;
				case LeanScreenDepth.ConversionType.PathClosest:    return height * 3;
			}

			return height;
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
		{
			var conversion = (LeanScreenDepth.ConversionType)property.FindPropertyRelative("Conversion").enumValueIndex;
			var height     = base.GetPropertyHeight(property, label);

			rect.height = height;

			DrawProperty(ref rect, property, label, "Conversion", label.text, label.tooltip);

			EditorGUI.indentLevel++;
			{
				DrawProperty(ref rect, property, label, "Camera");

				switch (conversion)
				{
					case LeanScreenDepth.ConversionType.CameraDistance:
					{
						var color = GUI.color; if (property.FindPropertyRelative("Distance").floatValue == 0.0f) GUI.color = Color.red;
						DrawProperty(ref rect, property, label, "Distance", "Distance", "The world space distance from the camera the point will be placed. This should be greater than 0.");
						GUI.color = color;
					}
					break;

					case LeanScreenDepth.ConversionType.DepthIntercept:
					{
						DrawProperty(ref rect, property, label, "Distance", "Z =", "The world space point along the Z axis the plane will be placed. For normal 2D scenes this should be 0.");
					}
					break;

					case LeanScreenDepth.ConversionType.PhysicsRaycast:
					{
						var color = GUI.color; if (property.FindPropertyRelative("Layers").intValue == 0) GUI.color = Color.red;
							DrawProperty(ref rect, property, label, "Layers");
						GUI.color = color;
						DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the raycast hit point.");
					}
					break;

					case LeanScreenDepth.ConversionType.PlaneIntercept:
					{
						DrawObjectProperty<LeanPlane>(ref rect, property, "Plane");
						DrawProperty(ref rect, property, label, "Distance", "Offset", "The world space offset from the intercept hit point.");
					}
					break;

					case LeanScreenDepth.ConversionType.PathClosest:
					{
						DrawObjectProperty<LeanPath>(ref rect, property, "Path");
					}
					break;
				}
			}
			EditorGUI.indentLevel--;
		}

		private void DrawObjectProperty<T>(ref Rect rect, SerializedProperty property, string title)
			where T : Object
		{
			var propertyObject = property.FindPropertyRelative("Object");
			var oldValue       = propertyObject.objectReferenceValue as T;

			var color = GUI.color; if (oldValue == null) GUI.color = Color.red;
				var mixed = EditorGUI.showMixedValue; EditorGUI.showMixedValue = propertyObject.hasMultipleDifferentValues;
					var newValue = EditorGUI.ObjectField(rect, title, oldValue, typeof(T), true);
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

			if (string.IsNullOrEmpty(overrideName) == false)
			{
				label.text    = overrideName;
				label.tooltip = overrideTooltip;

				EditorGUI.PropertyField(rect, childProperty, label);
			}
			else
			{
				EditorGUI.PropertyField(rect, childProperty);
			}

			rect.y += rect.height;
		}
	}
}
#endif

namespace Lean.Touch
{
	/// <summary>This struct allows you to convert from a screen point to a world point using a variety of different methods.</summary>
	[System.Serializable]
	public struct LeanScreenDepth
	{
		public enum ConversionType
		{
			CameraDistance,
			DepthIntercept,
			PhysicsRaycast,
			PlaneIntercept,
			PathClosest,
		}

		[Tooltip("The conversion method used to find a world point from a screen point.")]
		public ConversionType Conversion;

		[Tooltip("The camera the depth calculations will be done using (None = MainCamera).")]
		public Camera Camera;

		[Tooltip("The plane/path/etc that will be intercepted.")]
		public Object Object;

		[Tooltip("The layers used in the raycast.")]
		public LayerMask Layers;

		// Toolips are modified at runtime based on Conversion setting
		public float Distance;

		/// <summary>When performing a ScreenDepth conversion, the converted point can have a normal associated with it. This stores that.</summary>
		public static Vector3 LastWorldNormal = Vector3.forward;

		private static RaycastHit[] hits = new RaycastHit[128];

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
			var camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				switch (Conversion)
				{
					case ConversionType.CameraDistance:
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
							var scale = (ray.origin.z + Distance) / slope;

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
						var plane = Object as LeanPlane;

						if (plane != null)
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
						var path = Object as LeanPath;

						if (path != null)
						{
							var ray = camera.ScreenPointToRay(screenPoint);
							var hit = default(Vector3);

							if (path.TryGetClosest(ray, ref hit, path.Smoothing) == true)
							{
								position = hit;

								LastWorldNormal = LeanPath.LastWorldNormal;

								return true;
							}
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