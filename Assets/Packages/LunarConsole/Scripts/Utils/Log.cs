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


﻿//
//  Log.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2019 Alex Lementuev, SpaceMadness.
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

using UnityEngine;

namespace LunarConsolePluginInternal
{
    static class Log
    {
        static readonly string TAG = "[" + Constants.PluginDisplayName + "]";

        [System.Diagnostics.Conditional("LUNAR_CONSOLE_DEVELOPMENT")]
        public static void dev(string format, params object[] args)
        {
            Debug.Log(TAG + " " + StringUtils.TryFormat(format, args));
        }

        public static void e(Exception exception)
        {
            if (exception != null)
            {
                Debug.LogError(TAG + " " + exception.Message + "\n" + exception.StackTrace);
            }
            else
            {
                Debug.LogError(TAG + " Exception");
            }
        }

        public static void e(Exception exception, string format, params object[] args)
        {
            e(exception, StringUtils.TryFormat(format, args));
        }

        public static void e(Exception exception, string message)
        {
            if (exception != null)
            {
                Debug.LogError(TAG + " " + message + "\n" + exception.Message + "\n" + exception.StackTrace);
                Exception innerException = exception;
                while ((innerException = innerException.InnerException) != null)
                {
                    Debug.LogError(innerException.Message + "\n" + innerException.StackTrace);
                }
            }
            else
            {
                Debug.LogError(TAG + " " + message);
            }
        }

        public static void e(string format, params object[] args)
        {
            e(StringUtils.TryFormat(format, args));
        }

        public static void e(string message)
        {
            Debug.LogError(TAG + " " + message);
        }

        public static void w(string format, params object[] args)
        {
            w(StringUtils.TryFormat(format, args));
        }

        public static void w(string message)
        {
            Debug.LogWarning(TAG + " " + message);
        }
    }
}