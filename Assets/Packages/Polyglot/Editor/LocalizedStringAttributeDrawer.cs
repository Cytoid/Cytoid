using UnityEditor;
using UnityEngine;

namespace Polyglot
{
	[CustomPropertyDrawer(typeof(LocalizedStringAttribute), true)]
	public sealed class LocalizedStringAttributeDrawer : PropertyDrawer
	{
		public LocalizedStringAttributeDrawer()
		{
			showAutoComplete = true;
		}

		private Vector2 scroll;
		private bool showAutoComplete;


		public override bool CanCacheInspectorGUI(SerializedProperty property)
		{
			// Cache leaded to problems on the layout.
			return false;
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();

			var keyProperty = property;
			EditorGUI.PropertyField(position, keyProperty, label, true);

			var key = keyProperty.stringValue;
			var localizedString = Localization.Get(key);

			EditorGUILayout.LabelField("Localized Text", localizedString);

			if (!string.IsNullOrEmpty(key))
			{
				if (!Localization.KeyExist(key))
				{
					DrawAutoComplete(keyProperty);
				}
			}

			EditorGUI.EndProperty();
		}


		private void DrawAutoComplete(SerializedProperty property)
		{
			var localizedStrings = LocalizationImporter.GetLanguagesStartsWith(property.stringValue);

			if (localizedStrings.Count == 0)
			{
				localizedStrings = LocalizationImporter.GetLanguagesContains(property.stringValue);
			}

			var selectedLanguage = (int)Localization.Instance.SelectedLanguage;

			showAutoComplete = EditorGUILayout.Foldout(showAutoComplete, "Auto-Complete");
			if (showAutoComplete)
			{
				EditorGUI.indentLevel++;

				var height = EditorGUIUtility.singleLineHeight * (Mathf.Min(localizedStrings.Count, 6) + 1);
				scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(height));
				foreach (var local in localizedStrings)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel(local.Key);
					if (GUILayout.Button(local.Value[selectedLanguage], "CN CountBadge"))
					{
						property.stringValue = local.Key;
						GUIUtility.hotControl = 0;
						GUIUtility.keyboardControl = 0;
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndScrollView();
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndFadeGroup();

		}
	}
}
