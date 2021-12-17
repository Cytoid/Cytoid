//
//  LunarConsoleEditor.cs
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


﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using LunarConsolePlugin;
using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    [CustomEditor(typeof(LunarConsole))]
    class LunarConsoleEditor : Editor
    {
        private GUIStyle m_buttonStyle;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (LunarConsoleConfig.freeVersion)
            {
                if (m_buttonStyle == null)
                {
                    m_buttonStyle = new GUIStyle("LargeButton");
                }

                if (GUILayout.Button("Get PRO version", m_buttonStyle))
                {
                    Application.OpenURL("https://goo.gl/jvJzr7");
                }
            }
        }
    }
}