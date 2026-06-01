using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Polyglot
{
    public abstract class LocalizedEditor<T> : Editor where T : class, ILocalize
    {
        private Vector2 scroll;
        private AnimBool showAutoComplete;

        public virtual void OnEnable()
        {
            showAutoComplete = new AnimBool(true);
            showAutoComplete.valueChanged.AddListener(Repaint);
        }

        public void OnInspectorGUI(string propertyPath)
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            var iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                EditorGUILayout.PropertyField(iterator, true, new GUILayoutOption[0]);

                if (iterator.name == propertyPath)
                {
                    var key = iterator.stringValue;
                    var localizedString = Localization.Get(key);
                    EditorGUILayout.LabelField("Localized Text", localizedString);

                    if (!string.IsNullOrEmpty(key))
                    {
                        if (!Localization.KeyExist(key))
                        {
                            DrawAutoComplete(iterator);
                        }
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                var text = target as T;
                if (text != null)
                {
                    text.OnLocalize();
                }
            }
        }

        private void DrawAutoComplete(SerializedProperty property)
        {
            var localizedStrings = LocalizationImporter.GetLanguagesStartsWith(property.stringValue);
            
            if (localizedStrings.Count == 0)
            {
                localizedStrings = LocalizationImporter.GetLanguagesContains(property.stringValue);
            }

            var selectedLanguage = (int)Localization.Instance.SelectedLanguage;

            showAutoComplete.target = EditorGUILayout.Foldout(showAutoComplete.target, "Auto-Complete");
            if (EditorGUILayout.BeginFadeGroup(showAutoComplete.faded))
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