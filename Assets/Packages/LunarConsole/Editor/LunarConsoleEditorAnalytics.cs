//
//  LunarConsoleEditorAnalytics.cs
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
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    static class LunarConsoleEditorAnalytics
    {
        private static readonly string kPrefsLastKnownVersion = Constants.EditorPrefsKeyBase + ".LastKnownVersion";

        /// <summary>
        /// Notifies the server about plugin update.
        /// </summary>
        public static void TrackPluginVersionUpdate()
        {
#if !LUNAR_CONSOLE_ANALYTICS_DISABLED
            if (LunarConsoleConfig.consoleEnabled && LunarConsoleConfig.consoleSupported)
            {
                var lastKnownVersion = EditorPrefs.GetString(kPrefsLastKnownVersion);
                if (lastKnownVersion != Constants.Version)
                {
                    EditorPrefs.SetString(kPrefsLastKnownVersion, Constants.Version);
                    TrackEvent("Version", "updated_version");
                }
            }
#endif
        }

        public static void TrackEvent(string category, string action, int value = LunarConsoleAnalytics.kUndefinedValue)
        {
#if !LUNAR_CONSOLE_ANALYTICS_DISABLED
            if (LunarConsoleConfig.consoleEnabled && LunarConsoleConfig.consoleSupported)
            {
                var payloadStr = LunarConsoleAnalytics.CreatePayload(category, action, value);
                if (payloadStr != null)
                {
                    Log.d("Event track payload: " + payloadStr);

                    LunarConsoleHttpClient downloader = new LunarConsoleHttpClient(LunarConsoleAnalytics.TrackingURL);
                    downloader.UploadData(payloadStr, delegate (string result, Exception error)
                    {
                        if (error != null)
                        {
                            Log.e("Event track failed: " + error);
                        }
                        else
                        {
                            Log.d("Event track result: " + result);
                        }
                    });
                }
            }
#endif
        }
    }
}
