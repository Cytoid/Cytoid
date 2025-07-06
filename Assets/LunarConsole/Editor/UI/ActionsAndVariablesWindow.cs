//
//  ActionsAndVariablesWindow.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


using System;
using UnityEngine;
using UnityEditor;

using LunarConsolePlugin;
using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    class ActionsAndVariablesWindow : EditorWindow
    {
        static readonly string PrefsKeyFilterText = "com.spacemadness.LunarMobileConsole.Editor.FilterText";

        string m_filterText;
        Vector2 m_scrollPosition;

        GUIStyle m_filterTextStyle;
        GUIStyle m_filterButtonStyle;
        GUIStyle m_headerLabelStyle;
        GUIStyle m_resetButtonStyle;

        public ActionsAndVariablesWindow()
        {
            #if UNITY_5_0
            this.title = "Actions & Vars";
            #else
            this.titleContent = new GUIContent("Actions & Vars");
            #endif
        }

        void OnEnable()
        {
            m_filterText = EditorPrefs.GetString(PrefsKeyFilterText, "");
        }
        
        void OnGUI()
        {
            if (Application.isPlaying)
            {
                var instance = LunarConsole.instance;
                if (instance != null && instance.registry != null)
                {
                    OnRegistryGUI(instance.registry);
                }
                else
                {
                    GUILayout.Label("Instance is not initialized");
                }
            }
            else
            {
                GUILayout.Label("Only available in Play Mode");
            }
        }

        void OnRegistryGUI(CRegistry registry)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    var oldFilterText = m_filterText;
                    m_filterText = GUILayout.TextField(m_filterText, filterTextStyle);
                    if (GUILayout.Button(GUIContent.none, filterButtonStyle))
                    {
                        m_filterText = "";
                    }
                    if (oldFilterText != m_filterText)
                    {
                        EditorPrefs.SetString(PrefsKeyFilterText, m_filterText);
                    }
                }
                GUILayout.EndHorizontal();

                m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
                {
                    var filterText = m_filterText.ToLower();

                    GUILayout.Label("Actions", headerLabelStyle);
                    foreach (var action in registry.actions)
                    {
                        if (m_filterText.Length == 0 || action.Name.ToLower().Contains(filterText))
                        {
                            OnActionGUI(action);
                        }
                    }

                    GUILayout.Label("Variables", headerLabelStyle);
                    foreach (var cvar in registry.cvars)
                    {
                        if (m_filterText.Length == 0 || cvar.Name.ToLower().Contains(filterText))
                        {
                            OnVariableGUI(cvar);
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
        }

        void OnActionGUI(CAction action)
        {
            if (GUILayout.Button(action.Name))
            {
                action.Execute();
            }
        }

        void OnVariableGUI(CVar cvar)
        {
            if (cvar.IsHidden)
            {
                return;
            }
            
            if (cvar.IsDefault)
            {
                OnVariableFieldGUI(cvar);
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    OnVariableFieldGUI(cvar);

                    GUI.SetNextControlName("Reset Button");
                    if (GUILayout.Button("Reset", resetButtonStyle, GUILayout.Width(60)))
                    {
                        cvar.Value = cvar.DefaultValue;
                        GUI.FocusControl("Reset Button");
                        LunarConsole.instance.MarkVariablesDirty();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        static void OnVariableFieldGUI(CVar cvar)
        {
            EditorGUI.BeginChangeCheck();
            switch (cvar.Type)
            {
                case CVarType.Boolean:
                    cvar.BoolValue = EditorGUILayout.Toggle(cvar.Name, cvar.BoolValue);
                    break;
                case CVarType.Float:
                    if (cvar.HasRange)
                    {
                        cvar.FloatValue = EditorGUILayout.Slider(cvar.Name, cvar.FloatValue, cvar.Range.min, cvar.Range.max);
                    }
                    else
                    {
                        cvar.FloatValue = EditorGUILayout.FloatField(cvar.Name, cvar.FloatValue);
                    }
                    break;
                case CVarType.Integer:
                    cvar.IntValue = EditorGUILayout.IntField(cvar.Name, cvar.IntValue);
                    break;
                case CVarType.String:
                    cvar.Value = EditorGUILayout.TextField(cvar.Name, cvar.Value);
                    break;
                case CVarType.Enum:
                    var selectedIndex = Array.IndexOf(cvar.AvailableValues, cvar.Value);
                    var newSelectedIndex = EditorGUILayout.Popup(cvar.Name, selectedIndex, cvar.AvailableValues);
                    if (selectedIndex != newSelectedIndex)
                    {
                        cvar.Value = cvar.AvailableValues[newSelectedIndex];
                    }
                    break;
                default:
                    EditorGUILayout.LabelField(cvar.Name, cvar.Value);
                    break;
            }
            var changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                LunarConsole.instance.MarkVariablesDirty();
            }
        }

        public static void ShowWindow()
        {
            EditorWindow.GetWindow<ActionsAndVariablesWindow>();
        }

        private GUIStyle filterTextStyle
        {
            get
            {
                if (m_filterTextStyle == null)
                {
                    m_filterTextStyle = new GUIStyle("SearchTextField");
                }
                return m_filterTextStyle;
            }
        }

        private GUIStyle filterButtonStyle
        {
            get
            {
                if (m_filterButtonStyle == null)
                {
                    m_filterButtonStyle = new GUIStyle("SearchCancelButton");
                }
                return m_filterButtonStyle;
            }
        }

        private GUIStyle headerLabelStyle
        {
            get
            {
                if (m_headerLabelStyle == null)
                {
                    m_headerLabelStyle = new GUIStyle("HeaderLabel");
                }
                return m_headerLabelStyle;
            }
        }

        private GUIStyle resetButtonStyle
        {
            get
            {
                if (m_resetButtonStyle == null)
                {
                    m_resetButtonStyle = new GUIStyle("minibutton");
                }
                return m_resetButtonStyle;
            }
        }
    }
}
