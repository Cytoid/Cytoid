//
//  Compatibility.cs
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


﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LunarConsoleEditorInternal
{
#if UNITY_5_4_OR_NEWER
    class DisabledScopeCompat : IDisposable
    {
        private readonly EditorGUI.DisabledScope m_target;

        public DisabledScopeCompat(bool disabled)
        {
            m_target = new EditorGUI.DisabledScope(disabled);
        }

        public void Dispose()
        {
            m_target.Dispose();
        }
    }
#else
    class DisabledScopeCompat : IDisposable
    {
        public DisabledScopeCompat(bool disabled)
        {
            EditorGUI.BeginDisabledGroup(disabled);
        }

        public void Dispose()
        {
            EditorGUI.EndDisabledGroup();
        }
    }
#endif
}
