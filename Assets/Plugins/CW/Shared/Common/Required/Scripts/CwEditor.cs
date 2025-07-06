#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CW.Common
{
	/// <summary>This is the base class for all inspectors.</summary>
	public abstract class CwEditor : UnityEditor.Editor
	{
		private static Stack<object> datas = new Stack<object>();

		private static GUIContent customContent = new GUIContent();

		private static List<Color> colors = new List<Color>();

		private static List<float> labelWidths = new List<float>();

		private static List<bool> mixedValues = new List<bool>();

		public void GetTargets<T>(out T tgt, out T[] tgts)
			where T : Object
		{
			tgt  = (T)target;
			tgts = System.Array.ConvertAll(targets, item => (T)item);
		}

		public static void BeginData(SerializedObject newData)
		{
			datas.Push(newData);
		}

		public static void BeginData(SerializedProperty newData)
		{
			datas.Push(newData);
		}

		public static void EndData()
		{
			datas.Pop();
		}

		public override void OnInspectorGUI()
		{
			BeginData(serializedObject);

			ClearStacks();

			Separator();

			OnInspector();

			Separator();

			serializedObject.ApplyModifiedProperties();

			EndData();
		}

		protected void Each<T>(T[] tgts, System.Action<T> update, bool dirty = false, bool apply = false, string undo = null)
			where T : Object
		{
			if (string.IsNullOrEmpty(undo) == false)
			{
				Undo.RecordObjects(tgts, undo);
			}

			if (apply == true)
			{
				serializedObject.ApplyModifiedProperties();
			}

			foreach (var t in tgts)
			{
				update(t);

				if (dirty == true)
				{
					EditorUtility.SetDirty(t);
				}
			}
		}

		protected bool Any<T>(T[] tgts, System.Func<T, bool> check)
			where T : Object
		{
			foreach (var t in tgts)
			{
				if (check(t) == true)
				{
					return true;
				}
			}

			return false;
		}

		protected bool All<T>(T[] tgts, System.Func<T, bool> check)
			where T : Object
		{
			foreach (var t in tgts)
			{
				if (check(t) == false)
				{
					return false;
				}
			}

			return true;
		}

		public static Rect Reserve(float height = 19.0f)
		{
			var rect =
			EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField(string.Empty, GUILayout.Height(height), GUILayout.ExpandWidth(true), GUILayout.MinWidth(0.0f));
			EditorGUILayout.EndVertical();

			return rect;
		}

		public static void Info(string message)
		{
			EditorGUILayout.HelpBox(StripRichText(message), MessageType.Info); // Help boxes can't display rich text for some reason, so strip it
		}

		public static void Warning(string message)
		{
			EditorGUILayout.HelpBox(StripRichText(message), MessageType.Warning); // Help boxes can't display rich text for some reason, so strip it
		}

		public static void Error(string message)
		{
			EditorGUILayout.HelpBox(StripRichText(message), MessageType.Error); // Help boxes can't display rich text for some reason, so strip it
		}

		public static void Separator()
		{
			EditorGUILayout.Separator();
		}

		public static void BeginIndent()
		{
			EditorGUI.indentLevel += 1;
		}

		public static void EndIndent()
		{
			EditorGUI.indentLevel -= 1;
		}

		public static bool Button(string text)
		{
			return GUILayout.Button(text);
		}

		public static bool HelpButton(string helpText, UnityEditor.MessageType type, string buttonText, float buttonWidth)
		{
			var clicked = false;

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.HelpBox(helpText, type);

				var style = new GUIStyle(GUI.skin.button); style.wordWrap = true;

				clicked = GUILayout.Button(buttonText, style, GUILayout.ExpandHeight(true), GUILayout.Width(buttonWidth));
			}
			EditorGUILayout.EndHorizontal();

			return clicked;
		}

		public static void ClearStacks()
		{
			while (colors.Count > 0)
			{
				EndColor();
			}

			while (labelWidths.Count > 0)
			{
				EndLabelWidth();
			}

			while (mixedValues.Count > 0)
			{
				EndMixedValue();
			}
		}

		public static void BeginMixedValue(bool mixed = true)
		{
			mixedValues.Add(EditorGUI.showMixedValue);

			EditorGUI.showMixedValue = mixed;
		}

		public static void EndMixedValue()
		{
			if (mixedValues.Count > 0)
			{
				var index = mixedValues.Count - 1;

				EditorGUI.showMixedValue = mixedValues[index];

				mixedValues.RemoveAt(index);
			}
		}

		public static void BeginDisabled(bool disabled = true)
		{
			EditorGUI.BeginDisabledGroup(disabled);
		}

		public static void EndDisabled()
		{
			EditorGUI.EndDisabledGroup();
		}

		public static void BeginError(bool error = true)
		{
			BeginColor(Color.red, error);
		}

		public static void EndError()
		{
			EndColor();
		}

		public static void BeginColor(Color color, bool show = true)
		{
			colors.Add(GUI.color);

			GUI.color = show == true ? color : colors[0];
		}

		public static void EndColor()
		{
			if (colors.Count > 0)
			{
				var index = colors.Count - 1;

				GUI.color = colors[index];

				colors.RemoveAt(index);
			}
		}

		public static void BeginLabelWidth(float width)
		{
			labelWidths.Add(EditorGUIUtility.labelWidth);

			EditorGUIUtility.labelWidth = width;
		}

		public static void EndLabelWidth()
		{
			if (labelWidths.Count > 0)
			{
				var index = labelWidths.Count - 1;

				EditorGUIUtility.labelWidth = labelWidths[index];

				labelWidths.RemoveAt(index);
			}
		}

		public static bool DrawFoldout(string overrideText, string overrideTooltip, string propertyPath = "m_Name")
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);

			property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, customContent);

			return property.isExpanded;
		}

		public static bool DrawExpand(string propertyPath, ref bool modified, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var rect     = EditorGUILayout.BeginVertical(); EditorGUILayout.LabelField(string.Empty, GUILayout.Height(EditorGUI.GetPropertyHeight(property))); EditorGUILayout.EndVertical();
			var rectF    = rect; rectF.height = 16;

			property.isExpanded = EditorGUI.Foldout(rectF, property.isExpanded, GUIContent.none);

			EditorGUI.BeginChangeCheck();

			EditorGUI.PropertyField(rect, property, customContent, true);

			modified = EditorGUI.EndChangeCheck();

			return property.isExpanded;
		}

		public static bool DrawExpand(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var modified = false; return DrawExpand(propertyPath, ref modified, overrideTooltip, overrideText);
		}

		public static bool Draw(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(property, customContent, true);

			return EditorGUI.EndChangeCheck();
		}

		public static void Draw(string propertyPath, ref bool dirty, string overrideTooltip = null, string overrideText = null)
		{
			if (Draw(propertyPath, overrideTooltip, overrideText) == true)
			{
				dirty = true;
			}
		}

		public static void Draw(string propertyPath, ref bool dirty1, ref bool dirty2, string overrideTooltip = null, string overrideText = null)
		{
			if (Draw(propertyPath, overrideTooltip, overrideText) == true)
			{
				dirty1 = true;
				dirty2 = true;
			}
		}

		public static bool DrawSlider(string propertyPath, float min, float max, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var value    = property.floatValue;

			EditorGUI.BeginChangeCheck();

			value = EditorGUILayout.Slider(customContent, value, min, max);

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.floatValue = value;

				return true;
			}

			return false;
		}

		public static bool DrawIntSlider(string propertyPath, int min, int max, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var value    = property.intValue;

			EditorGUI.BeginChangeCheck();

			value = EditorGUILayout.IntSlider(customContent, value, min, max);

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.intValue = value;

				return true;
			}

			return false;
		}


		public static bool DrawMinMax(string propertyPath, float min, float max, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var value    = property.vector2Value;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.MinMaxSlider(customContent, ref value.x, ref value.y, min, max);

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.vector2Value = value;

				return true;
			}

			return false;
		}

		public static bool DrawVector4(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var value    = property.vector4Value;

			EditorGUI.BeginChangeCheck();

			value = EditorGUILayout.Vector4Field(customContent, value);

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.vector4Value = value;

				return true;
			}

			return false;
		}

		public static bool DrawIntPopup(int[] values, GUIContent[] contents, string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.IntPopup(property, contents, values, customContent);

			return EditorGUI.EndChangeCheck();
		}

		public static void DrawIntPopup(int[] values, GUIContent[] contents, string propertyPath, ref bool modified, string overrideTooltip = null, string overrideText = null)
		{
			if (DrawIntPopup(values, contents, propertyPath, overrideTooltip, overrideText) == true)
			{
				modified = true;
			}
		}

		public static bool DrawLayer(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var value    = property.intValue;

			EditorGUI.BeginChangeCheck();

			value = EditorGUILayout.LayerField(customContent, value);

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.intValue = value;

				return true;
			}

			return false;
		}

		public static bool DrawEulerAngles(string propertyPath, string overrideTooltip = null, string overrideText = null)
		{
			var property = GetPropertyAndSetCustomContent(propertyPath, overrideTooltip, overrideText);
			var value    = property.quaternionValue.eulerAngles;

			EditorGUI.BeginChangeCheck();

			BeginMixedValue(property.hasMultipleDifferentValues);
				value = EditorGUILayout.Vector3Field(customContent, value);
			EndMixedValue();

			if (EditorGUI.EndChangeCheck() == true)
			{
				property.quaternionValue = Quaternion.Euler(value);

				return true;
			}

			return false;
		}

		protected void DirtyAndUpdate()
		{
			for (var i = targets.Length - 1; i >= 0; i--)
			{
				EditorUtility.SetDirty(targets[i]);
			}

			serializedObject.Update();
		}

		public static SerializedProperty GetProperty(string propertyPath)
		{
			var data = datas.Peek();
			
			if (data != null)
			{
				var dataA = data as SerializedObject;

				if (dataA != null)
				{
					return dataA.FindProperty(propertyPath);
				}

				var dataB = data as SerializedProperty;

				if (dataB != null)
				{
					return dataB.FindPropertyRelative(propertyPath);
				}
			}

			return null;
		}

		private static SerializedProperty GetPropertyAndSetCustomContent(string propertyPath, string overrideTooltip, string overrideText)
		{
			var property = GetProperty(propertyPath);

			customContent.text    = string.IsNullOrEmpty(overrideText   ) == false ? overrideText    : property.displayName;
			customContent.tooltip = string.IsNullOrEmpty(overrideTooltip) == false ? overrideTooltip : property.tooltip;
			customContent.tooltip = StripRichText(customContent.tooltip); // Tooltips can't display rich text for some reason, so strip it

			return property;
		}

		private static string StripRichText(string s)
		{
			return s.Replace("<b>", "").Replace("</b>", "");
		}

		protected virtual void OnInspector()
		{
		}
	}
}
#endif