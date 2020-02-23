using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;

namespace SleekRender
{
    [CustomEditor(typeof(SleekRenderSettings))]
    public class SleekRenderSettingsInspector : Editor
    {
        private SerializedProperty _isBloomGroupExpandedProperty;
        private SerializedProperty _bloomEnabledProperty;
        private SerializedProperty _bloomThresholdProperty;
        private SerializedProperty _bloomIntensityProperty;
        private SerializedProperty _bloomTintProperty;
        private SerializedProperty _bloomPreserveAspectRatioProperty;
        private SerializedProperty _bloomWidthProperty;
        private SerializedProperty _bloomHeightProperty;
        private SerializedProperty _bloomLumaVectorProperty;
        private SerializedProperty _bloomSelectedLumaVectorTypeProperty;

        private string[] _bloomSizeVariants = new[] { "32", "64", "128" };
        private int[] _bloomSizeVariantInts = new[] { 32, 64, 128 };
        private int _selectedBloomWidthIndex = -1;
        private int _selectedBloomHeightIndex = -1;

        private LumaVectorType _selectedLumaVectorType;

        private SerializedProperty _isColorizeGroupExpandedProperty;
        private SerializedProperty _colorizeEnabledProperty;
        private SerializedProperty _colorizeProperty;

        private SerializedProperty _isVignetteExpandedProperty;
        private SerializedProperty _vignetteEnabledProperty;
        private SerializedProperty _vignetteBeginRadiusProperty;
        private SerializedProperty _vignetteExpandRadiusProperty;
        private SerializedProperty _vignetteColorProperty;

        private SerializedProperty _isContrastAndBrightnessEditorExpandedProperty;
        private SerializedProperty _contrastAndBrightnessEnabledProperty;
        private SerializedProperty _contrasteIntensity;
        private SerializedProperty _brightnesseIntensity;

        private void OnEnable()
        {
            SetupBloomProperties();

            _isColorizeGroupExpandedProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.colorizeExpanded));
            _colorizeEnabledProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.colorizeEnabled));
            _colorizeProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.colorize));

            _isVignetteExpandedProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.vignetteExpanded));
            _vignetteEnabledProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.vignetteEnabled));
            _vignetteBeginRadiusProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.vignetteBeginRadius));
            _vignetteExpandRadiusProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.vignetteExpandRadius));
            _vignetteColorProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.vignetteColor));

            _isContrastAndBrightnessEditorExpandedProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.brightnessContrastExpanded));
            _contrastAndBrightnessEnabledProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.brightnessContrastEnabled));
            _contrasteIntensity = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.contrast));
            _brightnesseIntensity = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.brightness));
        }

        private void SetupBloomProperties()
        {
            _isBloomGroupExpandedProperty =
                serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomExpanded));
            _bloomEnabledProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomEnabled));
            _bloomThresholdProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomThreshold));
            _bloomIntensityProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomIntensity));
            _bloomTintProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomTint));

            _bloomPreserveAspectRatioProperty =
                serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.preserveAspectRatio));

            _bloomWidthProperty = serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomTextureWidth));
            _selectedBloomWidthIndex = Array.IndexOf(_bloomSizeVariantInts, _bloomWidthProperty.intValue);
            _bloomHeightProperty =
                serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomTextureHeight));
            _selectedBloomHeightIndex = Array.IndexOf(_bloomSizeVariantInts, _bloomHeightProperty.intValue);

            _bloomLumaVectorProperty =
                serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomLumaVector));
            _bloomSelectedLumaVectorTypeProperty =
                serializedObject.FindProperty(GetMemberName((SleekRenderSettings s) => s.bloomLumaCalculationType));
            _selectedLumaVectorType = (LumaVectorType)_bloomSelectedLumaVectorTypeProperty.enumValueIndex;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            int indent = EditorGUI.indentLevel;

            DrawBloomEditor();
            EditorGUILayout.Space();

            DrawColorizeEditor();
            EditorGUILayout.Space();

            DrawVignetteEditor();
            EditorGUILayout.Space();

            DrawContrastAndBrightnessEditor();

            DrawTotalCost();

            EditorGUI.indentLevel = indent;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawContrastAndBrightnessEditor()
        {
            Header("Brightness / Contrast", _isContrastAndBrightnessEditorExpandedProperty, _contrastAndBrightnessEnabledProperty);

            if (_isContrastAndBrightnessEditorExpandedProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;

                EditorGUILayout.LabelField("Contrast Intensity");
                EditorGUILayout.Slider(_contrasteIntensity, -1f, 1f, "");

                EditorGUILayout.LabelField("Brightness Intensity");
                EditorGUILayout.Slider(_brightnesseIntensity, -1f, 1f, "");

                EditorGUI.indentLevel -= 1;
            }
        }

        private void DrawVignetteEditor()
        {
            Header("Vignette", _isVignetteExpandedProperty, _vignetteEnabledProperty);

            if (_isVignetteExpandedProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;

                EditorGUILayout.LabelField("Begin radius");
                EditorGUILayout.Slider(_vignetteBeginRadiusProperty, 0f, 1f, "");

                EditorGUILayout.LabelField("Expand radius");
                EditorGUILayout.Slider(_vignetteExpandRadiusProperty, 0f, 3f, "");

                EditorGUILayout.LabelField("Color");
                _vignetteColorProperty.colorValue = EditorGUILayout.ColorField("", _vignetteColorProperty.colorValue);

                EditorGUI.indentLevel -= 1;
            }
        }

        private void DrawColorizeEditor()
        {
            Header("Colorize", _isColorizeGroupExpandedProperty, _colorizeEnabledProperty);

            if (_isColorizeGroupExpandedProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.LabelField("Color");
                _colorizeProperty.colorValue = EditorGUILayout.ColorField("", _colorizeProperty.colorValue);
                EditorGUI.indentLevel -= 1;
            }
        }

        private void DrawBloomEditor()
        {
            Header("Bloom", _isBloomGroupExpandedProperty, _bloomEnabledProperty);

            if (_isBloomGroupExpandedProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;

                EditorGUILayout.LabelField("Bloom threshold");
                EditorGUILayout.Slider(_bloomThresholdProperty, 0f, 1f, "");
                EditorGUILayout.LabelField("Bloom intensity");
                EditorGUILayout.Slider(_bloomIntensityProperty, 0f, 15f, "");
                EditorGUILayout.LabelField("Bloom tint");
                _bloomTintProperty.colorValue = EditorGUILayout.ColorField("", _bloomTintProperty.colorValue);

                DrawBloomWidthProperties();
                DisplayLumaVectorProperties();

                EditorGUI.indentLevel -= 1;
            }
        }

        private void DrawTotalCost()
        {
            // Skipping control rect, ignore any statical analisys warnings
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.HelpBox(SleekRenderCostCalculator.GetTotalCostStringFor(target as SleekRenderSettings),
                MessageType.Info);
        }

        private void DisplayLumaVectorProperties()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brightpass Luma calculation");

            _selectedLumaVectorType = (LumaVectorType)EditorGUILayout.EnumPopup(_selectedLumaVectorType);
            _bloomSelectedLumaVectorTypeProperty.enumValueIndex = (int)_selectedLumaVectorType;
            switch (_selectedLumaVectorType)
            {
                case LumaVectorType.Custom:
                    EditorGUILayout.PropertyField(_bloomLumaVectorProperty, new GUIContent(""));
                    break;
                case LumaVectorType.Uniform:
                    var oneOverThree = 1f / 3f;
                    _bloomLumaVectorProperty.vector3Value = new Vector3(oneOverThree, oneOverThree, oneOverThree);
                    break;
                case LumaVectorType.sRGB:
                    _bloomLumaVectorProperty.vector3Value = new Vector3(0.2126f, 0.7152f, 0.0722f);
                    break;
            }

            var vector = _bloomLumaVectorProperty.vector3Value;
            if (!Mathf.Approximately(vector.x + vector.y + vector.z, 1f))
            {
                EditorGUILayout.HelpBox("Luma vector is not normalized.\nVector values should sum up to 1.",
                    MessageType.Warning);
            }
        }

        private void DrawBloomWidthProperties()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bloom texture size");

            _bloomPreserveAspectRatioProperty.boolValue = EditorGUILayout.ToggleLeft("Preserve aspect ratio", _bloomPreserveAspectRatioProperty.boolValue);

            var rect = EditorGUILayout.GetControlRect();
            var oneFourthOfWidth = rect.width * 0.25f;
            var xLabelRect = new Rect(rect.x, rect.y, oneFourthOfWidth, rect.height);
            var widthRect = new Rect(rect.x + oneFourthOfWidth, rect.y, oneFourthOfWidth, rect.height);
            var yLabelRect = new Rect(rect.x + oneFourthOfWidth * 2.0f, rect.y, oneFourthOfWidth, rect.height);
            var heightRect = new Rect(rect.x + oneFourthOfWidth * 3.0f, rect.y, oneFourthOfWidth, rect.height);

            if (!_bloomPreserveAspectRatioProperty.boolValue)
            {
                EditorGUI.LabelField(xLabelRect, "X");
                _selectedBloomWidthIndex = _selectedBloomWidthIndex != -1 ? _selectedBloomWidthIndex : 2;
                _selectedBloomWidthIndex = EditorGUI.Popup(widthRect, _selectedBloomWidthIndex, _bloomSizeVariants);
                _bloomWidthProperty.intValue = _bloomSizeVariantInts[_selectedBloomWidthIndex];
            }

            EditorGUI.LabelField(yLabelRect, "Y");
            _selectedBloomHeightIndex = _selectedBloomHeightIndex != -1 ? _selectedBloomHeightIndex : 2;
            _selectedBloomHeightIndex = EditorGUI.Popup(heightRect, _selectedBloomHeightIndex, _bloomSizeVariants);
            _bloomHeightProperty.intValue = _bloomSizeVariantInts[_selectedBloomHeightIndex];
        }

        public static bool Header(string title, SerializedProperty isExpanded, SerializedProperty enabledField)
        {
            var display = isExpanded == null || isExpanded.boolValue;
            var enabled = enabledField.boolValue;
            var rect = GUILayoutUtility.GetRect(16f, 22f, FxStyles.header);
            GUI.Box(rect, title, FxStyles.header);

            var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
            var e = Event.current;

            if (e.type == EventType.Repaint)
            {
                FxStyles.headerCheckbox.Draw(toggleRect, false, false, enabled, false);
            }

            if (e.type == EventType.MouseDown)
            {
                const float kOffset = 2f;
                toggleRect.x -= kOffset;
                toggleRect.y -= kOffset;
                toggleRect.width += kOffset * 2f;
                toggleRect.height += kOffset * 2f;

                if (toggleRect.Contains(e.mousePosition))
                {
                    enabledField.boolValue = !enabledField.boolValue;
                    e.Use();
                }
                else if (rect.Contains(e.mousePosition) && isExpanded != null)
                {
                    display = !display;
                    isExpanded.boolValue = !isExpanded.boolValue;
                    e.Use();
                }
            }

            return display;
        }

        public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
        {
            return ((MemberExpression)memberAccess.Body).Member.Name;
        }
    }
}