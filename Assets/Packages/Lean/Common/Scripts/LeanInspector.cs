#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Lean.Common
{
	/// <summary>This class allows you to quickly make custom inspectors with common features.</summary>
	public class LeanInspector<T> : Editor
		where T : Object
	{
		protected T Target;

		protected T[] Targets;

		private static readonly string[] propertyToExclude = new string[] { "m_Script" };

		private static List<Color> colors = new List<Color>();

		private static GUIContent customContent = new GUIContent();

		public static void BeginError(bool error)
		{
			BeginError(error, Color.red);
		}

		public static void BeginError(bool error, Color color)
		{
			colors.Add(GUI.color);

			GUI.color = error == true ? color : colors[0];
		}

		public static void EndError()
		{
			var index = colors.Count - 1;

			GUI.color = colors[index];

			colors.RemoveAt(index);
		}

		public static Rect Reserve()
		{
			var rect = EditorGUILayout.BeginVertical();
			EditorGUILayout.LabelField(GUIContent.none);
			EditorGUILayout.EndVertical();

			return rect;
		}

		public override void OnInspectorGUI()
		{
			colors.Clear();

			Target  = (T)target;
			Targets = targets.Select(t => (T)t).ToArray();

			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.Separator();

				serializedObject.Update();

				DrawInspector();

				serializedObject.ApplyModifiedProperties();

				EditorGUILayout.Separator();
			}
			if (EditorGUI.EndChangeCheck() == true)
			{
				GUI.changed = true; Repaint();

				Dirty();
			}
		}

		public virtual void OnSceneGUI()
		{
			Target = (T)target;

			DrawScene();
		}

		protected void Each(System.Action<T> update, bool dirty = true)
		{
			if (dirty == true)
			{
				Undo.RecordObjects(Targets, "Inspector");
			}

			foreach (var t in Targets)
			{
				update(t);
			}

			if (dirty == true)
			{
				Dirty();
			}
		}

		protected bool Any(System.Func<T, bool> check)
		{
			foreach (var t in Targets)
			{
				if (check(t) == true)
				{
					return true;
				}
			}

			return false;
		}

		protected bool All(System.Func<T, bool> check)
		{
			foreach (var t in Targets)
			{
				if (check(t) == false)
				{
					return false;
				}
			}

			return true;
		}

		protected bool DrawExpand(ref bool expand, string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var rect     = Reserve();
			var property = serializedObject.FindProperty(propertyPath);

			customContent.text    = string.IsNullOrEmpty(overrideText   ) == false ? overrideText    : property.displayName;
			customContent.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : property.tooltip;

			EditorGUI.BeginChangeCheck();

			EditorGUI.PropertyField(rect, property, customContent, true);

			var changed = EditorGUI.EndChangeCheck();

			expand = EditorGUI.Foldout(new Rect(rect.position, new Vector2(25.0f, rect.height)), expand, string.Empty);

			return changed;
		}

		protected bool DrawMinMax(string propertyPath, float min, float max, string overrideTooltip = null, string overrideText = null)
		{
			var property = serializedObject.FindProperty(propertyPath);
			var value    = property.vector2Value;

			customContent.text    = string.IsNullOrEmpty(overrideText   ) == false ? overrideText    : property.displayName;
			customContent.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : property.tooltip;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.MinMaxSlider(customContent, ref value.x, ref value.y, min, max);

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.vector2Value = value;

				return true;
			}

			return false;
		}

		protected bool DrawEulerAngles(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = serializedObject.FindProperty(propertyPath);
			var mixed    = EditorGUI.showMixedValue;

			customContent.text    = string.IsNullOrEmpty(overrideText   ) == false ? overrideText    : property.displayName;
			customContent.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : property.tooltip;

			EditorGUI.BeginChangeCheck();

			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

			var oldEulerAngles = property.quaternionValue.eulerAngles;
			var newEulerAngles = EditorGUILayout.Vector3Field(customContent, oldEulerAngles);

			if (oldEulerAngles != newEulerAngles)
			{
				property.quaternionValue = Quaternion.Euler(newEulerAngles);
			}

			EditorGUI.showMixedValue = mixed;

			return EditorGUI.EndChangeCheck();
		}

		protected bool Draw(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = serializedObject.FindProperty(propertyPath);

			customContent.text    = string.IsNullOrEmpty(overrideText   ) == false ? overrideText    : property.displayName;
			customContent.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : property.tooltip;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(property, customContent, true);

			return EditorGUI.EndChangeCheck();
		}

		protected virtual void DrawInspector()
		{
			DrawPropertiesExcluding(serializedObject, propertyToExclude);
		}

		protected virtual void DrawScene()
		{
		}

		protected void Dirty()
		{
			for (var i = targets.Length - 1; i >= 0; i--)
			{
				EditorUtility.SetDirty(targets[i]);
			}

			serializedObject.Update();
		}
	}
}
#endif