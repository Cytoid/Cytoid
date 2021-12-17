//
//  Log.cs
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


﻿using UnityEngine;

using System;
using System.Collections;

namespace LunarConsoleEditorInternal
{
    static class Log
    {
        [System.Diagnostics.Conditional("LUNAR_CONSOLE_DEVELOPMENT")]
        public static void d(string format, params object[] args)
        {
            Debug.Log(TryFormat(format, args));
        }

        [System.Diagnostics.Conditional("LUNAR_CONSOLE_DEVELOPMENT")]
        public static void e(string format, params object[] args)
        {
            Debug.LogError(TryFormat(format, args));
        }

        [System.Diagnostics.Conditional("LUNAR_CONSOLE_DEVELOPMENT")]
        public static void e(Exception e, string format, params object[] args)
        {
            Log.e(format, args);
            Debug.LogException(e);
        }

        private static string TryFormat(string format, object[] args)
        {
            try
            {
                return string.Format(format, args);
            }
            catch (Exception)
            {
                return format;
            }
        }
    }
}
