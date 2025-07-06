//
//  AndroidPlugin.cs
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


using UnityEngine;
using UnityEditor;
using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    internal static class AndroidPlugin
    {
        public static void SetEnabled(bool enabled, bool retrying = false)
        {
            if (retrying)
            {
                Log.d("Retrying Android Plugin import...");
            }

            var androidPathAAR = FileUtils.FixAssetPath(EditorConstants.EditorPathAndroidAAR);
            if (androidPathAAR == null || !FileUtils.AssetPathExists(androidPathAAR))
            {
                Debug.LogErrorFormat(
                    "Can't {0} Android plugin: missing required file '{1}'. Re-install {2} to fix the issue.",
                    enabled ? "enable" : "disable",
                    androidPathAAR,
                    Constants.PluginDisplayName);
                return;
            }

            var importer = (PluginImporter) AssetImporter.GetAtPath(androidPathAAR);
            if (importer == null)
            {
                // mleenhardt: Added delayed retry attempt to fix error caused by the fact that importer is
                // null the very first time the project is loaded when there is no Library folder yet.
                if (!retrying)
                {
                    EditorHelper.DelayCallOnce(() => SetEnabled(enabled, true));
                    return;
                }

                Debug.LogErrorFormat(
                    "Can't {0} Android plugin: unable to create importer for '{1}'. Re-install {2} to fix the issue.",
                    enabled ? "enable" : "disable",
                    androidPathAAR,
                    Constants.PluginDisplayName);
                return;
            }

            if (importer.GetCompatibleWithPlatform(BuildTarget.Android) != enabled)
            {
                importer.SetCompatibleWithPlatform(BuildTarget.Android, enabled);
                importer.SaveAndReimport();
            }
        }
    }
}