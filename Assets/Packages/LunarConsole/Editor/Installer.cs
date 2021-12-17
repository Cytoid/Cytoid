//
//  Installer.cs
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
using UnityEditor;

using System.Collections;
using System.IO;

using LunarConsolePlugin;
using LunarConsolePluginInternal;

namespace LunarConsoleEditorInternal
{
    public static class Installer
    {
        public static void Install(bool silent = true)
        {
            string prefabPath = EditorConstants.PrefabPath;
            string messageTitle = Constants.PluginDisplayName;

            string objectName = Path.GetFileNameWithoutExtension(prefabPath);

            if (!silent && !Utils.ShowDialog(messageTitle, "This will create " + objectName + " (" + prefabPath + ") game object in your scene.\n\nYou only need to do it once for the very first scene of your game\n\nContinue?"))
            {
                return;
            }

            LunarConsole[] existing = GameObject.FindObjectsOfType<LunarConsole>();
            if (existing != null)
            {
                foreach (LunarConsole c in existing)
                {
                    GameObject.DestroyImmediate(c.gameObject);
                }
            }

            GameObject lunarConsolePrefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject)) as GameObject;
            if (lunarConsolePrefab == null)
            {
                if (!silent)
                {
                    Utils.ShowDialog(messageTitle, "Can't instantiate " + prefabPath + ": asset not found");
                }
                return;
            }

            GameObject lunarConsole = PrefabUtility.InstantiatePrefab(lunarConsolePrefab) as GameObject;
            lunarConsole.name = objectName;

            // starting Unity 5.3 we need to add an undo operation or the scene would not be marked dirty
            #if UNITY_5_3_OR_NEWER
            Undo.RegisterCreatedObjectUndo(lunarConsole, "Install Lunar Console");
            #endif

            if (!silent)
            {
                Utils.ShowDialog(messageTitle, objectName + " game object created!\n\nDon't forget to save your scene changes!", "OK");
            }
        }

        public static void EnablePlugin()
        {
            SetLunarConsoleEnabled(true);
        }

        public static void DisablePlugin()
        {
            SetLunarConsoleEnabled(false);
        }

        public static void SetLunarConsoleEnabled(bool enabled)
        {
            if (LunarConsoleConfig.consoleEnabled == enabled)
                return;

            AndroidPlugin.SetEnabled(enabled);

            string pluginFile = LunarConsolePluginEditorHelper.ResolvePluginFile();
            if (pluginFile == null)
            {
                PrintError(enabled, "can't resolve plugin file");
                return;
            }

            string sourceCode = File.ReadAllText(pluginFile);

            string oldToken = "#define " + (enabled ? "LUNAR_CONSOLE_DISABLED" : "LUNAR_CONSOLE_ENABLED");
            string newToken = "#define " + (enabled ? "LUNAR_CONSOLE_ENABLED" : "LUNAR_CONSOLE_DISABLED");

            string newSourceCode = sourceCode.Replace(oldToken, newToken);
            if (newSourceCode == sourceCode)
            {
                PrintError(enabled, "can't find '" + oldToken + "' token");
                return;
            }

            File.WriteAllText(pluginFile, newSourceCode);

            // re-import asset to apply changes
            AssetDatabase.ImportAsset(FileUtils.GetAssetPath(pluginFile));

            LunarConsoleConfig.consoleEnabled = enabled;
        }

        static void PrintError(bool flag, string message)
        {
            Debug.LogError("Can't " + (flag ? "enable" : "disable") + " Lunar Console: " + message);
        }
    }
}
