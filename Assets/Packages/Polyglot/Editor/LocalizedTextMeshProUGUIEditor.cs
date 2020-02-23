#if TMP_PRESENT
#if UNITY_5
using JetBrains.Annotations;
#endif
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace Polyglot
{
#if UNITY_5
    [UsedImplicitly]
#endif
    [CustomEditor(typeof(LocalizedTextMeshProUGUI), true)]
    [CanEditMultipleObjects]
    public class LocalizedTextMeshProUGUIEditor : LocalizedEditor<LocalizedTextMeshProUGUI>
    {
        private AnimBool showParameters;

        public override void OnEnable()
        {
            base.OnEnable();
            showParameters = new AnimBool(true);
            showParameters.valueChanged.AddListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            OnInspectorGUI("key");

            if (!serializedObject.isEditingMultipleObjects)
            {
                var text = target as LocalizedTextMeshProUGUI;
                if (text != null)
                {
                    var parameters = text.Parameters;
                    if (parameters != null && parameters.Count > 0)
                    {
                        showParameters.value = EditorGUILayout.Foldout(showParameters.value, "Parameters");
                        if (EditorGUILayout.BeginFadeGroup(showParameters.faded))
                        {
                            EditorGUI.indentLevel++;
                            for (int index = 0; index < parameters.Count; index++)
                            {
                                var parameter = parameters[index];
                                EditorGUILayout.SelectableLabel(parameter != null ? parameter.ToString() : "null");
                            }
                            EditorGUI.indentLevel--;
                        }
                        EditorGUILayout.EndFadeGroup();
                    }
                }
            }
        }
    }
}
#endif